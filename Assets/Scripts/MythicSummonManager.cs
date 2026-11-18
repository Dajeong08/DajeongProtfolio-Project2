using System.Collections.Generic;
using UnityEngine;

// 일반 합성과 같은 슬롯 함수를 사용하되, 여러 종류의 재료를 소비한다.
public class MythicSummonManager : MonoBehaviour
{
    public SlotManager slotManager;
    public GameObject LobbyScreen;

    // 비활성 슬롯과 목록에 중복 등록된 슬롯은 검색에서 제외한다.
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

    // 같은 슬롯이 여러 번 들어가면 그 슬롯에서 여러 명을 꺼낸다는 뜻이다.
    private bool CollectMaterialSlots(MythicRecipe recipe, List<SlotController> materialSlots)
    {
        // 전달받은 리스트에 결과를 채운다. 여기서는 위치만 조사하고 실제 캐릭터는 아직 빼지 않는다.
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
            // 같은 재료는 한 항목에 수량으로 설정한다. 중복 항목은 잘못된 데이터다.
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
                // 필요한 남은 수량과 슬롯 인원 중 작은 수만 사용해 재료를 과하게 잡지 않는다.
                int count = Mathf.Min(remaining, slot.GetCharacterCount());
                for (int k = 0; k < count; k++)
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
            // 현재 빈칸이 없어도, 이 슬롯의 재료를 전부 쓰면 빈칸이 되므로 결과를 배치할 수 있다.
            if (consumedCount == slot.GetCharacterCount())
                return slot;
        }
        return null;
    }

    public bool CanSummon(MythicRecipe recipe)
    {
        List<SlotController> materialSlots = new List<SlotController>();
        if (!CollectMaterialSlots(recipe, materialSlots))
            return false;
        // 재료만 충분한 것으로 끝내지 않고, 소환 결과를 놓을 자리까지 있어야 true다.
        return FindResultSlot(materialSlots) != null;
    }

    public bool TrySummon(MythicRecipe recipe)
    {
        // 버튼 표시 후 재료가 바뀌었을 수 있으므로 클릭 순간 다시 검사한다.
        List<SlotController> materialSlots = new List<SlotController>();
        if (!CollectMaterialSlots(recipe, materialSlots))
            return false;
        SlotController targetSlot = FindResultSlot(materialSlots);
        if (targetSlot == null)
            return false;

        GameObject resultObject = Instantiate(recipe.Result.Prefab,
            targetSlot.Box_1.transform.position, recipe.Result.Prefab.transform.rotation);
        // 재료 소비와 슬롯 등록을 마친 뒤 켠다. 꺼진 동안에는 신화 목록 등록을 미룬다.
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
            // 실패하면 파괴하지 않고 원래 슬롯에 역순으로 되돌린다.
            for (int i = materials.Count - 1; i >= 0; i--)
                // 두 리스트의 같은 번호는 꺼낸 캐릭터와 원래 소속 슬롯을 가리킨다.
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
        return true;
    }
}
