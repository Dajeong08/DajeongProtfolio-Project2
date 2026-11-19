using UnityEngine;

// 도감과 상세 창 열기, 닫기
public class CharacterBookUI : MonoBehaviour
{
    public GameObject BookPanel; // 도감 전체 창
    private GameObject currentDetail; // 지금 열려 있는 상세 창

    // 상세 창을 닫고 도감 열기
    public void OpenBook()
    {
        CloseDetail();
        BookPanel.SetActive(true);
    }

    // 상세 창과 도감 닫기
    public void CloseBook()
    {
        CloseDetail();
        BookPanel.SetActive(false);
    }

    // 선택한 캐릭터 상세 창 열기
    public void OpenDetail(GameObject panel)
    {
        CloseDetail();
        currentDetail = panel;
        currentDetail.SetActive(true);
    }

    // 현재 상세 창 닫기
    public void CloseDetail()
    {
        if (currentDetail != null)
            currentDetail.SetActive(false);
        currentDetail = null;
    }
}
