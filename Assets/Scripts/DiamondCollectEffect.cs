using System.Collections;
using UnityEngine;

public sealed class DiamondCollectEffect : MonoBehaviour
{
    private static readonly int[] AngleOffsets = { -24, -10, 7, 19, 33, 48 };

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float startRotation;
    private float endRotation;
    private float duration;
    private Color startColor;

    public static void SpawnBurst(
        Vector3 origin,
        Sprite sprite,
        int minPieces,
        int maxPieces,
        float travelDistance,
        float duration,
        int sortingOrder)
    {
        if (sprite == null)
        {
            return;
        }

        int lowerCount = Mathf.Max(1, minPieces);
        int upperCount = Mathf.Max(lowerCount, maxPieces);
        int pieceCount = Random.Range(lowerCount, upperCount + 1);

        for (int i = 0; i < pieceCount; i++)
        {
            float baseAngle = 360f * i / pieceCount;
            float angle = baseAngle + AngleOffsets[i % AngleOffsets.Length] + Random.Range(-6f, 6f);
            float distance = travelDistance * Random.Range(0.75f, 1.15f);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
            Vector3 endPosition = origin + (Vector3)(direction * distance);

            GameObject piece = new GameObject("Collected Diamond");
            piece.transform.position = origin;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            DiamondCollectEffect effect = piece.AddComponent<DiamondCollectEffect>();
            effect.Play(endPosition, duration, Random.Range(-55f, 55f));
        }
    }

    private void Play(Vector3 targetPosition, float effectDuration, float rotationAmount)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        endPosition = targetPosition;
        duration = Mathf.Max(0.01f, effectDuration);
        startRotation = transform.eulerAngles.z;
        endRotation = startRotation + rotationAmount;
        startColor = spriteRenderer.color;

        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, easedProgress);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startRotation, endRotation, easedProgress));

            Color color = startColor;
            color.a = Mathf.SmoothStep(1f, 0f, progress);
            spriteRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}
