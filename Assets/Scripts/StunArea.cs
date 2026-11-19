using System.Collections.Generic;
using UnityEngine;

// 범위 안의 적 기절 처리
public class StunArea : MonoBehaviour
{
    public float StunSeconds = 3f; // 기절 지속 시간
    public float ActiveSeconds = 0.2f; // 영역 판정이 켜져 있는 시간
    public GameObject EffectPrefab; // 기절 이펙트 프리팹
    public Vector3 EffectRotation; // 이펙트 회전
    public float EffectSeconds = 0.5f; // 이펙트 재생 시간

    private float activeTimer; // 영역 활성화 후 지난 시간
    private List<MonsterManager> hitMonsters = new List<MonsterManager>(); // 이번 발동에 이미 맞은 적
    private ParticleSystem[] effectParticles; // 재사용할 파티클 목록
    private Transform effectTransform; // 이펙트 위치 참조
    private Vector3 effectPosition; // 공격 순간 이펙트 위치

    // 기절 이펙트 생성과 파티클 설정
    private void Awake()
    {
        if (EffectPrefab == null)
            return;

        // 영역이 꺼져도 연출은 남도록 형제 오브젝트로 생성
        GameObject effect = Instantiate(EffectPrefab, transform.parent);
        effectTransform = effect.transform;
        effect.transform.localPosition = transform.localPosition;
        effect.transform.localRotation = Quaternion.Euler(EffectRotation);
        // true: 비활성 자식 파티클도 가져오기
        effectParticles = effect.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < effectParticles.Length; i++)
        {
            effectParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = effectParticles[i].main;
            main.loop = false;
            main.duration = EffectSeconds;
            effectParticles[i].GetComponent<ParticleSystemRenderer>().sortingOrder = 30;
        }
    }

    // 적 위치로 기절 영역을 옮겨 활성화
    public void Activate(Vector3 position)
    {
        // 다시 켤 때 OnEnable이 타이머와 피격 목록 초기화
        gameObject.SetActive(false);
        transform.position = position; // 스턴 판정 영역 이동
        effectPosition = position; // 이펙트 위치
        gameObject.SetActive(true);
    }
    // 기절 대상 목록과 타이머 초기화, 이펙트 재생
    private void OnEnable()
    {
        activeTimer = 0f;
        hitMonsters.Clear();
        if (effectParticles == null)
            return;
        effectTransform.position = effectPosition;
        for (int i = 0; i < effectParticles.Length; i++)
        {
            effectParticles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            effectParticles[i].Play(false);
        }
    }

    // 판정 시간이 지나면 영역 끄기
    private void FixedUpdate()
    {
        activeTimer += Time.fixedDeltaTime;
        if (activeTimer >= ActiveSeconds)
            // 다시 켤 때 OnEnable이 타이머와 피격 목록 초기화
            gameObject.SetActive(false);
    }

    // 영역에 들어온 적 기절 처리
    private void OnTriggerEnter2D(Collider2D other)
    {
        StunMonster(other);
    }

    // 영역 안에 남아 있는 적도 확인
    private void OnTriggerStay2D(Collider2D other)
    {
        StunMonster(other);
    }

    // 이번 발동에 아직 맞지 않은 적만 기절
    private void StunMonster(Collider2D other)
    {
        MonsterManager monster = other.GetComponentInParent<MonsterManager>();
        if (monster == null || hitMonsters.Contains(monster))
            return;

        monster.ApplyStun(StunSeconds);
        // 같은 적은 이번 발동에 한 번만 기절
        hitMonsters.Add(monster);
    }
}
