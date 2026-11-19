using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;

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
    [SerializeField] private Color splitColor = new Color(0.25f, 0.9f, 1f);
    [SerializeField] private Color spawnColor = new Color(0.45f, 1f, 0.4f);
    [SerializeField] private Color despawnColor = new Color(0.75f, 0.45f, 1f);
    [SerializeField] private Color ballValueColor = new Color(1f, 0.56f, 0.24f);
    [SerializeField] private Color scoreColor = new Color(1f, 0.45f, 0.72f);
    [SerializeField] private Color goldColor = new Color(1f, 0.78f, 0.3f);
    [SerializeField] private Color ballStockColor = new Color(0.35f, 0.68f, 1f);

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
    private RectTransform layer;
    private TMP_FontAsset font;
    private RectTransform roundHud;
    private RectTransform goldHud;
    private RectTransform ballValueHud;
    private bool showingResults;
    private Material glowParticleMaterial;
    private readonly Dictionary<CardData.EffectType, GameObject> skillEffectPrefabs = new();

    private void Awake()
    {
        Instance = this;
        glowParticleMaterial = Resources.Load<Material>("SkillEffects/Effect_Glow");
        layer = CreateRect("Gameplay Feedback", transform, Center, Vector2.zero, Vector2.zero);
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
                    ballValueHud = rect;
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowRoundChange() => ShowDelta(roundHud, "+1", Color.white, alignRight: true);

    public void ShowGoldChange(int amount)
    {
        if (amount == 0) return;
        ShowDelta(goldHud, amount.ToString("+0;-0"), amount > 0 ? goldColor : new Color(1f, 0.55f, 0.42f),
            coinSprite, alignRight: true);
    }

    public void ShowBallValueChange(float amount) =>
        ShowDelta(ballValueHud, amount.ToString("+0.##"), ballValueColor);

    private void ShowDelta(RectTransform target, string value, Color color, Sprite icon = null, bool alignRight = false)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        const float popupWidth = 126f;
        float x = alignRight ? target.rect.xMax - popupWidth * 0.5f : target.rect.center.x;
        Vector2 start = ToLayer(target, new Vector2(x, target.rect.yMax)) + Vector2.up * 20f;
        RectTransform popup = CreateRect("HUD Change", layer, Center, start, new Vector2(popupWidth, 46f));
        Image background = AddImage(popup, new Color(0.07f, 0.05f, 0.1f, 0.94f));
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

        const float duration = 0.4f;
        float elapsed = 0f;
        while (card.isActiveAndEnabled && elapsed < duration)
        {
            flash.color = WithAlpha(Color.white, Mathf.Sin(elapsed / duration * Mathf.PI) * 0.55f);
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        Destroy(flash.gameObject);
    }

    public void Burst(CardEffectVisual effect, Vector3 position)
    {
        Material material = glowParticleMaterial != null ? glowParticleMaterial : particleMaterial;
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

        GameObject burst = new GameObject(effect + " Firework");
        burst.SetActive(false);
        burst.transform.position = position;

        // Small, dense sparks form the firework. The additive material makes every
        // overlapping spark read as emissive even without a Bloom post-process pass.
        ParticleSystem sparks = burst.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sparks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.72f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.1f, 5.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.startColor = effectColor;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.16f;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = sparks.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 44),
            new ParticleSystem.Burst(0.035f, 16)
        });

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.045f;
        shape.arc = 360f;

        ParticleSystem.SizeOverLifetimeModule size = sparks.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.24f));

        ParticleSystem.ColorOverLifetimeModule fade = sparks.colorOverLifetime;
        fade.enabled = true;
        Gradient sparkGradient = new Gradient();
        sparkGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(effectColor, 0.2f),
                new GradientColorKey(effectColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        fade.color = sparkGradient;

        ParticleSystemRenderer sparkRenderer = burst.GetComponent<ParticleSystemRenderer>();
        sparkRenderer.sharedMaterial = material;
        sparkRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        sparkRenderer.sortingOrder = 22;

        ParticleSystem halo = AddGlowHalo(burst.transform, effectColor, material);
        burst.SetActive(true);
        halo.Play();
        sparks.Play();
    }

    private static ParticleSystem AddGlowHalo(Transform parent, Color color, Material material)
    {
        GameObject haloObject = new GameObject("Soft Glow Halo");
        haloObject.transform.SetParent(parent, false);
        ParticleSystem halo = haloObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = halo.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = 0.2f;
        main.startSpeed = 0f;
        main.startSize = 0.42f;
        main.startColor = WithAlpha(color, 0.34f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = halo.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        ParticleSystem.ColorOverLifetimeModule fade = halo.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        ParticleSystemRenderer renderer = haloObject.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 21;
        return halo;
    }

    public void PlaySkillEffect(CardData.EffectType effectType, Vector3 position)
    {
        if (effectType == CardData.EffectType.None) return;

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
            system.Play(true);

        // Cleanup only protects against abandoned instances. Hand-made effects
        // can run for several seconds and still finish normally.
        Destroy(instance, 8f);
    }

    public void ShowResults(int score, bool cleared, CardInventory inventory)
    {
        if (showingResults) return;
        showingResults = true;
        RectTransform screen = Stretch("Results", transform);
        Image backdrop = AddImage(screen, new Color(0.07f, 0.047f, 0.11f, 0.97f));
        backdrop.raycastTarget = true;

        Vector2 top = new Vector2(0.5f, 0.82f);
        AddText(screen, "YOUR CARDS", 32f, goldColor, top, new Vector2(0f, 126f), new Vector2(360f, 50f));
        PassiveCard[] cards = inventory.GetComponentsInChildren<PassiveCard>();
        if (cards.Length == 0)
            AddText(screen, "No cards collected", 26f, Color.white, top, Vector2.zero, new Vector2(360f, 50f));
        for (int i = 0; i < cards.Length; i++)
        {
            float x = (i - (cards.Length - 1) * 0.5f) * 112f;
            RectTransform thumbnail = CreateRect(cards[i].data.cardName, screen, top, new Vector2(x, 15f), new Vector2(102f, 155f));
            AddImage(thumbnail, Color.white, cards[i].data.cardSprite).preserveAspect = true;
        }

        AddText(screen, cleared ? "ALL ROUNDS CLEARED" : "RUN OVER", 36f, goldColor, Center, new Vector2(0f, 50f), new Vector2(620f, 52f));
        AddText(screen, score.ToString("N0"), 112f, Color.white, Center, new Vector2(0f, -38f), new Vector2(650f, 138f));
        AddText(screen, "TOTAL SCORE", 25f, goldColor, Center, new Vector2(0f, -122f), new Vector2(300f, 40f));

        Button retry = AddButton(screen, "Play Again", -140f, goldColor, new Color(0.1f, 0.06f, 0.13f));
        retry.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));
        Button quit = AddButton(screen, "Quit Game", 140f, new Color(0.28f, 0.18f, 0.34f), Color.white);
        quit.onClick.AddListener(QuitGame);
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(retry.gameObject);
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
        RectTransform rect = CreateRect(label, parent, new Vector2(0.5f, 0.2f),
            new Vector2(horizontal, 0f), new Vector2(244f, 65f));
        Image image = AddImage(rect, fill);
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddText(rect, label, 30f, textColor, Center, Vector2.zero, rect.sizeDelta);
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
