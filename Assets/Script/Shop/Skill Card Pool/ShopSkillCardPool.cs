using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ShopSkillCardPool : MonoBehaviour
{
    [Header("PassiveCard Pool Setting")]
    public CardData[] cardDatas = new CardData[0];
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

        CardInventory inventory = CardInventory.instance;
        bool hasCapacityCard = inventory.CountEffect(CardData.EffectType.DuplicateShopSeventhSlot) > 0;
        List<CardData> shuffledCards = new List<CardData>();

        foreach (CardData data in cardDatas)
        {
            if (data == null || shuffledCards.Contains(data)) continue;
            if (hasCapacityCard && data.effectType == CardData.EffectType.DuplicateShopSeventhSlot) continue;
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
    private void Shuffle(List<CardData> cards)
    {
        for (int i = 0; i < cards.Count - 1; i++)
        {
            int randomIndex = Random.Range(i, cards.Count);
            CardData temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
}
