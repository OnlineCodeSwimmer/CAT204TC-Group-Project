using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    //Game Object
    [Header("UI GameObject")]
    public GameObject shop;

    [Header("Sence Object GameObject")]
    public GameObject matrix;

    private void Awake()
    {
        instance = this; 
    }

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


}
