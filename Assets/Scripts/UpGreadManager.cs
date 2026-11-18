using UnityEngine;

public class UpGreadManager : MonoBehaviour
{
    public GameObject UpGreadPanel;


    private void Start()
    {
        UpGreadPanel.SetActive(false);
    }
    public void OpenUpGreadPanel()
    {
        UpGreadPanel.SetActive(true);
    }
    public void CloseUpGreadPanel()
    {
        UpGreadPanel.SetActive(false);
    }
}
