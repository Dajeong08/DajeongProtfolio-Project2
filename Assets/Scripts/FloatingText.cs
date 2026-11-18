using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FloatingText : MonoBehaviour
{
    public TMP_Text AmountText;
    public float MoveSpeed = 60f;
    public float ShowSeconds = 0.8f;

    private RectTransform rect;
    private CanvasGroup group;
    private float timer;
    private float direction;

    public void Show(string message, RectTransform parent, Vector2 screenPosition, bool moveUp)
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
        AmountText.text = message;
        group.alpha = 1f;
        group.blocksRaycasts = false;
        timer = 0f;
        // 조건 ? 참일 때 값 : 거짓일 때 값. 획득은 위로, 데미지는 아래로 움직인다.
        direction = moveUp ? 1f : -1f;

        Vector2 position;
        // 화면 픽셀 좌표를 부모 UI 안의 좌표로 바꾼다. Overlay Canvas라 카메라는 null이다.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out position);
        // 변환된 좌표는 부모의 피벗 기준이므로, 텍스트 앵커도 같은 기준으로 맞춘다.
        rect.anchorMin = parent.pivot;
        rect.anchorMax = parent.pivot;
        rect.anchoredPosition = position;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        // 배속과 시간 정지에 관계없이 일정한 시간 동안 표시한다.
        timer += Time.unscaledDeltaTime;
        if (timer >= ShowSeconds)
        {
            Destroy(gameObject);
            return;
        }
        rect.anchoredPosition += Vector2.up * direction * MoveSpeed * Time.unscaledDeltaTime;
        // 경과 시간 / 표시 시간은 0에서 1로 증가한다. 1에서 빼면 점점 투명해진다.
        group.alpha = 1f - timer / ShowSeconds;
    }
}
