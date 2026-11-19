using System.Collections.Generic;
using UnityEngine;

// 신화 스킬 종류
public enum MythicSkillType
{
    None,
    Demon,
    Werewolf,
    Angel
}

// 소환된 캐릭터의 데이터, 소속, 마나 관리
public class SummonedCharacter : MonoBehaviour
{
    public CharacterData Data; // 이 캐릭터의 공통 데이터
    public SlotController CurrentSlot; // 현재 소속 슬롯
    public int SellCoin = 0; // 일반 캐릭터 판매 코인

    public float Mana; // 현재 마나
    public float MaxMana = 100f; // 최대 마나
    public MythicSkillType SkillType; // 신화 스킬 종류
    public CharacterAttack Attack; // 스킬 대상 목록을 가져올 공격 컴포넌트
    public StunArea SkillStunArea; // 웨어울프 스킬의 기절 영역
    public GameObject SkillEffectPrefab; // 신화 스킬 이펙트 프리팹
    [Min(0.1f)] public float SkillEffectSeconds = 2f; // 이펙트 표시 시간
    private const float ManaPerSecond = 15f; // 초당 마나 회복량

    // 슬롯에 있는 신화라면 관리 목록에 등록
    private void OnEnable()
    {
        if (CurrentSlot != null)
            SetSlot(CurrentSlot);
    }

    // 꺼진 신화를 목록에서 제거하고 천사 감속 갱신
    private void OnDisable()
    {
        // Remove는 실제로 목록에서 제거했을 때만 true
        if (BattleManager.instance != null
            && BattleManager.instance.MythicCharacters.Remove(this)
            && SkillType == MythicSkillType.Angel)
        {
            BattleManager.instance.AngleSteck--;
            BattleManager.instance.RefreshSpeedLoss();
        }
    }

    // 시간에 따라 마나 회복, 가득 차면 스킬 시도
    private void Update()
    {
        if (CurrentSlot == null || Data.Grade != CharacterGrade.Mythic
            || BattleManager.instance.IsGameEnded)
            return;

        AddMana(ManaPerSecond * Time.deltaTime);
        // &&는 앞 조건이 참일 때만 뒤 함수 실행
        if (Mana >= MaxMana && UseSkill())
            Mana = 0f; // 스킬 성공 시 마나 소모
    }

    // 소환된 캐릭터에 데이터 연결
    public void Initialize(CharacterData data) 
    {
        Data = data; // 복사하지 않고 같은 CharacterData를 참조
        Mana = 0f;
    }

    // 소속 슬롯과 신화 관리 목록 갱신
    public void SetSlot(SlotController slot)
    {
        CurrentSlot = slot;
        if (Data.Grade != CharacterGrade.Mythic)
            return;

        List<SummonedCharacter> mythics = BattleManager.instance.MythicCharacters;
        if (slot == null)
        {
            if (mythics.Remove(this) && SkillType == MythicSkillType.Angel)
            {
                BattleManager.instance.AngleSteck--;
                BattleManager.instance.RefreshSpeedLoss();
            }
        }
        else if (isActiveAndEnabled && !mythics.Contains(this))
        {
            mythics.Add(this);
            if (SkillType == MythicSkillType.Angel)
            {
                BattleManager.instance.AngleSteck++;
                BattleManager.instance.RefreshSpeedLoss();
            }
        }
    }

    // 최대 마나를 넘지 않게 회복
    public void AddMana(float amount)
    {
        Mana = Mathf.Clamp(Mana + amount, 0f, MaxMana);
    }

    // 천사, 악마, 웨어울프에 맞는 스킬 실행
    private bool UseSkill()
    {
        if (SkillType == MythicSkillType.Angel) // 천사 스킬
        {
            bool restored = false; 
            List<SummonedCharacter> mythics = BattleManager.instance.MythicCharacters;
            for (int i = 0; i < mythics.Count; i++)
            {
                // 천사끼리 마나를 주고받는 반복 방지
                if (mythics[i].SkillType == MythicSkillType.Angel) // 천사는 포함 하지 않음
                    continue;

                mythics[i].AddMana(100f); // 마나 채워주기
                restored = true;
            }
            if (restored)
                ShowSkillEffect();
            return restored;
        }

        List<MonsterManager> targets = Attack.GetSkillTargets();
        if (targets.Count == 0)
            return false; // 공격할 적이 없으면 마나를 보관한다.

        if (SkillType == MythicSkillType.Demon)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].TakeDamage(1000f);
            ShowSkillEffect();
        }
        else if (SkillType == MythicSkillType.Werewolf)
        {
            Vector3 hitPosition = targets[0].transform.position; // 피격 위치 보관
            targets[0].TakeDamage(300f); // 먼저 감지한 적 1명에게 피해
            SkillStunArea.Activate(hitPosition); // 피격 위치에 기절 영역 생성
        }
        else
        {
            return false;
        }
        return true;
    }

    // 현재 위치에 스킬 이펙트 생성
    private void ShowSkillEffect()
    {
        if (SkillEffectPrefab == null)
            return;

        // 부모 없이 생성해서 캐릭터가 이동해도 연출 위치 유지
        GameObject effect = Instantiate(SkillEffectPrefab, transform.position,
            SkillEffectPrefab.transform.rotation);
        Destroy(effect, Mathf.Max(0.1f, SkillEffectSeconds));
    }
}
