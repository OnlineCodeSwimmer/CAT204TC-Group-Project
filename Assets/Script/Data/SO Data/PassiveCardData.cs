using UnityEngine;

[CreateAssetMenu(fileName = "PassiveCardData", menuName = "Card Data/PassiveCard Data")]
public class PassiveCardData : ScriptableObject
{
    //Use for PassiveCard Skill
    public enum EffectType { 
        None, 
        AddOnePinball,
        AddPinballScore, 
        HalfSidesCenter075,
        Hit40AddOnePinball,
        SlotMultiplier15,
        FirstPinballTenTimes,
        PegHitScore,
        RandomDoubleSlot,
        DoubleGoldTargetUp,
        RethrowPinball,
        ThirdSlotGrowth,
        RoundEndGoldChance,
        PegHitExtraPinball,
        Spawn16AddOneGold,
        AllScoreMultiplier15,
        FirstSlotAddOnePinball,
        PegHitSplitPinball,
        RandomLeftBounceChance,
        UnusedThrowBonus,
        DuplicateShopSeventhSlot,
    }



    public string cardName;
    [TextArea] public string description;
    public Sprite cardSprite;
    public int price;

    //PassiveCard Skill Related
    public EffectType effectType;

}