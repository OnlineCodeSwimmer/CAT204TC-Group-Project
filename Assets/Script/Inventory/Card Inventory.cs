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


    public bool TryAddCard(PassiveCard card)
    {
        return TryAddCard(card, out _);
    }

    public bool TryAddCard(PassiveCard card, out PassiveCard inventoryCard)
    {
        inventoryCard = null;
        //If an DuplicateShopSeventhSlot card is already in inventory, another DuplicateShopSeventhSlot card cannot be added.
        bool isCapacityCard = card.data.effectType == CardData.EffectType.DuplicateShopSeventhSlot;
        bool hasCapacityCard = CountEffect(CardData.EffectType.DuplicateShopSeventhSlot) > 0;
        if (isCapacityCard && hasCapacityCard) return false;

        //Requires expansion card to add duplicate cards to inventory
        if (!hasCapacityCard)
        {
            foreach (PassiveCard ownedCard in GetComponentsInChildren<PassiveCard>(true))
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
            if (slots[i].GetComponentInChildren<PassiveCard>(true) != null) continue;

            inventoryCard = PoolManager.instance.Get(cardPrefab).GetComponent<PassiveCard>();
            inventoryCard.isTestCard = false; //All cards entering the Inventory are regular cards
            inventoryCard.SetData(card.data);

            //PassiveCard Size Setting
            RectTransform cardRect = inventoryCard.GetComponent<RectTransform>();
            cardRect.SetParent(slots[i], false);
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(138.2412f, 196.2f);

            //Apply Effect
            inventoryCard.ApplyEffect();

            //Maximum of 7 slots if have DuplicateShopSeventhSlot card
            if (isCapacityCard) UnlockSlot();

            return true;
        }

        return false;
    }

    public void SellCard(PassiveCard card)
    {
        if (!UIManager.instance.shop.activeInHierarchy && !UIManager.instance.skillCardTest.activeInHierarchy) return;

        CardData soldData = card.data;
        Transform freedSlot = card.transform.parent;
        GameFeedback.Instance?.PlaySale(card);
        card.RemoveEffect();
        card.RecycleCard();

        //If the DuplicateShopSeventhSlot card is sold, the card in the 7th slot will be moved to the position of the sold card.
        if (soldData.effectType == CardData.EffectType.DuplicateShopSeventhSlot)
        {
            PassiveCard seventhCard = slots.Length > 6 ? slots[6].GetComponentInChildren<PassiveCard>(true) : null;
            if (seventhCard != null)
            {
                RectTransform cardRect = seventhCard.GetComponent<RectTransform>();
                cardRect.SetParent(freedSlot, false);
                cardRect.anchoredPosition = Vector2.zero;
            }

            unlockedSlots = 6;
        }


        GameManager.instance.ChangeShopGold(2);
        cardTesting.RestoreCard(soldData);
    }


    public void UnlockSlot()
    {
        unlockedSlots = Mathf.Min(7, slots.Length);
    }

    //Count the number of cards in the inventory that have the same effect
    public int CountEffect(CardData.EffectType effectType)
    {
        int count = 0;
        foreach (PassiveCard card in GetComponentsInChildren<PassiveCard>(true))
        {
            if (card.data.effectType == effectType) count++;
        }
        return count;
    }



}

