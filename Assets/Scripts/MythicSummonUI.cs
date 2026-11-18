using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// StageCollection처럼 Inspector에서 신화 한 종류의 UI를 묶어 연결한다.
[Serializable]
public class MythicUIEntry
{
    public MythicRecipe Recipe;
    public Button QuickButton;
    public Button SummonButton;
    public Image[] MaterialIcons;
}

public class MythicSummonUI : MonoBehaviour
{
    public MythicSummonManager summonManager;
    public GameObject QuickSummonRoot;
    public List<MythicUIEntry> MythicList = new List<MythicUIEntry>();

    private void Start()
    {
        RefreshUI();
    }

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
                    // 숨겨진 버튼의 빈 공간 없이 위에서부터 배치한다.
                    RectTransform rect = entry.QuickButton.GetComponent<RectTransform>();
                    // visibleCount는 표시된 버튼만 센다. 목록 중간 버튼이 숨겨져도 빈 줄이 생기지 않는다.
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
                    // 마지막 값은 투명도. 없는 재료도 이미지는 남기고 흐리게 보여 준다.
                    icon.color = new Color(1f, 1f, 1f, 0.25f);
            }
        }
        if (QuickSummonRoot != null)
            QuickSummonRoot.SetActive(visibleCount > 0);
    }

    // 빠른 소환과 도감 소환 버튼 모두 같은 목록 번호를 전달한다.
    public void OnSummonClick(int index)
    {
        if (summonManager == null || index < 0 || index >= MythicList.Count
            || MythicList[index] == null)
            return;
        summonManager.TrySummon(MythicList[index].Recipe);
        RefreshUI();
    }
}
