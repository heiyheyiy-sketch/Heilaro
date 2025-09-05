using UnityEngine;

public class UIPanelSwitcher : MonoBehaviour
{
    [SerializeField] GameObject explainPanel; // 설명창
    [SerializeField] GameObject detailPanel;  // 자세히보기창

    void Start()
    {
        ShowExplain(); // 시작 상태: 설명창 ON, 자세히보기 OFF
    }

    public void ShowDetail()
    {
        explainPanel.SetActive(false);
        detailPanel.SetActive(true);
    }

    public void ShowExplain()
    {
        detailPanel.SetActive(false);
        explainPanel.SetActive(true);
    }
}
