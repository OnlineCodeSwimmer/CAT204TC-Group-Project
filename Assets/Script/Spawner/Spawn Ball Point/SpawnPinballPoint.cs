using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.InputSystem;

public class SpawnPinballPoint : MonoBehaviour
{
    //Generate Pinball Setting"
    [Header("Generate Pinball Setting")]
    public GameObject ballPrefab;
    public float generateBallInterval=0.1f;

    //Generate Pinball Check Varible
    private bool spawnIsWait;
    private bool spawnIsPressed;

    //Input System
    private PlayerInputSystem input;

    private void Awake()
    {
        input = InputSystemManager.instance.inputSystem;
    }

    private void OnEnable()
    {
        InputSubscribe();
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();
        StopAllCoroutines();
        spawnIsWait = false;
        InputUnsubscribe();
    }

    private void Update()
    {
        TrySpawnBall();
    }


    //Spawn Area
    private void TrySpawnBall() //Use for click generation
    {
        if (!spawnIsPressed || spawnIsWait) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!GameManager.instance.TryUseBall()) return;

        SpawnBall(transform.position);
        StartCoroutine(SpawnCooldown());
    }

    private GameObject SpawnBall(Vector2 position)
    {
        GameObject ball = PoolManager.instance.Get(ballPrefab);
        ball.transform.SetParent(null);
        ball.transform.position = transform.position;

        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        GameManager.instance.NotifyPinballSpawned();//Notify Pinball Spawn
        return ball;
    }

    public void SpawnExtraBall()
    {
        SpawnExtraBall(transform.position);
    }

    public GameObject SpawnExtraBall(Vector2 position)
    {
        GameManager.instance.activePinballs++;
        return SpawnBall(position);
    }

    private void StartSpawnBall(InputAction.CallbackContext context)
    {
        spawnIsPressed = true;
    }

    private void StopSpawnBall(InputAction.CallbackContext context)
    {
        spawnIsPressed = false;
    }

    private IEnumerator SpawnCooldown()
    {
        spawnIsWait = true;
        yield return new WaitForSeconds(generateBallInterval);
        spawnIsWait = false;
    }

    //Input related
    private void InputSubscribe()
    {
        input.Player.Click.started += StartSpawnBall;
        input.Player.Click.canceled += StopSpawnBall;
    }

    private void InputUnsubscribe()
    {
        input.Player.Click.started -= StartSpawnBall;
        input.Player.Click.canceled -= StopSpawnBall;
    }


}
