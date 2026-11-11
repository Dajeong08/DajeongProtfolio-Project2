using UnityEngine;
public class SummonedCharacter : MonoBehaviour
{
    public CharacterData Data; // 소환된 캐릭터가 가지는 데이터
    public SlotController CurrentSlot; // 캐릭터 현재 슬롯
    public int SellCoin = 0; // 판매시 얻는 코인 개수

    // SlotManager가 소환 직후 호출한다. 캐릭터가 가질 데이터를 저장한다.
    public void Initialize(CharacterData data) 
    {
        Data = data;
    }

    // SlotController가 등록할 때 호출해 소속 슬롯을 알려준다.
    public void SetSlot(SlotController slot)
    {
        CurrentSlot = slot;
    }
}
