using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDGeneralPackage : MonoBehaviour
{
    //HUD Type
    public enum HUDType
    { 
    Score,
    TargetScore,
    Round,
    Ball,
    Gold
    }

    [Header("HUD Type")]
    public HUDType hUDType;


    //UI Varible
    private TextMeshProUGUI text;
  

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    public void Update()
    {
        realTimeDisplay();
    }

    private void realTimeDisplay()
    {
        switch(hUDType)
        {
            case HUDType.Score:
                text.text = string.Format("{0}", (int)GameManager.instance.score);
                break;

            case HUDType.TargetScore:
                text.text = string.Format("{0}", GameManager.instance.currentTargetScore);
                break;

            case HUDType.Round:
                text.text = string.Format("{0}/{1}", GameManager.instance.currentRound, GameManager.instance.rounds);
                break;

            case HUDType.Ball:
                text.text = string.Format("{0}", GameManager.instance.ballNumber);
                break;

            case HUDType.Gold:
                text.text = string.Format("{0}", GameManager.instance.gold);
                break;

        }
    }
}
