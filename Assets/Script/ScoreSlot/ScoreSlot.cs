using UnityEngine;

public class ScoreSlot : MonoBehaviour
{
    //Score Setting
    [Header("Socre Setting")]
    public float multiplier;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            GameManager.instance.AddScore(multiplier);

            other.attachedRigidbody.linearVelocity = Vector2.zero;
            other.attachedRigidbody.angularVelocity = 0f;

            PoolManager.instance.Recycle(other.gameObject);
            GameManager.instance.BallReturned(); 
        }
    }
}
