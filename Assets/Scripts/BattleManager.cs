using UnityEngine;

public class BattleManager : MonoBehaviour
{
    /// <summary>
    /// 시작시 현재 스테이지의 스테이지 데이터를 받는다. 
    /// 
    /// 시간에 따른 웨이브 시작하기 
    /// </summary>
    public static BattleManager instance;

    // 몬스터 이동 포인트
    public Transform[] MonaterMovePoint; 
    public Transform MonaterSpawonPoint;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        GetBattleData(); // 배틀 데이터 받아오기
    }

    // 배틀 데이터 받아오기
    public void GetBattleData()
    {

    }
    // 배틀 세팅하기
    public void BattleSetting()
    {

    }
}
