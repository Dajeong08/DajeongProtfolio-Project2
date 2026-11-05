using System.Collections.Generic;
using UnityEngine;

// 슬롯 하나의 종류, 인원, 캐릭터 위치를 관리한다.
public class SlotController : MonoBehaviour
{
    public int SlotIndex;
    public GameObject Box_1;
    public GameObject Box_2;
    public GameObject Box_3;
    public CharacterData CurrentData;
    private List<SummonedCharacter> characters = new List<SummonedCharacter>();

    // 캐릭터가 한 명도 없으면 빈 슬롯이다.
    public bool IsEmpty()
    {
        if (characters.Count == 0)
            return true;
        return false;
    }

    // 한 슬롯의 최대 인원은 3명이다.
    public bool IsFull()
    {
        if (characters.Count >= 3)
            return true;
        return false;
    }

    // 세 배치 위치가 모두 연결되어 있는지 확인한다.
    public bool HasPositions()
    {
        if (Box_1 == null || Box_2 == null || Box_3 == null)
            return false;
        return true;
    }

    // 빈 슬롯이거나 같은 종류의 슬롯일 때만 추가할 수 있다.
    public bool CanAdd(CharacterData data)
    {
        if (data == null)
            return false;
        if (!HasPositions())
            return false;
        if (IsFull())
            return false;
        if (IsEmpty())
            return true;

        // 이름이나 ID 문자열이 아니라 같은 데이터 에셋인지 비교한다.
        if (CurrentData == data)
            return true;
        return false;
    }

    // 캐릭터를 등록하고 소속 슬롯과 위치를 설정한다.
    public bool TryAddCharacter(SummonedCharacter character)
    {
        if (character == null)
            return false;
        if (character.CurrentSlot != null)
            return false;
        if (!CanAdd(character.Data))
            return false;

        CurrentData = character.Data;
        characters.Add(character);
        // this는 현재 이 SlotController 자신이다.
        character.SetSlot(this);
        // true: 부모를 바꿀 때 기존 월드 크기와 위치를 유지한다.
        character.transform.SetParent(transform, true);
        RefreshPositions();
        return true;
    }

    // 목록 순서대로 첫째, 둘째, 셋째 위치에 세운다.
    private void RefreshPositions()
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (i == 0)
            {
                characters[i].transform.position = Box_1.transform.position;
            }
            else if (i == 1)
            {
                characters[i].transform.position = Box_2.transform.position;
            }
            else if (i == 2)
            {
                characters[i].transform.position = Box_3.transform.position;
            }
        }
    }
}
