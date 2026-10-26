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

    private void Awake()
    {
        instance = this;
    }


    public bool TryAddCard(Card card)
    {
        for (int i = 0; i < Mathf.Min(unlockedSlots, slots.Length); i++)
        {
            if (slots[i].GetComponentInChildren<Card>(true) != null) continue;

            // 修改：替换原第 23～24 行，从对象池获取副本
            Card inventoryCard = PoolManager.instance.Get(cardPrefab.gameObject).GetComponent<Card>();
            inventoryCard.SetData(card.data);

            RectTransform cardRect = inventoryCard.GetComponent<RectTransform>();
            cardRect.SetParent(slots[i]);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(163f, 241f);

            inventoryCard.GetComponent<Button>().interactable = false;
            return true;
        }

        return false;
    }


    public void UnlockSlot()
    {
        if (unlockedSlots >= slots.Length) return;

        unlockedSlots++;
    }
}

