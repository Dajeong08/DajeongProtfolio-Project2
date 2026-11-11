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

    private List<Collider2D> detectedColliders = new List<Collider2D>();
    private float attackCooldown;

    // Unity가 초기화할 때 호출한다. Inspector 참조가 비어 있으면 직접 찾는다.
    private void Awake()
    {
        if (character == null)
            character = GetComponent<SummonedCharacter>();
        if (attackRange == null)
            attackRange = GetComponent<CircleCollider2D>();
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

    // 매 프레임 타깃과 남은 대기시간을 확인해 공격한다.
    private void Update()
    {
        // 화면이 멈춰도 Update는 호출되므로 종료 후 공격을 막는다.
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

        // deltaTime은 지난 프레임부터 흐른 시간이다.
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
            // Animator Trigger는 애니메이션 신호다. 콜라이더의 Is Trigger와는 별개다.
            animator.SetTrigger("Attack");
        }
        // 현재는 타격 프레임을 기다리지 않고 공격 시작 시 피해를 준다.
        currentTarget.TakeDamage(character.Data.AttackDamage);
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

    // Scene에서 선택했을 때 확인용 범위를 그린다. 실제 판정은 콜라이더가 한다.
    private void OnDrawGizmosSelected()
    {
        CircleCollider2D range = attackRange;
        if (range == null)
            range = GetComponent<CircleCollider2D>();
        if (range == null)
            return;

        // TransformPoint: 로컬 좌표를 월드 좌표로 변환. lossyScale: 부모까지 반영한 크기.
        Vector3 center = range.transform.TransformPoint(range.offset);
        Vector3 scale = range.transform.lossyScale;
        float radius = range.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, radius);
    }

    // 살아 있는 몬스터의 콜라이더만 중복 없이 등록한다.
    private void RegisterMonster(Collider2D other)
    {
        if (detectedColliders.Contains(other))
            return;

        // 자식에 콜라이더가 있어도 부모의 MonsterManager를 찾는다.
        MonsterManager monster = other.GetComponentInParent<MonsterManager>();
        if (monster == null)
            return;
        // isActiveAndEnabled는 오브젝트와 이 스크립트가 모두 켜져 있는지 확인한다.
        if (!monster.isAlive || !monster.isActiveAndEnabled)
            return;

        detectedColliders.Add(other);
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
        if (!attackRange.IsTouching(targetCollider))
            return false;

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
            if (!CanAttack(detectedColliders[i]))
            {
                detectedColliders.RemoveAt(i);
            }
        }

        for (int i = 0; i < detectedColliders.Count; i++)
        {
            MonsterManager monster = detectedColliders[i].GetComponentInParent<MonsterManager>();
            if (monster == currentTarget)
            {
                return;
            }
        }

        currentTarget = null;
        if (detectedColliders.Count > 0)
        {
            currentTarget = detectedColliders[0].GetComponentInParent<MonsterManager>();
        }
    }

    // 공격 대상의 좌우 위치를 기준으로 바라본다. 같은 x 위치면 방향을 유지한다.
    private void FaceTarget()
    {
        if (characterSprite == null || currentTarget == null)
            return;

        float differenceX = currentTarget.transform.position.x - transform.position.x;
        if (Mathf.Abs(differenceX) < 0.001f)
            return;

        bool faceLeft = differenceX < 0;
        // flipX는 그림만 반전하므로 콜라이더 크기와 슬롯 배치에는 영향이 없다.
        if (SpriteFacesRight)
            characterSprite.flipX = faceLeft;
        else
            characterSprite.flipX = !faceLeft;
    }

}
