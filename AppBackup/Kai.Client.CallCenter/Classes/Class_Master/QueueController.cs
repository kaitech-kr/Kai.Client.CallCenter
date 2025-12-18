using System.Collections.Generic;
using System.Diagnostics;
using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;
using static Kai.Client.CallCenter.Classes.CommonVars;

namespace Kai.Client.CallCenter.Classes.Class_Master;

#nullable disable

/// <summary>
/// 자동배차 주문 큐 관리자
/// Queue 기반으로 주문을 관리하여 복잡성 제거
/// </summary>
public class QueueController
{
    #region Queues - 4개 외부앱별
    // 인성1
    private Queue<AutoAllocModel> _ordersInsung1 = new();

    // 인성2
    private Queue<AutoAllocModel> _ordersInsung2 = new();

    // 화물24시
    private Queue<AutoAllocModel> _ordersCargo24 = new();

    // 원콜
    private Queue<AutoAllocModel> _ordersOnecall = new();
    #endregion

    #region Properties
    /// <summary>
    /// 앱별 주문 큐 크기
    /// </summary>
    //public int GetQueueCount(string networkName)
    //{
    //    return GetQueue(networkName).Count;
    //}

    /// <summary>
    /// 전체 주문 큐 크기
    /// </summary>
    public int TotalCount =>
        _ordersInsung1.Count +
        _ordersInsung2.Count +
        _ordersCargo24.Count +
        _ordersOnecall.Count;
    #endregion

    #region Helper Methods
    /// <summary>
    /// 네트워크 이름으로 큐 가져오기
    /// </summary>
    private Queue<AutoAllocModel> GetQueue(string networkName)
    {
        return networkName switch
        {
            StdConst_Network.INSUNG1 => _ordersInsung1,
            StdConst_Network.INSUNG2 => _ordersInsung2,
            StdConst_Network.CARGO24 => _ordersCargo24,
            StdConst_Network.ONECALL => _ordersOnecall,
            _ => throw new ArgumentException($"Unknown network: {networkName}")
        };
    }

    /// <summary>
    /// 큐에서 특정 KeyCode의 최신 AutoAllocModel 찾기 (Race condition 방지용)
    /// 큐를 순회하여 해당 KeyCode가 있으면 반환 (원본 참조)
    /// </summary>
    /// <param name="networkName">네트워크 이름</param>
    /// <param name="keyCode">찾을 주문의 KeyCode</param>
    /// <returns>찾은 AutoAllocModel, 없으면 null</returns>
    public AutoAllocModel FindLatestInQueue(string networkName, long keyCode)
    {
        var queue = GetQueue(networkName);
        return queue.FirstOrDefault(item => item.NewOrder.KeyCode == keyCode);
    }
    #endregion

    #region 큐 적재
    /// <summary>
    /// 주문을 큐에 추가
    /// </summary>
    public void Enqueue(AutoAllocModel order, string networkName)
    {
        if (order == null)
        {
            Debug.WriteLine($"[AutoAllocQueue] Enqueue 실패: order가 null");
            return;
        }

        var queue = GetQueue(networkName);
        queue.Enqueue(order);

        Debug.WriteLine($"[AutoAllocQueue] Enqueue: {networkName}, KeyCode={order.KeyCode}, StateFlag={order.StateFlag}, 큐크기={queue.Count}");
    }

    /// <summary>
    /// 앱 시작 시 기존 주문 목록을 큐에 적재
    /// </summary>
    /// <param name="orders">DB에서 조회한 오늘의 주문 목록</param>
    /// <param name="networkName">네트워크 이름 (Insung1, Insung2 등)</param>
    public void LoadExistingOrders(List<TbOrder> orders, string networkName)
    {
        if (orders == null || orders.Count == 0)
        {
            Debug.WriteLine($"[AutoAllocQueue] 로드할 기존 주문이 없습니다: {networkName}");
            return;
        }

        Debug.WriteLine($"[AutoAllocQueue] 기존 주문 로드 시작: {networkName}, {orders.Count}개");

        var queue = GetQueue(networkName);
        int addedCount = 0;

        foreach (var order in orders)
        {
            // 네트워크별 SeqNo 필드 확인
            string seqNo = GetSeqNoByNetwork(order, networkName);
            bool hasSeqNo = !string.IsNullOrEmpty(seqNo);

            // StateFlag 결정
            var stateFlag = hasSeqNo
                ? PostgService_Common_OrderState.Existed_WithSeqno
                : PostgService_Common_OrderState.Existed_NonSeqno;

            // AutoAlloc 객체 생성 및 큐에 추가
            var autoAlloc = new AutoAllocModel(stateFlag, order);
            queue.Enqueue(autoAlloc);
            addedCount++;

            Debug.WriteLine($"[AutoAllocQueue] 추가: {networkName}, KeyCode={order.KeyCode}, SeqNo={seqNo ?? "(없음)"}, Flag={stateFlag}");
        }

        Debug.WriteLine($"[AutoAllocQueue] 기존 주문 로드 완료: {networkName}, {addedCount}개 추가, 큐 크기={queue.Count}");
    }

    /// <summary>
    /// 큐에서 모든 주문을 꺼내서 List로 반환
    /// </summary>
    public List<AutoAllocModel> DequeueAllToList(string networkName)
    {
        var queue = GetQueue(networkName);
        var list = new List<AutoAllocModel>();

        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            Debug.WriteLine($"[AutoAllocQueue] Dequeue: KeyCode={item.KeyCode}, RunStartTime={item.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={item.DriverPhone ?? "null"}");
            list.Add(item);
        }

        Debug.WriteLine($"[AutoAllocQueue] DequeueAllToList: {networkName}, {list.Count}개 꺼냄");
        return list;
    }

    /// <summary>
    /// 처리 완료 후 큐에 재적재
    /// </summary>
    /// <param name="order">재적재할 주문</param>
    /// <param name="networkName">네트워크 이름</param>
    /// <param name="newStateFlag">새로운 StateFlag (필수)</param>
    public void ReEnqueue(AutoAllocModel order, string networkName, PostgService_Common_OrderState newStateFlag)
    {
        if (order == null)
        {
            Debug.WriteLine($"[AutoAllocQueue] ReEnqueue 실패: order가 null");
            return;
        }

        // StateFlag 변경
        order.StateFlag = newStateFlag;

        var queue = GetQueue(networkName);
        queue.Enqueue(order);

        Debug.WriteLine($"[AutoAllocQueue] ReEnqueue: {networkName}, KeyCode={order.KeyCode}, StateFlag={order.StateFlag}, RunStartTime={order.RunStartTime?.ToString("HH:mm:ss") ?? "null"}, DriverPhone={order.DriverPhone ?? "null"}, 큐크기={queue.Count}");
    }

    /// <summary>
    /// 네트워크별 SeqNo 필드 가져오기
    /// </summary>
    private string GetSeqNoByNetwork(TbOrder order, string networkName)
    {
        return networkName switch
        {
            StdConst_Network.INSUNG1 => order.Insung1,
            StdConst_Network.INSUNG2 => order.Insung2,
            StdConst_Network.CARGO24 => order.Cargo24,
            StdConst_Network.ONECALL => order.Onecall,
            _ => null
        };
    }

    /// <summary>
    /// 지정된 큐들에서 특정 주문 제거 (SignalR 업데이트 시 사용)
    /// </summary>
    /// <param name="keyCode">제거할 주문의 KeyCode</param>
    /// <param name="targetQueues">제거할 큐 이름 목록</param>
    /// <returns>제거된 항목 개수</returns>
    public int RemoveFromQueues(long keyCode, List<string> targetQueues)
    {
        int removedCount = 0;

        foreach (var networkName in targetQueues)
        {
            var queue = GetQueue(networkName);
            removedCount += RemoveFromQueue(queue, keyCode, networkName);
        }

        if (removedCount > 0)
        {
            Debug.WriteLine($"[AutoAllocQueue] RemoveFromQueues: KeyCode={keyCode}, 총 {removedCount}개 제거");
        }

        return removedCount;
    }

    /// <summary>
    /// 특정 큐에서 주문 제거
    /// </summary>
    private int RemoveFromQueue(Queue<AutoAllocModel> queue, long keyCode, string networkName)
    {
        var tempList = new List<AutoAllocModel>();
        int removedCount = 0;

        // 큐에서 모든 항목 꺼내기
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();
            if (item.NewOrder.KeyCode == keyCode)
            {
                removedCount++;
                Debug.WriteLine($"[AutoAllocQueue] 제거: {networkName}, KeyCode={keyCode}, StateFlag={item.StateFlag}");
            }
            else
            {
                tempList.Add(item);
            }
        }

        // 제거되지 않은 항목만 다시 큐에 넣기
        foreach (var item in tempList)
        {
            queue.Enqueue(item);
        }

        return removedCount;
    }

    /// <summary>
    /// 모든 큐에서 주문 업데이트 또는 제거 (인스턴스 재사용)
    /// - 분류 규칙에 맞으면: 기존 인스턴스의 NewOrder, StateFlag만 업데이트
    /// - 분류 규칙에 안 맞으면: 큐에서 제거
    /// </summary>
    /// <param name="keyCode">업데이트할 주문의 KeyCode</param>
    /// <param name="newOrder">새로운 주문 정보</param>
    /// <param name="newStateFlag">새로운 StateFlag</param>
    public void UpdateOrRemoveInQueues(long keyCode, TbOrder newOrder, PostgService_Common_OrderState newStateFlag)
    {
        UpdateOrRemoveInQueue(_ordersInsung1, StdConst_Network.INSUNG1, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersInsung2, StdConst_Network.INSUNG2, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersCargo24, StdConst_Network.CARGO24, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersOnecall, StdConst_Network.ONECALL, keyCode, newOrder, newStateFlag);
    }

    /// <summary>
    /// 특정 큐에서 주문 업데이트 또는 제거
    /// </summary>
    private void UpdateOrRemoveInQueue(Queue<AutoAllocModel> queue, string networkName, long keyCode, TbOrder newOrder, PostgService_Common_OrderState newStateFlag)
    {
        bool found = false;
        var tempList = new List<AutoAllocModel>();

        // 큐에서 모든 항목 꺼내기
        while (queue.Count > 0)
        {
            var item = queue.Dequeue();

            if (item.NewOrder.KeyCode == keyCode)
            {
                found = true;

                // 이 큐에 있어야 하는 주문인가?
                if (ShouldBeInQueue(newOrder, networkName))
                {
                    // 기존 인스턴스 재사용 - NewOrder와 StateFlag만 업데이트
                    item.NewOrder = newOrder;
                    item.StateFlag = newStateFlag;
                    tempList.Add(item);
                    Debug.WriteLine($"[AutoAllocQueue] 업데이트: {networkName}, KeyCode={keyCode}, StateFlag={newStateFlag}");
                }
                else
                {
                    // 분류 규칙에 안 맞음 - 제거 (tempList에 추가 안함)
                    Debug.WriteLine($"[AutoAllocQueue] 제거: {networkName}, KeyCode={keyCode} (분류 규칙 불일치)");
                }
            }
            else
            {
                tempList.Add(item);
            }
        }

        // 큐에 없었는데 분류 규칙에 맞으면 새로 생성
        if (!found && ShouldBeInQueue(newOrder, networkName))
        {
            var newItem = new AutoAllocModel(newOrder);
            newItem.StateFlag = newStateFlag;
            tempList.Add(newItem);
            Debug.WriteLine($"[AutoAllocQueue] 신규 생성: {networkName}, KeyCode={keyCode}, StateFlag={newStateFlag}");
        }

        // 모든 항목 다시 큐에 넣기
        foreach (var item in tempList)
        {
            queue.Enqueue(item);
        }
    }

    /// <summary>
    /// 주문이 해당 큐에 있어야 하는지 판단 (분류 규칙)
    /// </summary>
    private bool ShouldBeInQueue(TbOrder order, string networkName)
    {
        // ✅ 1단계: 이미 등록된 주문은 해당 네트워크 큐에만 포함
        // - 이미 seqno가 있으면 그 큐에만 남아야 함 (상태 업데이트/취소 처리용)
        // - 다른 큐로 이동하면 안 됨!
        switch (networkName)
        {
            case StdConst_Network.INSUNG1:
                if (!string.IsNullOrEmpty(order.Insung1)) return true;  // 인성1 등록됨
                break;
            case StdConst_Network.INSUNG2:
                if (!string.IsNullOrEmpty(order.Insung2)) return true;  // 인성2 등록됨
                break;
            case StdConst_Network.CARGO24:
                if (!string.IsNullOrEmpty(order.Cargo24)) return true;  // Cargo24 등록됨
                break;
            case StdConst_Network.ONECALL:
                if (!string.IsNullOrEmpty(order.Onecall)) return true;  // Onecall 등록됨
                break;
        }

        // ✅ 2단계: 미등록 주문은 기존 분류 로직 적용
        // - 차량 타입과 CallCustFrom으로 판단
        bool isMotorcycle = order.CarType == "오토";
        bool isFlex = order.CarType == "플렉스";
        bool isLargeTruck = order.CarType == "트럭" && order.CarWeight != "1t" && order.CarWeight != "1.4t";

        return networkName switch
        {
            StdConst_Network.INSUNG1 => !isLargeTruck && order.CallCustFrom != StdConst_Network.INSUNG2,
            StdConst_Network.INSUNG2 => !isLargeTruck && order.CallCustFrom != StdConst_Network.INSUNG1,
            StdConst_Network.CARGO24 => !isMotorcycle && !isFlex,
            StdConst_Network.ONECALL => !isMotorcycle && !isFlex,
            _ => false
        };
    }
    #endregion

    #region 큐 조회
    /// <summary>
    /// 큐 상태 출력 (디버깅용)
    /// </summary>
    public void PrintQueueStatus()
    {
        Debug.WriteLine($"[AutoAllocQueue] 큐 상태:");
        Debug.WriteLine($"  인성1: {_ordersInsung1.Count}개");
        Debug.WriteLine($"  인성2: {_ordersInsung2.Count}개");
        Debug.WriteLine($"  화물24시: {_ordersCargo24.Count}개");
        Debug.WriteLine($"  원콜: {_ordersOnecall.Count}개");
        Debug.WriteLine($"  전체: {TotalCount}개");
    }
    #endregion

    #region 큐 초기화
    /// <summary>
    /// 모든 큐 클리어
    /// </summary>
    public void Clear()
    {
        _ordersInsung1.Clear();
        _ordersInsung2.Clear();
        _ordersCargo24.Clear();
        _ordersOnecall.Clear();
        Debug.WriteLine($"[AutoAllocQueue] 모든 큐 클리어 완료");
    }

    /// <summary>
    /// 특정 앱의 큐만 클리어
    /// </summary>
    /// <param name="networkName">네트워크 이름 (StdConst_Network)</param>
    public void ClearQueue(string networkName)
    {
        var queue = GetQueue(networkName);
        int count = queue.Count;
        queue.Clear();
        Debug.WriteLine($"[AutoAllocQueue] {networkName} 큐 클리어 완료 ({count}건)");
    }
    #endregion

    #region Helper - 페이지 계산
    /// <summary>
    /// 페이지별 첫 번호 배열 반환
    /// 인성 Datagrid 특성: 마지막 페이지도 항상 로우가 꽉 차 있음 (중복 표시)
    /// </summary>
    /// <param name="totItemCount">전체 데이터 개수</param>
    /// <param name="countPerPage">페이지당 로우 개수</param>
    /// <returns>페이지별 첫 번호 배열</returns>
    public static int[] GetPageFirstNumArray(int totItemCount, int countPerPage)
    {
        if (totItemCount == 0) return null;
        if (totItemCount <= countPerPage) return new int[1] { 1 };

        int pageCount = totItemCount / countPerPage;
        int remain = totItemCount % countPerPage;

        // 배열 크기: remain 있으면 페이지 1개 더
        int arraySize = (remain == 0) ? pageCount : (pageCount + 1);
        int[] arr = new int[arraySize];

        // 일반 페이지들
        for (int i = 0; i < pageCount; i++)
        {
            arr[i] = (i * countPerPage) + 1;
        }

        // 마지막 페이지 (중복 표시되므로: 전체 - 한페이지 + 1)
        if (remain > 0)
        {
            arr[pageCount] = totItemCount - countPerPage + 1;
        }

        return arr;
    }
    #endregion
}

#nullable restore
