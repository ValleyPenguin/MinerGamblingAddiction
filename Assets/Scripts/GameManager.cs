using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text currentScoreText;
    private int tempScore;
    private int activeScore;
    
    public void MineOre()
    {
        tempScore++;
        currentScoreText.text = $"Current Score: {tempScore}";
    }

    public int CashOut()
    {
        activeScore += tempScore;
        tempScore = 0;
        Debug.Log($"You scored {activeScore} diamonds");
        return activeScore;
    }
}
