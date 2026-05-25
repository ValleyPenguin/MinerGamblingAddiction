using UnityEngine;

public class GameManager : MonoBehaviour
{
    private int tempScore;
    private int activeScore;
    
    public void MineOre()
    {
        tempScore++;
        Debug.Log(tempScore);
    }

    public void CashOut()
    {
        activeScore += tempScore;
        Debug.Log($"You scored {activeScore} diamonds");
    }
}
