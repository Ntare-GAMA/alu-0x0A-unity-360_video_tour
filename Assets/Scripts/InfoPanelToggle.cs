using UnityEngine;

public class InfoPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public void Toggle()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }
}
