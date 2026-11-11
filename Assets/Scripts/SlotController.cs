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
    public GameObject RangeCircle;
    private List<SummonedCharacter> characters = new List<SummonedCharacter>();

    private void Awake()
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

    private void OnDisable()
    {
        HideAttackRange();
    }

    // 캐릭터가 한 명도 없으면 빈 슬롯이다.
    public bool IsEmpty()
    {
        if (characters.Count == 0)
            return true;
        return false;
    }

    public int GetCharacterCount()
    {
        return characters.Count;
    }

    // 일반 캐릭터는 3명, 신화는 한 슬롯에 1명만 배치한다.
    public bool IsFull()
    {
        if (CurrentData != null && CurrentData.Grade == CharacterGrade.Mythic)
            return characters.Count >= 1;
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

    // 마지막에 추가한 캐릭터를 슬롯 목록에서 빼서 판매 처리에 전달한다.
    public SummonedCharacter RemoveLastCharacter()
    {
        if (IsEmpty())
            return null;

        int lastIndex = characters.Count - 1;
        SummonedCharacter character = characters[lastIndex];
        characters.RemoveAt(lastIndex);
        if (character != null)
            character.SetSlot(null);

        if (IsEmpty())
        {
            CurrentData = null;
            HideAttackRange();
        }
        else
        {
            RefreshPositions();
        }
        return character;
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
