using System;
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

    private bool shopTransitioning;


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
        if (shop.activeSelf || shopTransitioning) return;

        //Player input switch
        InputSystemManager.instance.inputSystem.Player.Disable(); 
        InputSystemManager.instance.inputSystem.UI.Enable(); 

        ShowShopWithTransition(true, FinishOpeningShop);
    }

    private void FinishOpeningShop()
    {
        shop.SetActive(true);
        matrix.SetActive(false);
        RefreshShopCards();
    }

    public void RefreshShopCardsWithGold()
    {
        GameManager game = GameManager.instance;
        if (!shop.activeSelf || shopTransitioning || IsCardRefreshInProgress() || game.gold < 2) return;
        game.ChangeShopGold(-2);
        RefreshShopCards();
    }

    private bool IsCardRefreshInProgress()
    {
        return shopSkillCardPool.IsRefreshing || shopActiveCardPool.IsRefreshing;
    }

    private void RefreshShopCards()
    {
        shopSkillCardPool.RefreshCards();
        shopActiveCardPool.RefreshCards();
    }


    public void CloseShop()
    {
        //Immediately hide displayed sell prompts when closing the Shop
        foreach (PassiveCard card in PassiveCardInventory.instance.GetComponentsInChildren<PassiveCard>(true))
        {
            card.sellPanel.SetActive(false);
        }

        foreach (ActiveCard card in ActiveCardInventory.instance.GetComponentsInChildren<ActiveCard>(true))
        {
            card.sellPanel.SetActive(false);
        }

        ShowShopWithTransition(false, FinishClosingShop);
    }

    private void FinishClosingShop()
    {
        shop.SetActive(false);
        matrix.SetActive(true);
        InputSystemManager.instance.inputSystem.UI.Disable();
        InputSystemManager.instance.inputSystem.Player.Enable();
    }

    private void ShowShopWithTransition(bool opening, Action swap)
    {
        shopTransitioning = true;
        if (GameFeedback.Instance != null)
        {
            GameFeedback.Instance.PlayShopTransition(swap, opening, FinishShopTransition);
        }
        else
        {
            swap();
            FinishShopTransition();
        }
    }

    private void FinishShopTransition()
    {
        shopTransitioning = false;
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
        if (shopTransitioning || (!shop.activeSelf && !skillCardTest.activeSelf)) return;
        if (shop.activeSelf && IsCardRefreshInProgress()) return;

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
