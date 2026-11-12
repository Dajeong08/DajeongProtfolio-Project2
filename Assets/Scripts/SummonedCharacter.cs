using System.Collections.Generic;
using UnityEngine;

public enum MythicSkillType
{
    None,
    Demon,
    Werewolf,
    Angel
}

public class SummonedCharacter : MonoBehaviour
{
    public CharacterData Data; // 소환된 캐릭터가 가지는 데이터
    public SlotController CurrentSlot; // 캐릭터 현재 슬롯
    public int SellCoin = 0; // 판매시 얻는 코인 개수

    // 신화 마나와 스킬
    public float Mana;
    public float MaxMana = 100f;
    public MythicSkillType SkillType;
    public CharacterAttack Attack;
    private const float ManaPerSecond = 15f;

    private void OnEnable()
    {
        // 슬롯에 배치된 신화를 다시 켜면 목록에도 다시 등록한다.
        if (CurrentSlot != null)
            SetSlot(CurrentSlot);
    }

    private void OnDisable()
    {
        // 씬이 닫힐 때는 전투 매니저가 먼저 사라질 수 있다.
        if (BattleManager.instance != null)
            BattleManager.instance.MythicCharacters.Remove(this);
    }

    private void Update()
    {
        if (CurrentSlot == null || Data.Grade != CharacterGrade.Mythic
            || BattleManager.instance.IsGameEnded)
            return;

        AddMana(ManaPerSecond * Time.deltaTime);
        if (Mana >= MaxMana && UseSkill())
            Mana -= MaxMana; // 사용한 만큼만 빼서 초과 마나는 남긴다.
    }

    // SlotManager가 소환 직후 호출한다. 캐릭터가 가질 데이터를 저장한다.
    public void Initialize(CharacterData data) 
    {
        Data = data;
        Mana = 0f;
    }

    // SlotController가 등록할 때 호출해 소속 슬롯을 알려준다.
    public void SetSlot(SlotController slot)
    {
        CurrentSlot = slot;
        if (Data.Grade != CharacterGrade.Mythic)
            return;

        List<SummonedCharacter> mythics = BattleManager.instance.MythicCharacters;
        if (slot == null)
            mythics.Remove(this);
        else if (isActiveAndEnabled && !mythics.Contains(this))
            mythics.Add(this);
    }

    public void AddMana(float amount)
    {
        Mana += amount;
    }

    // 실제로 스킬을 사용했을 때만 true를 반환한다.
    private bool UseSkill()
    {
        if (SkillType == MythicSkillType.Angel)
        {
            bool restored = false;
            List<SummonedCharacter> mythics = BattleManager.instance.MythicCharacters;
            for (int i = 0; i < mythics.Count; i++)
            {
                // 천사끼리 마나를 주고받으며 스킬이 반복되는 것을 막는다.
                if (mythics[i].SkillType == MythicSkillType.Angel)
                    continue;

                mythics[i].AddMana(100f);
                restored = true;
            }
            return restored;
        }

        List<MonsterManager> targets = Attack.GetSkillTargets();
        if (targets.Count == 0)
            return false; // 공격할 적이 없으면 마나를 보관한다.

        if (SkillType == MythicSkillType.Demon)
        {
            for (int i = 0; i < targets.Count; i++)
                targets[i].TakeDamage(1000f);
        }
        else if (SkillType == MythicSkillType.Werewolf)
        {
            targets[0].TakeDamage(300f);
        }
        else
        {
            return false;
        }
        return true;
    }
}
