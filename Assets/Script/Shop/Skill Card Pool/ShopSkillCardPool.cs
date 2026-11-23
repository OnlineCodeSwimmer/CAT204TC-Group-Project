using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopSkillCardPool : MonoBehaviour
{
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

    [Header("Refresh Flip")]
    [SerializeField, Min(0.05f)] private float flipDuration = 0.2f;
    [SerializeField, Range(0f, 0.1f)] private float centerFlipDelay = 0.045f;

    [Header("Centered Card Layout")]
    [SerializeField, Min(0f)] private float cardSpacing = 272f;
    [SerializeField] private float centerCardHeight = 18f;

    private bool refreshing;

    private void Awake()
    {
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

        if (poolType == PoolType.Active)
            RefreshActiveCards();
        else
            StartCoroutine(RefreshPassiveCardsRoutine());
    }

    private IEnumerator RefreshPassiveCardsRoutine()
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

        List<PassiveCard> newCards = SpawnPassiveCards();
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

    private List<PassiveCard> SpawnPassiveCards()
    {
        PassiveCardInventory inventory = PassiveCardInventory.instance;
        bool hasCapacityCard = inventory.CountEffect(PassiveCardData.EffectType.DuplicateShopSeventhSlot) > 0;
        List<PassiveCardData> shuffledCards = new List<PassiveCardData>();

        foreach (PassiveCardData data in cardDatas)
        {
            if (data == null || shuffledCards.Contains(data)) continue;
            if (hasCapacityCard && data.effectType == PassiveCardData.EffectType.DuplicateShopSeventhSlot) continue;
            shuffledCards.Add(data);
        }

        if (!hasCapacityCard)
        {
            foreach (PassiveCard ownedCard in inventory.GetComponentsInChildren<PassiveCard>(true))
                shuffledCards.Remove(ownedCard.data);
        }

        Shuffle(shuffledCards);

        List<PassiveCard> cards = new List<PassiveCard>();
        for (int i = 0; i < Mathf.Min(passiveOfferCount, shuffledCards.Count); i++)
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

    private void RefreshActiveCards()
    {
        refreshing = true;

        foreach (ActiveCard card in GetComponentsInChildren<ActiveCard>(true))
            card.RecycleCard();

        List<ActiveCardData> cards = new List<ActiveCardData>();
        foreach (ActiveCardData data in activeCardDatas)
        {
            if (data != null && !cards.Contains(data)) cards.Add(data);
        }

        ActiveCardInventory inventory = ActiveCardInventory.instance;
        if (inventory != null)
        {
            foreach (ActiveCard ownedCard in inventory.GetComponentsInChildren<ActiveCard>(true))
                cards.Remove(ownedCard.data);
        }

        Shuffle(cards);
        for (int i = 0; i < Mathf.Min(activeOfferCount, cards.Count); i++)
        {
            ActiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<ActiveCard>();
            card.transform.SetParent(transform, false);
            card.isTestCard = false;
            card.SetData(cards[i]);
        }

        refreshing = false;
    }

    private static void Shuffle<T>(List<T> cards)
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
