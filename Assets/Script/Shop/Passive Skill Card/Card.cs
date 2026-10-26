using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //Card Data
    [Header("Card Data")]
    public CardData data;

    //Size
    private Vector3 originalScale;

    //UI Component
    public GameObject descriptionPanel;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public Transform cardImage;
    public GameObject pricePanel;
    public TMP_Text priceText;

    private void Awake()
    {
        originalScale = cardImage.localScale;
        //SO Data Loading
        SetData(data);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Zoom in and out
        cardImage.localScale = originalScale * 1.1f;
        //Diplay Description
        descriptionPanel.SetActive(true);
        //Display Price
        pricePanel.SetActive(GetComponent<Button>().interactable);


    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Zoom in and out
        cardImage.localScale = originalScale;
        //Hidden Description
        descriptionPanel.SetActive(false);
        //Hidden Price
        pricePanel.SetActive(false);
    }

    private void OnDisable()
    {
        //Size
        cardImage.localScale = originalScale;
        //Description
        descriptionPanel.SetActive(false);
        //Price
        pricePanel.SetActive(false);
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
    }



    //Card Inventory Related
    public void GeneraInInventory() //Button Subscribe
    {
        if (GameManager.instance.gold < data.price) return;

        if (!CardInventory.instance.TryAddCard(this)) return;

        GameManager.instance.gold -= data.price;
        GetComponent<Button>().interactable = false;
        cardImage.GetComponent<Image>().color = Color.gray;
        enabled = false;
        //Apply Card Effect
        ApplyEffect();
    }


    //Card Effect Related
    private void ApplyEffect()
    {
        switch (data.effectType)
        {
            case CardData.EffectType.AddBall:
                GameManager.instance.ballsPerRound += data.effectValue;
                break;
        }
    }
}