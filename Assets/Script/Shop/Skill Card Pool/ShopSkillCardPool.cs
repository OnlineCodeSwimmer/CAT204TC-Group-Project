using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ShopSkillCardPool : MonoBehaviour
{
    //Pool Type
    public enum PoolType { Passive, Active }
    [Header("Pool Type")]
    public PoolType poolType;



    [Header("Passive Card Pool Setting")]
    public PassiveCardData[] cardDatas = new PassiveCardData[0];
    public GameObject cardPrefab;
    public int passiveOfferCount = 3;

    [Header("Active Card Pool Setting")]
    public ActiveCardData[] activeCardDatas = new ActiveCardData[0];
    public int activeOfferCount = 2;




    public void RefreshCards()
    {
        if (poolType == PoolType.Active)
        {
            RefreshActiveCards();
        }
        else
        {
            RefreshPassiveCards();
        }
    }



     private void RefreshPassiveCards()
    {
        //Recycle Card
        foreach (PassiveCard card in GetComponentsInChildren<PassiveCard>(true))
        {
            card.RecycleCard();
        }
        // Filter Cards Before Shuffling
        PassiveCardInventory inventory = PassiveCardInventory.instance;
        bool hasCapacityCard = inventory.CountEffect(PassiveCardData.EffectType.DuplicateShopSeventhSlot) > 0;
        List<PassiveCardData> shuffledCards = new List<PassiveCardData>();

        foreach (PassiveCardData data in cardDatas)
        {
            if (data == null || shuffledCards.Contains(data)) continue;
            if (hasCapacityCard && data.effectType == PassiveCardData.EffectType.DuplicateShopSeventhSlot) continue;
            shuffledCards.Add(data);
        }

        //If there is no DuplicateShopSeventhSlot card, duplicate cards will not appear in the refresh pool.
        if (!hasCapacityCard)
        {
            foreach (PassiveCard ownedCard in inventory.GetComponentsInChildren<PassiveCard>(true))
            {
                shuffledCards.Remove(ownedCard.data);
            }
        }

        Shuffle(shuffledCards);

        for (int i = 0; i < Mathf.Min(passiveOfferCount, shuffledCards.Count); i++)
        {
            PassiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<PassiveCard>();
            card.transform.SetParent(transform);
            card.isTestCard = false;
            card.SetData(shuffledCards[i]);
        }
    }






    //Active Card Related
    private void RefreshActiveCards()
    {
        //Recycle Card
        foreach (ActiveCard card in GetComponentsInChildren<ActiveCard>(true))
        {
            card.RecycleCard();
        }
        // Filter Cards Before Shuffling
        List<ActiveCardData> cards = new List<ActiveCardData>();
        foreach (ActiveCardData data in activeCardDatas)
        {
            if (!cards.Contains(data)) cards.Add(data);
        }

        foreach (ActiveCard ownedCard in ActiveCardInventory.instance.GetComponentsInChildren<ActiveCard>(true))
        {
            cards.Remove(ownedCard.data);
        }

        //Shuffle Card
        Shuffle(cards);
        for (int i = 0; i < Mathf.Min(activeOfferCount, cards.Count); i++)
        {
            ActiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<ActiveCard>();
            card.transform.SetParent(transform, false);
            card.isTestCard = false;
            card.SetData(cards[i]);
        }
    }


        //Knuth¨CDurstenfeld Shuffle
        private void Shuffle<T>(List<T> cards)
    {
        for (int i = 0; i < cards.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, cards.Count);
            T temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
