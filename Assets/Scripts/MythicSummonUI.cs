using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
// 신화 한 종류의 레시피와 UI
public class MythicUIEntry
{
    public MythicRecipe Recipe; // 이 버튼의 조합 레시피
    public Button QuickButton; // 빠른 소환 버튼
    public Button SummonButton; // 조합 창의 소환 버튼
    public Image[] MaterialIcons; // 재료 보유 여부를 표시할 이미지
}

// 신화 재료와 소환 버튼 표시
public class MythicSummonUI : MonoBehaviour
{
    public MythicSummonManager summonManager; // 신화 조합 요청
    public GameObject QuickSummonRoot; // 빠른 소환 버튼 묶음
    public List<MythicUIEntry> MythicList = new List<MythicUIEntry>(); // 신화별 레시피와 UI 연결

    // 신화 소환 UI 초기 표시
    private void Start()
    {
        RefreshUI();
    }

    // 재료 보유 상태와 소환 버튼 갱신
    public void RefreshUI()
    {
        int visibleCount = 0;
        for (int i = 0; i < MythicList.Count; i++)
        {
            MythicUIEntry entry = MythicList[i];
            if (entry == null)
                continue;
            bool canSummon = summonManager != null && summonManager.CanSummon(entry.Recipe);
            if (entry.SummonButton != null)
            {
                entry.SummonButton.interactable = canSummon;
                Image buttonImage = entry.SummonButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    if (canSummon)
                        buttonImage.color = new Color(0.45f, 0.85f, 0.25f, 1f);
                    else
                        buttonImage.color = new Color(0.65f, 0.65f, 0.73f, 1f);
                }
            }
            if (entry.QuickButton != null)
            {
                entry.QuickButton.interactable = canSummon;
                entry.QuickButton.gameObject.SetActive(canSummon);
                if (canSummon)
                {
                    RectTransform rect = entry.QuickButton.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(0f, 88f - visibleCount * 88f);
                    visibleCount++;
                }
            }
            if (entry.Recipe == null || entry.Recipe.Materials == null || entry.MaterialIcons == null)
                continue;
            for (int j = 0; j < entry.MaterialIcons.Length; j++)
            {
                Image icon = entry.MaterialIcons[j];
                if (icon == null || j >= entry.Recipe.Materials.Count)
                    continue;
                MythicMaterial material = entry.Recipe.Materials[j];
                if (material == null)
                    continue;
                bool hasMaterial = summonManager != null && material.Character != null
                    && summonManager.CountMaterial(material.Character) >= material.Count;
                if (hasMaterial)
                    icon.color = Color.white;
                else
                    icon.color = new Color(1f, 1f, 1f, 0.25f);
            }
        }
        if (QuickSummonRoot != null)
            QuickSummonRoot.SetActive(visibleCount > 0);
    }

    // 선택한 레시피로 신화 소환 요청
    public void OnSummonClick(int index)
    {
        if (summonManager == null || index < 0 || index >= MythicList.Count
            || MythicList[index] == null)
            return;
        summonManager.TrySummon(MythicList[index].Recipe);
        RefreshUI();
    }
}
