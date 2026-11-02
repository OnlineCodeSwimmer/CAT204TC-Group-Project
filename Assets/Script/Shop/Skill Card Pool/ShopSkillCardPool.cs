using System.Collections.Generic;
using UnityEngine;

public class ShopSkillCardPool : MonoBehaviour
{
    [Header("Card Pool Setting")]
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

        foreach (Card card in GetComponentsInChildren<Card>(true)) card.RecycleCard();

        List<CardData> shuffledCards = new List<CardData>();

        foreach (CardData data in cardDatas)
        {
            if (data != null && !shuffledCards.Contains(data)) shuffledCards.Add(data);
        }

        foreach (Card ownedCard in CardInventory.instance.GetComponentsInChildren<Card>(true))
        {
            shuffledCards.Remove(ownedCard.data);
        }

        Shuffle(shuffledCards);

        for (int i = 0; i < Mathf.Min(3, shuffledCards.Count); i++)
        {
            Card card = PoolManager.instance.Get(cardPrefab).GetComponent<Card>();
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
