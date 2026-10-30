using UnityEngine;

public class ScoreSlot : MonoBehaviour
{
    //Score Setting
    [Header("Socre Setting")]
    public float multiplier;

    public enum SlotPosition { Normal, Side, Center } //Side refers to the two on the far right and the two on the far left, center is the one in the middle, and the rest are normal
    public SlotPosition position;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pinball"))
        {
            GameManager.instance.AddScore(this);
            GameManager.instance.NotifyPinballEnteredSlot(); //Notify the ball is in the hole


            other.attachedRigidbody.linearVelocity = Vector2.zero;
            other.attachedRigidbody.angularVelocity = 0f;

            PoolManager.instance.Recycle(other.gameObject);
            GameManager.instance.BallReturned(); 
        }
    }
}
