using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using UnityEngine.InputSystem;

public class InputSystemManager : MonoBehaviour
{
    public PlayerInputSystem InputSystem;
    public static InputSystemManager instance;

    private void Awake()
    {
        InputSystem =new PlayerInputSystem();
    }

    private void OnEnable()
    {
        UIInputSubscribe();
        InputSystem.Player.Enable();
    }

    private void OnDisable()
    {
        UIInputUnSubscribe();
        InputSystem.Player.Disable();
    }


    private void InitInstance()
    {
        if (instance != this)
        {
            Destroy(this);
        }
        else if(instance == null) 
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
    }


    private void UIInputSubscribe()
    {

    }



    private void UIInputUnSubscribe()
    {
    }
}

