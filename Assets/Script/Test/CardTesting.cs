using UnityEngine;
using UnityEngine.UI;

public class CardTesting : MonoBehaviour
{
    [Header("PassiveCard Pool Setting")]
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
            PassiveCard card = PoolManager.instance.Get(cardPrefab).GetComponent<PassiveCard>();
            card.transform.SetParent(testCardPool, false);
            card.transform.localScale = Vector3.one;
            card.isTestCard = true;
            card.SetData(data);
        }
    }

    public void RestoreCard(CardData cardData)
    {
        foreach (PassiveCard card in testCardPool.GetComponentsInChildren<PassiveCard>(true))
        {
            if (card.data != cardData) continue;

            card.cardImage.GetComponent<Image>().color = Color.white;
            card.GetComponent<Button>().interactable = true;
            card.enabled = true;
        }
    }

    //PassiveCard Test
    private void Test2()
    {
        Debug.Log("Update");
        Debug.Log("123");
        Debug.Log("XD");
        Debug.Log("321");
    }

}
