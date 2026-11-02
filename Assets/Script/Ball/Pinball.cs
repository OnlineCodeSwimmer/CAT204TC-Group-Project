using UnityEngine;
using UnityEngine.Splines.ExtrusionShapes;

public class Pinball : MonoBehaviour
{
    //Physics Varible
    private Rigidbody2D rb;
    private CircleCollider2D pinballCollider;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pinballCollider = GetComponent<CircleCollider2D>();

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Peg") && rb.linearVelocity.y < 0f)
        {
            GameManager.instance.NotifyPinballHitPeg(this, other);//Notification ball hits the stop pin

            BounceFromPeg(other, Random.value < 0.5f ? -1f : 1f);
        }
    }

    private void BounceFromPeg(Collider2D peg, float direction)
    {
        float duration = 0.8f;

        //Determine the landing position
        float offset = GetComponent<CircleCollider2D>().bounds.extents.y + peg.bounds.extents.y;
        Vector2 target = (Vector2)peg.transform.position + new Vector2(direction * 0.5f, -0.866f + offset);

        Vector2 gravity = Physics2D.gravity * rb.gravityScale;
        //Here use the equation of uniformly accelerated linear motion
        rb.linearVelocity = (target - rb.position) / duration - gravity * duration * 0.5f;
    }

    public void Split(Collider2D peg)
    {
        float direction = rb.linearVelocity.x > 0f ? -1f : 1f;
        Vector2 position = transform.position;
        position.x = peg.bounds.center.x + direction * (peg.bounds.extents.x + pinballCollider.bounds.extents.x + 0.01f);

        Pinball splitBall = GameManager.instance.spawnPinballPoint.SpawnExtraBall(position).GetComponent<Pinball>();
        splitBall.BounceFromPeg(peg, direction);
    }
}
