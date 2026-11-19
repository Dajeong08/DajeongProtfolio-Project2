using UnityEngine;
using UnityEngine.UI;

// 몬스터 이동, 피격, 사망 관리
public class MonsterManager : MonoBehaviour
{
    public float MAX_HP; // 이번 웨이브 최대 체력
    public float HP; // 현재 체력
    public float Speed; // 이동 속도
    public Slider HP_slider; // 개별 몬스터 체력바
    public WaveType poolType = WaveType.BasicMonster; // 반환할 일반 또는 보스 풀
    public Collider2D hitCollider; // 공격에 맞는 콜라이더
    public bool isAlive; // 생존 여부
    public int CurrentMovePoint; // 현재 향하는 경로 번호
    public bool IsStunned; // 기절 여부
    private float stunTimer; // 남은 기절 시간

    private Transform MovePoint; // 현재 목적지
    private int NextMovePoint; // 다음 목적지 번호
    private bool returnedToPool; // 중복 풀 반환 방지
    private Animator ani; // 몬스터 애니메이터
    private Rigidbody2D body; // 이동에 사용하는 Rigidbody2D

    // 애니메이터, 물리, 피격 컴포넌트 연결
    private void Awake()
    {
        ani = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();
        if (hitCollider == null)
            hitCollider = GetComponent<Collider2D>();
    }

    // 풀에서 나올 때 체력, 사망 상태, 이동 경로 초기화
    private void OnEnable()
    {
        HP = MAX_HP;
        isAlive = true;
        returnedToPool = false;
        IsStunned = false;
        stunTimer = 0f;
        if (hitCollider != null)
            hitCollider.enabled = true; // 콜라이더 활성화

        if (ani != null)
        {
            // 이전 애니메이션 상태 초기화
            ani.Rebind();
            ani.SetBool("Death", false);
            ani.SetInteger("Ani_Index", 0);
        }

        MovePoint = null; 
        CurrentMovePoint = 0;
        NextMovePoint = 1;

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

    // 기절 시간을 계산하고 이동
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

    // 기절 상태와 남은 시간 설정
    public void ApplyStun(float seconds)
    {
        if (!isAlive || !isActiveAndEnabled)
            return;
        IsStunned = true;
        stunTimer = seconds;
    }

    // 이번 웨이브 최대 체력 설정
    public void SetWaveHP(float maxHP)
    {
        MAX_HP = Mathf.Max(1, maxHP);
    }

    // 이동 지점을 순서대로 순환
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
            currentPosition = body.position; // 물리 위치 사용
        else
            currentPosition = transform.position; 

        Vector2 targetPosition = MovePoint.position;
        float slowPercent = 0f;
        // 천사 감속은 보스에게만 적용
        if (poolType == WaveType.BossMonster)
            slowPercent = Mathf.Clamp(battle.AngleSteck * 10f, 0f, 100f);
        // 속도 × 남은 비율 × 물리 프레임 시간
        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition, targetPosition, Speed * (1f - slowPercent / 100f) * Time.fixedDeltaTime);

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

    // 피해 적용과 데미지 텍스트 표시
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

    // 체력바 갱신, 체력이 0이면 사망 처리
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

    // 이동과 피격을 멈추고 사망 연출 재생
    public void Death()
    {
        if (!isAlive || !isActiveAndEnabled)
            return;

        HP = 0;
        // 사망 상태를 먼저 바꿔 처치 보상 중복 방지
        isAlive = false;
        if (BattleManager.instance != null)
            BattleManager.instance.MonsterDefeated(poolType, transform.position);
        if (hitCollider != null)
            hitCollider.enabled = false;

        if (ani != null && ani.runtimeAnimatorController != null)
            ani.SetBool("Death", true);
        else
            DeathAnimation_Complete();
    }

    // 사망 애니메이션이 끝나면 풀 반환 요청
    public void DeathAnimation_Complete()
    {
        if (isAlive || returnedToPool)
            return;

        returnedToPool = true;
        if (BattleManager.instance != null)
            BattleManager.instance.RemoveMonster(gameObject);
        else
            gameObject.SetActive(false); // 풀 반환
    }
}
