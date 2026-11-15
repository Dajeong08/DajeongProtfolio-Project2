using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RouletteManager : MonoBehaviour
{
    public int Token = 0;
    public Draw draw;
    public SlotManager slotManager;
    public GameObject LobbyScreen;
    public TMP_Text TokenText;
    public TMP_Text ResultText;
    public Button RareButton;
    public Button EpicButton;
    public Button LegendaryButton;
    public GameObject RoulettePanel;

    private void Awake()
    {
        Token = 0;
    }

    private void Start()
    {
        ClosePanel();
        RefreshUI();
    }

    // 재시작해도 토큰이 누적되지 않도록 초기값으로 설정한다.
    public void SetStartToken(int amount)
    {
        Token = Mathf.Max(0, amount);
        RefreshUI();
    }

    public void OpenPanel()
    {
        if (BattleManager.instance.IsGameEnded || LobbyScreen.activeInHierarchy)
            return;
        RefreshUI();
        RoulettePanel.SetActive(true);
    }

    public void ClosePanel()
    {
        RoulettePanel.SetActive(false);
    }

    // 보스 처치와 추후 미션 보상에서 함께 사용한다.
    public void AddToken(int amount)
    {
        Token += amount;
        RefreshUI();
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowTokenText(amount);
    }

    public void AddToken(int amount, Vector3 position)
    {
        AddToken(amount);
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowTokenText(amount, position);
    }

    public void OnRareClick()
    {
        Roll(CharacterGrade.Rare, 1, 60, "희귀");
    }

    public void OnEpicClick()
    {
        Roll(CharacterGrade.Epic, 1, 25, "에픽");
    }

    public void OnLegendaryClick()
    {
        Roll(CharacterGrade.Legendary, 2, 10, "전설");
    }

    private bool CanUseRoulette()
    {
        return BattleManager.instance != null && !BattleManager.instance.IsGameEnded
            && !LobbyScreen.activeInHierarchy && slotManager.FindEmptySlot() != null;
    }

    private void Roll(CharacterGrade grade, int cost, int chance, string gradeName)
    {
        // 버튼을 표시한 뒤 토큰이나 슬롯 상태가 바뀌었을 수 있어 다시 확인한다.
        if (!CanUseRoulette() || Token < cost)
            return;

        Token -= cost;
        string message = $"{gradeName} 룰렛 실패";
        // 정수 추첨은 0~99이며, 60 미만이면 정확히 60%다.
        if (Random.Range(0, 100) < chance)
        {
            CharacterData data = draw.PickFromList(grade);
            if (slotManager.TryPlaceCharacter(data))
            {
                message = $"{data.CharacterName} 소환 성공!";
            }
            else
            {
                // 배치 오류는 확률 실패와 다르므로 토큰을 돌려준다.
                Token += cost;
                message = "캐릭터를 배치할 수 없습니다.";
            }
        }
        if (ResultText != null)
            ResultText.text = message;

        // 소환 결과와 토큰 차감을 기존 UI 갱신 흐름에 반영한다.
        slotManager.NotifyCharactersChanged();
    }

    public void RefreshUI()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            ClosePanel();
        TokenText.text = Token.ToString();
        bool canUse = CanUseRoulette();
        RareButton.interactable = canUse && Token >= 1;
        EpicButton.interactable = canUse && Token >= 1;
        LegendaryButton.interactable = canUse && Token >= 2;
    }
}
