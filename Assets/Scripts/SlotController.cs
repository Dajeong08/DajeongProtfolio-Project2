using System.Collections.Generic;
using UnityEngine;

// 슬롯 하나의 캐릭터와 배치 관리
public class SlotController : MonoBehaviour
{
    public GameObject Box_1; // 첫 번째 배치 위치
    public GameObject Box_2; // 두 번째 배치 위치
    public GameObject Box_3; // 세 번째 배치 위치
    public CharacterData CurrentData; // 슬롯에 있는 캐릭터 종류
    public GameObject RangeCircle; // 선택 시 표시할 공격 범위 원
    private List<SummonedCharacter> characters = new List<SummonedCharacter>(); // 이 슬롯에 있는 캐릭터 목록

    // 공격 범위 표시 끄기
    private void Awake()
    {
        HideAttackRange();
    }

    // 슬롯이 꺼지면 범위 표시도 끄기
    private void OnDisable()
    {
        HideAttackRange();
    }

    // 선택된 공격 범위 원 숨기기
    public void HideAttackRange()
    {
        if (RangeCircle != null)
            RangeCircle.SetActive(false);
    }

    // 슬롯 캐릭터의 공격 반지름에 맞춰 원 표시
    public void ShowAttackRange()
    {
        HideAttackRange();
        if (RangeCircle == null || IsEmpty())
            return;

        CircleCollider2D range = null;
        float displayRadius = 0f;
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == null || !characters[i].isActiveAndEnabled)
                continue;
            CharacterAttack attack = characters[i].GetComponent<CharacterAttack>();
            if (attack != null)
            {
                range = attack.attackRange;
                if (range == null)
                    range = attack.GetComponent<CircleCollider2D>();
                displayRadius = attack.DisplayRangeRadius;
            }
            if (range != null)
                break;
        }
        if (range == null || !range.isActiveAndEnabled)
            return;

        SpriteRenderer circleSprite = RangeCircle.GetComponent<SpriteRenderer>();
        if (circleSprite == null || circleSprite.sprite == null)
            return;

        // lossyScale은 부모 배율까지 포함한 월드 배율
        Vector3 rangeScale = range.transform.lossyScale;
        float radius = displayRadius * Mathf.Max(Mathf.Abs(rangeScale.x), Mathf.Abs(rangeScale.y));
        Vector3 spriteSize = circleSprite.sprite.bounds.size;
        Vector3 parentScale = Vector3.one;
        if (RangeCircle.transform.parent != null)
            parentScale = RangeCircle.transform.parent.lossyScale;
        if (spriteSize.x <= 0 || spriteSize.y <= 0
            || Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f)
            return;

        // 지름으로 크기를 맞추고 슬롯 배율 보정
        RangeCircle.transform.localScale = new Vector3(
            radius * 2 / spriteSize.x / Mathf.Abs(parentScale.x),
            radius * 2 / spriteSize.y / Mathf.Abs(parentScale.y), 1);
        RangeCircle.transform.position = transform.position;
        RangeCircle.transform.position -= RangeCircle.transform.TransformVector(circleSprite.sprite.bounds.center);
        RangeCircle.SetActive(true);
    }

    // 슬롯이 비었는지 확인
    public bool IsEmpty()
    {
        if (characters.Count == 0)
            return true;
        return false;
    }

    // 일반 3명, 신화 1명이면 가득 찬 상태
    public bool IsFull()
    {
        if (CurrentData != null && CurrentData.Grade == CharacterGrade.Mythic) // 신화 등급이면 1명만 배치 가능
            return characters.Count >= 1; // 신화는 1명으로 가득 참
        if (characters.Count >= 3)
            return true; // 3명이면 가득 참
        return false;
    }

    // 현재 인원 반환
    public Sprite GetDragSprite()
    {
        if (IsEmpty())
            return null;

        CharacterAttack attack = characters[0].GetComponent<CharacterAttack>();
        return attack.characterSprite.sprite;
    }

    public int GetCharacterCount()
    {
        return characters.Count;
    }

    // 세 배치 위치가 연결됐는지 확인
    public bool HasPositions()
    {
        if (Box_1 == null || Box_2 == null || Box_3 == null)
            return false;
        return true;
    }

    // 같은 종류를 추가할 수 있는지 확인
    public bool CanAdd(CharacterData data)
    {
        if (data == null)
            return false;
        if (!HasPositions()) // 배치 위치 있는지 확인
            return false;
        if (IsFull()) // 꽉 찼는지 확인
            return false;
        if (IsEmpty()) // 비어있으면 어떤 캐릭터든 넣을 수 있다.
            return true;

        if (CurrentData == data)
            return true;
        return false;
    }

    // 캐릭터를 등록하고 소속 슬롯과 위치 설정
    public bool TryAddCharacter(SummonedCharacter character)
    {
        if (character == null)
            return false;
        if (character.CurrentSlot != null)
            return false;
        if (!CanAdd(character.Data)) // 넣을 수 없다면
            return false;

        CurrentData = character.Data; // 슬롯의 종류 설정
        characters.Add(character); // 캐릭터 등록
        character.SetSlot(this); // 소속 슬롯 연결
        // true: 부모가 바뀌어도 월드 위치와 크기 유지
        character.transform.SetParent(transform, true); // 슬롯을 부모로 설정

        RefreshPositions(); 
        return true;
    }

    // 마지막 캐릭터를 목록에서 빼서 반환
    public SummonedCharacter RemoveLastCharacter()
    {
        if (IsEmpty())
            return null; 

        int lastIndex = characters.Count - 1; 
        SummonedCharacter character = characters[lastIndex];
        characters.RemoveAt(lastIndex); // 목록에서 제거
        if (character != null)
            character.SetSlot(null); // 소속 슬롯 해제

        if (IsEmpty()) // 마지막 캐릭터를 뺀 경우
        {
            CurrentData = null;
            HideAttackRange();
        }
        else // 남은 캐릭터 위치 갱신
        {
            RefreshPositions();
        }
        return character; // 꺼낸 캐릭터 반환
    }

    // 두 슬롯의 캐릭터 목록과 종류 교환
    public void SwapCharacters(SlotController other)
    {
        // 한쪽 목록을 덮어쓰지 않게 임시 보관
        List<SummonedCharacter> temp = characters;
        characters = other.characters;
        other.characters = temp;

        CharacterData tempData = CurrentData;
        CurrentData = other.CurrentData;
        other.CurrentData = tempData;

        HideAttackRange();
        other.HideAttackRange();
        PlaceMovedCharacters();
        other.PlaceMovedCharacters();
    }

    // 이동한 캐릭터의 소속과 감지 목록 갱신
    private void PlaceMovedCharacters()
    {
        for (int i = 0; i < characters.Count; i++)
        {
            characters[i].SetSlot(this);
            characters[i].transform.SetParent(transform, true);
            characters[i].GetComponent<CharacterAttack>().ClearTargets();
        }
        RefreshPositions();
    }

    // 목록 순서대로 세 배치 위치에 정렬
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
