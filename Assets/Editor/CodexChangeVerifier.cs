using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CodexChangeVerifier
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string ResultPath = "Logs/CodexChangeVerification.txt";
    private const string ActiveKey = "CodexChangeVerifier.Active";
    private static readonly List<string> Failures = new List<string>();

    private static double playStartedAt;
    private static bool enteredPlayMode;
    private static bool shopOpened;
    private static bool secondRefreshStarted;
    private static ActiveCardData purchasedActiveData;
    private static bool finished;

    static CodexChangeVerifier()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        Application.logMessageReceived += CaptureLog;
        EditorApplication.update += Monitor;
    }

    [MenuItem("Tools/Codex/Verify Card Effect Changes")]
    public static void Run()
    {
        Failures.Clear();
        enteredPlayMode = false;
        shopOpened = false;
        secondRefreshStarted = false;
        purchasedActiveData = null;
        finished = false;
        SessionState.SetBool(ActiveKey, true);
        Application.logMessageReceived -= CaptureLog;
        Application.logMessageReceived += CaptureLog;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.update -= Monitor;
        EditorApplication.update += Monitor;
        EditorApplication.EnterPlaymode();
    }

    private static void Monitor()
    {
        if (finished || !EditorApplication.isPlaying) return;

        if (!enteredPlayMode)
        {
            enteredPlayMode = true;
            playStartedAt = EditorApplication.timeSinceStartup;
            return;
        }

        double elapsed = EditorApplication.timeSinceStartup - playStartedAt;
        if (!shopOpened && elapsed > 0.75d)
        {
            shopOpened = true;
            UIManager.instance.OpenShop();
            return;
        }

        if (shopOpened && !secondRefreshStarted && elapsed > 1.6d)
        {
            secondRefreshStarted = true;
            UIManager ui = UIManager.instance;
            BuyActiveCardOnShelf();
            int goldBefore = GameManager.instance.gold;
            Button refresh = GameObject.Find("Refresh").GetComponent<Button>();
            refresh.onClick.Invoke();
            int goldAfterFirstClick = GameManager.instance.gold;
            refresh.onClick.Invoke();
            Require(goldAfterFirstClick == GameManager.instance.gold,
                "Refresh charged gold again while the first flip animation was still running.");
            Require(goldBefore - goldAfterFirstClick == 2,
                "The first refresh did not charge exactly two gold.");
            Require(ui.shopSkillCardPool.IsRefreshing && ui.shopActiveCardPool.IsRefreshing,
                "The shared button did not start both flip animations.");
            return;
        }

        if (!shopOpened || elapsed < 2.75d) return;

        finished = true;
        Validate();
        EditorApplication.update -= Monitor;
        Application.logMessageReceived -= CaptureLog;
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.ExitPlaymode();

        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath) ?? "Logs");
        string report = Failures.Count == 0
            ? "PASS\nVisual effects, score feedback, shop layout, active purchases and shared refresh were verified.\n"
            : "FAIL\n" + string.Join("\n", Failures) + "\n";
        File.WriteAllText(ResultPath, report);
        Debug.Log("[CodexChangeVerifier]\n" + report);
        EditorApplication.Exit(Failures.Count == 0 ? 0 : 1);
    }

    private static void Validate()
    {
        Require(GameFeedback.Instance != null, "GameFeedback.Instance is missing.");
        Require(UIManager.instance != null, "UIManager.instance is missing.");
        Require(UIManager.instance?.shopSkillCardPool != null, "ShopSkillCardPool is missing.");
        Require(UIManager.instance?.shopActiveCardPool != null, "Active-card shop pool is missing.");
        Require(GameFeedback.Instance?.GetComponent<Canvas>() != null, "GameFeedback Canvas is missing.");
        Require(Resources.Load<Material>("SkillEffects/Effect_Glow") != null, "Effect_Glow material is missing.");
        TarotIconLibrary iconLibrary = Resources.Load<TarotIconLibrary>("TarotIconLibrary");
        Require(iconLibrary != null && iconLibrary.icons.Length >= 21,
            "Tarot icon_0..icon_20 library is missing or incomplete.");

        GameObject[] effectPrefabs = Resources.LoadAll<GameObject>("SkillEffects");
        Require(effectPrefabs.Length >= 20, "Fewer than 20 skill effect prefabs were found.");
        foreach (GameObject prefab in effectPrefabs)
            Require(prefab.GetComponent<ParticleSystem>() != null, prefab.name + " has no ParticleSystem.");

        if (UIManager.instance?.shopSkillCardPool == null || GameFeedback.Instance == null)
            return;

        Transform pool = UIManager.instance.shopSkillCardPool.transform;
        List<PassiveCard> cards = new List<PassiveCard>();
        foreach (PassiveCard card in pool.GetComponentsInChildren<PassiveCard>(false))
        {
            if (card.gameObject.activeInHierarchy)
                cards.Add(card);
        }

        Require(cards.Count == 3, "Expected 3 active shop cards after refresh, found " + cards.Count + ".");
        if (cards.Count == 3)
        {
            cards.Sort((left, right) =>
                ((RectTransform)left.transform).anchoredPosition.x.CompareTo(
                    ((RectTransform)right.transform).anchoredPosition.x));
            Vector2 left = ((RectTransform)cards[0].transform).anchoredPosition;
            Vector2 middle = ((RectTransform)cards[1].transform).anchoredPosition;
            Vector2 right = ((RectTransform)cards[2].transform).anchoredPosition;
            Require(Mathf.Abs(left.x + right.x) < 0.5f, "Side cards are not centered around the middle card.");
            Require(Mathf.Abs(middle.x) < 0.5f, "Middle shop card is not horizontally centered.");
        Require(middle.x - left.x >= 260f, "Shop card spacing was not widened enough.");
            Require(middle.y > left.y && Mathf.Approximately(left.y, right.y),
                "Middle card is not slightly above the side cards.");
            Require(Quaternion.Angle(cards[1].transform.localRotation, Quaternion.identity) < 1f,
                "Shop card flip did not settle back to its front face.");
        }

        ValidateActiveCardRefresh();

        GameFeedback.Instance.Burst(CardEffectVisual.Gold, Vector3.zero);
        GameObject firework = GameObject.Find("Gold Firework");
        Require(firework != null, "Firework burst was not created.");
        Require(firework != null && firework.GetComponentsInChildren<ParticleSystem>().Length >= 3,
            "Firework burst does not contain sparks, hard star flash and falling shards.");
        Require(GameObject.Find("Gold Coin Burst") != null,
            "Gold effect did not create the coin-shaped falling burst.");

        GameFeedback.Instance.PlaySkillEffect(PassiveCardData.EffectType.Spawn16AddOneGold, Vector3.zero);
        GameObject skillEffect = GameObject.Find("Effect_14 (Spawn16AddOneGold)");
        Require(skillEffect != null, "Mapped skill effect prefab was not instantiated.");
        if (skillEffect != null)
        {
            ParticleSystem particles = skillEffect.GetComponent<ParticleSystem>();
            Require(particles != null && particles.isPlaying, "Mapped skill effect did not start playing.");
            foreach (ParticleSystem system in skillEffect.GetComponentsInChildren<ParticleSystem>(true))
            {
                Color color = system.main.startColor.colorMax;
                Require(IsGoldPalette(color),
                    system.name + " still uses a color outside the white/gold/gold-red palette.");
                ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
                Require(renderer != null && renderer.renderMode == ParticleSystemRenderMode.Billboard,
                    system.name + " was not normalized to the 2D billboard particle style.");
            }
        }

        GameFeedback.Instance.Burst(CardEffectVisual.TemporarySpawn, Vector3.zero);
        GameFeedback.Instance.Burst(CardEffectVisual.Split, Vector3.zero);
        Require(GameObject.Find("Arcane +1") != null, "Extra-ball effect did not show its +1 glyph.");
        Require(GameObject.Find("Arcane \u221E") != null, "Split effect did not show its infinity glyph.");

        GameFeedback.Instance.PlaySkillEffect(PassiveCardData.EffectType.SlotMultiplier15,
            Vector3.left * 5f);
        GameObject triangleBurst = GameObject.Find("Triangle Line Burst");
        Require(triangleBurst != null, "Multiplier effects did not create the triangle-line burst.");
        Require(triangleBurst != null && triangleBurst.GetComponentsInChildren<ParticleSystem>().Length >= 2,
            "Triangle-line burst is missing its main lines or small sparks.");

        GameFeedback.Instance.PlaySkillEffect(PassiveCardData.EffectType.HalfSidesCenter075,
            Vector3.right * 5f);
        GameObject flowerBloom = GameObject.Find("Rotating Flower Bloom");
        Require(flowerBloom != null, "State effects did not create the rotating flower bloom.");
        if (flowerBloom != null)
        {
            ParticleSystem flower = flowerBloom.GetComponent<ParticleSystem>();
            Require(flower != null && flower.isPlaying && flower.rotationOverLifetime.enabled,
                "The flower particle did not start its rotating animation.");
            Require(flower != null && Mathf.Approximately(flower.main.duration, 0.06f),
                "The flower particle's duration was not applied.");
        }

        GameFeedback.Instance.PlaySkillEffect(
            PassiveCardData.EffectType.RandomDoubleSlot, Vector3.down * 3f, false);
        GameObject slotBurst = GameObject.Find("Effect_8 (RandomDoubleSlot)");
        Require(slotBurst != null, "Score-slot Effect_8 did not start.");
        if (slotBurst != null)
        {
            ParticleSystemRenderer slotRenderer = slotBurst.GetComponent<ParticleSystemRenderer>();
            Require(slotRenderer != null &&
                    slotRenderer.renderMode == ParticleSystemRenderMode.Stretch,
                "Effect_8 was not converted to streamline particles.");
        }

        ScoreSlot[] scoreSlots = UnityEngine.Object.FindObjectsByType<ScoreSlot>(
            FindObjectsInactive.Exclude);
        ScoreSlot bestSlot = null;
        foreach (ScoreSlot slot in scoreSlots)
        {
            if (bestSlot == null || slot.multiplier > bestSlot.multiplier)
                bestSlot = slot;
        }
        if (bestSlot != null)
        {
            GameManager.instance.AddScore(bestSlot);
            GameManager.instance.NotifyPinballEnteredSlot(bestSlot);
            Require(GameObject.Find("Score Slot Tarot Flash") != null,
                "High-scoring slot did not flash a tarot icon.");
        }

        GameManager.instance.score = 100f;
        GameFeedback.Instance.SendMessage("Update");
        GameObject milestone = GameObject.Find("Score Milestone");
        Require(milestone != null,
            "Reaching 100 points did not display the centered milestone number.");
        if (milestone != null)
        {
            Text milestoneText = milestone.GetComponentInChildren<Text>();
            Require(milestoneText != null && milestoneText.font != null,
                "Milestone number did not use the hard-edged font component.");

            GameManager.instance.score = 1000f;
            GameFeedback.Instance.SendMessage("Update");
            Require(GameObject.Find("Score Milestone") == milestone,
                "A newer milestone created another overlay instead of iterating in place.");
            Require(milestoneText != null && milestoneText.text == "1,000",
                "The active milestone did not switch directly to the newer score.");
        }

        GameFeedback.Instance.ShowResults(12345, true, PassiveCardInventory.instance);
        RectTransform results = GameObject.Find("Tarot Reading")?.GetComponent<RectTransform>();
        Require(results != null, "The redesigned tarot results panel was not created.");
        if (results != null)
        {
            Require(results.sizeDelta.x >= 1000f && results.sizeDelta.y >= 700f,
                "The tarot results panel is too small for the red/cream/gold layout.");
            Require(results.GetComponentInChildren<Button>(true) != null,
                "The tarot results panel has no interactive button.");
        }

        GameManager.instance.score = GameManager.instance.currentTargetScore;
        GameFeedback.Instance.SendMessage("Update");
        GameObject edgeFlow = GameObject.Find("Target Reached - Golden Edge Flow");
        Require(edgeFlow != null, "Reaching the score target did not start the golden edge flow.");
        if (edgeFlow != null)
        {
            foreach (Image image in edgeFlow.GetComponentsInChildren<Image>(true))
                Require(!image.raycastTarget, "Golden edge flow contains a UI raycast blocker.");
        }
    }

    private static void BuyActiveCardOnShelf()
    {
        ShopSkillCardPool pool = UIManager.instance.shopActiveCardPool;
        ActiveCard[] offers = pool.GetComponentsInChildren<ActiveCard>(false);
        Require(offers.Length == pool.activeOfferCount, "The active shop did not fill its offer slots.");
        if (offers.Length == 0) return;

        ActiveCard offer = offers[0];
        purchasedActiveData = offer.data;
        GameManager game = GameManager.instance;
        game.gold = purchasedActiveData.price + 6;
        offer.OnCardClicked();
        Require(game.gold == 6, "The active-card purchase did not charge its price.");
        Require(offer.transform.parent == pool.transform &&
            !offer.GetComponent<Button>().interactable &&
            offer.cardImage.GetComponent<Image>().color == Color.gray,
            "The purchased active card did not stay grayed out on its shelf.");
        ActiveCard owned = ActiveCardInventory.instance.GetComponentInChildren<ActiveCard>();
        Require(owned != null && owned.data == purchasedActiveData, "The purchase did not add the active card to the inventory.");

        // Reuse the previous offers as candidates so the owned card must be filtered out.
        ActiveCardData[] candidates = new ActiveCardData[offers.Length];
        for (int i = 0; i < offers.Length; i++) candidates[i] = offers[i].data;
        pool.activeCardDatas = candidates;
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition) Failures.Add(failure);
    }

    private static bool IsGoldPalette(Color color)
    {
        return color.r >= 0.75f && color.r >= color.g * 0.9f && color.g >= color.b;
    }

    private static void CaptureLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error ||
            (type == LogType.Warning && message.StartsWith("Setting the duration")))
            Failures.Add(type + ": " + message + "\n" + stackTrace);
    }

    // [Jinyang.Li] Regression check for the second active-card shop refresh.
    private static void ValidateActiveCardRefresh()
    {
        ShopSkillCardPool activePool = UIManager.instance?.shopActiveCardPool;
        if (activePool == null) return;

        HorizontalLayoutGroup layout = activePool.GetComponent<HorizontalLayoutGroup>();
        Require(layout != null && layout.enabled,
            "Active-card pool lost its HorizontalLayoutGroup and will drift into passive-card slots.");
        Require(!activePool.IsRefreshing,
            "Active-card refresh did not finish before the verification window closed.");

        RectTransform poolRect = (RectTransform)activePool.transform;
        int activeCount = 0;
        foreach (ActiveCard card in activePool.GetComponentsInChildren<ActiveCard>(false))
        {
            if (!card.gameObject.activeInHierarchy) continue;
            activeCount++;
            Require(card.data != purchasedActiveData, "An active card already in the inventory appeared again.");
            RectTransform cardRect = (RectTransform)card.transform;
            Vector2 localCenter = poolRect.InverseTransformPoint(
                cardRect.TransformPoint(cardRect.rect.center));
            Require(poolRect.rect.Contains(localCenter),
                "Active-card offer drifted outside the active-card pool after refresh.");
        }

        int eligibleCount = activePool.activeCardDatas.Length;
        if (purchasedActiveData != null) eligibleCount--;
        Require(activeCount == Mathf.Min(activePool.activeOfferCount, eligibleCount),
            "Unexpected active-card offer count after the second refresh.");
    }
}
