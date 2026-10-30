using UnityEngine;
using UnityEngine.UI;

public class CardTesting : MonoBehaviour
{
    [Header("Card Pool Setting")]
    public CardData[] cardDatas;
    public GameObject cardPrefab;
    public Transform testCardPool;

    private void Start()
    {
        GenerateCards();
    }

    private void GenerateCards()
    {
        foreach (CardData data in cardDatas)
        {
            Card card = PoolManager.instance.Get(cardPrefab).GetComponent<Card>();
            card.transform.SetParent(testCardPool, false);
            card.transform.localScale = Vector3.one;
            card.isTestCard = true;
            card.SetData(data);
        }
    }

    public void RestoreCard(CardData cardData)
    {
        foreach (Card card in testCardPool.GetComponentsInChildren<Card>(true))
        {
            if (card.data != cardData) continue;

            card.cardImage.GetComponent<Image>().color = Color.white;
            card.GetComponent<Button>().interactable = true;
            card.enabled = true;
        }
    }

}
