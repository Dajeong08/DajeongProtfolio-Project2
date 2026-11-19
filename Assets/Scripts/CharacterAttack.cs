using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SummonedCharacter), typeof(CircleCollider2D), typeof(Rigidbody2D))]
// 적 감지와 기본 공격
public class CharacterAttack : MonoBehaviour
{
    public SummonedCharacter character; // 공격할 캐릭터의 데이터
    public CircleCollider2D attackRange; // 공격 범위 콜라이더
    public Animator animator; // 공격 애니메이션
    public SpriteRenderer characterSprite; // 좌우 반전할 스프라이트
    public bool SpriteFacesRight = true; // 원본 그림이 오른쪽을 보는지 설정
    public MonsterManager currentTarget; // 현재 공격할 적
    public StunArea AttackStunArea; // 기본 공격에 사용할 기절 영역

    private List<Collider2D> detectedColliders = new List<Collider2D>(); // 범위에서 감지한 적 콜라이더 목록
    private float attackCooldown; // 다음 공격까지 남은 시간
    private const float AttackRangeMultiplier = 1.1f; // 실제 판정 반지름은 표시보다 10% 크게
    public float DisplayRangeRadius { get; private set; } // 선택 시 표시할 원래 반지름

    // 필요한 컴포넌트와 공격 반지름 설정
    private void Awake()
    {
        if (character == null)
            character = GetComponent<SummonedCharacter>();
        if (attackRange == null)
            attackRange = GetComponent<CircleCollider2D>();
        // 표시용은 원래 반지름, 실제 판정만 10% 확대
        DisplayRangeRadius = attackRange.radius;
        attackRange.radius = DisplayRangeRadius * AttackRangeMultiplier;

        if (animator == null)
            animator = GetComponent<Animator>();
        if (characterSprite == null)
            characterSprite = GetComponentInChildren<SpriteRenderer>();
    }

    // 공격 대기시간과 감지 목록 초기화
    private void OnEnable()
    {
        attackCooldown = 0;
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 기존 타깃과 감지 목록 비우기
    private void OnDisable()
    {
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 슬롯 이동 후 기존 타깃 비우기
    public void ClearTargets()
    {
        currentTarget = null;
        detectedColliders.Clear();
    }

    // 타깃을 확인하고 공속에 맞춰 공격
    private void Update()
    {
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
        Vector3 hitPosition = currentTarget.transform.position;
        float damage = character.Data.AttackDamage;
        if (UpGreadManager.instance != null)
            damage *= UpGreadManager.instance.GetAttackMultiplier(character.Data.Grade);
        currentTarget.TakeDamage(damage);
        if (AttackStunArea != null && Random.Range(0, 100) < 10)
        {
            AttackStunArea.Activate(hitPosition);
        }
        // 초당 공격 횟수를 공격 간격으로 변환
        attackCooldown = 1f / character.Data.AttackSpeed;
    }

    // 범위에 들어온 적 등록
    private void OnTriggerEnter2D(Collider2D other)
    {
        RegisterMonster(other);
    }

    // 이미 범위 안에 있는 적도 등록
    private void OnTriggerStay2D(Collider2D other)
    {
        RegisterMonster(other);
    }

    // 범위를 나간 적 제거
    private void OnTriggerExit2D(Collider2D other)
    {
        detectedColliders.Remove(other);
    }

    // 살아 있는 적을 중복 없이 등록
    private void RegisterMonster(Collider2D other)
    {
        if (detectedColliders.Contains(other)) // 이미 있으면 리턴
            return;

        // 콜라이더가 자식에 있어도 부모의 몬스터 컴포넌트 확인
        MonsterManager monster = other.GetComponentInParent<MonsterManager>();
        if (monster == null)
            return;
        if (!monster.isAlive || !monster.isActiveAndEnabled)
            return;

        detectedColliders.Add(other); // 공격 영역에 들어온 몬스터 리스트에 넣기
    }

    // 활성 상태이고 살아 있는 적인지 확인
    private bool CanAttack(Collider2D targetCollider)
    {
        if (targetCollider == null)
            return false;
        if (!targetCollider.isActiveAndEnabled)
            return false;
        if (!attackRange.isActiveAndEnabled)
            return false;

        MonsterManager monster = targetCollider.GetComponentInParent<MonsterManager>();
        if (monster == null)
            return false;
        if (!monster.isAlive || !monster.isActiveAndEnabled)
            return false;
        return true;
    }

    // 무효한 적을 지우고 현재 타깃 유지 또는 교체
    private void RefreshTarget()
    {
        // 뒤부터 지우면 아직 확인하지 않은 인덱스가 바뀌지 않음
        for (int i = detectedColliders.Count - 1; i >= 0; i--)
        {
            if (!CanAttack(detectedColliders[i])) // 공격할 수 없는 적 제거
            {
                detectedColliders.RemoveAt(i);
            }
        }

        for (int i = 0; i < detectedColliders.Count; i++) // 현재 타깃이 살아 있는지 확인한다.
        {
            MonsterManager monster = detectedColliders[i].GetComponentInParent<MonsterManager>();
            if (monster == currentTarget) // 현재 타깃 유지
            {
                return;
            }
        }

        currentTarget = null;
        if (detectedColliders.Count > 0)
        {
            currentTarget = detectedColliders[0].GetComponentInParent<MonsterManager>(); // 첫 번째 적을 타깃으로 선택
        }
    }

    // 스킬에 사용할 적 목록 반환
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

    // 적의 좌우 위치에 맞춰 스프라이트 반전
    private void FaceTarget()
    {
        if (characterSprite == null || currentTarget == null)
            return;

        float differenceX = currentTarget.transform.position.x - transform.position.x; // 적과 캐릭터의 좌우 차이
        if (Mathf.Abs(differenceX) < 0.001f) // 좌우 차이가 거의 없으면 방향 유지
            return;

        bool faceLeft = differenceX < 0;
        // flipX는 그림만 반전
        if (SpriteFacesRight)
            characterSprite.flipX = faceLeft;
        else
            characterSprite.flipX = !faceLeft;
    }

}
