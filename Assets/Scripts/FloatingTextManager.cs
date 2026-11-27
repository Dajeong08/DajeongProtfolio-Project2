using UnityEngine;

// 데미지와 재화 텍스트 생성
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance; // 텍스트 생성 매니저
    public RectTransform TextRoot; // 생성한 텍스트의 부모
    public FloatingText DamageTextPrefab; // 데미지 텍스트 프리팹
    public FloatingText CoinTextPrefab; // 코인 획득 텍스트 프리팹
    public FloatingText TokenTextPrefab; // 토큰 획득 텍스트 프리팹
    public RectTransform CoinGainPoint; // 코인 획득 표시 위치
    public RectTransform TokenGainPoint; // 토큰 획득 표시 위치
    private Camera textCamera; // 월드 위치를 화면 좌표로 바꿀 카메라

    // 매니저와 카메라 참조 등록
    private void Awake()
    {
        instance = this;
        textCamera = Camera.main;
    }

    // 삭제된 매니저 참조 해제
    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // 피격 위치에 데미지 표시
    public void ShowDamageText(float amount, Vector3 position)
    {
        CreateWorldText(DamageTextPrefab, amount.ToString("0.#"), position, false, 0f);
    }

    // 코인 획득량 표시
    public void ShowCoinText(int amount)
    {
        if (CoinGainPoint != null)
            CreateText(CoinTextPrefab, $"+{amount}",
                RectTransformUtility.WorldToScreenPoint(null, CoinGainPoint.position), true);
    }

    // 토큰 획득량 표시
    public void ShowTokenText(int amount)
    {
        if (TokenGainPoint != null)
            CreateText(TokenTextPrefab, $"+{amount}",
                RectTransformUtility.WorldToScreenPoint(null, TokenGainPoint.position), true);
    }

    // 코인 획득량 표시
    public void ShowCoinText(int amount, Vector3 position)
    {
        CreateWorldText(CoinTextPrefab, $"+{amount}", position, true, -20f);
    }

    // 토큰 획득량 표시
    public void ShowTokenText(int amount, Vector3 position)
    {
        CreateWorldText(TokenTextPrefab, $"+{amount}", position, true, 20f);
    }

    // 월드 위치를 화면 좌표로 바꿔 텍스트 생성
    private void CreateWorldText(FloatingText prefab, string message,
        Vector3 position, bool moveUp, float offsetX)
    {
        // 월드 좌표를 화면 픽셀 좌표로 변환
        Vector3 screen = textCamera.WorldToScreenPoint(position);
        // 카메라 앞에 있는 위치만 표시
        if (screen.z > 0f)
            CreateText(prefab, message, (Vector2)screen + new Vector2(offsetX, 20f), moveUp);
    }

    // 결과 화면에 남지 않도록 떠오르는 텍스트 정리
    public void ClearTexts()
    {
        for (int i = TextRoot.childCount - 1; i >= 0; i--)
        {
            GameObject text = TextRoot.GetChild(i).gameObject;
            text.SetActive(false);
            Destroy(text);
        }
    }

    // 프리팹을 생성하고 표시 정보 전달
    private void CreateText(FloatingText prefab, string message, Vector2 screenPosition, bool moveUp)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (prefab == null || TextRoot == null)
            return;
        FloatingText text = Instantiate(prefab, TextRoot);
        text.Show(message, TextRoot, screenPosition, moveUp);
    }

}
