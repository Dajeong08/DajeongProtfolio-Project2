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
    public GameObject MergeButton;
    public Vector3 SlotCanvasOffset;
    public MythicSummonUI mythicUI;

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
            if (MergeButton == null)
            {
                // Inspector 연결이 비어 있으면 꺼진 합성 버튼도 캔버스에서 찾는다.
                Button[] buttons = SlotCanvas.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].gameObject.name == "Button_Merge")
                    {
                        MergeButton = buttons[i].gameObject;
                        break;
                    }
                }
            }
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
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

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

        // 판매·합성 UI를 누를 때는 버튼이 처리할 슬롯을 알 수 있도록 선택을 유지한다.
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
        if (MergeButton != null)
            MergeButton.SetActive(CanMergeSelectedSlot());
        SlotCanvas.gameObject.SetActive(true);
    }

    private bool CanMergeSelectedSlot()
    {
        // 슬롯은 같은 데이터의 캐릭터만 받는다. 다음 등급 후보는 여기서 검사하지 않는다.
        return selectedSlot != null && selectedSlot.isActiveAndEnabled
            && selectedSlot.IsFull() && selectedSlot.CurrentData != null
            && selectedSlot.CurrentData.Grade < CharacterGrade.Legendary;
    }

    public void OnMergeClick()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (!CanMergeSelectedSlot() || draw == null)
            return;

        SlotController slot = selectedSlot;
        if (!slot.HasPositions())
            return;

        // 등급이 일반 → 희귀 → 에픽 → 전설 순서로 저장되어 있다.
        CharacterGrade nextGrade = slot.CurrentData.Grade + 1;
        CharacterData result = draw.PickCharacterByGrade(nextGrade);
        if (result == null)
        {
            Debug.LogWarning($"합성 결과로 사용할 {nextGrade} 캐릭터를 Character List에 등록해주세요.");
            return;
        }

        // 소환처럼 같은 캐릭터가 있는 칸을 우선하고, 없으면 재료 칸을 쓴다.
        SlotController targetSlot = FindMatchingSlot(result);
        if (targetSlot == null)
            targetSlot = slot;

        GameObject resultObject = Instantiate(result.Prefab,
            targetSlot.Box_1.transform.position, result.Prefab.transform.rotation);
        SummonedCharacter resultCharacter = resultObject.GetComponent<SummonedCharacter>();
        resultCharacter.Initialize(result);

        // 결과를 슬롯에 등록할 때까지 재료 오브젝트를 보관한다.
        SummonedCharacter[] materials = new SummonedCharacter[3];
        for (int i = 0; i < materials.Length; i++)
            materials[i] = slot.RemoveLastCharacter();

        if (!targetSlot.TryAddCharacter(resultCharacter))
        {
            resultObject.SetActive(false);
            Destroy(resultObject);
            for (int i = materials.Length - 1; i >= 0; i--)
                slot.TryAddCharacter(materials[i]);
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
                continue;
            materials[i].gameObject.SetActive(false);
            Destroy(materials[i].gameObject);
        }

        if (slot.IsEmpty())
            HideSelectedRange();
        else
            ShowSlotCanvas();
        NotifyCharactersChanged();
    }

    // 판매 버튼의 On Click에 연결한다. 클릭 한 번에 캐릭터 한 명을 판매한다.
    public void OnSellClick()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

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
        NotifyCharactersChanged();
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

    public void CloseBattleControls()
    {
        HideSelectedRange();
        if (draw != null)
            draw.RefreshButton();
        if (mythicUI != null)
            mythicUI.RefreshUI();
    }

    // 소환, 판매, 합성 후 바뀐 캐릭터 수를 UI에 반영한다.
    public void NotifyCharactersChanged()
    {
        if (draw != null)
            draw.RefreshButton();
        if (mythicUI != null)
            mythicUI.RefreshUI();
    }

    public void FinishMythicSummon(MythicRecipe recipe)
    {
        HideSelectedRange();
        for (int i = 0; i < recipe.Materials.Count; i++)
            FillMatchingSlots(recipe.Materials[i].Character);
        NotifyCharactersChanged();
    }

    // 배치 위치가 연결된 빈 슬롯을 찾는다. 없으면 null을 반환한다.
    public SlotController FindEmptySlot()
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy)
                continue;

            // GetComponent는 오브젝트에 이미 붙어 있는 스크립트를 가져온다.
            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null || !slot.isActiveAndEnabled)
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
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy)
                continue;

            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null || !slot.isActiveAndEnabled)
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
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return false;

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
        NotifyCharactersChanged();
        return true;
    }
}
