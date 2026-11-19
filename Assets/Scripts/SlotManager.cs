using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// 전체 슬롯의 소환, 판매, 합성 관리
public class SlotManager : MonoBehaviour
{
    public List<GameObject> SlotList = new List<GameObject>(); // 검색 순서대로 등록한 전체 슬롯

    public int Coin = 100; // 이번 전투 코인
    public int SummonCost = 20; // 다음 소환 비용
    private const int MaxSummonCost = 60; // 소환 비용 상한
    public Canvas SlotCanvas; // 선택한 슬롯의 버튼 UI
    public GameObject MergeButton; // 합성 버튼
    public GameObject SellButton; // 판매 버튼
    public Vector3 SlotCanvasOffset; // 슬롯 기준 UI 표시 위치
    public MythicSummonUI mythicUI; // 신화 소환 UI 갱신

    private Draw draw; // 확률 추첨과 소환 UI
    private SlotController selectedSlot; // 현재 선택한 슬롯

    // 소환 코드 연결, 슬롯 입력 등록, 선택 UI 초기화
    private void Awake()
    {
        draw = FindFirstObjectByType<Draw>(FindObjectsInactive.Include);
        for (int i = 0; i < SlotList.Count; i++)
        {
            SlotDragHandler handler = SlotList[i].GetComponent<SlotDragHandler>();
            if (handler == null)
                handler = SlotList[i].AddComponent<SlotDragHandler>();
            // this는 현재 SlotManager
            handler.slotManager = this;
        }
        if (SlotCanvas != null)
        {
            SlotCanvas.gameObject.SetActive(false); // 슬롯 선택 UI숨기기
        }
    }

    // 선택된 슬롯과 범위 표시 정리
    private void OnDisable()
    {
        HideSelectedRange();
    }

    // 빈 배경을 누르면 슬롯 선택 해제
    private void Update()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return; // 마우스 왼쪽 버튼을 눌렀을때만 처리

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        HideSelectedRange();
    }

    // 슬롯 선택과 판매, 합성 UI 표시
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

    // 두 슬롯의 캐릭터를 이동 또는 교환
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

    // 선택 UI와 범위 표시 숨기기
    private void HideSelectedRange()
    {
        if (selectedSlot != null)
            selectedSlot.HideAttackRange();
        selectedSlot = null;
        if (SlotCanvas != null)
            SlotCanvas.gameObject.SetActive(false);
    }

    // 선택한 슬롯에 판매, 합성 버튼 표시
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

    // 같은 캐릭터 3명이고 전설 미만인지 확인
    private bool CanMergeSelectedSlot()
    {
        return selectedSlot != null && selectedSlot.isActiveAndEnabled // 선택한 슬롯 활성 여부
            && selectedSlot.IsFull() && selectedSlot.CurrentData != null // 꽉 찼는지 && 데이터 확인
            && selectedSlot.CurrentData.Grade < CharacterGrade.Legendary; // 전설 미만 (일반·희귀·에픽)
    }

    // 3명을 소비하고 다음 등급 캐릭터 생성
    public void OnMergeClick()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded)
            return;
        if (!CanMergeSelectedSlot() || draw == null)
            return;

        SlotController slot = selectedSlot;
        if (!slot.HasPositions())
            return;

        // enum이 일반, 희귀, 에픽, 전설 순서라 +1이 다음 등급
        CharacterGrade nextGrade = slot.CurrentData.Grade + 1; // 다음 등급
        CharacterData result = draw.PickFromList(nextGrade); // 랜덤 캐릭터 반환

        SlotController targetSlot = FindMatchingSlot(result); // 랜덤 생성될 캐릭터와 같은 슬롯 찾기
        if (targetSlot == null)
            targetSlot = slot;

        GameObject resultObject = Instantiate(result.Prefab,
            targetSlot.Box_1.transform.position, result.Prefab.transform.rotation);
        SummonedCharacter resultCharacter = resultObject.GetComponent<SummonedCharacter>();
        resultCharacter.Initialize(result);

        // 결과 등록 실패 시 복구할 재료 보관
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

        AchievementManager.instance.AddProgress(AchievementType.Merge, 1);
        NotifyCharactersChanged(); // UI 갱신
    }

    // 캐릭터 1명 판매 후 같은 종류 재정렬
    public void OnSellClick()
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded) // 게임 끝인지 확인
            return;

        if (selectedSlot == null || !selectedSlot.isActiveAndEnabled) // 슬롯 없거나 비활성화 확인
            return;
        if (selectedSlot.IsEmpty() || selectedSlot.CurrentData.Grade == CharacterGrade.Mythic)
            return;

        // 마지막 캐릭터를 빼면 슬롯 데이터가 비워지므로 먼저 보관
        CharacterData soldData = selectedSlot.CurrentData; // 판매할 캐릭터 종류 저장
        Vector3 sellPosition = selectedSlot.transform.position;
        SummonedCharacter character = selectedSlot.RemoveLastCharacter(); // 삭제된 캐릭터 반환
        if (character == null)
            return;

        int reward = character.SellCoin; 
        // Destroy는 프레임 끝에 실행되므로 먼저 꺼서 공격 중단
        character.gameObject.SetActive(false);
        Destroy(character.gameObject);
        FillMatchingSlots(soldData);
        if (soldData.Grade == CharacterGrade.Normal)
        {
            AddCoin(reward, sellPosition);
            AchievementManager.instance.AddProgress(AchievementType.SellNormal, 1);
        }
        else if (soldData.Grade == CharacterGrade.Rare)
            draw.rouletteManager.AddToken(1, sellPosition);
        else if (soldData.Grade == CharacterGrade.Epic)
            draw.rouletteManager.AddToken(2, sellPosition);
        else if (soldData.Grade == CharacterGrade.Legendary)
            draw.rouletteManager.AddToken(4, sellPosition);
        NotifyCharactersChanged(); // UI 갱신
    }

    // 같은 종류의 남은 캐릭터를 앞 슬롯부터 채우기
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

    // 전투 코인 증감 후 UI와 도전과제 갱신
    public void AddCoin(int amount, bool countAchievement = true)
    {
        Coin += amount;
        if (amount > 0 && countAchievement)
            AchievementManager.instance.AddProgress(AchievementType.CollectCoin, amount);
        if (draw != null)
            draw.RefreshButton(); // 버튼 갱신
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowCoinText(amount);
    }

    // 전투 코인 증감 후 UI와 도전과제 갱신
    public void AddCoin(int amount, Vector3 position)
    {
        AddCoin(amount); // 재화 지급과 UI 갱신
        if (amount > 0 && FloatingTextManager.instance != null)
            FloatingTextManager.instance.ShowCoinText(amount, position);
    }

    // 전투 종료 후 선택 UI 정리
    public void CloseBattleControls()
    {
        UpGreadManager.instance.CloseUpGreadPanel();
        HideSelectedRange();
        if (draw != null)
            draw.RefreshButton();
        if (mythicUI != null)
            mythicUI.RefreshUI();
    }

    // 소환, 판매, 합성 후 관련 UI 갱신
    public void NotifyCharactersChanged()
    {
        ShowSlotCanvas(); // 선택 UI 갱신
        if (draw != null)
            draw.RefreshButton();
        if (mythicUI != null)
            mythicUI.RefreshUI();
    }

    // 신화 재료 슬롯을 정리하고 UI 갱신
    public void FinishMythicSummon(MythicRecipe recipe)
    {
        HideSelectedRange();
        for (int i = 0; i < recipe.Materials.Count; i++)
            FillMatchingSlots(recipe.Materials[i].Character);
        NotifyCharactersChanged();
    }

    // 캐릭터를 배치할 수 있는 빈 슬롯 찾기
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

    // 같은 종류가 있고 자리가 남은 슬롯 찾기
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

    // 코인을 지불하고 캐릭터 소환
    public bool TrySummon(CharacterData data)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsGameEnded) // 게임 끝인지 확인
            return false;

        if (Coin < SummonCost)
            return false;

        if (!TryPlaceCharacter(data))
            return false;

        Coin -= SummonCost;
        SummonCost = Mathf.Min(SummonCost + 10, MaxSummonCost);
        NotifyCharactersChanged();
        return true;
    }

    // 비용 차감 없이 캐릭터 생성과 배치
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
            Destroy(newCharacter);
            return false;
        }
        return true;
    }
}
