using UnityEngine;

[CreateAssetMenu(fileName = "ActiveCardData", menuName = "Card Data/Active Card Data")]
public class ActiveCardData : ScriptableObject
{
    public enum EffectType
    {
        FirstPinballTwentyTimes,
        AddRandomPinballs,
        EmperorScoreBoost,
        RandomLeftBounceChance,
        DoublePinballValue,
        AddThrowChance,
        RethrowPinballTenPercent,
        GainDoubleCurrentGold,
        DoubleScoreCostThrowChance,
        FirstSlotMultiplier15,
        DestroyLastPassiveCardAddPinballs,
        SpendAllGoldForPinballs,
        ReduceValueAddFourPinballs,
        ReduceAllTargetScores5Percent,
        AddTemporaryPinballValue30,
    }

    public string cardName;
    [TextArea] public string description;
    public Sprite cardSprite;
    public EffectType effectType;
}

