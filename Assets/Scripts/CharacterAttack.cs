using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// 필요한 컴포넌트를 지정한다. 반지름 등 세부 값은 Inspector에서 설정한다.
[RequireComponent(typeof(SummonedCharacter), typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class CharacterAttack : MonoBehaviour
{
    public SummonedCharacter character; 
    public CircleCollider2D attackRange;
    public Animator animator; 
    public SpriteRenderer characterSprite;
    public bool SpriteFacesRight = true; // 반전하지 않은 원본 그림이 오른쪽을 보는지 설정한다.
    public MonsterManager currentTarget; // 현재 공격할 적. null이면 공격하지 않는다.
    public StunArea AttackStunArea; // 연결한 캐릭터만 기본 공격 시 스턴 영역을 켠다.

    private List<Collider2D> detectedColliders = new List<Collider2D>(); // 공격 범위 안에 들어온 적의 콜라이더 목록. 중복 없이 등록한다.
    private float attackCooldown; // 공격 쿨타임
    private const float AttackRangeMultiplier = 1.1f; // 실제 공격 반지름만 10% 넓힌다.
    public float DisplayRangeRadius { get; private set; } // 선택 시 보여 줄 기존 반지름

    // Unity가 초기화할 때 호출한다. Inspector 참조가 비어 있으면 직접 찾는다.
    private void Awake()
    {
        if (character == null)
            character = GetComponent<SummonedCharacter>();
        if (attackRange == null)
            attackRange = GetComponent<CircleCollider2D>();
        // 표시용 원은 원래 크기를 보관하고, 실제 콜라이더만 넓힌다.
        DisplayRangeRadius = attackRange.radius;
        attackRange.radius = DisplayRangeRadius * AttackRangeMultiplier;

        if (animator == null)
            animator = GetComponent<Animator>();
        if (characterSprite == null)
            characterSprite = GetComponentInChildren<SpriteRenderer>();
    }

    // 활성화될 때 이전 타깃을 비우고 첫 공격을 바로 할 수 있게 한다.
    private void OnEnable()
    {
        attackCooldown = 0;
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 비활성화되면 이전 적 참조를 정리한다.
    private void OnDisable()
    {
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 슬롯 이동 후 새 위치의 적을 다시 감지한다. 공격 대기시간은 유지한다.
    public void ClearTargets()
    {
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 매 프레임 타깃과 남은 대기시간을 확인해 공격한다.
    private void Update()
    {
        // 화면이 멈춰도 Update는 호출되므로 종료 후 공격을 막는다.
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

        attackCooldown -= Time.deltaTime;
        if (attackCooldown < 0)
            attackCooldown = 0;
        if (character == null || character.Data == null)
            return;
        if (attackRange == null)
            return;

        RefreshTarget();
        if (currentTarget == null)
            return;
        if (attackCooldown > 0)
            return;
        if (character.Data.AttackSpeed <= 0 || character.Data.AttackDamage <= 0)
            return;

        FaceTarget();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Attack");
        }
        // 현재는 타격 프레임을 기다리지 않고 공격 시작 시 피해를 준다.
        Vector3 hitPosition = currentTarget.transform.position;
        currentTarget.TakeDamage(character.Data.AttackDamage);
        // 스턴 영역이 연결된 캐릭터는 공격 한 번마다 5% 확률로 발동한다.
        if (AttackStunArea != null && Random.Range(0, 100) < 10)
        {
            AttackStunArea.Activate(hitPosition);
        }
        attackCooldown = 1f / character.Data.AttackSpeed;
    }

    // 상대가 공격 범위에 들어오면 Unity 물리 시스템이 호출한다.
    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterMonster(other);
    }

    // 이미 범위와 겹친 상태로 활성화된 경우도 등록할 수 있다.
    private void OnTriggerStay2D(Collider2D other)
    {
        RegisterMonster(other);
    }

    // 범위를 나간 상대는 감지 목록에서 제거한다.
    private void OnTriggerExit2D(Collider2D other)
    {
        detectedColliders.Remove(other);
    }

    // 살아 있는 몬스터의 콜라이더만 중복 없이 등록한다.
    private void RegisterMonster(Collider2D other)
    {
        if (detectedColliders.Contains(other)) // 이미 있으면 리턴
            return;

        // 자식에 콜라이더가 있어도 부모의 MonsterManager를 찾는다.
        MonsterManager monster = other.GetComponentInParent<MonsterManager>();
        if (monster == null)
            return;
        // isActiveAndEnabled는 오브젝트와 이 스크립트가 모두 켜져 있는지 확인한다.
        if (!monster.isAlive || !monster.isActiveAndEnabled)
            return;

        detectedColliders.Add(other); // 공격 영역에 들어온 몬스터 리스트에 넣기
    }

    // 켜져 있고, 살아 있으며, 범위 안에 남은 적인지 확인한다.
    private bool CanAttack(Collider2D targetCollider)
    {
        if (targetCollider == null)
            return false;
        if (!targetCollider.isActiveAndEnabled)
            return false;
        if (!attackRange.isActiveAndEnabled)
            return false;
        // 마지막 물리 갱신의 접촉 정보를 확인한다.
        //if (!attackRange.IsTouching(targetCollider))
        //    return false;

        MonsterManager monster = targetCollider.GetComponentInParent<MonsterManager>();
        if (monster == null)
            return false;
        if (!monster.isAlive || !monster.isActiveAndEnabled)
            return false;
        return true;
    }

    // 기존 타깃이 없어졌을 때만 다른 적으로 교체한다.
    private void RefreshTarget()
    {
        // 뒤에서부터 삭제하면 아직 검사하지 않은 앞 항목의 번호는 바뀌지 않는다.
        for (int i = detectedColliders.Count - 1; i >= 0; i--)
        {
            if (!CanAttack(detectedColliders[i])) // 살아 있지 않거나 범위를 벗어난 적은 제거한다.
            {
                detectedColliders.RemoveAt(i);
            }
        }

        for (int i = 0; i < detectedColliders.Count; i++) // 현재 타깃이 살아 있는지 확인한다.
        {
            MonsterManager monster = detectedColliders[i].GetComponentInParent<MonsterManager>();
            if (monster == currentTarget) // 현재 타깃이 살아 있으면 그대로 유지한다.
            {
                return;
            }
        }

        currentTarget = null;
        if (detectedColliders.Count > 0)
        {
            currentTarget = detectedColliders[0].GetComponentInParent<MonsterManager>(); // 살아 있는 적이 있으면 첫 번째를 새 타깃으로 지정한다.
        }
    }

    // 스킬에 사용할 적 목록. 콜라이더가 여러 개인 몬스터도 한 번만 넣는다.
    public List<MonsterManager> GetSkillTargets()
    {
        List<MonsterManager> targets = new List<MonsterManager>();
        for (int i = 0; i < detectedColliders.Count; i++)
        {
            if (!CanAttack(detectedColliders[i]))
                continue;

            MonsterManager monster = detectedColliders[i].GetComponentInParent<MonsterManager>();
            if (!targets.Contains(monster))
                targets.Add(monster);
        }
        return targets;
    }

    // 공격 대상의 좌우 위치를 기준으로 바라본다. 같은 x 위치면 방향을 유지한다.
    private void FaceTarget()
    {
        if (characterSprite == null || currentTarget == null)
            return;

        float differenceX = currentTarget.transform.position.x - transform.position.x; // 타겟 위치 - 캐릭터 위치 
        if (Mathf.Abs(differenceX) < 0.001f) // 절대값으로 플레이어 위치랑 차이가 크지 않으면 리턴
            return;

        bool faceLeft = differenceX < 0;
        // flipX는 그림만 반전하므로 콜라이더 크기와 슬롯 배치에는 영향이 없다.
        if (SpriteFacesRight)
            characterSprite.flipX = faceLeft;
        else
            characterSprite.flipX = !faceLeft;
    }

}
