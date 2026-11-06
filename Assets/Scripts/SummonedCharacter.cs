using UnityEngine;

// 같은 오브젝트에 이 스크립트를 중복으로 붙이지 못하게 한다.
[DisallowMultipleComponent]
public class SummonedCharacter : MonoBehaviour
{
    public CharacterData Data;
    public SlotController CurrentSlot;
    // 캐릭터 프리팹에서 설정하는 판매 보상이다. 소환 비용에는 영향을 주지 않는다.
    public int SellCoin = 0;

    // SlotManager가 소환 직후 호출한다. 복제하지 않고 데이터 에셋 참조를 저장한다.
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
