using System.Collections.Generic;
using UnityEngine;

// 슬롯 하나의 종류, 인원, 캐릭터 위치를 관리한다.
public class SlotController : MonoBehaviour
{
    public GameObject Box_1;
    public GameObject Box_2;
    public GameObject Box_3;
    public CharacterData CurrentData;
    public GameObject RangeCircle;
    private List<SummonedCharacter> characters = new List<SummonedCharacter>();

    private void Awake()
    {
        HideAttackRange();
    }

    private void OnDisable()
    {
        HideAttackRange();
    }

    public void HideAttackRange()
    {
        if (RangeCircle != null)
            RangeCircle.SetActive(false);
    }

    public void ShowAttackRange()
    {
        HideAttackRange();
        if (RangeCircle == null || IsEmpty())
            return;

        CircleCollider2D range = null;
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
            }
            if (range != null)
                break;
        }
        if (range == null || !range.isActiveAndEnabled)
            return;

        SpriteRenderer circleSprite = RangeCircle.GetComponent<SpriteRenderer>();
        if (circleSprite == null || circleSprite.sprite == null)
            return;

        // 콜라이더의 로컬 반지름에 실제 월드 배율을 곱한다.
        Vector3 rangeScale = range.transform.lossyScale;
        float radius = range.radius * Mathf.Max(Mathf.Abs(rangeScale.x), Mathf.Abs(rangeScale.y));
        Vector3 spriteSize = circleSprite.sprite.bounds.size;
        Vector3 parentScale = Vector3.one;
        if (RangeCircle.transform.parent != null)
            parentScale = RangeCircle.transform.parent.lossyScale;
        if (spriteSize.x <= 0 || spriteSize.y <= 0
            || Mathf.Abs(parentScale.x) < 0.0001f || Mathf.Abs(parentScale.y) < 0.0001f)
            return;

        // 지름 = 반지름 * 2. 슬롯 배율을 나눠 원이 타원으로 늘어나지 않게 한다.
        RangeCircle.transform.localScale = new Vector3(
            radius * 2 / spriteSize.x / Mathf.Abs(parentScale.x),
            radius * 2 / spriteSize.y / Mathf.Abs(parentScale.y), 1);
        RangeCircle.transform.position = transform.position;
        RangeCircle.transform.position -= RangeCircle.transform.TransformVector(circleSprite.sprite.bounds.center);
        RangeCircle.SetActive(true);
    }

    // 캐릭터가 한 명도 없으면 빈 슬롯이다.
    public bool IsEmpty() // 비었는지 확인하는 함수
    {
        if (characters.Count == 0)
            return true;
        return false;
    }

    // 일반 캐릭터는 3명, 신화는 한 슬롯에 1명만 배치한다.
    public bool IsFull() // 꽉 찼는지 확인하는 함수
    {
        if (CurrentData != null && CurrentData.Grade == CharacterGrade.Mythic) // 신화 등급이면 1명만 배치 가능
            return characters.Count >= 1; // 현재 슬롯이 신화 등급이면 1명 이상에서 꽉찬 상태
        if (characters.Count >= 3)
            return true; // 3 이상이라면 트루
        return false;
    }

    public int GetCharacterCount()
    {
        return characters.Count;
    }

    // 세 배치 위치가 모두 연결되어 있는지 확인한다.
    public bool HasPositions()
    {
        if (Box_1 == null || Box_2 == null || Box_3 == null)
            return false;
        return true;
    }

    // 캐릭터를 넣을 수 있는지 확인
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

        // 같은 데이터인지 비교한다.
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
        if (!CanAdd(character.Data)) // 넣을 수 없다면 
            return false;

        CurrentData = character.Data; // 이 슬롯의 데이터를 생성되는 캐릭터 데이터로 설정한다.
        characters.Add(character); // 캐릭터를 리스트에 추가한다.
        character.SetSlot(this); // 생성되는 캐릭터의 슬롯을 현재 슬롯으로 설정한다.
        character.transform.SetParent(transform, true); // 생성되는 캐릭터의 부모를 현재 슬롯으로 설정한다.
                                                        // true: 부모를 바꿀 때 기존 월드 크기와 위치를 유지한다.

        RefreshPositions(); 
        return true;
    }

    // 마지막에 추가한 캐릭터를 슬롯 목록에서 빼서 판매 처리에 전달한다.
    public SummonedCharacter RemoveLastCharacter()
    {
        if (IsEmpty())
            return null; 

        int lastIndex = characters.Count - 1; 
        SummonedCharacter character = characters[lastIndex];
        characters.RemoveAt(lastIndex); // 리스트의 마지막 캐릭터를 제거한다.
        if (character != null)
            character.SetSlot(null); // 캐릭터의 슬롯을 null로 설정한다.

        if (IsEmpty()) // 제거후 슬롯이 비어있다면 CurrentData를 null로 설정하고 공격 범위를 숨긴다.
        {
            CurrentData = null;
            HideAttackRange();
        }
        else // 제거후 슬롯이 비어있지 않다면 배치 위치를 새로 갱신한다.
        {
            RefreshPositions();
        }
        return character; // 제거된 캐릭터를 반환한다.
    }

    // 목록 순서대로 첫째, 둘째, 셋째 위치에 세운다.
    private void RefreshPositions() // characters리스트에 있는 순서대로 배치 위치를 새로 갱신한다.
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
