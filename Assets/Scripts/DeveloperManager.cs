using UnityEngine;

// Play 모드에서 Inspector 메뉴로 캐릭터를 무료 생성한다.
public class DeveloperManager : MonoBehaviour
{
    public CharacterData Character;
    public int SlotNumber; // SlotList 순서 기준, 0부터 시작
    public SlotManager slotManager;
    public GameObject LobbyScreen;

    // 컴포넌트의 메뉴에서 직접 실행한다. UI 버튼에 연결하지 않아도 호출할 수 있다.
    [ContextMenu("선택한 슬롯에 캐릭터 생성")]
    private void SpawnCharacter()
    {
        // 편집 모드에서 실행해 씬에 캐릭터가 저장되지 않도록 플레이 중에만 허용한다.
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

        // 비용을 내는 일반 소환을 거치지 않았으므로 생성 후 UI는 여기서 직접 갱신한다.
        slotManager.NotifyCharactersChanged();
    }
}
