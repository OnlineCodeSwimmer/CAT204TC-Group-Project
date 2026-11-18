using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ShopPassiveCardPool : MonoBehaviour
{
    [Header("PassiveCard Pool Setting")]
    public PassiveCardData[] cardDatas = new PassiveCardData[0];
    public GameObject cardPrefab;

    public void RefreshCardsWithGold()
    {
        if (GameManager.instance.gold < 2) return;

        GameManager.instance.gold -= 2;
        RefreshCards();
    }

    public void RefreshCards()
    {
        foreach (PassiveCard card in GetComponentsInChildren<PassiveCard>(true))
        {
            card.RecycleCard();
        }

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

        for (int i = 0; i < Mathf.Min(3, shuffledCards.Count); i++)
        {
            PassiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<PassiveCard>();
            card.transform.SetParent(transform);
            card.isTestCard = false;
            card.SetData(shuffledCards[i]);
        }
    }



    //Knuth¨CDurstenfeld Shuffle
    private void Shuffle(List<PassiveCardData> cards)
    {
        for (int i = 0; i < cards.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, cards.Count);
            PassiveCardData temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
