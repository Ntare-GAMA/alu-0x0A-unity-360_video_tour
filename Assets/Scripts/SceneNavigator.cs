using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneNavigator : MonoBehaviour
{
    [SerializeField] private GameObject livingRoom;
    [SerializeField] private GameObject cantina;
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject mezzanine;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    void HideAll()
    {
        livingRoom.SetActive(false);
        cantina.SetActive(false);
        cube.SetActive(false);
        mezzanine.SetActive(false);
    }

    public void GoToLivingRoom() => StartTransition(livingRoom);
    public void GoToCantina() => StartTransition(cantina);
    public void GoToCube() => StartTransition(cube);
    public void GoToMezzanine() => StartTransition(mezzanine);

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