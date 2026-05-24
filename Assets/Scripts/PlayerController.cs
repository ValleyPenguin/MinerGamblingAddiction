using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2Int position;
    [SerializeField] private Vector2Int direction;
    [SerializeField] private GridSystem grid;
    private Vector2Int newPosition;
    private bool inputLocked;
    
    void Start()
    {
        transform.position = grid.GridToWorld(position);
        grid.SetPlayerCell(position, true);
    }

    public void OnMove(InputValue value)
    {
        if (inputLocked)
        {
            return;
        }

        direction = Vector2Int.RoundToInt(value.Get<Vector2>());
        if (direction == Vector2Int.zero)
        {
            return;
        }

        CalculateMovement();
        Debug.Log(direction);
    }

    private void CalculateMovement()
    {
        newPosition = position + direction;
        CheckMove();
        transform.position = grid.GridToWorld(position);
    }

    private void CheckMove()
    {
        //Checking for the boundary
        if (newPosition.x < 0) return;
        if (newPosition.y < 0) return;
        if (newPosition.x > grid.Columns - 1) return;
        if (newPosition.y > grid.Rows - 1) return;
        
        //Check if we've run into anything
        
        
        position = newPosition;
        grid.SetPlayerCell(position);

        if (grid.TryTriggerBombAt(position))
        {
            inputLocked = true;
        }
    }
}
