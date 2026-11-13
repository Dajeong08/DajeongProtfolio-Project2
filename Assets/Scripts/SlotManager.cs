using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// 전체 슬롯에서 자리를 찾고 캐릭터 프리팹을 생성한다.
public class SlotManager : MonoBehaviour
{
    // 이 목록에 등록된 순서대로 슬롯을 검색한다.
    public List<GameObject> SlotList = new List<GameObject>();

    // 상점 코인과 별개로, 이번 플레이에서 소환에 사용하는 코인이다.
    public int Coin = 100;
    public int SummonCost = 20;
    private const int MaxSummonCost = 60;
    public Canvas SlotCanvas;
    public GameObject MergeButton;
    public GameObject SellButton;
    public Vector3 SlotCanvasOffset;
    public MythicSummonUI mythicUI;

    private Draw draw;
    private SlotController selectedSlot;

    private void Awake()
    {
        // 비활성화된 UI도 찾아 두므로 Inspector 연결을 추가하지 않아도 된다.
        draw = FindFirstObjectByType<Draw>(FindObjectsInactive.Include);
        for (int i = 0; i < SlotList.Count; i++)
        {
            SlotDragHandler handler = SlotList[i].GetComponent<SlotDragHandler>();
            if (handler == null)
                handler = SlotList[i].AddComponent<SlotDragHandler>();
            handler.slotManager = this;
        }
        if (SlotCanvas != null)
        {
            SlotCanvas.gameObject.SetActive(false); // 슬롯 선택 UI숨기기
        }
    }

    private void OnDisable()
    {
        HideSelectedRange();
    }

    private void Update()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return; // 마우스 왼쪽 버튼을 눌렀을때만 처리

        // UI와 발판 클릭은 EventSystem에서 처리하고, 빈 배경을 누르면 선택을 닫는다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        HideSelectedRange();
    }

    public void SelectSlot(SlotController slot)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        HideSelectedRange();
        if (slot == null || slot.IsEmpty())
            return;
        selectedSlot = slot;
        ShowSlotCanvas();
    }

    public void MoveSlot(SlotController source, SlotController target)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (source.IsEmpty() || target == null || source == target)
            return;
        if (!SlotList.Contains(target.gameObject) || !source.HasPositions() || !target.HasPositions())
            return;

        HideSelectedRange();
        source.SwapCharacters(target);
        SelectSlot(target);
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
        if (selectedSlot == null || !selectedSlot.isActiveAndEnabled || selectedSlot.IsEmpty())
        {
            HideSelectedRange();
            return;
        }
        selectedSlot.ShowAttackRange();
        if (SlotCanvas == null)
            return;
        SlotCanvas.transform.position = selectedSlot.transform.position + SlotCanvasOffset;
        if (MergeButton != null)
            MergeButton.SetActive(CanMergeSelectedSlot());
        if (SellButton != null)
            SellButton.SetActive(selectedSlot.CurrentData.Grade != CharacterGrade.Mythic);
        SlotCanvas.gameObject.SetActive(true);
    }

    private bool CanMergeSelectedSlot()// 합성 가능한 슬롯인지 확인
    {
        // 슬롯은 같은 데이터의 캐릭터만 받는다. 다음 등급 후보는 여기서 검사하지 않는다.
        return selectedSlot != null && selectedSlot.isActiveAndEnabled // 슬롯 있는지, 비활성화 확인
            && selectedSlot.IsFull() && selectedSlot.CurrentData != null // 꽉차지 않음 && 데이터 확인
            && selectedSlot.CurrentData.Grade < CharacterGrade.Legendary; // 전설 이하
    }

    public void OnMergeClick() // 합성 버튼 클릭시
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (!CanMergeSelectedSlot() || draw == null)
            return;

        SlotController slot = selectedSlot;
        if (!slot.HasPositions())
            return;

        // 등급이 일반 → 희귀 → 에픽 → 전설 순서로 저장되어 있다.
        CharacterGrade nextGrade = slot.CurrentData.Grade + 1; // 다음 등급
        CharacterData result = draw.PickFromList(nextGrade); // 랜덤 캐릭터 반환

        // 소환처럼 같은 캐릭터가 있는 칸을 우선하고, 없으면 재료 칸을 쓴다.
        SlotController targetSlot = FindMatchingSlot(result); // 랜덤 생성될 캐릭터와 같은 슬롯 찾기
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

        if (!targetSlot.TryAddCharacter(resultCharacter)) // 등록 시도 후 실패시 
        {
            resultObject.SetActive(false);
            Destroy(resultObject);
            for (int i = materials.Length - 1; i >= 0; i--)
                slot.TryAddCharacter(materials[i]); // 원상복구
            return;
        }

        for (int i = 0; i < materials.Length; i++) // 합성된 캐릭터 삭제
        {
            if (materials[i] == null)
                continue;
            materials[i].gameObject.SetActive(false); // 비활성화
            Destroy(materials[i].gameObject); // 삭제
        }

        NotifyCharactersChanged(); // UI 갱신
    }

    // 판매 버튼의 On Click에 연결한다. 클릭 한 번에 캐릭터 한 명을 판매한다.
    public void OnSellClick()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded) // 게임 끝인지 확인
            return;

        if (selectedSlot == null || !selectedSlot.isActiveAndEnabled) // 슬롯 없거나 비활성화 확인
            return;
        // 신화는 버튼 외의 경로로 호출해도 판매하지 않는다.
        if (selectedSlot.IsEmpty() || selectedSlot.CurrentData.Grade == CharacterGrade.Mythic)
            return;

        // 마지막 한 명을 팔면 CurrentData가 비워지므로 종류를 먼저 기억한다.
        CharacterData soldData = selectedSlot.CurrentData; // 판매할 캐릭터 종류 저장
        Vector3 sellPosition = selectedSlot.transform.position;
        SummonedCharacter character = selectedSlot.RemoveLastCharacter(); // 삭제된 캐릭터 반환
        if (character == null)
            return;

        int reward = character.SellCoin; 
        // Destroy는 프레임 끝에 실행되므로 먼저 꺼서 공격도 즉시 중단한다.
        character.gameObject.SetActive(false);
        Destroy(character.gameObject);
        FillMatchingSlots(soldData);// 
        if (soldData.Grade == CharacterGrade.Normal)
            AddCoin(reward, sellPosition);
        else if (soldData.Grade == CharacterGrade.Rare)
            draw.rouletteManager.AddToken(1, sellPosition);
        else if (soldData.Grade == CharacterGrade.Epic)
            draw.rouletteManager.AddToken(2, sellPosition);
        else if (soldData.Grade == CharacterGrade.Legendary)
            draw.rouletteManager.AddToken(4, sellPosition);
        NotifyCharactersChanged(); // UI 갱신
    }

    // 판매 후 같은 종류끼리 앞 슬롯부터 3명씩 채운다.
    private void FillMatchingSlots(CharacterData data)
    {
        if (data == null)
            return;

        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy) // 슬롯이 없거나 비활성화 -> 건너뛰기
                continue;
            SlotController target = SlotList[i].GetComponent<SlotController>();
            if (target == null || !target.isActiveAndEnabled || target.IsEmpty()) // 슬롯 컨트롤러 없거나 비활성화, 비어있으면 건너뛰기
                continue;
            if (!target.CanAdd(data)) // 슬롯이 같은 종류가 아니거나 꽉 찼으면 건너뛰기
                continue;

            // 현재 슬롯보다 뒤에 있는 같은 종류 슬롯에서 캐릭터를 가져온다.
            for (int j = i + 1; j < SlotList.Count; j++) // 현재 슬롯 뒤의 슬롯 쭉 확인
            {
                if (SlotList[j] == null || !SlotList[j].activeInHierarchy) 
                    continue;
                SlotController source = SlotList[j].GetComponent<SlotController>();
                if (source == null || source == target || !source.isActiveAndEnabled) 
                    continue;
                if (source.CurrentData != data)
                    continue;

                while (target.CanAdd(data) && !source.IsEmpty()) // 비어있지 않지만 꽉차지는 않았다면
                {
                    SummonedCharacter movingCharacter = source.RemoveLastCharacter(); // 마지막 캐릭터 삭제후 반환
                    if (movingCharacter == null)
                        break;

                    // 기존 개체를 옮기므로 생성, 판매 보상, 소환 비용 처리는 하지 않는다.
                    if (!target.TryAddCharacter(movingCharacter)) // 자리 옮기기
                    {
                        source.TryAddCharacter(movingCharacter); // 자리 옮기기 실패시 원상복구
                        break;
                    }
                }
                if (target.IsFull())
                    break;
            }
        }
    }

    public void AddCoin(int amount) 
        // 일반 적 처치시 1코인, 보스 처치시 50코인, 판매시 캐릭터에 따른 코인 지급
    {
        Coin += amount;
        if (draw != null)
            draw.RefreshButton(); // 버튼 갱신
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowCoinText(amount);
    }

    public void AddCoin(int amount, Vector3 position)
    {
        AddCoin(amount); // 지급과 재화 UI 표시는 한 번만 실행한다.
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowCoinText(amount, position);
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
        ShowSlotCanvas(); // 매 프레임 대신 캐릭터가 바뀌었을 때 선택 UI를 갱신한다.
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
        for (int i = 0; i < SlotList.Count; i++) // 슬롯 목록을 순회한다.
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy)
                continue;

            SlotController slot = SlotList[i].GetComponent<SlotController>();
            if (slot == null || !slot.isActiveAndEnabled)
                continue;
            if (!slot.HasPositions())
                continue;
            if (slot.IsEmpty()) // 빈슬롯 반환
                return slot;
        }
        return null;
    }

    // 같은 종류가 들어 있고 3명 미만인 슬롯을 먼저 찾는다.
    private SlotController FindMatchingSlot(CharacterData data)
    {
        for (int i = 0; i < SlotList.Count; i++)
        {
            if (SlotList[i] == null || !SlotList[i].activeInHierarchy) // 슬롯이 없거나 비활성화 -> 건너뛰기
                continue;

            SlotController slot = SlotList[i].GetComponent<SlotController>(); 
            if (slot == null || !slot.isActiveAndEnabled) // 슬롯 컨트롤러 없거나 비활성화 -> 건너뛰기
                continue;
            if (slot.IsEmpty()) // 비어있으면 같은 종류아님 -> 건너뛰기
                continue;
            if (slot.CanAdd(data)) // 건너뛰지 않은 경우 슬롯 반환
                return slot;
        }
        return null;
    }

    // 생성과 슬롯 등록을 마치면 true, 실패하면 false를 반환한다.
    public bool TrySummon(CharacterData data)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded) // 게임 끝인지 확인
            return false;

        // 코인이 현재 소환 비용보다 적으면 소환하지 않는다.
        if (Coin < SummonCost)
            return false;

        if (!TryPlaceCharacter(data))
            return false;

        // 일반 소환만 골드를 사용하고 다음 소환 비용을 올린다.
        Coin -= SummonCost;
        SummonCost = Mathf.Min(SummonCost + 10, MaxSummonCost);
        NotifyCharactersChanged();
        return true;
    }

    // 골드 결제 없이 배치만 한다. 호출한 쪽에서 재화 처리 후 UI를 갱신한다.
    public bool TryPlaceCharacter(CharacterData data)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return false;

        SlotController emptySlot = FindEmptySlot(); // 빈슬롯 저장해두기
        if (emptySlot == null)
            return false;
        if (data == null || data.Prefab == null)
            return false;

        SlotController targetSlot = FindMatchingSlot(data); // 같은 종류 찾기
        if (targetSlot == null) // 같은 종류 없으면 
        {
            targetSlot = emptySlot; // 빈 슬롯 등록
        }

        GameObject newCharacter = Instantiate(
            data.Prefab,
            targetSlot.Box_1.transform.position, // 위치 정하는게 아니라 일단 생성하는 것임
            data.Prefab.transform.rotation
        );
        SummonedCharacter character = newCharacter.GetComponent<SummonedCharacter>();
        character.Initialize(data); // 생성된 캐릭터가 데이터 들고 있게 하기 

        bool added = targetSlot.TryAddCharacter(character); // 슬롯에 생성된 캐릭터 등록하고 자리 배치
        if (!added) // 등록 실패시
        {
            // 생성만 되고 슬롯에 들어가지 못한 개체는 제거한다.
            Destroy(newCharacter);
            return false;
        }
        return true;
    }
}
