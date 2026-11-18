using UnityEngine;

public class ActiveCardInventory : MonoBehaviour
{
    public static ActiveCardInventory instance;

    public RectTransform[] slots = new RectTransform[2];
    public ActiveCard cardPrefab;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {

    }

    public bool TryAddCard(ActiveCardData cardData)
    {
        if (cardData == null || cardPrefab == null) return false;

        //Active cards: you can only hold one copy of the same card at a time.
        foreach (ActiveCard ownedCard in GetComponentsInChildren<ActiveCard>(true))
        {
            if (ownedCard.data == cardData) return false;
        }


        int slotCount = Mathf.Min(2, slots.Length);
        for (int i = 0; i < slotCount; i++)
        {
            RectTransform slot = slots[i];
            if (slot == null || slot.GetComponentInChildren<ActiveCard>(true) != null) continue;

            ActiveCard card = PoolManager.instance.Get(cardPrefab.gameObject).GetComponent<ActiveCard>();
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.SetParent(slot, false);
            cardRect.sizeDelta = slot.rect.size;
            cardRect.anchoredPosition3D = Vector3.zero;
            card.SetData(cardData);
            card.isTestCard = false;
            return true;
        }

        return false;
    }
}
