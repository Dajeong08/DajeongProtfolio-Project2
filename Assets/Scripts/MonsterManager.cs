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
    private bool isAlive = true;
    private bool FirstOnEnable = false;

    private Animator ani;

    private void OnEnable()
    {
        if (!FirstOnEnable)
        {
            FirstOnEnable = true;
        }
        else
        {
            HP = MAX_HP; //Hp 풀로 채우기 
            // HP_UI_Update(); // HP UI 업데이트 
            //transform.position = BattleManager.instance.MonaterSpawonPoint.position;
        }
    }

    void Start()
    {
        ani = GetComponent<Animator>();

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
    public void TakeDamage(float Damage)
    {
        HP -= Damage; // 받은 데미지 많큼 감소
        // HP_UI_Update();
    }

    //public void HP_UI_Update()
    //{
    //    if (HP > 0)
    //    {
    //        HP_slider.value = HP;
    //    }
    //    else if (HP <= 0)
    //    {
    //        HP_slider.value = HP;
    //        isAlive = false; // 죽음
    //    }
    //}
    public void Death()
    {
        ani.SetBool("Death", true);
    }
    public void DeathAnimation_Complete()
    {
        BattleManager.instance.RemoveMonster(gameObject);
    }
}
