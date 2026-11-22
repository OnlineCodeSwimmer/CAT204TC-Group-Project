using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopSkillCardPool : MonoBehaviour
{
    [Header("PassiveCard Pool Setting")]
    public CardData[] cardDatas = new CardData[0];
    public GameObject cardPrefab;

    [Header("Refresh Flip")]
    [SerializeField, Min(0.05f)] private float flipDuration = 0.2f;
    [SerializeField, Range(0f, 0.1f)] private float centerFlipDelay = 0.045f;

    [Header("Centered Card Layout")]
    [SerializeField, Min(0f)] private float cardSpacing = 272f;
    [SerializeField] private float centerCardHeight = 18f;

    private bool refreshing;

    private void Awake()
    {
        // Positions are assigned explicitly so the whole hand can be centered and
        // the middle card can sit slightly above the side cards.
        HorizontalLayoutGroup layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;
    }

    public void RefreshCardsWithGold()
    {
        if (refreshing || GameManager.instance.gold < 2) return;

        GameManager.instance.gold -= 2;
        RefreshCards();
    }

    public void RefreshCards()
    {
        if (refreshing) return;
        StartCoroutine(RefreshCardsRoutine());
    }

    private IEnumerator RefreshCardsRoutine()
    {
        refreshing = true;

        List<PassiveCard> oldCards = GetShopCardsByHorizontalPosition();
        foreach (PassiveCard card in oldCards)
            card.SetRefreshAnimation(true);

        yield return AnimateFlip(oldCards, 0f, 90f);

        foreach (PassiveCard card in oldCards)
        {
            card.SetRefreshRotation(0f);
            card.RecycleCard();
        }

        List<PassiveCard> newCards = SpawnCards();
        LayoutCards(newCards);
        foreach (PassiveCard card in newCards)
        {
            card.SetRefreshRotation(90f);
            card.SetRefreshAnimation(true);
        }

        yield return AnimateFlip(newCards, 90f, 0f);

        foreach (PassiveCard card in newCards)
        {
            card.SetRefreshRotation(0f);
            card.SetRefreshAnimation(false);
        }

        refreshing = false;
    }

    private IEnumerator AnimateFlip(List<PassiveCard> cards, float fromAngle, float toAngle)
    {
        float duration = Mathf.Max(0.05f, flipDuration);
        float totalDuration = duration + GetCardDelay(cards.Count);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                float localTime = Mathf.Clamp01((elapsed - GetDelay(i, cards.Count)) / duration);
                float easedTime = localTime * localTime * (3f - 2f * localTime);
                cards[i].SetRefreshRotation(Mathf.Lerp(fromAngle, toAngle, easedTime));
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (PassiveCard card in cards)
            card.SetRefreshRotation(toAngle);
    }

    private float GetDelay(int index, int count)
    {
        return count == 3 && index == 1 ? centerFlipDelay : 0f;
    }

    private float GetCardDelay(int count)
    {
        return count == 3 ? centerFlipDelay : 0f;
    }

    private List<PassiveCard> GetShopCardsByHorizontalPosition()
    {
        List<PassiveCard> cards = new List<PassiveCard>(GetComponentsInChildren<PassiveCard>(true));
        cards.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return cards;
    }

    private List<PassiveCard> SpawnCards()
    {
        CardInventory inventory = CardInventory.instance;
        bool hasCapacityCard = inventory.CountEffect(CardData.EffectType.DuplicateShopSeventhSlot) > 0;
        List<CardData> shuffledCards = new List<CardData>();

        foreach (CardData data in cardDatas)
        {
            if (data == null || shuffledCards.Contains(data)) continue;
            if (hasCapacityCard && data.effectType == CardData.EffectType.DuplicateShopSeventhSlot) continue;
            shuffledCards.Add(data);
        }

        // If there is no DuplicateShopSeventhSlot card, owned cards are removed
        // from the refresh pool so an unavailable duplicate is never offered.
        if (!hasCapacityCard)
        {
            foreach (PassiveCard ownedCard in inventory.GetComponentsInChildren<PassiveCard>(true))
                shuffledCards.Remove(ownedCard.data);
        }

        Shuffle(shuffledCards);

        List<PassiveCard> cards = new List<PassiveCard>();
        for (int i = 0; i < Mathf.Min(3, shuffledCards.Count); i++)
        {
            PassiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<PassiveCard>();
            card.transform.SetParent(transform, false);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = cardPrefab.transform.localScale;
            card.isTestCard = false;
            card.SetData(shuffledCards[i]);
            cards.Add(card);
        }

        return cards;
    }

    private void LayoutCards(List<PassiveCard> cards)
    {
        if (cards.Count == 0) return;

        Vector2 cardSize = cardPrefab.GetComponent<RectTransform>().sizeDelta;
        float spacing = cardSpacing > 0f ? cardSpacing : cardSize.x + 30f;

        for (int i = 0; i < cards.Count; i++)
        {
            RectTransform rect = (RectTransform)cards[i].transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = cardSize;

            float x = (i - (cards.Count - 1) * 0.5f) * spacing;
            float y = cards.Count == 3 && i == 1 ? centerCardHeight : 0f;
            rect.anchoredPosition = new Vector2(x, y);
        }
    }

    // Knuth-Fisher-Yates Shuffle
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
