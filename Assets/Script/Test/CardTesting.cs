using UnityEngine;
using UnityEngine.UI;

public class CardTesting : MonoBehaviour
{
    [Header("PassiveCard Pool Setting")]
    public PassiveCardData[] passiveCardDatas;
    public ActiveCardData[] activeCardDatas;
    public GameObject PassivecardPrefab;
    public GameObject ActivecardPrefab;
    public Transform testCardPool;

    private void Start()
    {
        GeneratePassiveCards();
        GenerateActiveCards();
    }

    private void GeneratePassiveCards()
    {
        foreach (PassiveCardData data in passiveCardDatas)
        {
            PassiveCard card = PoolManager.instance.Get(PassivecardPrefab).GetComponent<PassiveCard>();
            card.transform.SetParent(testCardPool, false);
            card.transform.localScale = Vector3.one;
            card.isTestCard = true;
            card.SetData(data);
        }
    }

    private void GenerateActiveCards()
    {
        ActiveCardInventory inventory = ActiveCardInventory.instance;

        foreach (ActiveCardData cardData in activeCardDatas)
        {
            ActiveCard card = PoolManager.instance.Get(ActivecardPrefab).GetComponent<ActiveCard>();
            card.transform.SetParent(testCardPool, false);
            card.SetData(cardData);
            card.isTestCard = true;
            card.SetData(cardData);
        }
    }

        public void RestoreCard(PassiveCardData cardData)
    {
        foreach (PassiveCard card in testCardPool.GetComponentsInChildren<PassiveCard>(true))
        {
            if (card.data != cardData) continue;

            card.cardImage.GetComponent<Image>().color = Color.white;
            card.GetComponent<Button>().interactable = true;
            card.enabled = true;
        }
    }



}
