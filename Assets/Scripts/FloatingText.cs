using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
// 떠오르는 텍스트 이동과 투명도
public class FloatingText : MonoBehaviour
{
    public TMP_Text AmountText; // 표시할 텍스트
    public float MoveSpeed = 60f; // 텍스트 이동 속도
    public float ShowSeconds = 0.8f; // 표시 시간

    private RectTransform rect; // 텍스트 위치와 크기
    private CanvasGroup group; // 텍스트 묶음의 투명도
    private float timer; // 표시 후 지난 시간
    private float direction; // 위로 1, 아래로 -1

    // 텍스트 내용, 위치, 이동 방향 초기화
    public void Show(string message, RectTransform parent, Vector2 screenPosition, bool moveUp)
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        AmountText.text = message;
        group.alpha = 1f;
        group.blocksRaycasts = false;
        timer = 0f;
        // 조건 ? 참일 때 값 : 거짓일 때 값
        direction = moveUp ? 1f : -1f;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out position);
        rect.anchorMin = parent.pivot;
        rect.anchorMax = parent.pivot;
        rect.anchoredPosition = position;
    }

    // 텍스트를 이동시키고 서서히 투명하게 만들기
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= ShowSeconds)
        {
            Destroy(gameObject);
            return;
        }
        rect.anchoredPosition += Vector2.up * direction * MoveSpeed * Time.deltaTime;
        group.alpha = 1f - timer / ShowSeconds;
    }
}
