using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    //Score Setting
    [Header("Socre Setting")]
    public float pinballScore = 20f;
    public int[] targetScores;

    [HideInInspector] public float currentTargetScore;
    [HideInInspector] public float score = 0f;

    //Pinball Setting
    [Header("Pinball Setting")]
    public SpawnPinballPoint spawnPinballPoint;
    public int pinballsPerRound = 8;
    [HideInInspector] public int pinballNumber;
    [HideInInspector] public int activePinballs;

    //Throw Chance
    public int throwChancesPerRound = 2;
    [HideInInspector] public int throwChance;
    private bool hasStartedThrow;
    public int previousRoundUnusedThrows;



    //Gold Setting
    public int gold = 5;

    //Turn Varible
    [HideInInspector] public int rounds;
    [HideInInspector] public int currentRound;

    //Checking Varible
    //Effect Checking
    public bool firstPinballScored;




    //Round Checking
    private bool allRoundsFinished;


    //Event
    public event System.Action<ScoreSlot> PinballEnteredSlot;
    public event System.Action<Pinball, Collider2D> PinballHitPeg;
    public event ScoreCalculationInScoreSlotHandler ScoreCalculationInScoreSlot; 
    public event ScoreCalculationHandler ScoreCalculating; //Triggered upon scoring
    public event System.Action RoundStarted;
    public event System.Action RoundEnded;
    public event System.Action PinballSpawned;
    public event GoldCalculationHandler GoldCalculating;
    public event TargetScoreCalculationHandler TargetScoreCalculating;
    public delegate void ScoreCalculationInScoreSlotHandler(ScoreSlot slot, ref float multiplier);
    public delegate void ScoreCalculationHandler(ref float amount);
    public delegate void GoldCalculationHandler(ref int amount);
    public delegate void TargetScoreCalculationHandler(ref float targetScore);




    private void Awake()
    {
        instance = this;
        InitInAwake();
    }



    public void AddScore(ScoreSlot slot)
    {
        float multiplier = slot.multiplier;

        ScoreCalculationInScoreSlot?.Invoke(slot, ref multiplier); 

        firstPinballScored = true;

        GainScore(pinballScore * multiplier);
    }

    public void GainScore(float amount)
    {
        ScoreCalculating?.Invoke(ref amount); 
        score += amount;
    }



    public void AddGold(int amount)
    {
        GoldCalculating?.Invoke(ref amount);
        gold += amount;
    }

    public void UpdateTargetScore()
    {
        currentTargetScore = targetScores[currentRound - 1];
        TargetScoreCalculating?.Invoke(ref currentTargetScore);
    }

    //Check methods
    private void CheckRound()
    {
        if (allRoundsFinished) return;

        //If the target is not met but there's still a chance, add more balls and continue the current round.
        if (score < currentTargetScore && throwChance > 0)
        {
            pinballNumber = pinballsPerRound;
            hasStartedThrow = false;
            return;
        }

        RoundEnded?.Invoke(); //Notify Round End


        //Check if the game has failed
        if (score < currentTargetScore)
        {
            Debug.Log("Game Failed");
            return;
        }
        //Check if the game has Finsih
        if (currentRound >= targetScores.Length)
        {
            
                allRoundsFinished = true;
                Debug.Log("Finish all round");
                return;
            
        }

        AddGold(4);
        UIManager.instance.OpenShop();
    }

    //Enter Next Round
    public void NextRound() //Button listening
    {
        if (!UIManager.instance.shop.activeSelf) return;
        //Reset when entering the next round
        currentRound++;
        UpdateTargetScore();
        score = 0f;

        //Rstore Thorw Chance
        previousRoundUnusedThrows = throwChance;
        throwChance = throwChancesPerRound;
        hasStartedThrow = false;

        firstPinballScored = false; //FirstPinballTenTimes Effect

        RoundStarted?.Invoke();

        pinballNumber = pinballsPerRound;

        UIManager.instance.CloseShop();
    }

    //Pinball-related methods 
    public bool TryUseBall()//
    {
        if (pinballNumber <= 0 || allRoundsFinished) return false;

        if (!hasStartedThrow) //Deduct ball opportunity
        {
            if (throwChance <= 0) return false;

            throwChance--;
            hasStartedThrow = true;
        }


        pinballNumber--;
        activePinballs++;
        return true;
    }

    public void BallReturned()
    {
        activePinballs--;

        if (pinballNumber == 0 && activePinballs == 0) //Check whether you can proceed to the next round only after discarding all balls
        {
            CheckRound();
        }
    }




    //Init Setting
    private void InitInAwake()
    {
        currentRound = 1;
        rounds = targetScores.Length;
        UpdateTargetScore();
        pinballNumber = pinballsPerRound;
        Pinball.leftBounceChance = 0.5f;

        throwChance = throwChancesPerRound;
        hasStartedThrow = false;

    }



    //Broadcast a notification
    public void NotifyPinballEnteredSlot(ScoreSlot slot)
    {
        PinballEnteredSlot?.Invoke(slot);
    }

    public void NotifyPinballHitPeg(Pinball pinball, Collider2D peg)
    {
        PinballHitPeg?.Invoke(pinball, peg);
    }

    public void NotifyPinballSpawned()
    {
        PinballSpawned?.Invoke();
    }



}
