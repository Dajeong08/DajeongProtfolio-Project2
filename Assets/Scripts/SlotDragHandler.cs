using UnityEngine;
using UnityEngine.EventSystems;

// 슬롯 클릭, 드래그 입력 전달
public class SlotDragHandler : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public SlotManager slotManager; // 선택, 이동 요청을 받을 매니저
    private SlotController slot; // 이 입력이 붙은 슬롯
    private bool dragging; // 드래그 중인지 확인

    private void OnDisable()
    {
        if (!dragging)
            return;

        dragging = false;
        slotManager.HideDragPreview();
    }

    // 현재 슬롯 컴포넌트 연결
    private void Awake()
    {
        slot = GetComponent<SlotController>();
    }

    // 드래그가 아니면 슬롯 선택 요청
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            slotManager.SelectSlot(slot);
    }

    // 드래그 시작 기록
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = eventData.button == PointerEventData.InputButton.Left && !slot.IsEmpty()
            && (BattleManager.instance == null || !BattleManager.instance.IsGameEnded);
        if (dragging)
        {
            // 드래그 종료가 클릭으로 다시 처리되지 않게 함
            eventData.eligibleForClick = false;
            slotManager.SelectSlot(null);
            slotManager.ShowDragPreview(slot.GetDragSprite(), eventData.position);
        }
    }

    // 드래그 중에는 미리보기만 커서를 따라 이동
    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
            slotManager.MoveDragPreview(eventData.position);
    }

    // 마우스를 놓은 슬롯으로 이동 요청
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;
        dragging = false;
        slotManager.HideDragPreview();

        GameObject hit = eventData.pointerCurrentRaycast.gameObject; // 마우스를 놓은 위치의 오브젝트
        if (hit != null)
            slotManager.MoveSlot(slot, hit.GetComponentInParent<SlotController>());
    }
}
