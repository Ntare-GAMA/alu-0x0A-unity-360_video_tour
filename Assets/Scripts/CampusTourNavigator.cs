using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CampusTourNavigator : MonoBehaviour
{
    [SerializeField] private GameObject stairs;
    [SerializeField] private GameObject entrance;
    [SerializeField] private GameObject fabLab;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    void HideAll()
    {
        stairs.SetActive(false);
        entrance.SetActive(false);
        fabLab.SetActive(false);
    }

    public void GoToStairs() => StartTransition(stairs);
    public void GoToEntrance() => StartTransition(entrance);
    public void GoToFabLab() => StartTransition(fabLab);

    private void StartTransition(GameObject target)
    {
        if (!isTransitioning)
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

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(color.r, color.g, color.b, endAlpha);
    }
}
