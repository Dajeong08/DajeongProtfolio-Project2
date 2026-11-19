using UnityEngine;

// 인스펙터에서 캐릭터 생성
public class DeveloperManager : MonoBehaviour
{
    public CharacterData Character; // 생성할 캐릭터 데이터
    public int SlotNumber; // SlotList 기준 슬롯 번호, 0부터 시작
    public SlotManager slotManager; // 캐릭터를 배치할 슬롯 목록
    public GameObject LobbyScreen; // 로비에서는 생성하지 않도록 확인

    // 컴포넌트 제목 우클릭 메뉴에 추가
    [ContextMenu("선택한 슬롯에 캐릭터 생성")]
    // 플레이 중 지정한 슬롯에 캐릭터 무료 생성
    private void SpawnCharacter()
    {
        if (!Application.isPlaying)
            return;
        if (BattleManager.instance.IsGameEnded || LobbyScreen.activeInHierarchy)
            return;
        if (Character == null || Character.Prefab == null
            || SlotNumber < 0 || SlotNumber >= slotManager.SlotList.Count)
            return;

        SlotController slot = slotManager.SlotList[SlotNumber].GetComponent<SlotController>();
        if (!slot.isActiveAndEnabled || !slot.CanAdd(Character))
        {
            Debug.Log("선택한 슬롯에 캐릭터를 넣을 수 없습니다.");
            return;
        }

        GameObject characterObject = Instantiate(Character.Prefab,
            slot.Box_1.transform.position, Character.Prefab.transform.rotation);
        SummonedCharacter character = characterObject.GetComponent<SummonedCharacter>();
        character.Initialize(Character);

        if (!slot.TryAddCharacter(character))
        {
            characterObject.SetActive(false);
            Destroy(characterObject);
            return;
        }

        slotManager.NotifyCharactersChanged();
    }
}
