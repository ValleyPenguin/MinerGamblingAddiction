using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class GridSystem : MonoBehaviour
{
    private const string GeneratedRootName = "Generated Grid Preview";

    [Header("Canvas")]
    [SerializeField] private int canvasWidth = 640;
    [SerializeField] private int canvasHeight = 360;
    [SerializeField] private int pixelsPerUnit = 32;
    [SerializeField] private bool frameCamera = true;
    [SerializeField] private Camera cameraToFrame;

    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 20;
    [SerializeField, Min(1)] private int rows = 11;
    [SerializeField, Min(1)] private int cellPixelSize = 32;
    [SerializeField] private bool centerGridOnThisObject = true;
    [SerializeField] private Color gridLineColor = new Color(0.18f, 0.22f, 0.27f, 1f);
    [SerializeField, Min(1f)] private float gridLinePixels = 1f;

    [Header("Ores")]
    [SerializeField] private GameObject orePrefab;
    [SerializeField] private bool fitOrePrefabToCell = true;
    [SerializeField, Range(0f, 1f)] private float oreSpawnChance = 0.18f;
    [SerializeField, Min(0)] private int maximumOres = 28;
    [SerializeField] private int randomSeed = 12345;
    [SerializeField] private Color fallbackOreColor = new Color(0.9f, 0.55f, 0.12f, 1f);

    [Header("Bombs")]
    [SerializeField, Min(0)] private int bombCount = 5;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private bool fitBombPrefabToCell = true;
    [SerializeField] private bool previewBombsInEditor = true;
    [SerializeField, Min(0f)] private float bombRevealDuration = 0.75f;
    [SerializeField] private Color fallbackBombColor = new Color(0.95f, 0.12f, 0.1f, 1f);

    private static Material gridMaterial;
    private static Sprite fallbackOreSprite;
    private static Sprite fallbackBombSprite;
    private readonly Dictionary<Vector2Int, BombTrap> bombsByCell = new Dictionary<Vector2Int, BombTrap>();

    public int Columns => columns;
    public int Rows => rows;
    public int CellPixelSize => cellPixelSize;
    public float CellWorldSize => (float)cellPixelSize / pixelsPerUnit;

    private void Awake()
    {
        Regenerate();
    }

    private void OnEnable()
    {
        Regenerate();
    }

    private void OnValidate()
    {
        canvasWidth = Mathf.Max(1, canvasWidth);
        canvasHeight = Mathf.Max(1, canvasHeight);
        pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        cellPixelSize = Mathf.Max(1, cellPixelSize);
        gridLinePixels = Mathf.Max(1f, gridLinePixels);
        maximumOres = Mathf.Max(0, maximumOres);
        bombCount = Mathf.Max(0, bombCount);
        bombRevealDuration = Mathf.Max(0f, bombRevealDuration);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall -= RegenerateAfterValidation;
            EditorApplication.delayCall += RegenerateAfterValidation;
            return;
        }
#endif

        Regenerate();
    }

    [ContextMenu("Regenerate Grid")]
    public void Regenerate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ClearPreview();
        FrameCamera();

        Transform previewRoot = CreatePreviewRoot();
        DrawGrid(previewRoot);
        HashSet<Vector2Int> occupiedCells = SpawnOres(previewRoot);
        SpawnBombs(previewRoot, occupiedCells);
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        Vector2 origin = GridOrigin();
        float cellSize = CellWorldSize;

        return new Vector3(
            origin.x + (cell.x + 0.5f) * cellSize,
            origin.y + (cell.y + 0.5f) * cellSize,
            transform.position.z);
    }

    public bool TryWorldToGrid(Vector3 worldPosition, out Vector2Int cell)
    {
        Vector2 origin = GridOrigin();
        float cellSize = CellWorldSize;

        cell = new Vector2Int(
            Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize),
            Mathf.FloorToInt((worldPosition.y - origin.y) / cellSize));

        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    public bool TryTriggerBombAt(Vector2Int cell)
    {
        if (bombsByCell.TryGetValue(cell, out BombTrap bomb))
        {
            bomb.Trigger();
            return true;
        }

        return false;
    }

    private void DrawGrid(Transform parent)
    {
        Vector2 origin = GridOrigin();
        float cellSize = CellWorldSize;
        float width = columns * cellSize;
        float height = rows * cellSize;

        for (int x = 0; x <= columns; x++)
        {
            float worldX = origin.x + x * cellSize;
            AddLine(parent, $"Column {x}", new Vector2(worldX, origin.y), new Vector2(worldX, origin.y + height));
        }

        for (int y = 0; y <= rows; y++)
        {
            float worldY = origin.y + y * cellSize;
            AddLine(parent, $"Row {y}", new Vector2(origin.x, worldY), new Vector2(origin.x + width, worldY));
        }
    }

    private HashSet<Vector2Int> SpawnOres(Transform parent)
    {
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        if (oreSpawnChance <= 0f || maximumOres == 0)
        {
            return occupiedCells;
        }

        List<Vector2Int> cells = AllCellsShuffled();
        int oresCreated = 0;
        System.Random random = new System.Random(randomSeed + 1);

        foreach (Vector2Int cell in cells)
        {
            if (oresCreated >= maximumOres)
            {
                return occupiedCells;
            }

            if (random.NextDouble() <= oreSpawnChance)
            {
                AddOre(parent, cell, oresCreated);
                occupiedCells.Add(cell);
                oresCreated++;
            }
        }

        return occupiedCells;
    }

    private void SpawnBombs(Transform parent, HashSet<Vector2Int> occupiedCells)
    {
        bombsByCell.Clear();
        if (bombCount == 0)
        {
            return;
        }

        List<Vector2Int> cells = AllCellsShuffled(randomSeed + 2);
        int bombsCreated = 0;

        foreach (Vector2Int cell in cells)
        {
            if (bombsCreated >= bombCount)
            {
                return;
            }

            if (occupiedCells.Contains(cell))
            {
                continue;
            }

            AddBomb(parent, cell, bombsCreated);
            bombsCreated++;
        }
    }

    private List<Vector2Int> AllCellsShuffled()
    {
        return AllCellsShuffled(randomSeed);
    }

    private List<Vector2Int> AllCellsShuffled(int seed)
    {
        List<Vector2Int> cells = new List<Vector2Int>(columns * rows);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        System.Random random = new System.Random(seed);
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            Vector2Int temp = cells[i];
            cells[i] = cells[swapIndex];
            cells[swapIndex] = temp;
        }

        return cells;
    }

    private void AddOre(Transform parent, Vector2Int cell, int oreIndex)
    {
        GameObject ore = orePrefab != null ? CreateOreFromPrefab(parent) : CreateFallbackOre(parent);
        SetPreviewHideFlags(ore);
        ore.name = $"Ore {oreIndex:00}";
        ore.transform.position = GridToWorld(cell);

        if (orePrefab == null || fitOrePrefabToCell)
        {
            FitToCell(ore);
        }
    }

    private void AddBomb(Transform parent, Vector2Int cell, int bombIndex)
    {
        GameObject bomb = bombPrefab != null ? CreateBombFromPrefab(parent) : CreateFallbackBomb(parent);
        SetPreviewHideFlags(bomb);
        bomb.name = $"Bomb {bombIndex:00}";
        bomb.transform.position = GridToWorld(cell);

        if (bombPrefab == null || fitBombPrefabToCell)
        {
            FitToCell(bomb);
        }

        BombTrap bombTrap = bomb.GetComponent<BombTrap>();
        if (bombTrap == null)
        {
            bombTrap = bomb.AddComponent<BombTrap>();
        }

        bombTrap.Configure(bombRevealDuration, previewBombsInEditor);
        bombsByCell[cell] = bombTrap;
    }

    private GameObject CreateOreFromPrefab(Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(orePrefab, parent);
        }
#endif

        return Instantiate(orePrefab, parent);
    }

    private GameObject CreateBombFromPrefab(Transform parent)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(bombPrefab, parent);
        }
#endif

        return Instantiate(bombPrefab, parent);
    }

    private GameObject CreateFallbackOre(Transform parent)
    {
        GameObject ore = new GameObject("Ore");
        SetPreviewHideFlags(ore);
        ore.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = ore.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetFallbackOreSprite();
        spriteRenderer.color = fallbackOreColor;
        spriteRenderer.sortingOrder = 1;

        return ore;
    }

    private GameObject CreateFallbackBomb(Transform parent)
    {
        GameObject bomb = new GameObject("Bomb");
        SetPreviewHideFlags(bomb);
        bomb.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = bomb.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetFallbackBombSprite();
        spriteRenderer.color = fallbackBombColor;
        spriteRenderer.sortingOrder = 2;

        return bomb;
    }

    private void FitToCell(GameObject target)
    {
        SpriteRenderer spriteRenderer = target.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            target.transform.localScale = Vector3.one;
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        target.transform.localScale = Vector3.one * (CellWorldSize / largestSide);
    }

    private void AddLine(Transform parent, string lineName, Vector2 start, Vector2 end)
    {
        GameObject lineObject = new GameObject(lineName);
        SetPreviewHideFlags(lineObject);
        lineObject.transform.SetParent(parent, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = gridLinePixels / pixelsPerUnit;
        line.endWidth = gridLinePixels / pixelsPerUnit;
        line.startColor = gridLineColor;
        line.endColor = gridLineColor;
        line.material = GetGridMaterial();
        line.sortingOrder = 0;
    }

    private void FrameCamera()
    {
        if (!frameCamera)
        {
            return;
        }

        Camera targetCamera = cameraToFrame != null ? cameraToFrame : Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        Vector2 gridCenter = GridOrigin() + new Vector2(columns * CellWorldSize, rows * CellWorldSize) * 0.5f;
        targetCamera.orthographic = true;
        targetCamera.orthographicSize = canvasHeight / (pixelsPerUnit * 2f);
        targetCamera.transform.position = new Vector3(gridCenter.x, gridCenter.y, targetCamera.transform.position.z);
    }

    private Vector2 GridOrigin()
    {
        if (!centerGridOnThisObject)
        {
            return transform.position;
        }

        return new Vector2(
            transform.position.x - columns * CellWorldSize * 0.5f,
            transform.position.y - rows * CellWorldSize * 0.5f);
    }

    private Transform CreatePreviewRoot()
    {
        GameObject previewRoot = new GameObject(GeneratedRootName);
        SetPreviewHideFlags(previewRoot);
        previewRoot.transform.SetParent(transform, false);

        return previewRoot.transform;
    }

    private static void SetPreviewHideFlags(GameObject gameObject)
    {
        if (Application.isPlaying)
        {
            return;
        }

        gameObject.hideFlags = HideFlags.DontSaveInEditor;
        foreach (Transform child in gameObject.transform)
        {
            SetPreviewHideFlags(child.gameObject);
        }
    }

    private void ClearPreview()
    {
        bombsByCell.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName || child.name == "Grid Lines" || child.name == "Ores")
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }
    }

    private void DestroyGeneratedObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private static Material GetGridMaterial()
    {
        if (gridMaterial != null)
        {
            return gridMaterial;
        }

        gridMaterial = new Material(Shader.Find("Sprites/Default"))
        {
            name = "Generated Grid Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        return gridMaterial;
    }

    private static Sprite GetFallbackOreSprite()
    {
        if (fallbackOreSprite != null)
        {
            return fallbackOreSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Generated Ore Texture",
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackOreSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackOreSprite.name = "Generated Ore Sprite";
        fallbackOreSprite.hideFlags = HideFlags.HideAndDontSave;

        return fallbackOreSprite;
    }

    private static Sprite GetFallbackBombSprite()
    {
        if (fallbackBombSprite != null)
        {
            return fallbackBombSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Generated Bomb Texture",
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackBombSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackBombSprite.name = "Generated Bomb Sprite";
        fallbackBombSprite.hideFlags = HideFlags.HideAndDontSave;

        return fallbackBombSprite;
    }

#if UNITY_EDITOR
    private void RegenerateAfterValidation()
    {
        if (this != null && isActiveAndEnabled)
        {
            Regenerate();
        }
    }
#endif
}
