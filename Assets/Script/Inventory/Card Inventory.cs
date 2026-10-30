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
        //Each card can only have one copy
        foreach (Card ownedCard in GetComponentsInChildren<Card>(true))
        {
            if (ownedCard.data == card.data) return false;
        }


        for (int i = 0; i < Mathf.Min(unlockedSlots, slots.Length); i++)
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
            return true;
        }

        return false;
    }

    public void SellCard(Card card)
    {
        card.RemoveEffect();
        card.RecycleCard();
        GameManager.instance.AddGold(2);
        cardTesting.RestoreCard(card.data);
    }


    public void UnlockSlot()
    {
        if (unlockedSlots >= slots.Length) return;

        unlockedSlots++;
    }
}

