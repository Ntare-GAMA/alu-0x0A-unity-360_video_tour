using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSceneLoader : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    public void LoadScene(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;
            Color color = fadeImage.color;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
    }
}
