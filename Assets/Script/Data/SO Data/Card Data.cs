using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Shop Data/Card Data")]
public class CardData : ScriptableObject
{
    //Use for Card Skill
    public enum EffectType { None, AddBall }



    public string cardName;
    [TextArea] public string description;
    public Sprite cardSprite;
    public int price;

    //Card Skill Related
    public EffectType effectType;
    public int effectValue = 1;

}