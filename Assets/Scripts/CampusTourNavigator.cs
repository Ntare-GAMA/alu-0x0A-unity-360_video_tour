using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CampusTourNavigator : MonoBehaviour
{
    [SerializeField] private GameObject stairs;
    [SerializeField] private GameObject entrance;
    [SerializeField] private GameObject fabLab;

    [SerializeField] private Image fadeImage;
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    private void HideAll()
    {
        SetActive(stairs, false);
        SetActive(entrance, false);
        SetActive(fabLab, false);
    }

    public void GoToStairs() => StartTransition(stairs);
    public void GoToEntrance() => StartTransition(entrance);
    public void GoToFabLab() => StartTransition(fabLab);

    private void StartTransition(GameObject target)
    {
        if (!isTransitioning && target != null)
        {
            StartCoroutine(FadeTransition(target));
        }
    }

    private IEnumerator FadeTransition(GameObject target)
    {
        isTransitioning = true;
        yield return StartCoroutine(Fade(0f, 1f));
        HideAll();
        target.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f));
        isTransitioning = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        float elapsed = 0f;
        Color color = fadeImage.color;

        float duration = Mathf.Max(0f, fadeDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, endAlpha);
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }
}
