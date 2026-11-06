using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 전체 슬롯에서 자리를 찾고 캐릭터 프리팹을 생성한다.
public class SlotManager : MonoBehaviour
{
    // SlotIndex가 아니라 이 목록에 등록된 순서대로 검색한다.
    public List<GameObject> SlotList = new List<GameObject>();

    // 상점 코인과 별개로, 이번 플레이에서 소환에 사용하는 코인이다.
    public int Coin = 100;
    public int SummonCost = 20;
    public Canvas SlotCanvas;
    public Vector3 SlotCanvasOffset;

    private Draw draw;
    private SlotController selectedSlot;
    private Camera slotCamera;
    private List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        // 비활성화된 UI도 찾아 두므로 Inspector 연결을 추가하지 않아도 된다.
        draw = FindFirstObjectByType<Draw>(FindObjectsInactive.Include);
        slotCamera = Camera.main;
        if (SlotCanvas != null)
        {
            SlotCanvas.worldCamera = slotCamera;
            // 기존 캔버스 내부 패널의 편집용 위치를 중앙으로 맞춘다.
            if (SlotCanvas.transform.childCount > 0)
            {
                RectTransform panel = SlotCanvas.transform.GetChild(0).GetComponent<RectTransform>();
                if (panel != null)
                    panel.anchoredPosition = Vector2.zero;
            }
            SlotCanvas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        Vector2 screenPosition;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return;
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;
            screenPosition = Mouse.current.position.ReadValue();
        }

        // 판매 UI를 누를 때는 선택을 유지해야 버튼에서 판매할 슬롯을 알 수 있다.
        if (EventSystem.current != null)
        {
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = screenPosition;
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            for (int i = 0; i < uiHits.Count; i++)
            {
                if (uiHits[i].module is GraphicRaycaster)
                {
                    if (SlotCanvas != null
                        && uiHits[i].gameObject.transform.IsChildOf(SlotCanvas.transform))
                        return;
                    HideSelectedRange();
                    return;
                }
            }
        }
        HideSelectedRange();
        if (slotCamera == null)
            slotCamera = Camera.main;
        if (slotCamera == null)
            return;

        Ray ray = slotCamera.ScreenPointToRay(screenPosition);
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy)
                continue;
            SlotController slot = SlotList[i].GetComponent<SlotController>();
            SpriteRenderer slotSprite = SlotList[i].GetComponent<SpriteRenderer>();
            if (slot == null || !slot.isActiveAndEnabled || slotSprite == null || !slotSprite.enabled)
                continue;

            // 공격 범위가 아니라 슬롯 이미지 영역으로 클릭을 판정한다.
            Plane slotPlane = new Plane(Vector3.forward, slot.transform.position);
            float distance;
            if (!slotPlane.Raycast(ray, out distance))
                continue;
            if (!slotSprite.bounds.Contains(ray.GetPoint(distance)))
                continue;
            if (slot.IsEmpty())
                return;

            selectedSlot = slot;
            selectedSlot.ShowAttackRange();
            ShowSlotCanvas();
            return;
        }
    }

    private void LateUpdate()
    {
        // 표시 중에 반지름이 바뀌거나 캐릭터가 사라져도 반영한다.
        if (selectedSlot != null)
        {
            if (!selectedSlot.isActiveAndEnabled || selectedSlot.IsEmpty())
            {
                HideSelectedRange();
                return;
            }
            selectedSlot.ShowAttackRange();
            ShowSlotCanvas();
        }
    }

    private void HideSelectedRange()
    {
        if (selectedSlot != null)
            selectedSlot.HideAttackRange();
        selectedSlot = null;
        if (SlotCanvas != null)
            SlotCanvas.gameObject.SetActive(false);
    }

    private void ShowSlotCanvas()
    {
        if (SlotCanvas == null || selectedSlot == null)
            return;
        SlotCanvas.transform.position = selectedSlot.transform.position + SlotCanvasOffset;
        SlotCanvas.gameObject.SetActive(true);
    }

    // 판매 버튼의 On Click에 연결한다. 클릭 한 번에 캐릭터 한 명을 판매한다.
    public void OnSellClick()
    {
        if (selectedSlot == null || !selectedSlot.isActiveAndEnabled)
            return;

        // 마지막 한 명을 팔면 CurrentData가 비워지므로 종류를 먼저 기억한다.
        CharacterData soldData = selectedSlot.CurrentData;
        SummonedCharacter character = selectedSlot.RemoveLastCharacter();
        if (character == null)
            return;

        int reward = Mathf.Max(0, character.SellCoin);
        // Destroy는 프레임 끝에 실행되므로 먼저 꺼서 공격도 즉시 중단한다.
        character.gameObject.SetActive(false);
        Destroy(character.gameObject);
        FillMatchingSlots(soldData);
        if (selectedSlot.IsEmpty())
            HideSelectedRange();
        AddCoin(reward);
    }

    // 판매 후 같은 종류끼리 앞 슬롯부터 3명씩 채운다.
    private void FillMatchingSlots(CharacterData data)
    {
        if (data == null)
            return;

        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy)
                continue;
            SlotController target = SlotList[i].GetComponent<SlotController>();
            if (target == null || !target.isActiveAndEnabled || target.IsEmpty())
                continue;
            if (!target.CanAdd(data))
                continue;

            // 현재 슬롯보다 뒤에 있는 같은 종류 슬롯에서 캐릭터를 가져온다.
            for (int j = i + 1; j < SlotList.Count; j++)
            {
                if (SlotList[j] == null || !SlotList[j].activeInHierarchy)
                    continue;
                SlotController source = SlotList[j].GetComponent<SlotController>();
                if (source == null || source == target || !source.isActiveAndEnabled)
                    continue;
                if (source.CurrentData != data)
                    continue;

                while (target.CanAdd(data) && !source.IsEmpty())
                {
                    SummonedCharacter movingCharacter = source.RemoveLastCharacter();
                    if (movingCharacter == null)
                        break;

                    // 기존 개체를 옮기므로 생성, 판매 보상, 소환 비용 처리는 하지 않는다.
                    if (!target.TryAddCharacter(movingCharacter))
                    {
                        source.TryAddCharacter(movingCharacter);
                        break;
                    }
                }
                if (target.IsFull())
                    break;
            }
        }
    }

    private void OnDisable()
    {
        HideSelectedRange();
    }

    public void AddCoin(int amount)
    {
        Coin += amount;
        if (draw != null)
            draw.RefreshButton();
    }

    // 배치 위치가 연결된 빈 슬롯을 찾는다. 없으면 null을 반환한다.
    public SlotController FindEmptySlot()
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null)
                continue;

            // GetComponent는 오브젝트에 이미 붙어 있는 스크립트를 가져온다.
            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null)
                continue;
            if (!slot.HasPositions())
                continue;
            if (slot.IsEmpty())
                return slot;
        }
        return null;
    }

    // 같은 종류가 들어 있고 3명 미만인 슬롯을 먼저 찾는다.
    private SlotController FindMatchingSlot(CharacterData data)
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null)
                continue;

            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null)
                continue;
            if (slot.IsEmpty())
                continue;
            if (slot.CanAdd(data))
                return slot;
        }
        return null;
    }

    // 생성과 슬롯 등록을 마치면 true, 실패하면 false를 반환한다.
    public bool TrySummon(CharacterData data)
    {
        // 코인이 현재 소환 비용보다 적으면 소환하지 않는다.
        if (Coin < SummonCost)
            return false;

        SlotController emptySlot = FindEmptySlot();
        if (emptySlot == null)
            return false;
        if (data == null || data.Prefab == null)
            return false;

        SummonedCharacter prefabCharacter = data.Prefab.GetComponent<SummonedCharacter>();
        if (prefabCharacter == null)
            return false;

        SlotController targetSlot = FindMatchingSlot(data);
        if (targetSlot == null)
        {
            targetSlot = emptySlot;
        }

        // Instantiate는 원본 프리팹으로부터 실제 오브젝트를 생성한다.
        GameObject newCharacter = Instantiate(
            data.Prefab,
            targetSlot.Box_1.transform.position,
            data.Prefab.transform.rotation
        );
        SummonedCharacter character = newCharacter.GetComponent<SummonedCharacter>();
        character.Initialize(data);

        bool added = targetSlot.TryAddCharacter(character);
        if (!added)
        {
            // 생성만 되고 슬롯에 들어가지 못한 개체는 제거한다.
            Destroy(newCharacter);
            return false;
        }
        // 현재 비용을 먼저 차감한 뒤, 다음 소환 비용을 10 올린다.
        Coin -= SummonCost;
        SummonCost += 10;
        return true;
    }
}
