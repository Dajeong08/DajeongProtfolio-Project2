using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MonsterManager : MonoBehaviour
{
    public string Name;
    public float MAX_HP;
    public float HP;
    public float Speed;
    public Slider HP_slider;

    private Transform MovePoint;
    private int NextMovePoint;

    void Start()
    {
        gameObject.transform.position = BattleManager.instance.MonaterSpawonPoint.position;
        MovePoint = BattleManager.instance.MonaterMovePoint[0]; // 첫번째 도착점을 기본 등록
        NextMovePoint = 1;
    }
    void Update()
    {
        MonsterMove();
    }

    public void MonsterMove()
    {
        // 무브 포인트 까지 이동
        // 무브 포인트 도착시 다음 무브 포인트 이동
        // 반복
        transform.position = Vector3.MoveTowards(transform.position, MovePoint.position, Speed * Time.deltaTime);
        if (transform.position == MovePoint.position)
        {
            MovePoint = BattleManager.instance.MonaterMovePoint[NextMovePoint];
            NextMovePoint++;
            if (NextMovePoint == 4) NextMovePoint = 0;
        }
    }
}
