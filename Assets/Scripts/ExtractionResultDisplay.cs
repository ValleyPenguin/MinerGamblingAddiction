using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ExtractionResultDisplay : MonoBehaviour
{
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float restartDelay = 3f;

    private Coroutine restartCoroutine;

    private void Awake()
    {
        if (messageRoot != null)
        {
            messageRoot.SetActive(false);
        }
    }

    public void Show(int diamonds)
    {
        if (messageText == null)
        {
            CreateFallbackMessageUI();
        }

        if (messageRoot != null)
        {
            messageRoot.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = GetMessage(diamonds, Mathf.CeilToInt(restartDelay));
        }

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
        }

        restartCoroutine = StartCoroutine(RestartAfterCountdown(diamonds));
    }

    private void CreateFallbackMessageUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Extraction Message Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject messageObject = new GameObject("Extraction Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(canvas.transform, false);
        messageRoot = messageObject;
        messageText = messageObject.GetComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 54f;
        messageText.color = Color.white;

        RectTransform rectTransform = messageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(1200f, 180f);
    }

    private IEnumerator RestartAfterCountdown(int diamonds)
    {
        int secondsRemaining = Mathf.CeilToInt(restartDelay);
        while (secondsRemaining > 0)
        {
            if (messageText != null)
            {
                messageText.text = GetMessage(diamonds, secondsRemaining);
            }

            yield return new WaitForSeconds(1f);
            secondsRemaining--;
        }

        ReloadCurrentScene();
    }

    private string GetMessage(int diamonds, int secondsRemaining)
    {
        return $"You successfully escaped with {diamonds} diamonds\nRestarting in {secondsRemaining}";
    }

    private void ReloadCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

#if UNITY_EDITOR
        if (activeScene.buildIndex < 0 && !string.IsNullOrEmpty(activeScene.path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                activeScene.path,
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif

        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }
}
