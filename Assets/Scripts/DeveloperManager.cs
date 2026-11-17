using UnityEngine;

// Play 모드에서 Inspector 메뉴로 캐릭터를 무료 생성한다.
public class DeveloperManager : MonoBehaviour
{
    public CharacterData Character;
    public int SlotNumber; // SlotList 순서 기준, 0부터 시작
    public SlotManager slotManager;
    public GameObject LobbyScreen;

    [ContextMenu("선택한 슬롯에 캐릭터 생성")]
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
