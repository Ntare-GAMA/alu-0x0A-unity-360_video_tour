using UnityEngine;

[DisallowMultipleComponent]
public class InfoPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public bool IsVisible => panel != null && panel.activeSelf;

    public void Toggle()
    {
        if (panel != null)
        {
            SetVisible(!panel.activeSelf);
        }
    }

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    private void SetVisible(bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }
}
