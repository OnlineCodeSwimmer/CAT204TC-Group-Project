using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PassiveCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //PassiveCard Data
    [Header("PassiveCard Data")]
    public CardData data;

    //Original slot
    private float originalCenterMultiplier;


    //Size
    private Vector3 originalScale;

    //UI Component
    //Descripiton
    public GameObject descriptionPanel;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    //Image
    public Transform cardImage;
    //Price
    public GameObject pricePanel;
    public TMP_Text priceText;
    //Sell
    public GameObject sellPanel;

    //Test
    [HideInInspector] public bool isTestCard;

    //Each card stores its own effect state record
    private int pinHitCount;
    private int spawnedPinballCount;
    private int thirdSlotTriggerCount; // ThirdSlotGrowth PassiveCard will use this
    private ScoreSlot doubleScoreSlot; // RandomDoubleSlot PassiveCard will use this
    private float leftBounceOffset; //RandomLeftBounceChance PassiveCard will use this
    private int activeUnusedThrowCount; //UnusedThrowBonus PassiveCard will use this
    private CanvasGroup purchaseGroup;


    private void Awake()
    {
        originalScale = cardImage.localScale;
        //SO Data Loading
        if (data != null) SetData(data);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Zoom in and out
        cardImage.localScale = originalScale * 1.1f;
        //Diplay Description
        descriptionPanel.SetActive(true);
        //Display Price
        pricePanel.SetActive(!isTestCard && GetComponentInParent<CardInventory>() == null && GetComponent<Button>().interactable);
        //Sell Related
        sellPanel.SetActive((UIManager.instance.shop.activeInHierarchy || UIManager.instance.skillCardTest.activeInHierarchy) && GetComponentInParent<CardInventory>() != null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Zoom in and out
        cardImage.localScale = originalScale;
        //Hidden Description
        descriptionPanel.SetActive(false);
        //Hidden Price
        pricePanel.SetActive(false);
        //Sell Related
        sellPanel.SetActive(false);
    }

    private void OnDisable()
    {
        SetPurchaseAnimation(false);
        //Size
        cardImage.localScale = originalScale;
        //Description
        descriptionPanel.SetActive(false);
        //Price
        pricePanel.SetActive(false);
        //Sell
        sellPanel.SetActive(false);
    }



    //SO Data Related
    public void SetData(CardData cardData)
    {
        data = cardData;
        nameText.text = data.cardName;
        descriptionText.text = data.description;
        cardImage.GetComponent<Image>().sprite = data.cardSprite;
        priceText.text = data.price.ToString();
        pricePanel.SetActive(false);
        sellPanel.SetActive(false);
    }



    //PassiveCard Inventory Related
    public void GeneraInInventory() //Button Subscribe
    {
        //Sell Related
        if (GetComponentInParent<CardInventory>() != null)
        {
            CardInventory.instance.SellCard(this);
            return;
        }



        //Purchase Related
        if (!isTestCard && GameManager.instance.gold < data.price) return;

        if (!CardInventory.instance.TryAddCard(this, out PassiveCard inventoryCard)) return;

        GameFeedback.Instance?.PlayPurchase(this, inventoryCard);
        if (!isTestCard) GameManager.instance.ChangeShopGold(-data.price);
        GetComponent<Button>().interactable = false;
        cardImage.GetComponent<Image>().color = Color.gray;
        enabled = false;
    }

    public void SetPurchaseAnimation(bool playing)
    {
        if (playing && purchaseGroup == null)
            purchaseGroup = gameObject.AddComponent<CanvasGroup>();
        if (purchaseGroup == null) return;

        purchaseGroup.alpha = playing ? 0f : 1f;
        purchaseGroup.interactable = !playing;
        purchaseGroup.blocksRaycasts = !playing;
    }

    public void RecycleCard()
    {
        PoolManager.instance.Recycle(gameObject);

        //Restore Size
        GetComponent<RectTransform>().sizeDelta = CardInventory.instance.cardPrefab.GetComponent<RectTransform>().sizeDelta;
        transform.localScale = CardInventory.instance.cardPrefab.transform.localScale;

        //Restore Color
        cardImage.GetComponent<Image>().color = Color.white;

        //Restore Enable
        GetComponent<Button>().interactable = true;
        enabled = true;

        //Restore Effect State Record
        originalCenterMultiplier = 0f;
        pinHitCount = 0;
        spawnedPinballCount = 0;
        thirdSlotTriggerCount = 0;
        doubleScoreSlot = null;
        leftBounceOffset = 0f;
        activeUnusedThrowCount = 0;
    }




    //PassiveCard Effect Related
    public void ApplyEffect()
    {
        if (data.effectType == CardData.EffectType.AddOnePinball ||
            data.effectType == CardData.EffectType.AddPinballScore ||
            data.effectType == CardData.EffectType.HalfSidesCenter075 ||
            data.effectType == CardData.EffectType.SlotMultiplier15 ||
            data.effectType == CardData.EffectType.DuplicateShopSeventhSlot)
            Flash();

        switch (data.effectType)
        {
            case CardData.EffectType.AddOnePinball:
                GameManager.instance.pinballsPerRound += 1;
                break;

            case CardData.EffectType.AddPinballScore:
                GameManager.instance.ChangeBallValue(50);
                break;

            case CardData.EffectType.HalfSidesCenter075:
                UpdateHalfSidesCenter075(true);
                break;

            case CardData.EffectType.Hit40AddOnePinball:
                GameManager.instance.PinballHitPeg += RecordPinHit;
                break;

            case CardData.EffectType.SlotMultiplier15:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    slot.multiplier *= 1.5f;
                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.FirstPinballTenTimes:
                GameManager.instance.ScoreCalculationInScoreSlot += FirstPinballTenTimes;
                break;

            case CardData.EffectType.PegHitScore:
                GameManager.instance.PinballHitPeg += AddPegHitScore;
                break;

            case CardData.EffectType.RandomDoubleSlot:
                GameManager.instance.RoundStarted += SelectDoubleSlot;
                GameManager.instance.ScoreCalculationInScoreSlot += DoubleSlotScore; 
                break;

            case CardData.EffectType.DoubleGoldTargetUp:
                GameManager.instance.GoldCalculating += DoubleGold;
                GameManager.instance.TargetScoreCalculating += IncreaseTargetScore20Percent;
                break;

            case CardData.EffectType.RethrowPinball:
                GameManager.instance.PinballEnteredSlot += TryRethrowPinball;
                break;

            case CardData.EffectType.ThirdSlotGrowth:
                GameManager.instance.PinballEnteredSlot += GrowThirdSlotValue;
                break;

            case CardData.EffectType.RoundEndGoldChance:
                GameManager.instance.RoundEnded += TryGainRoundEndGold;
                break;

            case CardData.EffectType.PegHitExtraPinball:
                GameManager.instance.PinballHitPeg += TrySpawnExtraPinballOnPegHit;
                break;

            case CardData.EffectType.Spawn16AddOneGold:
                GameManager.instance.PinballSpawned += RecordSpawnedPinball;
                break;

            case CardData.EffectType.AllScoreMultiplier15:
                GameManager.instance.ScoreCalculating += MultiplyAllScore15;
                break;

            case CardData.EffectType.FirstSlotAddOnePinball:
                GameManager.instance.PinballEnteredSlot += AddPinballOnFirstSlot;
                break;

            case CardData.EffectType.PegHitSplitPinball:
                GameManager.instance.PinballHitPeg += TrySplitPinball;
                break;

            case CardData.EffectType.RandomLeftBounceChance:
                GameManager.instance.RoundStarted += RandomizeLeftBounceChance;
                break;

            case CardData.EffectType.UnusedThrowBonus:
                GameManager.instance.RoundStarted += ApplyUnusedThrowBonus;
                break;

        }
    }


    public void RemoveEffect()
    {
        switch (data.effectType)
        {
            case CardData.EffectType.AddOnePinball:
                GameManager.instance.pinballsPerRound -= 1;
                break;

            case CardData.EffectType.AddPinballScore:
                GameManager.instance.ChangeBallValue(-50);
                break;

            case CardData.EffectType.HalfSidesCenter075:
                UpdateHalfSidesCenter075(false);
                break;

            case CardData.EffectType.Hit40AddOnePinball:
                GameManager.instance.PinballHitPeg -= RecordPinHit;
                break;

            case CardData.EffectType.SlotMultiplier15:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    slot.multiplier /= 1.5f;
                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.FirstPinballTenTimes:
                GameManager.instance.ScoreCalculationInScoreSlot -= FirstPinballTenTimes;
                break;

            case CardData.EffectType.PegHitScore:
                GameManager.instance.PinballHitPeg -= AddPegHitScore;
                break;

            case CardData.EffectType.RandomDoubleSlot:
                GameManager.instance.RoundStarted -= SelectDoubleSlot;
                GameManager.instance.ScoreCalculationInScoreSlot -= DoubleSlotScore;
                break;

            case CardData.EffectType.DoubleGoldTargetUp:
                GameManager.instance.GoldCalculating -= DoubleGold;
                GameManager.instance.TargetScoreCalculating -= IncreaseTargetScore20Percent; 
                break;

            case CardData.EffectType.RethrowPinball:
                GameManager.instance.PinballEnteredSlot -= TryRethrowPinball;
                break;

            case CardData.EffectType.ThirdSlotGrowth:
                GameManager.instance.PinballEnteredSlot -= GrowThirdSlotValue;
                break;


            case CardData.EffectType.RoundEndGoldChance:
                GameManager.instance.RoundEnded -= TryGainRoundEndGold;
                break;

            case CardData.EffectType.PegHitExtraPinball:
                GameManager.instance.PinballHitPeg -= TrySpawnExtraPinballOnPegHit;
                break;

            case CardData.EffectType.Spawn16AddOneGold:
                GameManager.instance.PinballSpawned -= RecordSpawnedPinball;
                break;

            case CardData.EffectType.AllScoreMultiplier15:
                GameManager.instance.ScoreCalculating -= MultiplyAllScore15;
                break;

            case CardData.EffectType.FirstSlotAddOnePinball:
                GameManager.instance.PinballEnteredSlot -= AddPinballOnFirstSlot;
                break;

            case CardData.EffectType.PegHitSplitPinball:
                GameManager.instance.PinballHitPeg -= TrySplitPinball;
                break;

            case CardData.EffectType.RandomLeftBounceChance:
                GameManager.instance.RoundStarted -= RandomizeLeftBounceChance;
                leftBounceOffset = 0f;
                RefreshLeftBounceChance();
                break;

            case CardData.EffectType.UnusedThrowBonus:
                GameManager.instance.RoundStarted -= ApplyUnusedThrowBonus;
                GameManager.instance.pinballsPerRound -= activeUnusedThrowCount * 2;
                GameManager.instance.ChangeBallValue(-activeUnusedThrowCount * 8f);
                break;
        }
    }



    //Skill PassiveCard Realted
    //Pinball Hit Pegs Realted
    // This method only works after the corresponding skill card has been purchased
    private void RecordPinHit(Pinball pinball, Collider2D peg)
    {
        pinHitCount++;
        if (pinHitCount < 40) return;

        pinHitCount = 0;
        Flash();
        GameManager.instance.spawnPinballPoint.SpawnExtraBall();
    }

    //Select Double Slot PassiveCard Realted
    private void SelectDoubleSlot()
    {
        doubleScoreSlot = null;

        ScoreSlot[] slots = FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include);
        if (slots.Length == 0) return;

        doubleScoreSlot = slots[Random.Range(0, slots.Length)];
        Flash();
    }
    private void DoubleSlotScore(ScoreSlot slot, ref float multiplier)
    {
        if (slot != doubleScoreSlot) return;
        multiplier *= 2f;
        Flash();
    }

    //HalfSidesCenter075 Skill PassiveCard Effect
    private void UpdateHalfSidesCenter075(bool applying)
    {
        //Find matching cards and apply the same effect if found
        CardInventory inventory = CardInventory.instance;
        PassiveCard otherCard = null;
        foreach (PassiveCard ownedCard in inventory.GetComponentsInChildren<PassiveCard>(true))
        {
            if (ownedCard == this || ownedCard.data.effectType != CardData.EffectType.HalfSidesCenter075) continue;
            otherCard = ownedCard;
            break;
        }

        //Effect Implementation
        float slotFactor = Mathf.Pow(1.5f, inventory.CountEffect(CardData.EffectType.SlotMultiplier15));
        foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
        {
            if (slot.position == ScoreSlot.SlotPosition.First || slot.position == ScoreSlot.SlotPosition.Second)
            {
                slot.multiplier *= applying ? 0.5f : 2f;
            }

            else if (slot.position == ScoreSlot.SlotPosition.Center)
            {
                if (applying) originalCenterMultiplier = otherCard != null ? otherCard.originalCenterMultiplier : slot.multiplier / slotFactor;
                float centerMultiplier = applying || otherCard != null ? 0.75f : originalCenterMultiplier;
                slot.multiplier = centerMultiplier * slotFactor;
            }

            else continue;

            slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
        }
    }




    //RethrowPinball Skill PassiveCard Effect
    private void TryRethrowPinball(ScoreSlot slot)
    {
        if (Random.value >= 0.05f) return;
        Flash();
        GameManager.instance.spawnPinballPoint.SpawnExtraBall();
    }


    //PegHitScore Skill PassiveCard Effect
    private void AddPegHitScore(Pinball pinball, Collider2D peg)
    {
        GameManager game = GameManager.instance;
        game.GainScore(game.pinballScore / 20f);
        Flash(CardEffectVisual.Score, pinball.transform.position);
    }

    //FirstPinballTenTimes Skill PassiveCard Effect
    private void FirstPinballTenTimes(ScoreSlot slot, ref float multiplier)
    {
        if (GameManager.instance.firstPinballScored) return;
        multiplier *= 10f;
        Flash();
    }

    //IncreaseTargetScore20Percent Skill PassiveCard Effect
    public void DoubleGold(ref int amount)
    {
        amount *= 2;
        if (amount != 0) Flash();
    }

    public void IncreaseTargetScore20Percent(ref float targetScore)
    {
        targetScore = (float)(targetScore * 1.2);
        Flash();
    }

    //Third Slot Growth PassiveCard Effect
    private void GrowThirdSlotValue(ScoreSlot slot)
    {
        if (slot.position != ScoreSlot.SlotPosition.Third) return;

        GameManager.instance.ChangeBallValue(5 + thirdSlotTriggerCount, slot.transform.position);
        thirdSlotTriggerCount++;
        Flash();
    }

    //RoundEndGoldChance Realted
    private void TryGainRoundEndGold()
    {
        if (Random.value >= 0.5f) return;
        GameManager.instance.AddGold(4);
        Flash();
    }

    //PegHitExtraPinball Effect
    private void TrySpawnExtraPinballOnPegHit(Pinball pinball, Collider2D peg)
    {
        if (Random.value >= 0.03f) return;

        Flash();
        GameManager.instance.spawnPinballPoint.SpawnExtraBall();
    }

    //Spawn16AddOneGold Effect
    private void RecordSpawnedPinball(Vector3 position)
    {
        spawnedPinballCount++;
        if (spawnedPinballCount < 16) return;

        spawnedPinballCount = 0;
        GameManager.instance.AddGold(1);
        Flash(CardEffectVisual.Gold, position);
    }

    //AllScoreMultiplier15 Effect
    public void MultiplyAllScore15(ref float amount)
    {
        amount *= 1.5f;
        if (amount != 0f) Flash();
    }

    //FirstSlotAddOnePinball Effect
    private void AddPinballOnFirstSlot(ScoreSlot slot)
    {
        if (slot.position != ScoreSlot.SlotPosition.First) return;
        GameManager.instance.pinballNumber++;
        Flash(CardEffectVisual.BallStock, slot.transform.position);
    }

    //PegHitSplitPinball Effect
    public void TrySplitPinball(Pinball pinball, Collider2D peg)
    {
        if (Random.value >= 0.05f) return;

        pinball.Split(peg);
        Flash();
    }

    //RandomLeftBounceChance Effect
    private void RandomizeLeftBounceChance()
    {
        leftBounceOffset = Random.value < 0.5f ? -0.15f : 0.15f;
        RefreshLeftBounceChance();
        Flash();
    }

    private void RefreshLeftBounceChance()
    {
        float chance = 0.5f;
        foreach (PassiveCard card in CardInventory.instance.GetComponentsInChildren<PassiveCard>(true))
        {
            if (card.data.effectType == CardData.EffectType.RandomLeftBounceChance) chance += card.leftBounceOffset;
        }
        Pinball.leftBounceChance = Mathf.Clamp01(chance);
    }

    //UnusedThrowBonus Effect
    private void ApplyUnusedThrowBonus()
    {
        GameManager game = GameManager.instance;
        int bonusCount = game.previousRoundUnusedThrows;
        int bonusChange = bonusCount - activeUnusedThrowCount;

        game.pinballsPerRound += bonusChange * 2;
        game.ChangeBallValue(bonusChange * 8f);
        activeUnusedThrowCount = bonusCount;
        if (bonusCount > 0) Flash();
    }

    private void Flash()
    {
        GameFeedback.Instance?.FlashCard(this);
    }

    private void Flash(CardEffectVisual effect, Vector3 position)
    {
        Flash();
        GameFeedback.Instance?.Burst(effect, position);
    }


}
