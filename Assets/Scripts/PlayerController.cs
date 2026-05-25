using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Vector2Int position;
    [SerializeField] private Vector2Int direction;
    [SerializeField] private float waitTime;
    [SerializeField] private GridSystem grid;
    [SerializeField] private GameManager gm;
    private Vector2Int newPosition;
    private bool inputLocked;
    private bool timerLock;
    
    void Start()
    {
        transform.position = grid.GridToWorld(position);
        grid.SetPlayerCell(position, true);
    }

    public void OnMove(InputValue value)
    {
        if (inputLocked || timerLock)
        {
            return;
        }

        direction = Vector2Int.RoundToInt(value.Get<Vector2>());
        if (direction == Vector2Int.zero)
        {
            return;
        }

        CalculateMovement();
        StartCoroutine(walkTimer());
    }

    public void OnCashOut(InputValue value)
    {
        gm.CashOut();
        inputLocked = true;
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

        if (grid.OccupiedCells.Contains(position))
        {
            grid.DestroyOre(position);
            gm.MineOre();
        }

        if (grid.TryTriggerBombAt(position))
        {
            inputLocked = true;
        }
    }

    private IEnumerator walkTimer()
    {
        timerLock = true;
        yield return new WaitForSeconds(waitTime);
        timerLock = false;
    }
}
