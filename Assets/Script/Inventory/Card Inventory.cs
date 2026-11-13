using UnityEngine;
using UnityEngine.UI;

public class CardInventory : MonoBehaviour
{
    //Singleton
    public static CardInventory instance;

    //Inventory slots setting
    [Header("Inventory Slots Setting")]
    public RectTransform[] slots;
    public int unlockedSlots = 6;

    //Prefab
    public GameObject cardPrefab;

    //Test
    public CardTesting cardTesting;

    private void Awake()
    {
        instance = this;
    }


    public bool TryAddCard(Card card)
    {
        //If an DuplicateShopSeventhSlot card is already in inventory, another DuplicateShopSeventhSlot card cannot be added.
        bool isCapacityCard = card.data.effectType == CardData.EffectType.DuplicateShopSeventhSlot;
        bool hasCapacityCard = CountEffect(CardData.EffectType.DuplicateShopSeventhSlot) > 0;
        if (isCapacityCard && hasCapacityCard) return false;

        //Requires expansion card to add duplicate cards to inventory
        if (!hasCapacityCard)
        {
            foreach (Card ownedCard in GetComponentsInChildren<Card>(true))
            {
                if (ownedCard.data == card.data) return false;
            }
        }

        int slotCount = Mathf.Min(unlockedSlots, slots.Length);
        //If the card being added is DuplicateShopSeventhSlot, it will only be placed in the first 6 slots.
        if (isCapacityCard) 
        {
            slotCount = Mathf.Min(slotCount, 6); 
        }

        for (int i = 0; i < slotCount; i++)
        {
            if (slots[i].GetComponentInChildren<Card>(true) != null) continue;

            Card inventoryCard = PoolManager.instance.Get(cardPrefab).GetComponent<Card>();
            inventoryCard.isTestCard = false; //All cards entering the Inventory are regular cards
            inventoryCard.SetData(card.data);

            //Card Size Setting
            RectTransform cardRect = inventoryCard.GetComponent<RectTransform>();
            cardRect.SetParent(slots[i]);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(163f, 241f);

            //Apply Effect
            inventoryCard.ApplyEffect();

            //Maximum of 7 slots if have DuplicateShopSeventhSlot card
            if (isCapacityCard) UnlockSlot();

            return true;
        }

        return false;
    }

    public void SellCard(Card card)
    {
        if (!UIManager.instance.shop.activeInHierarchy && !UIManager.instance.skillCardTest.activeInHierarchy) return;

        CardData soldData = card.data;
        Transform freedSlot = card.transform.parent;
        card.RemoveEffect();
        card.RecycleCard();

        //If the DuplicateShopSeventhSlot card is sold, the card in the 7th slot will be moved to the position of the sold card.
        if (soldData.effectType == CardData.EffectType.DuplicateShopSeventhSlot)
        {
            Card seventhCard = slots.Length > 6 ? slots[6].GetComponentInChildren<Card>(true) : null;
            if (seventhCard != null)
            {
                RectTransform cardRect = seventhCard.GetComponent<RectTransform>();
                cardRect.SetParent(freedSlot, false);
                cardRect.anchoredPosition = Vector2.zero;
            }

            unlockedSlots = 6;
        }


        GameManager.instance.AddGold(2);
        cardTesting.RestoreCard(card.data);
    }


    public void UnlockSlot()
    {
        unlockedSlots = Mathf.Min(7, slots.Length);
    }

    //Count the number of cards in the inventory that have the same effect
    public int CountEffect(CardData.EffectType effectType)
    {
        int count = 0;
        foreach (Card card in GetComponentsInChildren<Card>(true))
        {
            if (card.data.effectType == effectType) count++;
        }
        return count;
    }



}

