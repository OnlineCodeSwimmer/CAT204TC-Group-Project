using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    //Score Setting
    [Header("Socre Setting")]
    public float pinballScore = 20f;
    [HideInInspector] public float pinballScoreMultiplier = 1f;
    [HideInInspector] public float temporaryPinballScoreBonus; //Record temporary pinball bonus
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
    public int throwChancesPerRound = 3;
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
    private int completedRoundScore;


    //Event
    public event System.Action<ScoreSlot> PinballEnteredSlot;
    public event System.Action<Pinball, Collider2D> PinballHitPeg;
    public event ScoreCalculationInScoreSlotHandler ScoreCalculationInScoreSlot; 
    public event ScoreCalculationHandler ScoreCalculating; //Triggered upon scoring
    public event System.Action RoundStarted;
    public event System.Action RoundEnded;
    public event System.Action<Vector3> PinballSpawned;
    public event System.Action ThrowChanceEnded;
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

        ScoreCalculationInScoreSlot?.Invoke(slot, ref multiplier);  //Calculate goal score

        firstPinballScored = true;

        // [Jinyang.Li] Score-slot multiplier feedback.
        if (!Mathf.Approximately(slot.multiplier, multiplier))
            GameFeedback.Instance?.Burst(CardEffectVisual.Score, slot.transform.position);
        GainScore(GetCurrentPinballScore() * multiplier, slot.transform.position);
    }

    public float GetCurrentPinballScore()
    {
        return (pinballScore + temporaryPinballScoreBonus) * pinballScoreMultiplier;
    }

    public void GainScore(float amount, Vector3? effectPosition = null)
    {
        float baseAmount = amount;
        ScoreCalculating?.Invoke(ref amount); 
        score += amount;
        // [Jinyang.Li] Show a burst when a card changes the awarded score.
        if (effectPosition.HasValue && !Mathf.Approximately(baseAmount, amount))
            GameFeedback.Instance?.Burst(CardEffectVisual.Score, effectPosition.Value);
    }



    public void AddGold(int amount, int maxGain = int.MaxValue)
    {
        GoldCalculating?.Invoke(ref amount);
        gold += Mathf.Min(amount, maxGain);
    }

    public void ChangeShopGold(int amount)
    {
        int previousGold = gold;
        // Preserve the gold bonus on sales and charge the advertised price on purchases.
        if (amount > 0) AddGold(amount);
        else gold += amount;
        // [Jinyang.Li] Gold HUD delta and UI coin burst.
        GameFeedback.Instance?.ShowGoldChange(gold - previousGold);
    }

    public void ChangeBallValue(float amount, Vector3? effectPosition = null)
    {
        pinballScore += amount;
        if (amount <= 0) return;
        // [Jinyang.Li] Ball-value HUD feedback.
        GameFeedback.Instance?.ShowBallValueChange(amount);
        if (effectPosition.HasValue)
            GameFeedback.Instance?.Burst(CardEffectVisual.BallValue, effectPosition.Value);
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

        ThrowChanceEnded?.Invoke();

        //If the target is not met but there's still a chance, add more balls and continue the current round.
        if (score < currentTargetScore && throwChance > 0)
        {
            throwChance--;
            pinballNumber = pinballsPerRound;
            hasStartedThrow = false;
            return;
        }

        RoundEnded?.Invoke(); //Notify Round End


        completedRoundScore += (int)score;
        bool cleared = score >= currentTargetScore;
        if (!cleared || currentRound >= rounds)
        {
            allRoundsFinished = true;
            UIManager.instance.ShowResults(completedRoundScore, cleared);
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
        GameFeedback.Instance?.ShowRoundChange();
    }





    //Pinball-related methods 
    public bool TryUseBall()//
    {
        if (pinballNumber <= 0 || allRoundsFinished) return false;

        hasStartedThrow = true;
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
        Pinball.activeLeftBounceOffset = 0f;

        throwChance = throwChancesPerRound;
        hasStartedThrow = false;

    }


    //Active Card Related
    public bool CanUseActiveCard()
    {
        return !allRoundsFinished && !hasStartedThrow && pinballNumber > 0;
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

    public void NotifyPinballSpawned(Vector3 position)
    {
        PinballSpawned?.Invoke(position);
    }

    //Test
    [ContextMenu("Test Pass Round")]
    private void TestPassRound()
    {
        if (!Application.isPlaying || allRoundsFinished) return;

        UIManager ui = UIManager.instance;
        if (ui.shop.activeSelf || ui.skillCardTest.activeSelf) return;

        foreach (Pinball pinball in FindObjectsByType<Pinball>(FindObjectsInactive.Exclude))
        {
            PoolManager.instance.Recycle(pinball.gameObject);
        }

        activePinballs = 0;
        pinballNumber = 0;
        score = Mathf.Max(score, currentTargetScore);
        CheckRound();
    }

}
