using System.Diagnostics;

using Kai.Common.StdDll_Common;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Models;
using Kai.Server.Main.KaiWork.DBs.Postgres.KaiDB.Services;

namespace Kai.Client.CallCenter.Classes.Class_Master;

#nullable disable

// 자동배차 주문 큐 관리자
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
    // 앱별 주문 큐 크기
    //public int GetQueueCount(string networkName)
    //{
    //    return GetQueue(networkName).Count;
    //}

    // 전체 주문 큐 크기
    public int TotalCount =>
        _ordersInsung1.Count +
        _ordersInsung2.Count +
        _ordersCargo24.Count +
        _ordersOnecall.Count;
    #endregion

    #region Helper Methods
    // 네트워크 이름으로 큐 가져오기
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

    // 큐에서 특정 KeyCode의 최신 AutoAllocModel 찾기 (Race condition 방지용)
    public AutoAllocModel FindLatestInQueue(string networkName, long keyCode)
    {
        var queue = GetQueue(networkName);
        return queue.FirstOrDefault(item => item.NewOrder.KeyCode == keyCode);
    }

    // 네트워크별 SeqNo 필드 가져오기
    public string GetSeqNoByNetwork(TbOrder order, string networkName)
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
    
    // 페이지별 첫 번호 배열 반환 (마지막 페이지도 항상 로우가 꽉 차 있음)
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

    #region 큐 적재
    // 주문을 큐에 추가
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

    // 앱 시작 시 기존 주문 목록을 큐에 적재
    public void LoadExistingOrders(List<TbOrder> orders, string networkName)
    {
        // ...
    }

    // 큐에서 모든 주문을 꺼내서 List로 반환
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

    // 처리 완료 후 큐에 재적재
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

    // 지정된 큐들에서 특정 주문 제거 (SignalR 업데이트 시 사용)
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

    // 특정 큐에서 주문 제거
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

    // 모든 큐에서 주문 업데이트 또는 제거 (분류 규칙에 맞으면 업데이트, 안 맞으면 제거)
    public void UpdateOrRemoveInQueues(long keyCode, TbOrder newOrder, PostgService_Common_OrderState newStateFlag)
    {
        UpdateOrRemoveInQueue(_ordersInsung1, StdConst_Network.INSUNG1, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersInsung2, StdConst_Network.INSUNG2, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersCargo24, StdConst_Network.CARGO24, keyCode, newOrder, newStateFlag);
        UpdateOrRemoveInQueue(_ordersOnecall, StdConst_Network.ONECALL, keyCode, newOrder, newStateFlag);
    }

    // 특정 큐에서 주문 업데이트 또는 제거
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

    // 주문이 해당 큐에 있어야 하는지 판단 (분류 규칙)
    private bool ShouldBeInQueue(TbOrder order, string networkName)
    {
        if (order == null) return false;

        // 해당 네트워크의 접수번호(SeqNo)가 있는 경우에만 큐에 포함
        return networkName switch
        {
            StdConst_Network.INSUNG1 => !string.IsNullOrEmpty(order.Insung1),
            StdConst_Network.INSUNG2 => !string.IsNullOrEmpty(order.Insung2),
            StdConst_Network.CARGO24 => !string.IsNullOrEmpty(order.Cargo24),
            StdConst_Network.ONECALL => !string.IsNullOrEmpty(order.Onecall),
            _ => false
        };
    }

    // 큐 상태 출력 (디버깅용)
    public void PrintQueueStatus()
    {
        Debug.WriteLine($"[AutoAllocQueue] 큐 상태:");
        Debug.WriteLine($"  인성1: {_ordersInsung1.Count}개");
        Debug.WriteLine($"  인성2: {_ordersInsung2.Count}개");
        Debug.WriteLine($"  화물24시: {_ordersCargo24.Count}개");
        Debug.WriteLine($"  원콜: {_ordersOnecall.Count}개");
        Debug.WriteLine($"  전체: {TotalCount}개");
    }

    // 모든 큐 클리어
    public void Clear()
    {
        _ordersInsung1.Clear();
        _ordersInsung2.Clear();
        _ordersCargo24.Clear();
        _ordersOnecall.Clear();
        Debug.WriteLine($"[AutoAllocQueue] 모든 큐 클리어 완료");
    }

    // 특정 앱의 큐만 클리어
    public void ClearQueue(string networkName)
    {
        var queue = GetQueue(networkName);
        int count = queue.Count;
        queue.Clear();
        Debug.WriteLine($"[AutoAllocQueue] {networkName} 큐 클리어 완료 ({count}건)");
    }
    #endregion
}

#nullable restore
