using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class SpawnBallPoint : MonoBehaviour
{
    //Generate Ball Setting"
    [Header("Generate Ball Setting")]
    public GameObject ballPrefab;
    public float generateBallInterval=0.1f;

    //Generate Ball Check Varible
    private bool spawnIsWait;

    //Input System
    private PlayerInputSystem input;

    private void Awake()
    {
        input = InputSystemManager.instance.inputSystem;
    }

    private void OnEnable()
    {
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();
        StopAllCoroutines();
        spawnIsWait = false;
    }

    private void Update()
    {
        TrySpawnBall();

    }


    //Spawn Area
    private void TrySpawnBall() 
    {
        if (!input.Player.Click.IsPressed()|| spawnIsWait) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (!GameManager.instance.TryUseBall()) return;


        GameObject ball = PoolManager.instance.Get(ballPrefab);

        ball.transform.SetParent(null);
        ball.transform.position = transform.position;

        StartCoroutine(SpawnCooldown());
    }

    private IEnumerator SpawnCooldown()
    {
        spawnIsWait = true;
        yield return new WaitForSeconds(generateBallInterval);
        spawnIsWait = false;
    }

}
