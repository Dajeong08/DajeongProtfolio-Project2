using UnityEngine;
using UnityEngine.UI;

// 몬스터 하나의 이동, 체력, 사망과 풀 반환 요청을 담당한다.
public class MonsterManager : MonoBehaviour
{
    public float MAX_HP;
    public float HP;
    public float Speed;
    public Slider HP_slider;
    public WaveType poolType = WaveType.BasicMonster;
    public Collider2D hitCollider;
    public bool isAlive;
    public int CurrentMovePoint; // 지금 향하는 이동 지점 번호다.
    public bool IsStunned;
    private float stunTimer;

    private Transform MovePoint;
    private int NextMovePoint;
    private bool returnedToPool;
    private Animator ani;
    private Rigidbody2D body;

    // 자주 사용할 컴포넌트는 초기화할 때 가져와 보관한다.
    private void Awake()
    {
        ani = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();
    }

    // 풀에서 SetActive(true)로 꺼낼 때도 호출된다. 체력과 경로를 매번 초기화한다.
    private void OnEnable()
    {
        // 채력 채우기, 살아있는 상태, 풀에 반환되지 않는 상태
        HP = MAX_HP;
        isAlive = true;
        returnedToPool = false;
        IsStunned = false;
        stunTimer = 0f;
        if (hitCollider != null)
            hitCollider.enabled = true; // 콜라이더 활성화

        if (ani != null)
        {
            ani.Rebind();
            ani.SetBool("Death", false);
            ani.SetInteger("Ani_Index", 0);
        }

        // 이동 지점 초기화
        MovePoint = null; 
        CurrentMovePoint = 0;
        NextMovePoint = 1;

        // 출발 위치로 이동하고, 이동 지점이 있으면 첫 번째 지점을 향하도록 설정한다.
        BattleManager battle = BattleManager.instance;
        if (battle != null)
        {
            if (battle.MonaterSpawonPoint != null)
                transform.position = battle.MonaterSpawonPoint.position;
            if (battle.MonaterMovePoint != null && battle.MonaterMovePoint.Length > 0)
                MovePoint = battle.MonaterMovePoint[0];
        }
        if (body != null)
            body.position = transform.position;
        HP_UI_Update();
    }

    // Rigidbody2D 이동은 물리 갱신 주기에 맞춰 처리한다.
    private void FixedUpdate()
    {
        if (IsStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer > 0f)
                return;
            IsStunned = false;
        }
        if (isAlive)
            MonsterMove();
    }

    // 남은 시간에 더하지 않고, 마지막으로 맞은 순간부터 다시 계산한다.
    public void ApplyStun(float seconds)
    {
        if (!isAlive || !isActiveAndEnabled)
            return;
        IsStunned = true;
        stunTimer = seconds;
    }

    // 풀에서 꺼내 활성화하기 전에 새 웨이브의 최대 체력을 전달한다.
    public void SetWaveHP(float maxHP)
    {
        MAX_HP = Mathf.Max(1, maxHP);
    }

    // 목표에 도착하면 다음 지점을 골라 경로를 순환한다. (Todo)
    public void MonsterMove()
    {
        if (!isAlive || IsStunned || MovePoint == null)
            return;

        BattleManager battle = BattleManager.instance;
        if (battle == null)
            return;
        if (battle.MonaterMovePoint == null || battle.MonaterMovePoint.Length == 0)
            return;

        Vector2 currentPosition;
        if (body != null)
            currentPosition = body.position; // Rigidbody2D가 있으면 물리 위치를 사용한다.
        else
            currentPosition = transform.position; 

        Vector2 targetPosition = MovePoint.position;
        // 속도에 물리 시간 간격을 곱해 이번 이동량을 구한다.
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition, targetPosition, Speed * Time.fixedDeltaTime);

        if (body != null)
        {
            body.MovePosition(nextPosition);
        }
        else
        {
            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
        }

        if (Vector2.Distance(nextPosition, targetPosition) <= 0.001f) // 목적지 도달시
        {
            if (NextMovePoint >= battle.MonaterMovePoint.Length) // 마지막 지점이면 순환
                NextMovePoint = 0;
            if (ani != null)
                ani.SetInteger("Ani_Index", NextMovePoint);

            CurrentMovePoint = NextMovePoint;
            MovePoint = battle.MonaterMovePoint[CurrentMovePoint]; // 다음 이동 지점으로 갱신
            NextMovePoint++; // 다음 이동 지점 상승
            if (NextMovePoint >= battle.MonaterMovePoint.Length) // 마지막 지점이면 순환
                NextMovePoint = 0;
        }
    }

    // CharacterAttack이 공격력을 전달한다. 죽은 적은 추가 피해를 받지 않는다.
    public void TakeDamage(float damage)
    {
        if (!isAlive || !isActiveAndEnabled)
            return;
        if (damage <= 0)
            return;

        float actualDamage = Mathf.Min(HP, damage);
        HP -= damage;
        if (HP < 0)
            HP = 0;
        if (FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowDamageText(actualDamage, transform.position);
        HP_UI_Update(); // 체력바 갱신
    }

    // 체력바를 갱신하고 0이면 사망 처리로 연결한다.
    public void HP_UI_Update()
    {
        if (HP_slider != null)
        {
            HP_slider.minValue = 0;
            HP_slider.maxValue = Mathf.Max(1f, MAX_HP);
            HP_slider.value = HP;
        }
        if (poolType == WaveType.BossMonster && BattleManager.instance != null)
            BattleManager.instance.RefreshBossHP(this);

        if (HP <= 0 && isAlive)
            Death();
    }

    // 이동과 피격을 멈춘 뒤 사망 애니메이션을 재생한다.
    public void Death()
    {
        if (!isAlive || !isActiveAndEnabled)
            return;

        HP = 0;
        isAlive = false;
        // isAlive를 먼저 false로 바꾸므로 처치 보상은 한 번만 요청한다.
        if (BattleManager.instance != null)
            BattleManager.instance.MonsterDefeated(poolType, transform.position);
        if (hitCollider != null)
            hitCollider.enabled = false;

        if (ani != null && ani.runtimeAnimatorController != null)
            ani.SetBool("Death", true);
        else
            DeathAnimation_Complete();
    }

    // Death.anim의 애니메이션 이벤트가 마지막 프레임에 호출한다.
    public void DeathAnimation_Complete()
    {
        if (isAlive || returnedToPool)
            return;

        // 이벤트가 중복 호출되어도 반환은 한 번만 한다.
        returnedToPool = true;
        if (BattleManager.instance != null)
            BattleManager.instance.RemoveMonster(gameObject);
        else
            gameObject.SetActive(false); // 풀 반환
    }
}
