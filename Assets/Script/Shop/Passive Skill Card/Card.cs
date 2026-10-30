using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Card Data
    [Header("Card Data")]
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
        sellPanel.SetActive(GetComponentInParent<CardInventory>() != null);

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



    //Card Inventory Related
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

        if (!CardInventory.instance.TryAddCard(this)) return;

        if (!isTestCard) GameManager.instance.gold -= data.price;
        GetComponent<Button>().interactable = false;
        cardImage.GetComponent<Image>().color = Color.gray;
        enabled = false;
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
    }




    //Card Effect Related
    public void ApplyEffect()
    {
        switch (data.effectType)
        {
            case CardData.EffectType.AddOnePinball:
                GameManager.instance.pinballsPerRound += 1;
                break;

            case CardData.EffectType.AddPinballScore:
                GameManager.instance.pinballScore += 50;
                break;

            case CardData.EffectType.HalfSidesCenter075:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    if (slot.position == ScoreSlot.SlotPosition.Side) slot.multiplier *= 0.5f;
                    else if (slot.position == ScoreSlot.SlotPosition.Center)
                    {
                        originalCenterMultiplier = HasEffect(CardData.EffectType.SlotMultiplier15) ? slot.multiplier / 1.5f : slot.multiplier;
                        slot.multiplier = HasEffect(CardData.EffectType.SlotMultiplier15) ? 0.75f * 1.5f : 0.75f;
                    }
                    else continue;

                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.Hit40AddOnePinball:
                GameManager.instance.PinballHitPeg += GameManager.instance.RecordPinHit;
                break;

            case CardData.EffectType.SlotMultiplier15:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    slot.multiplier *= 1.5f;
                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.FirstPinballTenTimes:
                GameManager.instance.ScoreCalculating += GameManager.instance.FirstPinballTenTimes;
                break;

            case CardData.EffectType.PegHitScore:
                GameManager.instance.PinballHitPeg += GameManager.instance.AddPegHitScore;
                break;

            case CardData.EffectType.RandomDoubleSlot:
                GameManager.instance.RoundStarted += GameManager.instance.SelectDoubleSlot;
                GameManager.instance.ScoreCalculating += GameManager.instance.DoubleSlotScore; 
                break;

            case CardData.EffectType.DoubleGoldTargetUp:
                GameManager.instance.GoldCalculating += GameManager.instance.DoubleGold;
                GameManager.instance.TargetScoreCalculating += GameManager.instance.IncreaseTargetScore20Percent; 
                break;

            case CardData.EffectType.RethrowPinball:
                GameManager.instance.PinballEnteredSlot += GameManager.instance.TryRethrowPinball;
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
                GameManager.instance.pinballScore -= 50;
                break;

            case CardData.EffectType.HalfSidesCenter075:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    if (slot.position == ScoreSlot.SlotPosition.Side) slot.multiplier *= 2f;

                    else if (slot.position == ScoreSlot.SlotPosition.Center) slot.multiplier = originalCenterMultiplier * (HasEffect(CardData.EffectType.SlotMultiplier15) ? 1.5f : 1f);
                    else continue;

                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.Hit40AddOnePinball:
                GameManager.instance.PinballHitPeg -= GameManager.instance.RecordPinHit;
                break;

            case CardData.EffectType.SlotMultiplier15:
                foreach (ScoreSlot slot in FindObjectsByType<ScoreSlot>(FindObjectsInactive.Include))
                {
                    slot.multiplier /= 1.5f;
                    slot.GetComponentInChildren<TMP_Text>(true).text = string.Format("{0}x", slot.multiplier);
                }
                break;

            case CardData.EffectType.FirstPinballTenTimes:
                GameManager.instance.ScoreCalculating -= GameManager.instance.FirstPinballTenTimes;
                break;

            case CardData.EffectType.PegHitScore:
                GameManager.instance.PinballHitPeg -= GameManager.instance.AddPegHitScore;
                break;

            case CardData.EffectType.RandomDoubleSlot:
                GameManager.instance.RoundStarted -= GameManager.instance.SelectDoubleSlot;
                GameManager.instance.ScoreCalculating -= GameManager.instance.DoubleSlotScore;
                GameManager.instance.ClearDoubleSlot(); GameManager.instance.SelectDoubleSlot();
                break;

            case CardData.EffectType.DoubleGoldTargetUp:
                GameManager.instance.GoldCalculating -= GameManager.instance.DoubleGold;
                GameManager.instance.TargetScoreCalculating -= GameManager.instance.IncreaseTargetScore20Percent; 
                break;

            case CardData.EffectType.RethrowPinball:
                GameManager.instance.PinballEnteredSlot -= GameManager.instance.TryRethrowPinball;
                break;
        }
    }

    private bool HasEffect(CardData.EffectType effectType)
    {
        foreach (Card card in CardInventory.instance.GetComponentsInChildren<Card>(true))
        {
            if (card.data.effectType == effectType) return true;
        }

        return false;
    }
}