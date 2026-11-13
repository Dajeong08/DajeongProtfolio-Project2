using System.Collections.Generic;
using UnityEngine;

// 캐릭터의 자식 영역에 붙인다. 켤 때마다 한 번의 스턴 발동으로 처리한다.
public class StunArea : MonoBehaviour
{
    public float StunSeconds = 3f;
    public float ActiveSeconds = 0.2f;
    public GameObject EffectPrefab;
    public float EffectScale = 1f;
    public Vector3 EffectRotation;
    public float EffectSeconds = 0.5f;

    private float activeTimer;
    private List<MonsterManager> hitMonsters = new List<MonsterManager>();
    private ParticleSystem[] effectParticles;
    private Transform effectTransform;
    private Vector3 effectPosition;

    private void Awake()
    {
        if (EffectPrefab == null)
            return;

        // 스턴 영역과 형제로 만들어 판정이 꺼져도 연출은 끝까지 재생한다.
        GameObject effect = Instantiate(EffectPrefab, transform.parent);
        effectTransform = effect.transform;
        effect.transform.localPosition = transform.localPosition;
        effect.transform.localRotation = Quaternion.Euler(EffectRotation);
        effect.transform.localScale *= EffectScale;
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

    // 공격 순간의 적 위치를 전달하고, 영역을 켜면서 이펙트를 재생한다.
    public void Activate(Vector3 position)
    {
        gameObject.SetActive(false);
        effectPosition = position;
        gameObject.SetActive(true);
    }

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

    private void FixedUpdate()
    {
        activeTimer += Time.fixedDeltaTime;
        if (activeTimer >= ActiveSeconds)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StunMonster(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        StunMonster(other);
    }

    private void StunMonster(Collider2D other)
    {
        MonsterManager monster = other.GetComponentInParent<MonsterManager>();
        if (monster == null || hitMonsters.Contains(monster))
            return;

        monster.ApplyStun(StunSeconds);
        hitMonsters.Add(monster);
    }
}
