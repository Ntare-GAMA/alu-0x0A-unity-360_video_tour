using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSceneLoader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isLoading;

    public void LoadScene(string sceneName)
    {
        if (isLoading || string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        isLoading = true;
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;
            Color color = fadeImage.color;
            float duration = Mathf.Max(0f, fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
        isLoading = false;
    }
}
