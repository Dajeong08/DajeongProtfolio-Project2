using System.Collections.Generic;
using UnityEngine;

// 신화 조합 재료 확인과 소환
public class MythicSummonManager : MonoBehaviour
{
    public SlotManager slotManager; // 재료와 빈 슬롯 확인
    public GameObject LobbyScreen; // 로비에서 조합을 막기 위한 확인

    // 중복 등록되지 않은 활성 슬롯 가져오기
    private SlotController GetSlot(int index)
    {
        GameObject slotObject = slotManager.SlotList[index];
        if (slotObject == null || !slotObject.activeInHierarchy)
            return null;
        for (int i = 0; i < index; i++)
        {
            if (slotManager.SlotList[i] == slotObject)
                return null;
        }
        SlotController slot = slotObject.GetComponent<SlotController>();
        if (slot == null || !slot.isActiveAndEnabled || !slot.HasPositions())
            return null;
        return slot;
    }

    // 필드에 있는 해당 재료 캐릭터 수 계산
    public int CountMaterial(CharacterData data)
    {
        if (slotManager == null || data == null)
            return 0;
        int count = 0;
        for (int i = 0; i < slotManager.SlotList.Count; i++)
        {
            SlotController slot = GetSlot(i);
            if (slot != null && slot.CurrentData == data)
                count += slot.GetCharacterCount();
        }
        return count;
    }

    // 재료를 꺼낼 슬롯 목록 작성
    private bool CollectMaterialSlots(MythicRecipe recipe, List<SlotController> materialSlots)
    {
        // 리스트에 꺼낼 슬롯만 기록, 아직 재료는 소비하지 않음
        materialSlots.Clear();
        if (slotManager == null || BattleManager.instance == null
            || BattleManager.instance.IsGameEnded)
            return false;
        if (LobbyScreen != null && LobbyScreen.activeInHierarchy)
            return false;
        if (recipe == null || recipe.Result == null
            || recipe.Result.Grade != CharacterGrade.Mythic || recipe.Result.Prefab == null)
            return false;
        SummonedCharacter prefab = recipe.Result.Prefab.GetComponent<SummonedCharacter>();
        if (prefab == null || prefab.CurrentSlot != null || recipe.Materials == null
            || recipe.Materials.Count == 0)
            return false;

        for (int i = 0; i < recipe.Materials.Count; i++)
        {
            MythicMaterial material = recipe.Materials[i];
            if (material == null || material.Character == null || material.Count <= 0
                || material.Character.Grade == CharacterGrade.Mythic)
                return false;
            for (int j = 0; j < i; j++)
            {
                if (recipe.Materials[j].Character == material.Character)
                    return false;
            }
            int remaining = material.Count;
            for (int j = slotManager.SlotList.Count - 1; j >= 0; j--)
            {
                SlotController slot = GetSlot(j);
                if (slot == null || slot.CurrentData != material.Character)
                    continue;
                int count = Mathf.Min(remaining, slot.GetCharacterCount());
                for (int k = 0; k < count; k++)
                    // 같은 슬롯을 여러 번 넣으면 그만큼 캐릭터를 꺼냄
                    materialSlots.Add(slot);
                remaining -= count;
                if (remaining == 0)
                    break;
            }
            if (remaining > 0)
                return false;
        }
        return true;
    }

    // 빈 슬롯 또는 재료 소비 후 비게 될 슬롯 찾기
    private SlotController FindResultSlot(List<SlotController> materialSlots)
    {
        SlotController emptySlot = slotManager.FindEmptySlot();
        if (emptySlot != null)
            return emptySlot;
        for (int i = 0; i < materialSlots.Count; i++)
        {
            SlotController slot = materialSlots[i];
            int consumedCount = 0;
            for (int j = 0; j < materialSlots.Count; j++)
            {
                if (materialSlots[j] == slot)
                    consumedCount++;
            }
            // 재료를 모두 소비해 비는 슬롯도 결과 배치 가능
            if (consumedCount == slot.GetCharacterCount())
                return slot;
        }
        return null;
    }

    // 재료와 결과 배치 공간 확인
    public bool CanSummon(MythicRecipe recipe)
    {
        List<SlotController> materialSlots = new List<SlotController>();
        if (!CollectMaterialSlots(recipe, materialSlots))
            return false;
        return FindResultSlot(materialSlots) != null;
    }

    // 재료를 소비하고 신화 조합 소환
    public bool TrySummon(MythicRecipe recipe)
    {
        List<SlotController> materialSlots = new List<SlotController>();
        if (!CollectMaterialSlots(recipe, materialSlots))
            return false;
        SlotController targetSlot = FindResultSlot(materialSlots);
        if (targetSlot == null)
            return false;

        GameObject resultObject = Instantiate(recipe.Result.Prefab,
            targetSlot.Box_1.transform.position, recipe.Result.Prefab.transform.rotation);
        // 재료 소비와 슬롯 등록이 끝난 뒤 활성화
        resultObject.SetActive(false);
        SummonedCharacter resultCharacter = resultObject.GetComponent<SummonedCharacter>();
        resultCharacter.Initialize(recipe.Result);

        List<SummonedCharacter> materials = new List<SummonedCharacter>();
        bool removedAll = true;
        for (int i = 0; i < materialSlots.Count; i++)
        {
            SummonedCharacter character = materialSlots[i].RemoveLastCharacter();
            if (character == null)
            {
                removedAll = false;
                break;
            }
            materials.Add(character);
        }

        if (!removedAll || !targetSlot.TryAddCharacter(resultCharacter))
        {
            // 실패 시 꺼낸 역순으로 원래 슬롯에 복구
            for (int i = materials.Count - 1; i >= 0; i--)
                materialSlots[i].TryAddCharacter(materials[i]);
            Destroy(resultObject);
            slotManager.NotifyCharactersChanged();
            return false;
        }

        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].gameObject.SetActive(false);
            Destroy(materials[i].gameObject);
        }
        resultObject.SetActive(true);
        slotManager.FinishMythicSummon(recipe);
        AchievementManager.instance.AddProgress(AchievementType.SummonMythic, 1);
        return true;
    }
}
