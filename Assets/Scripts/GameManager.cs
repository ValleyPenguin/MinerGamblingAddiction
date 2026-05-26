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

    public int CashOut()
    {
        activeScore += tempScore;
        tempScore = 0;
        Debug.Log($"You scored {activeScore} diamonds");
        return activeScore;
    }
}
