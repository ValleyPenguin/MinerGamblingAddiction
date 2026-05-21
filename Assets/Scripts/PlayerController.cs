using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2Int position;
    [SerializeField] private Vector2Int direction;
    [SerializeField] private GridSystem grid;
    private Vector2Int newPosition;
    
    
    void Start()
    {
        transform.position = grid.GridToWorld(position);
    }

    public void OnMove(InputValue value)
    {
        direction = Vector2Int.RoundToInt(value.Get<Vector2>());
        CalculateMovement();
        Debug.Log(direction);
    }

    private void CalculateMovement()
    {
        newPosition = position + direction;
        CheckValidMove();
        transform.position = grid.GridToWorld(position);
    }

    private void CheckValidMove()
    {
        //Checking for the boundary
        if (newPosition.x < 0) return;
        if (newPosition.y < 0) return;
        if (newPosition.x > grid.Columns - 1) return;
        if (newPosition.y > grid.Rows - 1) return;
        
        position = newPosition;
    }
}
