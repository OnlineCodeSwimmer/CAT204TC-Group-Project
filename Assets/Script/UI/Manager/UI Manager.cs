using System;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    //Game Object
    [Header("UI GameObject")]
    public GameObject shop;
    public GameObject skillCardTest;
    public ShopSkillCardPool shopSkillCardPool;
    public ShopSkillCardPool shopActiveCardPool;


    [Header("Sence Object GameObject")]
    public GameObject matrix;


    private void Awake()
    {
        instance = this; 
    }


    private void OnEnable()
    {
        InputSubscribe();
    }

    private void OnDisable()
    {
        InputUnsubscribe();
    }



    //Shop Related
    //Open and Close Shop
    public void OpenShop()
    {
        if (shop.activeSelf) return;

        //Player input switch
        InputSystemManager.instance.inputSystem.Player.Disable(); 
        InputSystemManager.instance.inputSystem.UI.Enable(); 

        ShowShopWithTransition(true, () =>
        {
            shop.SetActive(true);
            matrix.SetActive(false);

            //Refresh passive and active card pools.
            RefreshShopCards();
        });
    }

    public void RefreshShopCardsWithGold()
    {
        GameManager game = GameManager.instance;
        if (game.gold < 2) return;
        game.gold -= 2;
        RefreshShopCards();
    }

    private void RefreshShopCards()
    {
        shopSkillCardPool.RefreshCards();
        shopActiveCardPool.RefreshCards();
    }


    public void CloseShop()
    {
        //Player input switch
        InputSystemManager.instance.inputSystem.UI.Disable(); 
        InputSystemManager.instance.inputSystem.Player.Enable();

        //Immediately hide displayed sell prompts when closing the Shop
        foreach (PassiveCard card in PassiveCardInventory.instance.GetComponentsInChildren<PassiveCard>(true))
        {
            card.sellPanel.SetActive(false);
        }

        ActiveCardInventory activeInventory = ActiveCardInventory.instance;
        if (activeInventory != null)
        {
            foreach (ActiveCard card in activeInventory.GetComponentsInChildren<ActiveCard>(true))
                card.sellPanel.SetActive(false);
        }

        ShowShopWithTransition(false, () =>
        {
            shop.SetActive(false);
            matrix.SetActive(true);
        });
    }

    private void ShowShopWithTransition(bool opening, Action swap)
    {
        if (GameFeedback.Instance != null)
            GameFeedback.Instance.PlayShopTransition(swap, opening);
        else
            swap();
    }

    public void ShowResults(int score, bool cleared)
    {
        InputSystemManager.instance.inputSystem.Player.Disable();
        InputSystemManager.instance.inputSystem.UI.Enable();
        shop.SetActive(false);
        skillCardTest.SetActive(false);
        GameFeedback.Instance.ShowResults(score, cleared, PassiveCardInventory.instance);
    }

    //testCardPool
    private void OpenTestCardPool(InputAction.CallbackContext context)
    {
        if (!shop.activeSelf && !skillCardTest.activeSelf) return;

        shop.SetActive(!shop.activeSelf);
        skillCardTest.SetActive(!shop.activeSelf);
    }


    //Input Realted
    private void InputSubscribe()
    {
        InputSystemManager.instance.inputSystem.UI.SwitchPage.started += OpenTestCardPool;
    }

    private void InputUnsubscribe()
    {
        InputSystemManager.instance.inputSystem.UI.SwitchPage.started -= OpenTestCardPool;
    }


}
