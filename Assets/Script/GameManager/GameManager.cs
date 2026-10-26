using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    //Score Setting
    [Header("Socre Setting")]
    public float ballScore = 20f;
    public int[] targetScores;

   [HideInInspector] public float currentTargetScore;
   [HideInInspector] public float score = 0f;

    //Ball Setting
    [Header("Ball Setting")]
    public int ballsPerRound = 8;
    [HideInInspector] public int ballNumber;
    private int activeBalls;

    //Gold Setting
    public int gold = 5;

    //Turn Varible
    [HideInInspector] public int rounds;
    [HideInInspector] public int currentRound;

    //Checking Varible
    private bool allRoundsFinished;



    private void Awake()
    {
        instance = this;
        InitInAwake();
    }

    private void Update()
    {
    }

    public void AddScore(float multiplier)
    {
        score += ballScore * multiplier;
    }



    //Check methods
    private void CheckRound()
    {
        if (allRoundsFinished) return;
        //Check if the game has failed
        if (score < currentTargetScore)
        {
            Debug.Log("Game Failed");
            return;
        }
        //Check if the game has Finsih
        if (currentRound >= targetScores.Length)
        {
            if (score >= currentTargetScore)
            {
                allRoundsFinished = true;
                Debug.Log("Finish all round");
                return;
            }
        }

        gold += 4;
        UIManager.instance.OpenShop();
    }

    //Enter Next Round
    public void NextRound() //Button listening
    {
        if (!UIManager.instance.shop.activeSelf) return;
        //Reset when entering the next round
        currentRound++;
        currentTargetScore = targetScores[currentRound - 1];
        ballNumber = ballsPerRound;
        score = 0f;

        UIManager.instance.CloseShop();
    }

    //Ball-related methods 
    public bool TryUseBall()
    {
        if (ballNumber <= 0 || allRoundsFinished) return false;

        ballNumber--;
        activeBalls++;
        return true;
    }

    public void BallReturned()
    {
        activeBalls--;

        if (ballNumber == 0 && activeBalls == 0) //Check whether you can proceed to the next round only after discarding all balls
        {
            CheckRound();
        }
    }





    //Init Setting
    private void InitInAwake()
    {
        currentRound = 1;
        rounds = targetScores.Length;
        currentTargetScore = targetScores[0];
        ballNumber = ballsPerRound;
    }
}
