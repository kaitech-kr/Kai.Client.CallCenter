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
            list.Add(queue.Dequeue());
        }

        Debug.WriteLine($"[AutoAllocQueue] DequeueAllToList: {networkName}, {list.Count}개 꺼냄");
        return list;
    }

    /// <summary>
    /// 처리 완료 후 큐에 재적재
    /// </summary>
    public void ReEnqueue(AutoAllocModel order, string networkName)
    {
        if (order == null)
        {
            Debug.WriteLine($"[AutoAllocQueue] ReEnqueue 실패: order가 null");
            return;
        }

        // StateFlag를 NotChanged로 변경
        order.StateFlag = PostgService_Common_OrderState.NotChanged;

        var queue = GetQueue(networkName);
        queue.Enqueue(order);

        Debug.WriteLine($"[AutoAllocQueue] ReEnqueue: {networkName}, KeyCode={order.KeyCode}, 큐크기={queue.Count}");
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
    #endregion
}

#nullable restore
