using UnityEngine;

// [Jinyang.Li] Runtime index for the icon_0..icon_20 sub-sprites in icon.png.
[CreateAssetMenu(fileName = "TarotIconLibrary", menuName = "UI/Tarot Icon Library")]
public class TarotIconLibrary : ScriptableObject
{
    public Sprite[] icons = new Sprite[0];
}
