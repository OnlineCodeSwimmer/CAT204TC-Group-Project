using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;
using UnityEngine.InputSystem;

public class InputSystemManager : MonoBehaviour
{
    public PlayerInputSystem inputSystem;
    public static InputSystemManager instance;


    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
            inputSystem = new PlayerInputSystem();
        }
        else if(instance != this) 
        {
            Destroy(gameObject);
        }
    }




}

