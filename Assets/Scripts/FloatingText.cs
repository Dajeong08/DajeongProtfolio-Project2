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
        direction = moveUp ? 1f : -1f;

        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, null, out position);
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
        group.alpha = 1f - timer / ShowSeconds;
    }
}
