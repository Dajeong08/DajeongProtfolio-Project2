using UnityEngine;

// 데미지와 재화 획득 프리팹 생성만 담당한다.
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;
    // Screen Space Overlay Canvas 아래의 표시 위치와 사용자 제작 프리팹이다.
    public RectTransform TextRoot;
    public FloatingText DamageTextPrefab;
    public FloatingText CoinTextPrefab;
    public FloatingText TokenTextPrefab;
    public RectTransform CoinGainPoint;
    public RectTransform TokenGainPoint;
    private Camera textCamera;

    private void Awake()
    {
        instance = this;
        textCamera = Camera.main;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // 데미지는 맞은 위치에서 아래로 내려간다.
    public void ShowDamageText(float amount, Vector3 position)
    {
        CreateWorldText(DamageTextPrefab, amount.ToString("0.#"), position, false, 0f);
    }

    // 위치를 받지 않는 호출은 재화 UI 위에만 표시한다.
    public void ShowCoinText(int amount)
    {
        if (CoinGainPoint != null)
            CreateText(CoinTextPrefab, $"+{amount}",
                RectTransformUtility.WorldToScreenPoint(null, CoinGainPoint.position), true);
    }

    public void ShowTokenText(int amount)
    {
        if (TokenGainPoint != null)
            CreateText(TokenTextPrefab, $"+{amount}",
                RectTransformUtility.WorldToScreenPoint(null, TokenGainPoint.position), true);
    }

    // 처치와 판매는 현장에도 표시한다. 코인과 토큰은 좌우로 살짝 나눈다.
    public void ShowCoinText(int amount, Vector3 position)
    {
        CreateWorldText(CoinTextPrefab, $"+{amount}", position, true, -20f);
    }

    public void ShowTokenText(int amount, Vector3 position)
    {
        CreateWorldText(TokenTextPrefab, $"+{amount}", position, true, 20f);
    }

    private void CreateWorldText(FloatingText prefab, string message,
        Vector3 position, bool moveUp, float offsetX)
    {
        Vector3 screen = textCamera.WorldToScreenPoint(position);
        if (screen.z > 0f)
            CreateText(prefab, message, (Vector2)screen + new Vector2(offsetX, 20f), moveUp);
    }

    private void CreateText(FloatingText prefab, string message, Vector2 screenPosition, bool moveUp)
    {
        // 프리팹을 아직 연결하지 않았으면 연출만 건너뛴다.
        if (prefab == null || TextRoot == null)
            return;
        FloatingText text = Instantiate(prefab, TextRoot);
        text.Show(message, TextRoot, screenPosition, moveUp);
    }

}
