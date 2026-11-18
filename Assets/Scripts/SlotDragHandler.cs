using UnityEngine;
using UnityEngine.EventSystems;

// SlotManager가 각 발판에 자동으로 붙인다.
// 인터페이스를 구현하면 EventSystem이 클릭·드래그 순간에 아래 함수를 호출한다.
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
            // 드래그를 끝낸 동작이 다시 클릭으로 처리되지 않도록 클릭 자격을 끈다.
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

        // 드래그 시작 위치가 아니라, 마우스를 놓은 순간 감지된 오브젝트를 가져온다.
        GameObject hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit != null)
            slotManager.MoveSlot(slot, hit.GetComponentInParent<SlotController>());
    }
}
