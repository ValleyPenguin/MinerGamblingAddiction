using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class BombProximityVignette : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float effectDistance = 3.5f;
    [SerializeField, Min(0f)] private float fullStrengthDistance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.28f;
    [SerializeField, Min(0f)] private float fadeSpeed = 4f;
    [SerializeField] private Color vignetteColor = new Color(0.9f, 0.03f, 0.01f);
    [SerializeField, Range(0f, 1f)] private float smoothness = 0.55f;

    private Volume volume;
    private Vignette vignette;
    private BombTrap[] bombs = System.Array.Empty<BombTrap>();
    private float currentIntensity;

    private void Awake()
    {
        EnsurePostProcessingEnabled();
        CreateRuntimeVignette();
        RefreshBombs();
    }

    private void OnEnable()
    {
        RefreshBombs();
    }

    private void Update()
    {
        if (vignette == null)
        {
            return;
        }

        if (bombs.Length == 0 || HasMissingBombReference())
        {
            RefreshBombs();
        }

        float targetIntensity = CalculateTargetIntensity();
        currentIntensity = fadeSpeed <= 0f
            ? targetIntensity
            : Mathf.MoveTowards(currentIntensity, targetIntensity, fadeSpeed * Time.deltaTime);

        vignette.intensity.value = currentIntensity;
    }

    private void OnDisable()
    {
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }

    private void CreateRuntimeVignette()
    {
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        vignette = volume.profile.Add<Vignette>(true);
        vignette.color.Override(vignetteColor);
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0f);
        vignette.smoothness.Override(smoothness);
        vignette.rounded.Override(false);
    }

    private void EnsurePostProcessingEnabled()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
        }
    }

    private void RefreshBombs()
    {
        bombs = FindObjectsByType<BombTrap>(FindObjectsSortMode.None);
    }

    private bool HasMissingBombReference()
    {
        for (int i = 0; i < bombs.Length; i++)
        {
            if (bombs[i] == null)
            {
                return true;
            }
        }

        return false;
    }

    private float CalculateTargetIntensity()
    {
        float nearestDistance = float.PositiveInfinity;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < bombs.Length; i++)
        {
            BombTrap bomb = bombs[i];
            if (bomb == null)
            {
                continue;
            }

            float distance = Vector2.Distance(playerPosition, bomb.transform.position);
            nearestDistance = Mathf.Min(nearestDistance, distance);
        }

        if (float.IsPositiveInfinity(nearestDistance))
        {
            return 0f;
        }

        float proximity = Mathf.InverseLerp(effectDistance, fullStrengthDistance, nearestDistance);
        proximity = Mathf.SmoothStep(0f, 1f, proximity);
        return proximity * maxIntensity;
    }
}
