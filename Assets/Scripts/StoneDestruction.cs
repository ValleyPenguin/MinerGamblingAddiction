using UnityEngine;

public class StoneDestruction : MonoBehaviour
{
    [SerializeField] private GridSystem grid;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerController player;
    private Vector2Int gridPosition;

    private void Start()
    {
        if(grid == null)
            grid = GameObject.FindGameObjectWithTag("Grid").GetComponent<GridSystem>();
        
        grid.TryWorldToGrid(transform.position, out gridPosition);
        
        if(player == null)
            player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (player.Position == gridPosition)
            spriteRenderer.enabled = false;
    }
}
