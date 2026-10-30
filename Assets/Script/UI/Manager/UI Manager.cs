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
    }

    public void CloseShop()
    {
        //Player input switch
        InputSystemManager.instance.inputSystem.UI.Disable(); 
        InputSystemManager.instance.inputSystem.Player.Enable(); 

        shop.SetActive(false);
        matrix.SetActive(true);
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
