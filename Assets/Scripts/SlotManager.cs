using System.Collections.Generic;
using UnityEngine;

// 전체 슬롯에서 자리를 찾고 캐릭터 프리팹을 생성한다.
public class SlotManager : MonoBehaviour
{
    // SlotIndex가 아니라 이 목록에 등록된 순서대로 검색한다.
    public List<GameObject> SlotList = new List<GameObject>();

    // 배치 위치가 연결된 빈 슬롯을 찾는다. 없으면 null을 반환한다.
    public SlotController FindEmptySlot()
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null)
                continue;

            // GetComponent는 오브젝트에 이미 붙어 있는 스크립트를 가져온다.
            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null)
                continue;
            if (!slot.HasPositions())
                continue;
            if (slot.IsEmpty())
                return slot;
        }
        return null;
    }

    // 같은 종류가 들어 있고 3명 미만인 슬롯을 먼저 찾는다.
    private SlotController FindMatchingSlot(CharacterData data)
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null)
                continue;

            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null)
                continue;
            if (slot.IsEmpty())
                continue;
            if (slot.CanAdd(data))
                return slot;
        }
        return null;
    }

    // 생성과 슬롯 등록을 마치면 true, 실패하면 false를 반환한다.
    public bool TrySummon(CharacterData data)
    {
        SlotController emptySlot = FindEmptySlot();
        if (emptySlot == null)
            return false;
        if (data == null || data.Prefab == null)
            return false;

        SummonedCharacter prefabCharacter = data.Prefab.GetComponent<SummonedCharacter>();
        if (prefabCharacter == null)
            return false;

        SlotController targetSlot = FindMatchingSlot(data);
        if (targetSlot == null)
        {
            targetSlot = emptySlot;
        }

        // Instantiate는 원본 프리팹으로부터 실제 오브젝트를 생성한다.
        GameObject newCharacter = Instantiate(
            data.Prefab,
            targetSlot.Box_1.transform.position,
            data.Prefab.transform.rotation
        );
        SummonedCharacter character = newCharacter.GetComponent<SummonedCharacter>();
        character.Initialize(data);

        bool added = targetSlot.TryAddCharacter(character);
        if (!added)
        {
            // 생성만 되고 슬롯에 들어가지 못한 개체는 제거한다.
            Destroy(newCharacter);
            return false;
        }
        return true;
    }
}
