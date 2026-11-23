using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;
using ColorBlock = UnityEngine.UI.ColorBlock;
using Random = UnityEngine.Random;

public enum CardEffectVisual
{
    Split, TemporarySpawn, TemporaryDespawn, BallValue, Score, Gold, BallStock
}

[RequireComponent(typeof(Canvas))]
public class GameFeedback : MonoBehaviour
{
    public static GameFeedback Instance { get; private set; }

    [SerializeField] private Sprite coinSprite;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color splitColor = new Color(1f, 0.84f, 0.42f);
    [SerializeField] private Color spawnColor = new Color(1f, 0.92f, 0.68f);
    [SerializeField] private Color despawnColor = new Color(1f, 0.48f, 0.28f);
    [SerializeField] private Color ballValueColor = new Color(1f, 0.74f, 0.32f);
    [SerializeField] private Color scoreColor = Color.white;
    [SerializeField] private Color goldColor = new Color(1f, 0.78f, 0.34f);
    [SerializeField] private Color ballStockColor = new Color(1f, 0.44f, 0.27f);

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private static readonly Color Ivory = new Color(1f, 0.96f, 0.84f, 1f);
    private static readonly Color Gold = new Color(1f, 0.78f, 0.32f, 1f);
    private static readonly Color BrightGold = new Color(1f, 0.88f, 0.52f, 1f);
    private static readonly Color GoldRed = new Color(0.88f, 0.29f, 0.16f, 1f);
    private static readonly Color Crimson = new Color(0.45f, 0.06f, 0.055f, 1f);
    private static readonly Color Ink = new Color(0.035f, 0.025f, 0.028f, 1f);
    private static readonly Color Parchment = new Color(0.91f, 0.84f, 0.69f, 1f);
    private static readonly Color EffectWhite = new Color(0.93f, 0.9f, 0.82f, 1f);
    private static readonly Color EffectIvory = new Color(0.95f, 0.89f, 0.77f, 1f);
    private static readonly Color EffectGold = new Color(0.92f, 0.76f, 0.54f, 1f);
    private static readonly Color EffectGoldRed = new Color(0.81f, 0.52f, 0.38f, 1f);
    private static readonly Color EffectAmber = new Color(0.8f, 0.43f, 0.16f, 1f);
    private static readonly Color EffectRoseGold = new Color(0.82f, 0.47f, 0.36f, 1f);
    private RectTransform layer;
    private TMP_FontAsset font;
    private RectTransform roundHud;
    private RectTransform goldHud;
    private RectTransform ballValueHud;
    private bool showingResults;
    private Material glowParticleMaterial;
    private Material hardSparkMaterial;
    private Material hardStarMaterial;
    private Material coinParticleMaterial;
    private Material triangleLineMaterial;
    private Material flowerPetalMaterial;
    private Sprite hardStarSprite;
    private Sprite hardDiamondSprite;
    private Sprite coinParticleSprite;
    private Sprite softGlowSprite;
    private Sprite orbitRingSprite;
    private Texture2D hardStarTexture;
    private Texture2D hardDiamondTexture;
    private Texture2D coinParticleTexture;
    private Texture2D softGlowTexture;
    private Texture2D orbitRingTexture;
    private Texture2D triangleLineTexture;
    private Texture2D flowerPetalTexture;
    private int celebratedRound = -1;
    private bool targetCelebrated;
    private RectTransform activeEdgeFlow;
    private float lastBurstTime = -10f;
    private Vector3 lastBurstPosition;
    private readonly Dictionary<PassiveCardData.EffectType, GameObject> skillEffectPrefabs = new();

    private void Awake()
    {
        Instance = this;
        glowParticleMaterial = Resources.Load<Material>("SkillEffects/Effect_Glow");
        CreateHardEdgeMaterials();
        layer = Stretch("Gameplay Feedback", transform);
        foreach (HUDGeneralPackage hud in GetComponentsInChildren<HUDGeneralPackage>(true))
        {
            RectTransform rect = (RectTransform)hud.transform;
            switch (hud.hUDType)
            {
                case HUDGeneralPackage.HUDType.Round:
                    roundHud = (RectTransform)rect.parent;
                    font = hud.GetComponent<TMP_Text>().font;
                    break;
                case HUDGeneralPackage.HUDType.Gold:
                    goldHud = (RectTransform)rect.parent;
                    break;
                case HUDGeneralPackage.HUDType.BallValue:
                    ballValueHud = (RectTransform)rect.parent;
                    break;
            }
        }
    }

    private void Start()
    {
        if (GameManager.instance != null)
            GameManager.instance.RoundEnded += StopTargetCelebration;
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null)
            GameManager.instance.RoundEnded -= StopTargetCelebration;
        Destroy(hardSparkMaterial);
        Destroy(hardStarMaterial);
        Destroy(coinParticleMaterial);
        Destroy(triangleLineMaterial);
        Destroy(flowerPetalMaterial);
        if (hardStarSprite != null) Destroy(hardStarSprite);
        if (hardDiamondSprite != null) Destroy(hardDiamondSprite);
        if (coinParticleSprite != null) Destroy(coinParticleSprite);
        if (softGlowSprite != null) Destroy(softGlowSprite);
        if (orbitRingSprite != null) Destroy(orbitRingSprite);
        if (hardStarTexture != null) Destroy(hardStarTexture);
        if (hardDiamondTexture != null) Destroy(hardDiamondTexture);
        if (coinParticleTexture != null) Destroy(coinParticleTexture);
        if (softGlowTexture != null) Destroy(softGlowTexture);
        if (orbitRingTexture != null) Destroy(orbitRingTexture);
        if (triangleLineTexture != null) Destroy(triangleLineTexture);
        if (flowerPetalTexture != null) Destroy(flowerPetalTexture);
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        GameManager game = GameManager.instance;
        if (game == null || game.currentTargetScore <= 0f) return;

        if (celebratedRound != game.currentRound)
        {
            celebratedRound = game.currentRound;
            targetCelebrated = false;
            if (activeEdgeFlow != null)
            {
                Destroy(activeEdgeFlow.gameObject);
                activeEdgeFlow = null;
            }
        }

        if (game.score >= game.currentTargetScore && !targetCelebrated)
            PlayTargetEdgeFlow();
    }

    public void ShowRoundChange() => ShowDelta(roundHud, "+1", EffectWhite);

    public void ShowGoldChange(int amount)
    {
        if (amount == 0) return;
        ShowDelta(goldHud, amount.ToString("+0;-0"), amount > 0 ? EffectGold : EffectGoldRed, coinSprite);
        if (amount > 0 && goldHud != null)
            PlayUiCoinBurst(goldHud);
    }

    public void ShowBallValueChange(float amount) =>
        ShowDelta(ballValueHud, amount.ToString("+0.##"), ballValueColor);

    private void ShowDelta(RectTransform target, string value, Color color, Sprite icon = null)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        const float popupWidth = 126f;
        Vector2 start = ToLayer(target, new Vector2(target.rect.center.x, target.rect.yMax)) +
                        Vector2.up * 16f;
        start.x = Mathf.Clamp(start.x, layer.rect.xMin + popupWidth * 0.5f,
            layer.rect.xMax - popupWidth * 0.5f);
        start.y = Mathf.Min(start.y, layer.rect.yMax - 23f);
        RectTransform popup = CreateRect("HUD Change", layer, Center, start, new Vector2(popupWidth, 46f));
        Image background = AddImage(popup, new Color(0.035f, 0.025f, 0.028f, 0.94f));
        background.raycastTarget = false;
        CanvasGroup opacity = popup.gameObject.AddComponent<CanvasGroup>();
        float labelOffset = icon == null ? 0f : 15f;
        AddText(popup, value, 27f, color, Center, new Vector2(labelOffset, 0f), new Vector2(96f, 44f));
        if (icon != null)
            AddImage(CreateRect("Coin", popup, Center, new Vector2(-40f, 0f), new Vector2(23f, 23f)), Color.white, icon);

        StartCoroutine(Animate(1f, t =>
        {
            popup.anchoredPosition = start + Vector2.up * (Mathf.Sin(t * Mathf.PI) * 26f);
            opacity.alpha = Mathf.Min(Mathf.Clamp01(t / 0.14f), Mathf.Clamp01((1f - t) / 0.24f));
        }, () => Destroy(popup.gameObject)));
    }

    public void PlayPurchase(PassiveCard source, PassiveCard destination)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform ghost = CreateCardGhost(source, out Image border);
        Vector2 start = ghost.anchoredPosition;
        Vector2 size = ghost.sizeDelta;
        RectTransform target = (RectTransform)destination.cardImage;
        destination.SetPurchaseAnimation(true);

        StartCoroutine(Animate(0.6f, t =>
        {
            float travel = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, t));
            ghost.anchoredPosition = Vector2.Lerp(start, ToLayer(target, target.rect.center), travel);
            ghost.sizeDelta = Vector2.Lerp(size, SizeInLayer(target), travel);
            ghost.localRotation = Quaternion.Euler(0f, travel * 360f, 0f);
            border.color = WithAlpha(goldColor, Mathf.Lerp(1f, 0.25f, t));
        }, () =>
        {
            destination.SetPurchaseAnimation(false);
            Destroy(ghost.gameObject);
        }));
    }

    public void PlaySale(PassiveCard card)
    {
        RectTransform ghost = CreateCardGhost(card, out _);
        CanvasGroup opacity = ghost.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(Animate(0.32f, t =>
        {
            ghost.localScale = Vector3.one * Mathf.Lerp(1f, 0.86f, t);
            opacity.alpha = 1f - Mathf.InverseLerp(0.25f, 1f, t);
        }, () => Destroy(ghost.gameObject)));
    }

    private RectTransform CreateCardGhost(PassiveCard card, out Image border)
    {
        RectTransform source = (RectTransform)card.cardImage;
        RectTransform ghost = CreateRect("Card Transaction", layer, Center,
            ToLayer(source, source.rect.center), SizeInLayer(source));
        border = AddImage(Stretch("Gold Edge", ghost, 4f), goldColor);
        AddImage(Stretch("Card Art", ghost), Color.white, card.data.cardSprite);
        return ghost;
    }

    public void FlashCard(PassiveCard card)
    {
        RectTransform art = (RectTransform)card.cardImage;
        const string flashName = "Card Skill Flash";
        if (art.Find(flashName) != null) return;
        Image flash = AddImage(Stretch(flashName, art), Color.clear);
        StartCoroutine(AnimateSkillFlash(card, flash));
    }

    private IEnumerator AnimateSkillFlash(PassiveCard card, Image flash)
    {
        yield return null;
        CanvasGroup purchase = card.GetComponent<CanvasGroup>();
        while (card.isActiveAndEnabled && purchase != null && purchase.alpha == 0f)
            yield return null;

        const float duration = 0.34f;
        float elapsed = 0f;
        while (card.isActiveAndEnabled && elapsed < duration)
        {
            flash.color = WithAlpha(EffectWhite, Mathf.Sin(elapsed / duration * Mathf.PI) * 0.34f);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        Destroy(flash.gameObject);
    }

    public void Burst(CardEffectVisual effect, Vector3 position)
    {
        Material material = hardSparkMaterial != null
            ? hardSparkMaterial
            : glowParticleMaterial != null ? glowParticleMaterial : particleMaterial;
        if (material == null) return;

        Color effectColor = effect switch
        {
            CardEffectVisual.Split => splitColor,
            CardEffectVisual.TemporarySpawn => spawnColor,
            CardEffectVisual.TemporaryDespawn => despawnColor,
            CardEffectVisual.BallValue => ballValueColor,
            CardEffectVisual.Score => scoreColor,
            CardEffectVisual.Gold => goldColor,
            _ => ballStockColor,
        };
        effectColor = RestrictToEffectPalette(effectColor);

        GameObject burst = new GameObject(effect + " Firework");
        burst.SetActive(false);
        burst.transform.position = position;

        // A denser cloud of small, softly feathered sparks reads as luminous
        // without turning into a large solid shape on top of the board.
        ParticleSystem sparks = burst.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.66f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.7f, 4.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.078f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.72f), WithAlpha(effectColor, 0.54f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.2f;
        main.maxParticles = 190;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 82),
            new ParticleSystem.Burst(0.028f, 38),
            new ParticleSystem.Burst(0.058f, 24)
        });

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.055f;
        shape.arc = 360f;
        shape.radiusThickness = 0.35f;

        ParticleSystem.SizeOverLifetimeModule size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 1.06f),
                new Keyframe(0.22f, 1f),
                new Keyframe(0.72f, 0.76f),
                new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(effectColor, 0.9f);

        ParticleSystemRenderer sparkRenderer = burst.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.sharedMaterial = material;
        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkRenderer.sortingOrder = 23;
        sparkRenderer.maxParticleSize = 0.28f;

        ParticleSystem halo = AddSoftStarFlash(burst.transform, effectColor);
        ParticleSystem shards = AddFallingCoinShards(burst.transform, effectColor);
        burst.SetActive(true);
        halo.Play();
        shards.Play();
        sparks.Play();

        lastBurstTime = Time.unscaledTime;
        lastBurstPosition = position;
        PlayBurstSignature(effect, position);
    }

    private ParticleSystem AddSoftStarFlash(Transform parent, Color color)
    {
        GameObject haloObject = new GameObject("Soft Star Flash");
        haloObject.transform.SetParent(parent, false);
        ParticleSystem halo = haloObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = halo.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.32f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.31f);
        main.startColor = WithAlpha(EffectWhite, 0.52f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = halo.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ColorOverLifetimeModule fade = halo.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(color, 0.42f);

        ParticleSystem.SizeOverLifetimeModule size = halo.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.62f),
                new Keyframe(0.24f, 1f),
                new Keyframe(1f, 1.12f)));

        ParticleSystem.RotationOverLifetimeModule rotation = halo.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = 0.38f;

        ParticleSystemRenderer renderer = haloObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = hardStarMaterial != null ? hardStarMaterial : glowParticleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 24;
        return halo;
    }

    private ParticleSystem AddFallingCoinShards(Transform parent, Color color)
    {
        GameObject shardObject = new GameObject("Gold Edge Shards");
        shardObject.transform.SetParent(parent, false);
        ParticleSystem shards = shardObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = shards.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.62f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 3.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.055f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.58f), WithAlpha(color, 0.44f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.72f;
        main.maxParticles = 48;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = shards.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.02f, 28) });

        ParticleSystem.ShapeModule shape = shards.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;
        shape.radiusThickness = 0.1f;

        ParticleSystem.ColorOverLifetimeModule fade = shards.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(color, 0.58f);

        ParticleSystem.RotationOverLifetimeModule rotation = shards.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        ParticleSystemRenderer renderer = shardObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = hardDiamondMaterialOrFallback();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 22;
        return shards;
    }

    private Material hardDiamondMaterialOrFallback()
    {
        if (hardSparkMaterial != null) return hardSparkMaterial;
        return glowParticleMaterial != null ? glowParticleMaterial : particleMaterial;
    }

    private void PlayBurstSignature(CardEffectVisual effect, Vector3 position)
    {
        switch (effect)
        {
            case CardEffectVisual.Gold:
                PlayCoinBurst(position + Vector3.up * 0.08f, 22, 1f);
                break;
            case CardEffectVisual.TemporarySpawn:
            case CardEffectVisual.BallStock:
                ShowWorldGlyph("+1", position + Vector3.up * 0.3f, EffectWhite, true);
                break;
            case CardEffectVisual.Split:
                ShowWorldGlyph("\u221E", position + Vector3.up * 0.5f, EffectIvory, false);
                break;
        }
    }

    public void PlaySkillEffect(PassiveCardData.EffectType effectType, Vector3 position)
    {
        if (effectType == PassiveCardData.EffectType.None) return;

        if (!skillEffectPrefabs.TryGetValue(effectType, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>("SkillEffects/Effect_" + (int)effectType);
            skillEffectPrefabs.Add(effectType, prefab);
        }

        if (prefab == null) return;

        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        instance.name = "Effect_" + (int)effectType + " (" + effectType + ")";
        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in systems)
        {
            NormalizeParticlePalette(system, effectType);
            system.Play(true);
        }

        if (Time.unscaledTime - lastBurstTime > 0.06f ||
            Vector3.SqrMagnitude(lastBurstPosition - position) > 0.01f)
        {
            if (IsGoldEffect(effectType))
                PlayCoinBurst(position + Vector3.up * 0.15f, 16, 0.88f);
            else if (effectType == PassiveCardData.EffectType.PegHitSplitPinball)
                ShowWorldGlyph("\u221E", position + Vector3.up * 0.44f, EffectIvory, false);
            else if (IsExtraBallEffect(effectType))
                ShowWorldGlyph("+1", position + Vector3.up * 0.25f, EffectWhite, true);
            else if (IsTriangleEffect(effectType))
                PlayTriangleLineBurst(position);
            else if (IsFlowerEffect(effectType))
                PlayFlowerBloom(position);
        }

        // Cleanup only protects against abandoned instances. Hand-made effects
        // can run for several seconds and still finish normally.
        Destroy(instance, 8f);
    }

    private void PlayTriangleLineBurst(Vector3 position)
    {
        GameObject burst = new GameObject("Triangle Line Burst");
        burst.transform.position = position;

        ParticleSystem triangles = burst.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = triangles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.06f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.105f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.62f), WithAlpha(EffectAmber, 0.52f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.05f;
        main.maxParticles = 20;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = triangles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9) });

        ParticleSystem.ShapeModule shape = triangles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.025f;
        shape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule fade = triangles.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(EffectAmber, 0.56f);

        ParticleSystem.RotationOverLifetimeModule rotation = triangles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2.8f, 2.8f);

        ParticleSystem.SizeOverLifetimeModule size = triangles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.72f),
                new Keyframe(0.3f, 1f),
                new Keyframe(1f, 0.46f)));

        ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = triangleLineMaterial != null
            ? triangleLineMaterial
            : hardDiamondMaterialOrFallback();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 25;
        renderer.maxParticleSize = 0.18f;

        AddSignatureSparks(burst.transform, EffectAmber, 18, 0.012f, 0.034f);
        triangles.Play(true);
        Destroy(burst, 2.2f);
    }

    private void PlayFlowerBloom(Vector3 position)
    {
        GameObject bloom = new GameObject("Rotating Flower Bloom");
        bloom.transform.position = position;

        ParticleSystem flower = bloom.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule flowerMain = flower.main;
        flowerMain.playOnAwake = false;
        flowerMain.loop = false;
        flowerMain.duration = 0.06f;
        flowerMain.startLifetime = 0.76f;
        flowerMain.startSpeed = 0f;
        flowerMain.startSize = new ParticleSystem.MinMaxCurve(0.34f, 0.42f);
        flowerMain.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.64f), WithAlpha(EffectRoseGold, 0.5f));
        flowerMain.simulationSpace = ParticleSystemSimulationSpace.World;
        flowerMain.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule flowerEmission = flower.emission;
        flowerEmission.rateOverTime = 0f;
        flowerEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
        ParticleSystem.ShapeModule flowerShape = flower.shape;
        flowerShape.shapeType = ParticleSystemShapeType.Circle;
        flowerShape.radius = 0.001f;
        flowerShape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule flowerFade = flower.colorOverLifetime;
        flowerFade.enabled = true;
        flowerFade.color = CreateGoldGradient(EffectRoseGold, 0.55f);

        ParticleSystem.SizeOverLifetimeModule flowerSize = flower.sizeOverLifetime;
        flowerSize.enabled = true;
        flowerSize.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f, 0.14f),
                new Keyframe(0.3f, 0.82f),
                new Keyframe(0.76f, 1f),
                new Keyframe(1f, 1.04f)));

        ParticleSystem.RotationOverLifetimeModule flowerRotation = flower.rotationOverLifetime;
        flowerRotation.enabled = true;
        flowerRotation.z = 1.35f;

        ParticleSystemRenderer flowerRenderer = bloom.GetComponent<ParticleSystemRenderer>();
        flowerRenderer.sharedMaterial = flowerPetalMaterial != null
            ? flowerPetalMaterial
            : hardStarMaterial;
        flowerRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        flowerRenderer.sortingOrder = 25;
        flowerRenderer.maxParticleSize = 0.32f;

        ParticleSystem petals = AddSignatureSparks(bloom.transform, EffectRoseGold, 12, 0.022f, 0.052f,
            0.55f, 1.35f);
        ParticleSystemRenderer petalRenderer = petals.GetComponent<ParticleSystemRenderer>();
        petalRenderer.sharedMaterial = flowerPetalMaterial != null
            ? flowerPetalMaterial
            : hardSparkMaterial;

        flower.Play(true);
        petals.Play(true);
        Destroy(bloom, 2.2f);
    }

    private ParticleSystem AddSignatureSparks(
        Transform parent,
        Color accent,
        short count,
        float minSize,
        float maxSize,
        float minSpeed = 0.9f,
        float maxSpeed = 2.4f)
    {
        GameObject sparkObject = new GameObject("Flower Petal Sparks");
        sparkObject.transform.SetParent(parent, false);
        ParticleSystem sparks = sparkObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.06f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.34f, 0.62f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.54f), WithAlpha(accent, 0.44f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.02f;
        shape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(accent, 0.48f);

        ParticleSystem.RotationOverLifetimeModule rotation = sparks.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-4.5f, 4.5f);

        ParticleSystemRenderer renderer = sparks.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = hardSparkMaterial != null ? hardSparkMaterial :
            hardDiamondMaterialOrFallback();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 24;
        renderer.maxParticleSize = 0.1f;
        return sparks;
    }

    public void ShowResults(int score, bool cleared, PassiveCardInventory inventory)
    {
        if (showingResults) return;
        showingResults = true;

        RectTransform screen = Stretch("Results", transform);
        screen.SetAsLastSibling();
        Image backdrop = AddImage(screen, new Color(Ink.r, Ink.g, Ink.b, 0.97f));
        backdrop.raycastTarget = true;

        RectTransform panel = CreateRect("Tarot Reading", screen, Center, new Vector2(0f, 4f),
            new Vector2(1120f, 790f));
        AddImage(panel, Parchment);
        AddBorder(panel, Gold, 5f);
        AddBorder(panel, Crimson, 1.5f, 16f);
        AddCornerMotifs(panel);

        RectTransform crown = CreateRect("Ink Crown", panel, new Vector2(0.5f, 1f),
            new Vector2(0f, -76f), new Vector2(1064f, 122f));
        AddImage(crown, Ink);
        AddLine(crown, new Vector2(0.5f, 0f), new Vector2(982f, 2f), Gold);
        AddText(crown, "THE CARDS HAVE SPOKEN", 20f, BrightGold, new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(780f, 34f));
        AddText(crown, "FORTUNE'S LAST WORD", 39f, Parchment, new Vector2(0.5f, 0.5f),
            new Vector2(0f, -7f), new Vector2(900f, 52f));
        AddDiamond(crown, new Vector2(0f, 1f), new Vector2(-468f, -30f), 11f, Gold);
        AddDiamond(crown, new Vector2(0f, 1f), new Vector2(468f, -30f), 11f, Gold);

        AddText(panel, cleared ? "FATE FAVOURS YOU" : "THE THREAD IS SEVERED",
            35f, cleared ? Crimson : GoldRed, Center, new Vector2(0f, 219f), new Vector2(840f, 52f));
        AddOrnamentalDivider(panel, new Vector2(0f, 171f));

        AddText(panel, score.ToString("N0"), 124f, Ink, Center, new Vector2(0f, 74f),
            new Vector2(900f, 155f));
        AddText(panel, "TOTAL SCORE", 23f, Crimson, Center, new Vector2(0f, -16f),
            new Vector2(320f, 38f));

        PassiveCard[] cards = inventory.GetComponentsInChildren<PassiveCard>(true);
        AddText(panel, "CARDS DRAWN", 19f, Crimson, Center, new Vector2(0f, -55f),
            new Vector2(320f, 34f));
        if (cards.Length == 0)
        {
            AddText(panel, "No cards were drawn.", 24f, Ink, Center, new Vector2(0f, -158f),
                new Vector2(520f, 50f));
        }
        else
        {
            int displayCount = Mathf.Min(7, cards.Length);
            float spacing = displayCount > 1 ? Mathf.Min(128f, 884f / (displayCount - 1)) : 0f;
            for (int i = 0; i < displayCount; i++)
            {
                float x = (i - (displayCount - 1) * 0.5f) * spacing;
                RectTransform cardFrame = CreateRect(cards[i].data.cardName, panel, Center,
                    new Vector2(x, -158f), new Vector2(105f, 149f));
                RectTransform shadow = Stretch("Card Shadow", cardFrame, 4f);
                shadow.anchoredPosition = new Vector2(3f, -4f);
                AddImage(shadow, new Color(Ink.r, Ink.g, Ink.b, 0.42f));
                AddImage(cardFrame, Ink);
                AddBorder(cardFrame, Gold, 2.5f, 1f);
                Image art = AddImage(Stretch("Card Art", cardFrame, 5f),
                    Color.white, cards[i].data.cardSprite);
                art.preserveAspect = true;
            }
        }

        Button retry = AddButton(panel, "PLAY AGAIN", -154f, Ink, Parchment);
        retry.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        Button quit = AddButton(panel, "QUIT GAME", 154f, Crimson, Parchment);
        quit.onClick.AddListener(QuitGame);
        AddDiamond(panel, Center, new Vector2(0f, -331f), 16f, Gold);

        CanvasGroup reveal = screen.gameObject.AddComponent<CanvasGroup>();
        reveal.alpha = 0f;
        panel.localScale = Vector3.one * 0.965f;
        StartCoroutine(AnimateResults(reveal, panel, retry));
    }

    private static IEnumerator AnimateResults(CanvasGroup reveal, RectTransform panel, Button firstButton)
    {
        const float duration = 0.38f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            reveal.alpha = eased;
            panel.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, eased);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        reveal.alpha = 1f;
        panel.localScale = Vector3.one;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(firstButton.gameObject);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private Button AddButton(RectTransform parent, string label, float horizontal, Color fill, Color textColor)
    {
        RectTransform rect = CreateRect(label, parent, Center,
            new Vector2(horizontal, -331f), new Vector2(266f, 62f));
        Image image = AddImage(rect, fill);
        image.raycastTarget = true;
        AddBorder(rect, Gold, 2f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.62f, 1f);
        colors.pressedColor = new Color(0.8f, 0.35f, 0.22f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        AddText(rect, label, 25f, textColor, Center, Vector2.zero, rect.sizeDelta);
        return button;
    }

    private void AddText(RectTransform parent, string value, float size, Color color, Vector2 anchor, Vector2 position, Vector2 bounds)
    {
        RectTransform rect = CreateRect(value, parent, anchor, position, bounds);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = value;
        label.color = color;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
    }

    private void AddBorder(RectTransform parent, Color color, float thickness, float inset = 0f)
    {
        RectTransform top = Stretch("Top Rule", parent);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.anchoredPosition = new Vector2(0f, -inset);
        top.sizeDelta = new Vector2(-inset * 2f, thickness);
        AddImage(top, color);

        RectTransform bottom = Stretch("Bottom Rule", parent);
        bottom.anchorMin = new Vector2(0f, 0f);
        bottom.anchorMax = new Vector2(1f, 0f);
        bottom.pivot = new Vector2(0.5f, 0f);
        bottom.anchoredPosition = new Vector2(0f, inset);
        bottom.sizeDelta = new Vector2(-inset * 2f, thickness);
        AddImage(bottom, color);

        RectTransform left = Stretch("Left Rule", parent);
        left.anchorMin = new Vector2(0f, 0f);
        left.anchorMax = new Vector2(0f, 1f);
        left.pivot = new Vector2(0f, 0.5f);
        left.anchoredPosition = new Vector2(inset, 0f);
        left.sizeDelta = new Vector2(thickness, -inset * 2f);
        AddImage(left, color);

        RectTransform right = Stretch("Right Rule", parent);
        right.anchorMin = new Vector2(1f, 0f);
        right.anchorMax = new Vector2(1f, 1f);
        right.pivot = new Vector2(1f, 0.5f);
        right.anchoredPosition = new Vector2(-inset, 0f);
        right.sizeDelta = new Vector2(thickness, -inset * 2f);
        AddImage(right, color);
    }

    private void AddCornerMotifs(RectTransform panel)
    {
        AddDiamond(panel, new Vector2(0f, 1f), new Vector2(33f, -33f), 18f, Gold);
        AddDiamond(panel, new Vector2(1f, 1f), new Vector2(-33f, -33f), 18f, Gold);
        AddDiamond(panel, new Vector2(0f, 0f), new Vector2(33f, 33f), 18f, Gold);
        AddDiamond(panel, new Vector2(1f, 0f), new Vector2(-33f, 33f), 18f, Gold);

        AddLine(panel, new Vector2(0f, 1f), new Vector2(92f, 2f), Crimson);
        RectTransform topRightLine = AddLine(panel, new Vector2(1f, 1f), new Vector2(92f, 2f), Crimson);
        topRightLine.anchoredPosition = new Vector2(-46f, -44f);
        RectTransform topLeftLine = AddLine(panel, new Vector2(0f, 1f), new Vector2(92f, 2f), Crimson);
        topLeftLine.anchoredPosition = new Vector2(46f, -44f);
        AddLine(panel, new Vector2(0f, 0f), new Vector2(92f, 2f), Crimson).anchoredPosition =
            new Vector2(46f, 44f);
        AddLine(panel, new Vector2(1f, 0f), new Vector2(92f, 2f), Crimson).anchoredPosition =
            new Vector2(-46f, 44f);
    }

    private RectTransform AddLine(RectTransform parent, Vector2 anchor, Vector2 size, Color color)
    {
        RectTransform line = CreateRect("Gold Rule", parent, anchor, new Vector2(0f, -1f), size);
        AddImage(line, color);
        return line;
    }

    private void AddDiamond(RectTransform parent, Vector2 anchor, Vector2 position, float size, Color color)
    {
        RectTransform diamond = CreateRect("Diamond", parent, anchor, position, new Vector2(size, size));
        AddImage(diamond, color, hardDiamondSprite);
    }

    private void AddOrnamentalDivider(RectTransform parent, Vector2 position)
    {
        RectTransform line = CreateRect("Reading Divider", parent, Center, position,
            new Vector2(560f, 2f));
        AddImage(line, GoldRed);
        RectTransform left = CreateRect("Left Pin", parent, Center,
            position + new Vector2(-282f, 0f), new Vector2(8f, 8f));
        AddImage(left, Gold, hardDiamondSprite);
        RectTransform right = CreateRect("Right Pin", parent, Center,
            position + new Vector2(282f, 0f), new Vector2(8f, 8f));
        AddImage(right, Gold, hardDiamondSprite);
        AddDiamond(parent, Center, position, 17f, Crimson);
        AddDiamond(parent, Center, position, 8f, Parchment);
    }

    public void PlayShopTransition(Action swap, bool opening, Action completed)
    {
        StartCoroutine(AnimateShopTransition(swap, opening, completed));
    }

    private IEnumerator AnimateShopTransition(Action swap, bool opening, Action completed)
    {
        RectTransform veil = Stretch("Tarot Shop Transition", transform);
        veil.SetAsLastSibling();
        Image backdrop = AddImage(veil, new Color(Ink.r, Ink.g, Ink.b, 0f));
        backdrop.raycastTarget = true;

        RectTransform orbitRoot = CreateRect("Two-Light Orbit", veil, Center, Vector2.zero,
            new Vector2(92f, 92f));
        Image orbitRing = AddImage(Stretch("Soft Orbit Ring", orbitRoot),
            WithAlpha(EffectGold, 0f), orbitRingSprite);
        Image centerGlow = AddImage(CreateRect("Orbit Center Glow", orbitRoot, Center, Vector2.zero,
            new Vector2(38f, 38f)), WithAlpha(EffectIvory, 0f), softGlowSprite);
        Image firstLight = AddImage(CreateRect("White Orbit Light", orbitRoot, Center, Vector2.zero,
            new Vector2(23f, 23f)), WithAlpha(EffectWhite, 0f), softGlowSprite);
        Image secondLight = AddImage(CreateRect("Gold Orbit Light", orbitRoot, Center, Vector2.zero,
            new Vector2(19f, 19f)), WithAlpha(EffectGold, 0f), softGlowSprite);

        const float fadeIn = 0.18f;
        const float fadeOut = 0.26f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            float t = Mathf.Clamp01(elapsed / fadeIn);
            float eased = t * t * (3f - 2f * t);
            backdrop.color = new Color(Ink.r, Ink.g, Ink.b, eased);
            UpdateOrbitLights(orbitRoot, orbitRing, centerGlow, firstLight, secondLight,
                elapsed, opening, eased);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        swap();
        yield return null;

        elapsed = 0f;
        while (elapsed < fadeOut)
        {
            float t = Mathf.Clamp01(elapsed / fadeOut);
            backdrop.color = new Color(Ink.r, Ink.g, Ink.b, 1f - t);
            UpdateOrbitLights(orbitRoot, orbitRing, centerGlow, firstLight, secondLight,
                fadeIn + elapsed, opening, 1f - t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(veil.gameObject);
        completed();
    }

    private static void UpdateOrbitLights(
        RectTransform orbitRoot,
        Image ring,
        Image center,
        Image first,
        Image second,
        float time,
        bool opening,
        float visibility)
    {
        float direction = opening ? 1f : -1f;
        float angle = time * 4.6f * direction;
        const float radius = 34f;
        first.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        second.rectTransform.anchoredPosition =
            new Vector2(Mathf.Cos(angle + Mathf.PI), Mathf.Sin(angle + Mathf.PI)) * radius;
        first.rectTransform.localScale = Vector3.one * (0.9f + Mathf.Sin(time * 9f) * 0.08f);
        second.rectTransform.localScale = Vector3.one * (0.86f + Mathf.Cos(time * 8f) * 0.08f);
        orbitRoot.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, visibility);
        ring.color = WithAlpha(EffectGold, visibility * 0.22f);
        center.color = WithAlpha(EffectIvory, visibility * 0.1f);
        first.color = WithAlpha(EffectWhite, visibility * 0.86f);
        second.color = WithAlpha(EffectGold, visibility * 0.76f);
    }

    private void ShowWorldGlyph(string value, Vector3 worldPosition, Color color, bool plusFlash)
    {
        if (layer == null) return;
        Camera worldCamera = Camera.main;
        if (worldCamera == null) return;
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f) return;

        Canvas canvas = GetComponent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPoint, uiCamera,
                out Vector2 localPoint))
            return;

        RectTransform glyph = CreateRect("Arcane " + value, layer, Center, localPoint,
            new Vector2(plusFlash ? 82f : 92f, plusFlash ? 76f : 84f));
        glyph.SetAsLastSibling();
        CanvasGroup opacity = glyph.gameObject.AddComponent<CanvasGroup>();

        Image softFlash = null;
        if (plusFlash)
        {
            softFlash = AddImage(CreateRect("Soft White-Gold Glow", glyph, Center, Vector2.zero,
                new Vector2(74f, 74f)), WithAlpha(EffectGold, 0.34f), softGlowSprite);
            softFlash.raycastTarget = false;
        }

        AddText(glyph, value, plusFlash ? 34f : 56f, color, Center,
            new Vector2(0f, plusFlash ? 0f : -1f),
            new Vector2(plusFlash ? 82f : 92f, plusFlash ? 66f : 78f));

        StartCoroutine(AnimateWorldGlyph(glyph, opacity, softFlash));
    }

    private static IEnumerator AnimateWorldGlyph(RectTransform glyph, CanvasGroup opacity, Image softFlash)
    {
        const float duration = 0.52f;
        Vector2 start = glyph.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration && glyph != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float rise = Mathf.Sin(t * Mathf.PI * 0.5f);
            float pop = t < 0.2f ? Mathf.Lerp(0.82f, 1.05f, t / 0.2f) :
                Mathf.Lerp(1.05f, 1f, Mathf.InverseLerp(0.2f, 0.4f, t));
            glyph.anchoredPosition = start + Vector2.up * (rise * 24f);
            glyph.localScale = Vector3.one * pop;
            opacity.alpha = Mathf.Clamp01(t / 0.06f) * (1f - Mathf.InverseLerp(0.58f, 1f, t));
            if (softFlash != null)
                softFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.86f, 1.08f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (glyph != null) Destroy(glyph.gameObject);
    }

    private void PlayCoinBurst(Vector3 position, int count, float scale)
    {
        GameObject coinBurst = new GameObject("Gold Coin Burst");
        coinBurst.transform.position = position;
        ParticleSystem coins = coinBurst.AddComponent<ParticleSystem>();
        coins.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = coins.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.62f, 1.08f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.15f * scale, 3.35f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.095f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.7f), WithAlpha(EffectGold, 0.55f));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.84f;
        main.maxParticles = Mathf.Max(8, count + 4);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = coins.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, count)) });

        ParticleSystem.ShapeModule shape = coins.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.045f;
        shape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule fade = coins.colorOverLifetime;
        fade.enabled = true;
        fade.color = CreateGoldGradient(EffectGold, 0.62f);

        ParticleSystem.RotationOverLifetimeModule rotation = coins.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-4.5f, 4.5f);

        ParticleSystemRenderer renderer = coinBurst.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = coinParticleMaterial != null ? coinParticleMaterial :
            hardDiamondMaterialOrFallback();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 26;
        renderer.maxParticleSize = 0.22f;
        coins.Play(true);
    }

    private void PlayUiCoinBurst(RectTransform target)
    {
        if (target == null) return;
        Vector2 center = ToLayer(target, target.rect.center);
        for (int i = 0; i < 7; i++)
        {
            RectTransform coin = CreateRect("HUD Gold Spark", layer, Center,
                center + Random.insideUnitCircle * 18f, new Vector2(12f, 12f));
            Image image = AddImage(coin, i % 3 == 0 ? EffectWhite : EffectGold, coinParticleSprite);
            StartCoroutine(AnimateUiCoin(coin, image, Random.insideUnitCircle.normalized * 54f));
        }
    }

    private static IEnumerator AnimateUiCoin(RectTransform coin, Image image, Vector2 velocity)
    {
        const float duration = 0.5f;
        Vector2 start = coin.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration && coin != null)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            coin.anchoredPosition = start + velocity * t + Vector2.down * (44f * t * t);
            coin.localRotation = Quaternion.Euler(0f, 0f, t * 240f);
            image.color = WithAlpha(image.color, 1f - t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (coin != null) Destroy(coin.gameObject);
    }

    private void PlayTargetEdgeFlow()
    {
        targetCelebrated = true;
        RectTransform edge = Stretch("Target Reached - Golden Edge Flow", layer);
        edge.SetAsFirstSibling();
        activeEdgeFlow = edge;

        Image[] rules =
        {
            AddImage(CreateRect("Top Gold Flow", edge, new Vector2(0.5f, 1f), new Vector2(0f, -9f),
                new Vector2(0f, 3f)), WithAlpha(Gold, 0f)),
            AddImage(CreateRect("Bottom Gold Flow", edge, new Vector2(0.5f, 0f), new Vector2(0f, 9f),
                new Vector2(0f, 3f)), WithAlpha(Gold, 0f)),
            AddImage(CreateRect("Left Gold Flow", edge, new Vector2(0f, 0.5f), new Vector2(9f, 0f),
                new Vector2(3f, 0f)), WithAlpha(Gold, 0f)),
            AddImage(CreateRect("Right Gold Flow", edge, new Vector2(1f, 0.5f), new Vector2(-9f, 0f),
                new Vector2(3f, 0f)), WithAlpha(Gold, 0f))
        };
        rules[0].rectTransform.anchorMin = new Vector2(0.03f, 1f);
        rules[0].rectTransform.anchorMax = new Vector2(0.97f, 1f);
        rules[1].rectTransform.anchorMin = new Vector2(0.03f, 0f);
        rules[1].rectTransform.anchorMax = new Vector2(0.97f, 0f);
        rules[2].rectTransform.anchorMin = new Vector2(0f, 0.06f);
        rules[2].rectTransform.anchorMax = new Vector2(0f, 0.94f);
        rules[3].rectTransform.anchorMin = new Vector2(1f, 0.06f);
        rules[3].rectTransform.anchorMax = new Vector2(1f, 0.94f);

        RectTransform[] motes = new RectTransform[12];
        for (int i = 0; i < motes.Length; i++)
        {
            motes[i] = CreateRect("Gold Mote", edge, Center, Vector2.zero,
                Vector2.one * (i % 4 == 0 ? 27f : 17f));
            Color headColor = i % 5 == 0 ? BrightGold : i % 3 == 0 ? GoldRed : Gold;
            AddImage(CreateRect("Gold Trail", motes[i], Center, new Vector2(-26f, 0f),
                new Vector2(54f, 2.5f)), WithAlpha(headColor, 0.5f));
            AddImage(motes[i], headColor, hardStarSprite);
        }

        const int inwardCount = 192;
        RectTransform[] inwardParticles = new RectTransform[inwardCount];
        Image[] inwardImages = new Image[inwardCount];
        float[] progress = new float[inwardCount];
        float[] lifetime = new float[inwardCount];
        float[] baseSize = new float[inwardCount];
        float[] sway = new float[inwardCount];
        float[] brightness = new float[inwardCount];
        Vector2[] starts = new Vector2[inwardCount];
        Color[] colors = new Color[inwardCount];
        for (int i = 0; i < inwardCount; i++)
        {
            inwardParticles[i] = CreateRect("Inward White-Gold Glow", edge, Center, Vector2.zero,
                Vector2.one * 8f);
            inwardImages[i] = AddImage(inwardParticles[i], Color.clear, softGlowSprite);
            ResetInwardParticle(i, edge.rect, starts, progress, lifetime, baseSize, sway, brightness, colors);
            progress[i] = Random.Range(0f, 0.04f);
        }

        StartCoroutine(AnimateEdgeFlow(edge, rules, motes, inwardParticles, inwardImages,
            progress, lifetime, baseSize, sway, brightness, starts, colors));
    }

    private IEnumerator AnimateEdgeFlow(
        RectTransform edge,
        Image[] rules,
        RectTransform[] motes,
        RectTransform[] inwardParticles,
        Image[] inwardImages,
        float[] progress,
        float[] lifetime,
        float[] baseSize,
        float[] sway,
        float[] brightness,
        Vector2[] starts,
        Color[] colors)
    {
        float elapsed = 0f;
        while (edge != null && targetCelebrated)
        {
            float envelope = Mathf.Clamp01(elapsed / 0.32f) * (0.76f + Mathf.Sin(elapsed * 4.2f) * 0.24f);
            float shimmer = 0.72f + Mathf.Sin(elapsed * 15f) * 0.28f;
            for (int i = 0; i < rules.Length; i++)
            {
                Color ruleColor = i % 2 == 0 ? Gold : BrightGold;
                rules[i].color = WithAlpha(ruleColor, envelope * 0.42f * shimmer);
            }

            for (int i = 0; i < motes.Length; i++)
            {
                float moteProgress = Mathf.Repeat(elapsed * 0.56f + i / (float)motes.Length, 1f);
                Vector2 point = EdgePoint(edge.rect, moteProgress, 12f);
                Vector2 previousPoint = EdgePoint(edge.rect, moteProgress - 0.002f, 12f);
                Vector2 tangent = point - previousPoint;
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                motes[i].anchoredPosition = point;
                motes[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                CanvasGroup moteOpacity = motes[i].GetComponent<CanvasGroup>();
                if (moteOpacity == null) moteOpacity = motes[i].gameObject.AddComponent<CanvasGroup>();
                moteOpacity.alpha = envelope * (0.34f + 0.44f *
                    Mathf.Clamp01(Mathf.Sin((moteProgress + i * 0.07f) * Mathf.PI)));
            }

            float delta = Time.unscaledDeltaTime;
            for (int i = 0; i < inwardParticles.Length; i++)
            {
                progress[i] += delta / lifetime[i];
                if (progress[i] >= 1f)
                {
                    ResetInwardParticle(i, edge.rect, starts, progress, lifetime, baseSize,
                        sway, brightness, colors);
                }

                float t = Mathf.Clamp01(progress[i]);
                float eased = t * t * (3f - 2f * t);
                Vector2 start = starts[i];
                Vector2 perpendicular = new Vector2(-start.y, start.x).normalized;
                Vector2 point = Vector2.Lerp(start, Vector2.zero, eased) +
                                perpendicular * (Mathf.Sin(t * Mathf.PI) * sway[i]);
                inwardParticles[i].anchoredPosition = point;
                inwardParticles[i].localScale =
                    Vector3.one * (baseSize[i] / 8f) * Mathf.Lerp(1f, 0.64f, t);

                float alpha = Mathf.Clamp01(t / 0.055f) * Mathf.Pow(1f - t, 1.35f);
                alpha *= brightness[i] * (0.9f + shimmer * 0.1f);
                inwardImages[i].color = WithAlpha(colors[i], alpha);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (edge != null) Destroy(edge.gameObject);
        if (activeEdgeFlow == edge) activeEdgeFlow = null;
    }

    private static void ResetInwardParticle(
        int index,
        Rect rect,
        Vector2[] starts,
        float[] progress,
        float[] lifetime,
        float[] baseSize,
        float[] sway,
        float[] brightness,
        Color[] colors)
    {
        progress[index] = 0f;
        lifetime[index] = Random.Range(0.9f, 2.1f);
        baseSize[index] = Random.Range(3.2f, 13.5f);
        sway[index] = Random.Range(-54f, 54f);
        brightness[index] = Random.Range(0.3f, 0.64f);
        starts[index] = EdgePoint(rect, Random.value, Random.Range(0f, 5f));
        colors[index] = Random.value < 0.72f ? EffectWhite : EffectGold;
    }

    private void StopTargetCelebration()
    {
        if (activeEdgeFlow != null)
            Destroy(activeEdgeFlow.gameObject);
        activeEdgeFlow = null;
    }

    private static Vector2 EdgePoint(Rect rect, float progress, float inset)
    {
        float width = Mathf.Max(1f, rect.width - inset * 2f);
        float height = Mathf.Max(1f, rect.height - inset * 2f);
        float perimeter = width * 2f + height * 2f;
        float distance = Mathf.Repeat(progress, 1f) * perimeter;
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        if (distance < width) return new Vector2(-halfW + distance, halfH);
        distance -= width;
        if (distance < height) return new Vector2(halfW, halfH - distance);
        distance -= height;
        if (distance < width) return new Vector2(halfW - distance, -halfH);
        distance -= width;
        return new Vector2(-halfW, -halfH + distance);
    }

    private static Image AddImage(RectTransform rect, Color color, Sprite sprite = null)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Stretch(string name, Transform parent, float outset = 0f)
    {
        RectTransform rect = CreateRect(name, parent, Center, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * -outset;
        rect.offsetMax = Vector2.one * outset;
        return rect;
    }

    private Vector2 ToLayer(RectTransform target, Vector2 point) =>
        layer.InverseTransformPoint(target.TransformPoint(point));

    private Vector2 SizeInLayer(RectTransform target) =>
        ToLayer(target, target.rect.max) - ToLayer(target, target.rect.min);

    private void CreateHardEdgeMaterials()
    {
        hardStarTexture = CreateStarTexture(64);
        hardDiamondTexture = CreateDiamondTexture(48);
        coinParticleTexture = CreateCoinTexture(64);
        softGlowTexture = CreateSoftGlowTexture(48);
        orbitRingTexture = CreateOrbitRingTexture(96);
        triangleLineTexture = CreateTriangleLineTexture(64);
        flowerPetalTexture = CreateFlowerPetalTexture(96);
        hardStarSprite = Sprite.Create(hardStarTexture,
            new Rect(0f, 0f, hardStarTexture.width, hardStarTexture.height), Center, 100f);
        hardDiamondSprite = Sprite.Create(hardDiamondTexture,
            new Rect(0f, 0f, hardDiamondTexture.width, hardDiamondTexture.height), Center, 100f);
        coinParticleSprite = Sprite.Create(coinParticleTexture,
            new Rect(0f, 0f, coinParticleTexture.width, coinParticleTexture.height), Center, 100f);
        softGlowSprite = Sprite.Create(softGlowTexture,
            new Rect(0f, 0f, softGlowTexture.width, softGlowTexture.height), Center, 100f);
        orbitRingSprite = Sprite.Create(orbitRingTexture,
            new Rect(0f, 0f, orbitRingTexture.width, orbitRingTexture.height), Center, 100f);
        hardSparkMaterial = CreateParticleMaterial(hardDiamondTexture, "Soft Gold Spark");
        hardStarMaterial = CreateParticleMaterial(hardStarTexture, "Soft Arcane Star");
        coinParticleMaterial = CreateParticleMaterial(coinParticleTexture, "Soft Gold Coin");
        triangleLineMaterial = CreateParticleMaterial(triangleLineTexture, "Triangle Line Burst");
        flowerPetalMaterial = CreateParticleMaterial(flowerPetalTexture, "Rotating Flower Bloom");
    }

    private static Material CreateParticleMaterial(Texture2D texture, string materialName)
    {
        Shader shader = Shader.Find("Custom/CardEffectParticleGlow");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        Material material = new Material(shader) { name = materialName, mainTexture = texture };
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", Color.white);
        return material;
    }

    private static Texture2D CreateStarTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Soft Arcane Star");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float starAlpha = Mathf.Clamp01((1.29f - Mathf.Sqrt(nx) - Mathf.Sqrt(ny)) / 0.075f);
                float vertical = Mathf.Min((0.05f - nx) / 0.05f, (0.62f - ny) / 0.08f);
                float horizontal = Mathf.Min((0.05f - ny) / 0.05f, (0.62f - nx) / 0.08f);
                float coreAlpha = Mathf.Clamp01((0.19f - Mathf.Sqrt(nx * nx + ny * ny)) / 0.07f);
                float alpha = Mathf.Max(starAlpha, Mathf.Max(vertical, Mathf.Max(horizontal, coreAlpha)));
                pixels[y * size + x] = AlphaPixel(alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateDiamondTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Soft Gold Diamond");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float alpha = Mathf.Clamp01((0.94f - nx - ny) / 0.1f);
                pixels[y * size + x] = AlphaPixel(alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateCoinTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Soft Gold Coin");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                bool disc = radius <= 0.84f;
                bool groove = radius > 0.62f && radius < 0.68f;
                bool centerMark = Mathf.Abs(nx) + Mathf.Abs(ny) <= 0.16f;
                float edgeAlpha = Mathf.Clamp01((0.84f - radius) / 0.09f);
                float alpha = disc && (!groove || centerMark) ? edgeAlpha : 0f;
                pixels[y * size + x] = AlphaPixel(alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateTriangleLineTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Triangle Line Burst");
        Color32[] pixels = new Color32[size * size];
        Vector2 a = new Vector2(0f, -0.82f);
        Vector2 b = new Vector2(-0.76f, 0.56f);
        Vector2 c = new Vector2(0.76f, 0.56f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / size * 2f - 1f,
                    (y + 0.5f) / size * 2f - 1f);
                float distance = Mathf.Min(DistanceToSegment(point, a, b),
                    Mathf.Min(DistanceToSegment(point, b, c), DistanceToSegment(point, c, a)));
                float alpha = 1f - Mathf.Clamp01((distance - 0.028f) / 0.026f);
                pixels[y * size + x] = AlphaPixel(alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateFlowerPetalTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Rotating Flower Bloom");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2((x + 0.5f) / size * 2f - 1f,
                    (y + 0.5f) / size * 2f - 1f);
                float radius = point.magnitude;
                float alpha = 0f;
                for (int petalIndex = 0; petalIndex < 6; petalIndex++)
                {
                    float angle = petalIndex * Mathf.PI / 3f;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                    float radial = Vector2.Dot(point, direction) - 0.33f;
                    float sideways = Vector2.Dot(point, perpendicular);
                    float ellipse = radial * radial / (0.29f * 0.29f) +
                                    sideways * sideways / (0.14f * 0.14f);
                    float petal = 1f - Mathf.Clamp01((ellipse - 0.88f) / 0.14f);
                    alpha = Mathf.Max(alpha, petal);
                }

                float ring = 1f - Mathf.Clamp01((Mathf.Abs(radius - 0.76f) - 0.022f) / 0.026f);
                float center = 1f - Mathf.Clamp01((radius - 0.09f) / 0.05f);
                alpha = Mathf.Max(alpha, Mathf.Max(ring, center * 0.88f));
                pixels[y * size + x] = AlphaPixel(alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, start + segment * t);
    }

    private static Texture2D NewFeedbackTexture(int size, string textureName)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    private static Texture2D CreateSoftGlowTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Soft White-Gold Glow");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                pixels[y * size + x] = AlphaPixel(Mathf.Pow(Mathf.Clamp01(1f - radius), 1.8f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateOrbitRingTexture(int size)
    {
        Texture2D texture = NewFeedbackTexture(size, "Soft Orbit Ring");
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);
                float ring = 1f - Mathf.Clamp01(Mathf.Abs(radius - 0.72f) / 0.11f);
                pixels[y * size + x] = AlphaPixel(ring * ring);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Color32 AlphaPixel(float alpha)
    {
        return new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
    }

    private static Gradient CreateGoldGradient(Color accent, float opacity)
    {
        accent = RestrictToEffectPalette(accent);
        opacity *= 0.72f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(EffectWhite, 0f),
                new GradientColorKey(EffectGold, 0.2f),
                new GradientColorKey(accent, 0.68f),
                new GradientColorKey(accent, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(opacity, 0.06f),
                new GradientAlphaKey(opacity, 0.58f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Color RestrictToEffectPalette(Color color)
    {
        Color best = color.r + color.g + color.b > 2.78f ? EffectWhite : EffectGold;
        float bestDistance = ColorDistance(color, best);
        Color[] palette =
        {
            EffectWhite, EffectIvory, EffectGold, EffectGoldRed, EffectAmber, EffectRoseGold
        };
        foreach (Color candidate in palette)
        {
            float distance = ColorDistance(color, candidate);
            if (distance >= bestDistance) continue;
            best = candidate;
            bestDistance = distance;
        }
        return best;
    }

    private static float ColorDistance(Color left, Color right)
    {
        float r = left.r - right.r;
        float g = left.g - right.g;
        float b = left.b - right.b;
        return r * r + g * g + b * b;
    }

    private void NormalizeParticlePalette(ParticleSystem system, PassiveCardData.EffectType effectType)
    {
        Color accent = IsGoldEffect(effectType) ? EffectGold :
            effectType == PassiveCardData.EffectType.PegHitSplitPinball ? EffectGoldRed :
            IsTriangleEffect(effectType) ? EffectAmber :
            IsFlowerEffect(effectType) ? EffectRoseGold : EffectIvory;

        ParticleSystem.MainModule main = system.main;
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(EffectWhite, 0.6f), WithAlpha(accent, 0.42f));
        main.startSizeMultiplier *= 0.68f;

        ParticleSystem.ColorOverLifetimeModule colorLifetime = system.colorOverLifetime;
        if (colorLifetime.enabled) colorLifetime.color = CreateGoldGradient(accent, 0.74f);

        ParticleSystemRenderer[] renderers = system.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            renderer.sharedMaterial = hardStarMaterial != null ? hardStarMaterial :
                glowParticleMaterial != null ? glowParticleMaterial : particleMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.38f;
        }
    }

    private static bool IsGoldEffect(PassiveCardData.EffectType effectType)
    {
        return effectType == PassiveCardData.EffectType.DoubleGoldTargetUp ||
               effectType == PassiveCardData.EffectType.RoundEndGoldChance ||
               effectType == PassiveCardData.EffectType.Spawn16AddOneGold;
    }

    private static bool IsExtraBallEffect(PassiveCardData.EffectType effectType)
    {
        return effectType == PassiveCardData.EffectType.AddOnePinball ||
               effectType == PassiveCardData.EffectType.Hit40AddOnePinball ||
               effectType == PassiveCardData.EffectType.RethrowPinball ||
               effectType == PassiveCardData.EffectType.PegHitExtraPinball ||
               effectType == PassiveCardData.EffectType.FirstSlotAddOnePinball;
    }

    private static bool IsTriangleEffect(PassiveCardData.EffectType effectType)
    {
        return effectType == PassiveCardData.EffectType.AddPinballScore ||
               effectType == PassiveCardData.EffectType.SlotMultiplier15 ||
               effectType == PassiveCardData.EffectType.FirstPinballTenTimes ||
               effectType == PassiveCardData.EffectType.PegHitScore ||
               effectType == PassiveCardData.EffectType.RandomDoubleSlot ||
               effectType == PassiveCardData.EffectType.AllScoreMultiplier15;
    }

    private static bool IsFlowerEffect(PassiveCardData.EffectType effectType)
    {
        return effectType == PassiveCardData.EffectType.HalfSidesCenter075 ||
               effectType == PassiveCardData.EffectType.ThirdSlotGrowth ||
               effectType == PassiveCardData.EffectType.RandomLeftBounceChance ||
               effectType == PassiveCardData.EffectType.UnusedThrowBonus ||
               effectType == PassiveCardData.EffectType.DuplicateShopSeventhSlot;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static IEnumerator Animate(float duration, Action<float> update, Action complete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            update(elapsed / duration);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        complete();
    }
}
