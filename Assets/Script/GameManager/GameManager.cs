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
    private int pinHitCount;


    //Gold Setting
    public int gold = 5;

    //Turn Varible
    [HideInInspector] public int rounds;
    [HideInInspector] public int currentRound;

    //Checking Varible
    //Effect Checking
    private ScoreSlot doubleScoreSlot;
    private bool firstPinballScored;
    private int thirdSlotTriggerCount; //Third Slot Growth Card Effect
    private int spawnedPinballCount;  //Spawn16AddOneGold Effect



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
            if (score >= currentTargetScore)
            {
                allRoundsFinished = true;
                Debug.Log("Finish all round");
                return;
            }
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
        pinballNumber = pinballsPerRound;
        score = 0f;

        firstPinballScored = false; //FirstPinballTenTimes Effect

        RoundStarted?.Invoke();

        UIManager.instance.CloseShop();
    }

    //Pinball-related methods 
    public bool TryUseBall()//
    {
        if (pinballNumber <= 0 || allRoundsFinished) return false;

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


    //Skill Card Realted
    //Pinball Hit Pegs Realted
    public void RecordPinHit(Pinball pinball, Collider2D peg)// This method only works after the corresponding skill card has been purchased
    {
        pinHitCount++;
        if (pinHitCount < 40) return;

        pinHitCount = 0;
        spawnPinballPoint.SpawnExtraBall();
    }

    //Select Double Slot Card Realted
    public void SelectDoubleSlot()
    {
        doubleScoreSlot = null;

        ScoreSlot[] slots = FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include);
        if (slots.Length == 0) return;

        doubleScoreSlot = slots[Random.Range(0, slots.Length)];
    }
    public void DoubleSlotScore(ScoreSlot slot, ref float multiplier)
    {
        if (slot == doubleScoreSlot) multiplier *= 2f;
    }
    public void ClearDoubleSlot()
    {
        doubleScoreSlot = null;
    }


    //RethrowPinball Skill Card Effect
    public void TryRethrowPinball(ScoreSlot slot)
    {
        if (Random.value >= 0.05f) return;

        spawnPinballPoint.SpawnExtraBall();
    }


    //PegHitScore Skill Card Effect
    public void AddPegHitScore(Pinball pinball, Collider2D peg)
    {
        GainScore(pinballScore / 20f);
    }

    //FirstPinballTenTimes Skill Card Effect
    public void FirstPinballTenTimes(ScoreSlot slot, ref float multiplier)
    {
        if (!firstPinballScored) multiplier *= 10f;
    }

    //IncreaseTargetScore20Percent Skill Card Effect
    public void DoubleGold(ref int amount)
    {
        amount *= 2;
    }

    public void IncreaseTargetScore20Percent(ref float targetScore)
    {
        targetScore = (float)(targetScore * 1.2);
    }

    //Third Slot Growth Card Effect
    public void GrowThirdSlotValue(ScoreSlot slot)
    {
        if (slot.position != ScoreSlot.SlotPosition.Third) return;

        pinballScore += 5 + thirdSlotTriggerCount;
        thirdSlotTriggerCount++;
    }

    public void ResetThirdSlotGrowth()
    {
        thirdSlotTriggerCount = 0;

    }


    //RoundEndGoldChance Realted
    public void TryGainRoundEndGold() 
    { 
        if (Random.value < 0.5f) AddGold(4); 
    }

    //PegHitExtraPinball Effect
    public void TrySpawnExtraPinballOnPegHit(Pinball pinball, Collider2D peg)
    {
        if (Random.value >= 0.03f) return;

        spawnPinballPoint.SpawnExtraBall();
    }

    //Spawn16AddOneGold Effect
    public void RecordSpawnedPinball()
    {
        spawnedPinballCount++;
        if (spawnedPinballCount < 16) return;

        spawnedPinballCount = 0;
        AddGold(1);
    }

    public void ResetSpawnedPinballCount()
    {
        spawnedPinballCount = 0;
    }

    //AllScoreMultiplier15 Effect
    public void MultiplyAllScore15(ref float amount)
    {
        amount *= 1.5f;
    }

    //FirstSlotAddOnePinball Effect
    public void AddPinballOnFirstSlot(ScoreSlot slot)
    {
        if (slot.position != ScoreSlot.SlotPosition.First) return;

        pinballNumber++;
    }

    //PegHitSplitPinball Effect
    public void TrySplitPinball(Pinball pinball, Collider2D peg)
    {
        if (Random.value >= 0.05f) return;

        pinball.Split(peg);
    }
}
