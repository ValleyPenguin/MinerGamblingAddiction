using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BombTrap : MonoBehaviour
{
    private SpriteRenderer[] spriteRenderers;
    private float revealDuration = 0.75f;
    private bool triggered;

    private void Awake()
    {
        CacheRenderers();

        if (Application.isPlaying)
        {
            SetVisible(false);
        }
    }

    public void Configure(float revealDuration, bool visibleWhileEditing)
    {
        this.revealDuration = Mathf.Max(0f, revealDuration);
        CacheRenderers();

        SetVisible(!Application.isPlaying && visibleWhileEditing);
        if (Application.isPlaying)
        {
            SetVisible(false);
        }
    }

    public void Trigger()
    {
        if (triggered)
        {
            return;
        }

        triggered = true;
        SetVisible(true);
        StartCoroutine(ReloadSceneAfterReveal());
    }

    private IEnumerator ReloadSceneAfterReveal()
    {
        if (revealDuration > 0f)
        {
            yield return new WaitForSeconds(revealDuration);
        }

        ReloadCurrentScene();
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

    private void CacheRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void SetVisible(bool visible)
    {
        if (spriteRenderers == null)
        {
            return;
        }

        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            spriteRenderer.enabled = visible;
        }
    }
}
