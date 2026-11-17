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

    //Open and Close Shop
    public void OpenShop()
    {
        //Player input switch
        InputSystemManager.instance.inputSystem.Player.Disable(); 
        InputSystemManager.instance.inputSystem.UI.Enable(); 

        shop.SetActive(true);
        matrix.SetActive(false);

        //Refresh PassiveCard Pool
        shopSkillCardPool.RefreshCards();

    }

    public void CloseShop()
    {
        //Player input switch
        InputSystemManager.instance.inputSystem.UI.Disable(); 
        InputSystemManager.instance.inputSystem.Player.Enable();

        //Immediately hide displayed sell prompts when closing the Shop
        foreach (PassiveCard card in CardInventory.instance.GetComponentsInChildren<PassiveCard>(true))
        {
            card.sellPanel.SetActive(false);
        }

        shop.SetActive(false);
        matrix.SetActive(true);
    }

    public void ShowResults(int score, bool cleared)
    {
        InputSystemManager.instance.inputSystem.Player.Disable();
        shop.SetActive(false);
        skillCardTest.SetActive(false);
        GameFeedback.Instance.ShowResults(score, cleared, CardInventory.instance);
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
