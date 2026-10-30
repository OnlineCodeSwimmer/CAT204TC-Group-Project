using UnityEngine;

public class Pinball : MonoBehaviour
{
    //Physics Varible
    private Rigidbody2D rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Dot") && rb.linearVelocity.y < 0f)
        {
            GameManager.instance.NotifyPinballHitPeg();//Notification ball hits the stop pin

            float direction = Random.value < 0.5f ? -1f : 1f;
            float duration = 0.8f;

            //Determine the landing position
            float offset = GetComponent<CircleCollider2D>().bounds.extents.y + other.bounds.extents.y;
            Vector2 target = (Vector2)other.transform.position + new Vector2(direction * 0.5f, -0.866f + offset);

            Vector2 gravity = Physics2D.gravity * rb.gravityScale;
            //Here use the equation of uniformly accelerated linear motion
            rb.linearVelocity = (target - rb.position) / duration - gravity * duration * 0.5f;
        }
    }
}
