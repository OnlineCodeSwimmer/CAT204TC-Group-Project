using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ActiveCardData data;
    public GameObject descriptionPanel;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Transform cardImage;
    public GameObject sellPanel;

    private Vector3 originalScale;

    //Save the probability of left pinball when using the card
    private static float previousLeftBounceChance;

    //Record the slot where the first pinball enters in this chance
    private static ScoreSlot boostedScoreSlot;

    //Use In Test Pool
    [HideInInspector] public bool isTestCard;

    //Price Setting
    public GameObject pricePanel;
    public TMP_Text priceText;

    // [Jinyang.Li] Active-card shop refresh flip state.
    private CanvasGroup refreshGroup;
    private bool refreshAnimating;


    private void Awake()
    {
        originalScale = cardImage.localScale;
        if (data != null) SetData(data);
    }

    public void SetData(ActiveCardData cardData)
    {
        data = cardData;
        priceText.text = data.price.ToString();
        nameText.text = data.cardName;
        descriptionText.text = data.description;
        cardImage.GetComponent<Image>().sprite = data.cardSprite;
        ResetHover();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (refreshAnimating) return;
        pricePanel.SetActive(IsShopOffer());
        cardImage.localScale = originalScale * 1.1f;
        descriptionPanel.SetActive(true);
        sellPanel.SetActive(IsTradingOpen() && GetComponentInParent<ActiveCardInventory>() != null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (refreshAnimating) return;
        ResetHover();
    }

    private void OnDisable()
    {
        refreshAnimating = false;
        transform.localRotation = Quaternion.identity;
        if (refreshGroup != null)
        {
            refreshGroup.alpha = 1f;
            refreshGroup.interactable = true;
            refreshGroup.blocksRaycasts = true;
        }
        ResetHover();
    }

    // [Jinyang.Li] Shared hooks for the active-card flip refresh.
    public void SetRefreshAnimation(bool playing)
    {
        if (refreshGroup == null)
            refreshGroup = gameObject.AddComponent<CanvasGroup>();

        refreshAnimating = playing;
        refreshGroup.alpha = 1f;
        refreshGroup.interactable = !playing;
        refreshGroup.blocksRaycasts = !playing;
        if (playing) ResetHover();
    }

    public void SetRefreshRotation(float angle)
    {
        transform.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    // [Jinyang.Li] Hides the destination card while the purchase ghost flies in.
    public void SetPurchaseAnimation(bool playing)
    {
        if (refreshGroup == null)
            refreshGroup = gameObject.AddComponent<CanvasGroup>();

        refreshGroup.alpha = playing ? 0f : 1f;
        refreshGroup.interactable = !playing;
        refreshGroup.blocksRaycasts = !playing;
        if (playing) ResetHover();
    }

    //Hover Related
    private void ResetHover()
    {
        cardImage.localScale = originalScale;
        descriptionPanel.SetActive(false);
        sellPanel.SetActive(false);
        pricePanel.SetActive(false);
    }

    public void OnCardClicked()//Button Event
    {
        ActiveCardInventory inventory = ActiveCardInventory.instance;

        if (!isActiveAndEnabled || data == null || inventory == null) return;

        if (isTestCard)
        {
            inventory.TryAddCard(data);
            return;
        }



        GameManager game = GameManager.instance;

        if (IsShopOffer())
        {
            int price = data.price;
            if (game.gold < price) return;
            if (!inventory.TryAddCard(data, out ActiveCard inventoryCard)) return;
            GameFeedback.Instance?.PlayPurchase(this, inventoryCard);
            game.gold -= price;
            GetComponent<Button>().interactable = false;
            cardImage.GetComponent<Image>().color = Color.gray;
            enabled = false;
            return;
        }

        if (GetComponentInParent<ActiveCardInventory>() == null) return;


        if (IsTradingOpen())
        {
            game.AddGold(2);
        }
        else if (!game.CanUseActiveCard() || !ApplyEffect())
        {
            return;
        }

        RecycleCard();
       }

    private bool IsShopOffer()
    {
        return GetComponentInParent<ShopSkillCardPool>() != null;
    }

    //Trading Related
    private bool IsTradingOpen()
    {
        UIManager ui = UIManager.instance;
        return (ui.shop.activeInHierarchy || ui.skillCardTest.activeInHierarchy);
    }

    private bool ApplyEffect()
    {
        GameManager game = GameManager.instance;

        switch (data.effectType)
        {
            case ActiveCardData.EffectType.FirstPinballTwentyTimes:
                GameManager.instance.ScoreCalculationInScoreSlot += ApplyFirstPinballTwentyTimes;
                return true;

            case ActiveCardData.EffectType.AddRandomPinballs:
                game.pinballNumber += Random.Range(1, 6);
                return true;

            case ActiveCardData.EffectType.EmperorScoreBoost:
                game.ScoreCalculationInScoreSlot += ApplyEmperorScoreBoost;
                game.ThrowChanceEnded += ClearEmperorScoreBoost;
                return true;

            case ActiveCardData.EffectType.RandomLeftBounceChance:
                Pinball.activeLeftBounceOffset = Random.value < 0.5f ? -0.15f : 0.15f;
                game.ThrowChanceEnded += RestoreHierophantBounceChance;
                return true;

            case ActiveCardData.EffectType.DoublePinballValue:
                game.pinballScoreMultiplier *= 2f;
                game.RoundEnded += ClearChariotEffect;
                return true;

            case ActiveCardData.EffectType.AddThrowChance:
                game.throwChance++;
                return true;

            case ActiveCardData.EffectType.RethrowPinballTenPercent:
                game.PinballEnteredSlot += TryRethrowPinballTenPercent;
                game.RoundEnded += ClearRethrowPinballEffect;
                return true;

            case ActiveCardData.EffectType.GainDoubleCurrentGold:
                int bonus = Mathf.Clamp(game.gold, 0, 10) * 2;
                game.AddGold(bonus, 20);
                return true;

            case ActiveCardData.EffectType.DoubleScoreCostThrowChance:
                if (game.throwChance <= 0) return false;
                game.throwChance--;
                game.ScoreCalculating += DoubleThrowChanceScore;
                game.ThrowChanceEnded += ClearDoubleThrowChanceScore;
                return true;

            case ActiveCardData.EffectType.FirstSlotMultiplier15:
                game.ScoreCalculationInScoreSlot += ApplyFirstSlotMultiplier15;
                game.ThrowChanceEnded += ClearFirstSlotMultiplier15;
                return true;

            case ActiveCardData.EffectType.DestroyLastPassiveCardAddPinballs:
                PassiveCardInventory inventory = PassiveCardInventory.instance;
                if (inventory == null || !inventory.TryDestroyLastBoughtCard()) return false;
                game.pinballNumber += 5;
                return true;

            case ActiveCardData.EffectType.SpendAllGoldForPinballs:
                int goldGroups = game.gold / 4;
                game.gold = 0;
                game.pinballNumber += goldGroups;
                game.pinballScore += goldGroups * 15f;
                return true;

            case ActiveCardData.EffectType.ReduceValueAddFourPinballs:
                if (game.pinballScore < 50f) return false;
                game.pinballScore -= 50f;
                game.pinballNumber += 4;
                return true;

            case ActiveCardData.EffectType.ReduceAllTargetScores5Percent:
                game.TargetScoreCalculating += ReduceAllTargetScores5Percent;
                game.UpdateTargetScore();
                return true;

            case ActiveCardData.EffectType.AddTemporaryPinballValue30:
                game.temporaryPinballScoreBonus += 30f;
                game.RoundEnded += ClearSunEffect;
                return true;

            case ActiveCardData.EffectType.ReduceCurrentRoundTargetScore20Percent:
                game.TargetScoreCalculating += ReduceCurrentRoundTargetScore20Percent;
                game.RoundEnded += ClearJudgementEffect;
                game.UpdateTargetScore();
                return true;

            default:
                return false;
        }
    }

    //Recycle Card 
    public void RecycleCard()
    {
        ActiveCard prefab = ActiveCardInventory.instance.cardPrefab;
        PoolManager.instance.Recycle(gameObject);

        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform prefabRect = prefab.GetComponent<RectTransform>();
        cardRect.sizeDelta = prefabRect.sizeDelta;
        cardRect.localScale = prefabRect.localScale;
        cardRect.localRotation = prefabRect.localRotation;
        refreshAnimating = false;
        if (refreshGroup != null)
        {
            refreshGroup.alpha = 1f;
            refreshGroup.interactable = true;
            refreshGroup.blocksRaycasts = true;
        }

        Image image = cardImage.GetComponent<Image>();
        Image prefabImage = prefab.cardImage.GetComponent<Image>();
        image.color = prefabImage.color;
        image.sprite = prefabImage.sprite;
        nameText.text = prefab.nameText.text;
        descriptionText.text = prefab.descriptionText.text;

        GetComponent<Button>().interactable = prefab.GetComponent<Button>().interactable;
        enabled = prefab.enabled;
        data = null;
    }


    //Card Effect Realted
    //FirstPinballTwentyTimes Card Related
    private static void ApplyFirstPinballTwentyTimes(ScoreSlot slot, ref float multiplier)
    {
        multiplier *= 20f;
        GameManager.instance.ScoreCalculationInScoreSlot -= ApplyFirstPinballTwentyTimes;
    }

    //EmperorScoreBoost Card Related
    private static void ApplyEmperorScoreBoost(ScoreSlot slot, ref float multiplier)
    {
        if (Random.value < 0.5f) multiplier *= 1.25f;
    }
    private static void ClearEmperorScoreBoost()
    {
        GameManager game = GameManager.instance;
        game.ScoreCalculationInScoreSlot -= ApplyEmperorScoreBoost;
        game.ThrowChanceEnded -= ClearEmperorScoreBoost;
    }

    //RandomLeftBounceChance Card Related
    private static void RestoreHierophantBounceChance()
    {
        Pinball.activeLeftBounceOffset = 0f;
        GameManager.instance.ThrowChanceEnded -= RestoreHierophantBounceChance;
    }

    //DoublePinballValue Card Related
    private static void ClearChariotEffect()
    {
        GameManager game = GameManager.instance;
        game.pinballScoreMultiplier /= 2f;
        game.RoundEnded -= ClearChariotEffect;
    }

    //RethrowPinballTenPercent Card Related
    private static void TryRethrowPinballTenPercent(ScoreSlot slot)
    {
        if (Random.value >= 0.1f) return;
        GameManager.instance.spawnPinballPoint.SpawnExtraBall();
    }

    private static void ClearRethrowPinballEffect()
    {
        GameManager game = GameManager.instance;
        game.PinballEnteredSlot -= TryRethrowPinballTenPercent;
        game.RoundEnded -= ClearRethrowPinballEffect;
    }

    //DoubleScoreCostThrowChance Card Related
    private static void DoubleThrowChanceScore(ref float amount)
    {
        amount *= 2f;
    }

    private static void ClearDoubleThrowChanceScore()
    {
        GameManager game = GameManager.instance;
        game.ScoreCalculating -= DoubleThrowChanceScore;
        game.ThrowChanceEnded -= ClearDoubleThrowChanceScore;
    }

    //FirstSlotMultiplier15 Card Realted
    private static void ApplyFirstSlotMultiplier15(ScoreSlot slot, ref float multiplier)
    {
        if (boostedScoreSlot == null)
        {
            boostedScoreSlot = slot;
            UpdateBoostedSlotMultiplierText(1.5f);
        }

        if (slot == boostedScoreSlot) multiplier *= 1.5f;
    }

    private static void ClearFirstSlotMultiplier15()
    {
        GameManager game = GameManager.instance;
        game.ScoreCalculationInScoreSlot -= ApplyFirstSlotMultiplier15;
        game.ThrowChanceEnded -= ClearFirstSlotMultiplier15;

        UpdateBoostedSlotMultiplierText(1f);
        boostedScoreSlot = null;
    }

    private static void UpdateBoostedSlotMultiplierText(float factor)
    {
        if (boostedScoreSlot == null) return;

        TMP_Text label = boostedScoreSlot.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = string.Format("{0}x", boostedScoreSlot.multiplier * factor);
    }

    //ReduceAllTargetScores5Percent Card Related
    private static void ReduceAllTargetScores5Percent(ref float targetScore)
    {
        targetScore *= 0.95f;
    }

    //AddTemporaryPinballValue30 Card Related
    private static void ClearSunEffect()
    {
        GameManager game = GameManager.instance;
        game.temporaryPinballScoreBonus -= 30f;
        game.RoundEnded -= ClearSunEffect;
    }

    //ReduceCurrentRoundTargetScore20Percent Card Related
    private static void ReduceCurrentRoundTargetScore20Percent(ref float targetScore)
    {
        targetScore *= 0.8f;
    }

    private static void ClearJudgementEffect()
    {
        GameManager game = GameManager.instance;
        game.TargetScoreCalculating -= ReduceCurrentRoundTargetScore20Percent;
        game.RoundEnded -= ClearJudgementEffect;
    }
}
