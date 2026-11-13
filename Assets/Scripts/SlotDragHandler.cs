using UnityEngine;
using UnityEngine.EventSystems;

// SlotManager가 각 발판에 자동으로 붙인다.
public class SlotDragHandler : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SlotManager slotManager;
    private SlotController slot;
    private bool dragging;

    private void Awake()
    {
        slot = GetComponent<SlotController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            slotManager.SelectSlot(slot);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = eventData.button == PointerEventData.InputButton.Left && !slot.IsEmpty();
        if (dragging)
        {
            eventData.eligibleForClick = false;
            slotManager.SelectSlot(null);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 캐릭터는 마우스를 놓았을 때 이동한다.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;
        dragging = false;

        GameObject hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit != null)
            slotManager.MoveSlot(slot, hit.GetComponentInParent<SlotController>());
    }
}
