using UnityEngine;

public class ScoreSlot : MonoBehaviour
{
    //Score Setting
    [Header("Socre Setting")]
    public float multiplier;

    public enum SlotPosition { First, Second, Third, Fourth, Center } //Side refers to the two on the far right and the two on the far left, center is the one in the middle, and the rest are normal
    public SlotPosition position;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Pinball"))
        {
            if (other.GetComponent<Pinball>().IsTemporary)
                // [Jinyang.Li] Temporary-ball despawn effect.
                GameFeedback.Instance?.Burst(CardEffectVisual.TemporaryDespawn, other.transform.position);
            GameManager.instance.AddScore(this);
            // [Jinyang.Li] Triggers random Effect_6/7 and fixed Effect_8.
            GameManager.instance.NotifyPinballEnteredSlot(this); //Notify the ball is in the hole


            other.attachedRigidbody.linearVelocity = Vector2.zero;
            other.attachedRigidbody.angularVelocity = 0f;

            PoolManager.instance.Recycle(other.gameObject);
            GameManager.instance.BallReturned(); 
        }
    }
}
