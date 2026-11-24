using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// [Jinyang.Li] Skill-effect trigger/mapping/pooling.
// This file deliberately owns only effect spawning so it stays separate from
// the leader's card gameplay and the general UI feedback implementation.
public partial class GameFeedback
{
    private const int MaxConcurrentEffectsPerType = 3;
    private const float SkillEffectLifetime = 1.25f;
    private const float SlotFlashWindow = 1.2f;
    private const float SlotFlashScoreThreshold = 150f;
    private const float SlotFlashCooldown = 1.15f;
    private const float MilestonePeakOpacity = 0.22f;
    private const float MilestoneGlowOpacity = 0.04f;

    // Effect_1..Effect_5 are reserved for every valid peg hit.
    private static readonly PassiveCardData.EffectType[] PegHitEffectPool =
    {
        PassiveCardData.EffectType.AddOnePinball,
        PassiveCardData.EffectType.AddPinballScore,
        PassiveCardData.EffectType.HalfSidesCenter075,
        PassiveCardData.EffectType.Hit40AddOnePinball,
        PassiveCardData.EffectType.SlotMultiplier15
    };

    // Effect_6 and Effect_7 are reserved for score-slot entry.
    private static readonly PassiveCardData.EffectType[] SlotEntryEffectPool =
    {
        PassiveCardData.EffectType.FirstPinballTenTimes,
        PassiveCardData.EffectType.PegHitScore
    };

    // Cached Resources prefabs, reusable instances and their ParticleSystems.
    private readonly Dictionary<PassiveCardData.EffectType, GameObject> skillEffectPrefabs = new();
    private readonly Dictionary<PassiveCardData.EffectType, Stack<GameObject>> skillEffectPool = new();
    private readonly Dictionary<GameObject, ParticleSystem[]> skillEffectSystems = new();
    private readonly List<PlayingSkillEffect> activeSkillEffects = new(16);
    private readonly Dictionary<ParticleSystem, EffectBaseline> effectBaselines = new();
    private readonly Dictionary<ScoreSlot, SlotFlashState> slotFlashStates = new();
    private TarotIconLibrary tarotIconLibrary;
    private int scoreMilestoneIndex;
    private int scoreMilestoneRound = -1;
    private RectTransform activeMilestoneContainer;
    private Text activeMilestoneLabel;
    private Image activeMilestoneGlow;
    private CanvasGroup activeMilestoneOpacity;
    private Font milestoneFont;
    private bool milestoneFontOwned;
    private MilestonePhase milestonePhase;
    private float milestonePhaseStartedAt;
    private int displayedMilestone;

    private enum MilestonePhase
    {
        None,
        Appearing,
        Holding,
        Fading
    }

    private struct PlayingSkillEffect
    {
        public PassiveCardData.EffectType effectType;
        public GameObject instance;
        public float expiresAt;
    }

    private struct EffectBaseline
    {
        public float startSizeMultiplier;
        public Color startColorMin;
        public Color startColorMax;
    }

    private sealed class SlotFlashState
    {
        public float windowStartedAt;
        public float accumulatedScore;
        public float nextAllowedAt;
    }

    // [Jinyang.Li] Loads Resources/SkillEffects/Effect_N and plays it at a world
    // position. The optional signature burst is disabled for collision effects to
    // avoid spawning several extra particle systems on every peg or slot hit.
    public void PlaySkillEffect(PassiveCardData.EffectType effectType, Vector3 position,
        bool playSignature = true, float overallScale = 1f,
        float particleSizeMultiplier = 1f, float opacityMultiplier = 1f)
    {
        if (effectType == PassiveCardData.EffectType.None) return;

        if (!skillEffectPrefabs.TryGetValue(effectType, out GameObject prefab))
        {
            prefab = Resources.Load<GameObject>("SkillEffects/Effect_" + (int)effectType);
            skillEffectPrefabs.Add(effectType, prefab);
        }

        if (prefab == null) return;

        GameObject instance = AcquireSkillEffect(effectType, prefab);
        instance.transform.SetPositionAndRotation(position, Quaternion.identity);
        instance.transform.localScale = Vector3.one * overallScale;
        instance.SetActive(true);

        bool isNewInstance = !skillEffectSystems.TryGetValue(instance, out ParticleSystem[] systems);
        if (isNewInstance)
        {
            systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            skillEffectSystems.Add(instance, systems);
        }

        foreach (ParticleSystem system in systems)
        {
            // [Jinyang.Li] Fixes Unity's "Setting duration while system is still
            // playing" warning. Always stop/clear before Play/particle settings.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (isNewInstance)
            {
                NormalizeParticlePalette(system, effectType);
                if (effectType == PassiveCardData.EffectType.RandomDoubleSlot)
                    ConfigureEffect8(system);
                effectBaselines[system] = new EffectBaseline
                {
                    startSizeMultiplier = system.main.startSizeMultiplier,
                    startColorMin = system.main.startColor.colorMin,
                    startColorMax = system.main.startColor.colorMax
                };
            }

            ApplyEffectPresentation(system, particleSizeMultiplier, opacityMultiplier);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            system.Play(true);
        }

        activeSkillEffects.Add(new PlayingSkillEffect
        {
            effectType = effectType,
            instance = instance,
            expiresAt = Time.unscaledTime + SkillEffectLifetime
        });

        if (playSignature && (Time.unscaledTime - lastBurstTime > 0.06f ||
            Vector3.SqrMagnitude(lastBurstPosition - position) > 0.01f))
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
    }

    // [Jinyang.Li] Every valid peg contact randomly chooses Effect_1..Effect_5.
    private void OnPinballHitPeg(Pinball pinball, Collider2D peg)
    {
        if (pinball == null || peg == null) return;

        PassiveCardData.EffectType effect =
            PegHitEffectPool[Random.Range(0, PegHitEffectPool.Length)];
        Vector2 hitPoint = peg.ClosestPoint(pinball.transform.position);
        // Peg hits stay subtle so rapid collisions do not cover the board.
        PlaySkillEffect(effect, hitPoint, false, 0.64f, 0.78f, 0.7f);
    }

    // [Jinyang.Li] Score-slot entry randomly chooses Effect_6/Effect_7 and always
    // adds Effect_8 (RandomDoubleSlot).
    private void OnPinballEnteredSlot(ScoreSlot slot)
    {
        if (slot == null) return;

        Vector3 position = slot.transform.position;
        PassiveCardData.EffectType effect =
            SlotEntryEffectPool[Random.Range(0, SlotEntryEffectPool.Length)];
        // [Jinyang.Li] Slot scoring is intentionally stronger than peg hits.
        PlaySkillEffect(effect, position, false, 1.18f, 1.16f, 1f);
        PlaySkillEffect(PassiveCardData.EffectType.RandomDoubleSlot, position,
            false, 1.14f, 1.12f, 1f);
        TryPlaySlotIconFlash(slot);
    }

    // [Jinyang.Li] Applies per-trigger scale and opacity without changing prefab
    // gravity; peg hits use the subtle values from OnPinballHitPeg.
    private void ApplyEffectPresentation(ParticleSystem system,
        float particleSizeMultiplier, float opacityMultiplier)
    {
        if (!effectBaselines.TryGetValue(system, out EffectBaseline baseline))
        {
            baseline = new EffectBaseline
            {
                startSizeMultiplier = system.main.startSizeMultiplier,
                startColorMin = system.main.startColor.colorMin,
                startColorMax = system.main.startColor.colorMax
            };
            effectBaselines[system] = baseline;
        }

        ParticleSystem.MainModule main = system.main;
        float safeOpacity = Mathf.Clamp01(opacityMultiplier);
        Color colorMin = baseline.startColorMin;
        Color colorMax = baseline.startColorMax;
        colorMin.a *= safeOpacity;
        colorMax.a *= safeOpacity;
        main.startColor = new ParticleSystem.MinMaxGradient(colorMin, colorMax);
        main.startSizeMultiplier = baseline.startSizeMultiplier *
                                   Mathf.Max(0.1f, particleSizeMultiplier);
    }

    // [Jinyang.Li] Effect_8 is a compact, equal-sized radial streamline burst.
    private void ConfigureEffect8(ParticleSystem system)
    {
        ParticleSystem.MainModule main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.44f);
        main.startSpeed = 1.8f;
        main.startSize = 0.048f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0f;
        main.maxParticles = 36;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.015f;
        shape.radiusThickness = 0f;
        shape.arc = 360f;

        ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = 2.4f;

        ParticleSystemRenderer[] renderers =
            system.GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer renderer in renderers)
        {
            renderer.sharedMaterial = streamlineMaterial != null
                ? streamlineMaterial
                : hardSparkMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.1f;
            renderer.lengthScale = 2.1f;
            renderer.maxParticleSize = 0.12f;
            renderer.sortingOrder = 25;
        }
    }

    // [Jinyang.Li] Detects a high-scoring slot within a short time window and
    // chooses a random tarot icon for the flash.
    private void TryPlaySlotIconFlash(ScoreSlot slot)
    {
        GameManager game = GameManager.instance;
        if (game == null || game.LastPinballAward <= 0f) return;

        float now = Time.unscaledTime;
        if (!slotFlashStates.TryGetValue(slot, out SlotFlashState state))
        {
            state = new SlotFlashState
            {
                windowStartedAt = now,
                nextAllowedAt = now
            };
            slotFlashStates.Add(slot, state);
        }

        if (now - state.windowStartedAt > SlotFlashWindow)
        {
            state.windowStartedAt = now;
            state.accumulatedScore = 0f;
        }

        state.accumulatedScore += game.LastPinballAward;
        if (now < state.nextAllowedAt ||
            state.accumulatedScore < SlotFlashScoreThreshold)
            return;

        if (tarotIconLibrary == null)
            tarotIconLibrary = Resources.Load<TarotIconLibrary>("TarotIconLibrary");
        if (tarotIconLibrary == null || tarotIconLibrary.icons.Length == 0) return;

        state.accumulatedScore = 0f;
        state.windowStartedAt = now;
        state.nextAllowedAt = now + SlotFlashCooldown;
        Sprite icon = tarotIconLibrary.icons[
            Random.Range(0, tarotIconLibrary.icons.Length)];
        StartCoroutine(PlaySlotIconFlash(slot.transform.position, icon));
    }

    // [Jinyang.Li] Top-layer tarot icon flash: pulse, enlarge and fade at a slot.
    private IEnumerator PlaySlotIconFlash(Vector3 worldPosition, Sprite icon)
    {
        Camera worldCamera = Camera.main;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            worldCamera, worldPosition);
        Canvas canvas = GetComponent<Canvas>();
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layer, screenPosition, uiCamera, out Vector2 localPosition))
            yield break;

        RectTransform container = CreateRect("Score Slot Tarot Layer", layer, Center,
            localPosition, new Vector2(250f, 250f));
        container.SetAsLastSibling();
        Canvas iconCanvas = container.gameObject.AddComponent<Canvas>();
        iconCanvas.overrideSorting = true;
        iconCanvas.sortingOrder = 500;
        CanvasGroup containerOpacity = container.gameObject.AddComponent<CanvasGroup>();
        containerOpacity.alpha = 0f;

        RectTransform glowRect = CreateRect("Score Slot Tarot Glow", container, Center,
            Vector2.zero, new Vector2(260f, 260f));
        Image glow = AddImage(glowRect, WithAlpha(Gold, 0f), softGlowSprite);

        RectTransform iconRect = CreateRect("Score Slot Tarot Flash", container, Center,
            Vector2.zero, new Vector2(210f, 210f));
        Image image = AddImage(iconRect, new Color(1f, 0.88f, 0.68f, 1f), icon);
        image.preserveAspect = true;

        const float holdDuration = 0.34f;
        const float fadeDuration = 0.44f;
        float elapsed = 0f;
        while (elapsed < holdDuration + fadeDuration)
        {
            if (elapsed < holdDuration)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 36f);
                float alpha = 0.32f + pulse * 0.22f;
                container.localScale = Vector3.one * (1f + pulse * 0.035f);
                containerOpacity.alpha = alpha;
                glow.color = WithAlpha(Gold, alpha * 0.35f);
            }
            else
            {
                float t = Mathf.Clamp01((elapsed - holdDuration) / fadeDuration);
                float eased = t * t * (3f - 2f * t);
                container.localScale = Vector3.one * Mathf.Lerp(1f, 1.55f, eased);
                containerOpacity.alpha = Mathf.Lerp(0.54f, 0f, eased);
                glow.color = WithAlpha(Gold, Mathf.Lerp(0.22f, 0f, eased));
                container.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(t * Mathf.PI) * 4f);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(container.gameObject);
    }

    // [Jinyang.Li] Detects 100/200/500/1000/1500 milestones and every 500 after.
    // The newest value is rendered by one reusable state-driven number.
    private void UpdateScoreMilestones(GameManager game)
    {
        if (scoreMilestoneRound != game.currentRound)
        {
            scoreMilestoneRound = game.currentRound;
            scoreMilestoneIndex = 0;
            ClearScoreMilestone();
        }

        int crossed = 0;
        while (crossed < 8 && game.score >= ScoreMilestoneAt(scoreMilestoneIndex))
        {
            int milestone = ScoreMilestoneAt(scoreMilestoneIndex);
            if (activeMilestoneContainer == null)
                ShowNewScoreMilestone(milestone);
            else
                UpdateScoreMilestoneValue(milestone);
            scoreMilestoneIndex++;
            crossed++;
        }

        AnimateScoreMilestone();
    }

    // [Jinyang.Li] Returns the milestone threshold for a score index.
    private static int ScoreMilestoneAt(int index)
    {
        return index switch
        {
            0 => 100,
            1 => 200,
            2 => 500,
            _ => 1000 + (index - 3) * 500
        };
    }

    // [Jinyang.Li] Creates the single reusable milestone number and moves it
    // slightly above screen center.
    private void ShowNewScoreMilestone(int value)
    {
        activeMilestoneContainer = CreateRect("Score Milestone", layer, Center,
            new Vector2(0f, 48f), new Vector2(2200f, 850f));
        activeMilestoneContainer.SetAsLastSibling();
        Canvas milestoneCanvas = activeMilestoneContainer.gameObject.AddComponent<Canvas>();
        milestoneCanvas.overrideSorting = true;
        milestoneCanvas.sortingOrder = 400;
        activeMilestoneOpacity =
            activeMilestoneContainer.gameObject.AddComponent<CanvasGroup>();
        activeMilestoneOpacity.alpha = 0f;

        RectTransform glowRect = CreateRect("Score Milestone Glow",
            activeMilestoneContainer, Center, Vector2.zero,
            new Vector2(1900f, 700f));
        activeMilestoneGlow = AddImage(glowRect,
            WithAlpha(MutedDarkGold, 0f), softGlowSprite);

        RectTransform numberRect = CreateRect("Score Milestone Number",
            activeMilestoneContainer, Center, Vector2.zero,
            new Vector2(2200f, 850f));
        activeMilestoneLabel = numberRect.gameObject.AddComponent<Text>();
        activeMilestoneLabel.font = GetMilestoneFont();
        activeMilestoneLabel.fontSize = 590;
        activeMilestoneLabel.fontStyle = FontStyle.Bold;
        activeMilestoneLabel.alignment = TextAnchor.MiddleCenter;
        activeMilestoneLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        activeMilestoneLabel.verticalOverflow = VerticalWrapMode.Overflow;
        activeMilestoneLabel.raycastTarget = false;
        activeMilestoneLabel.color = MutedDarkGold;

        displayedMilestone = value;
        activeMilestoneLabel.text = value.ToString("N0");
        activeMilestoneOpacity.alpha = 0f;
        activeMilestoneContainer.localScale = Vector3.one * 0.94f;
        milestonePhase = MilestonePhase.Appearing;
        milestonePhaseStartedAt = Time.unscaledTime;
    }

    // [Jinyang.Li] Directly iterates to a newer milestone and resets the hold.
    // This also cancels an in-progress fade and returns to the stable hold state.
    private void UpdateScoreMilestoneValue(int value)
    {
        displayedMilestone = value;
        activeMilestoneLabel.text = value.ToString("N0");

        // A newer milestone takes over immediately. If the old number was fading,
        // this cancels the fade and returns it to the fully stable hold phase.
        milestonePhase = MilestonePhase.Holding;
        milestonePhaseStartedAt = Time.unscaledTime;
        activeMilestoneContainer.localScale = Vector3.one;
        activeMilestoneOpacity.alpha = MilestonePeakOpacity;
        activeMilestoneGlow.color = WithAlpha(MutedDarkGold, MilestoneGlowOpacity);
    }

    // [Jinyang.Li] Three non-flashing phases: appear, hold, enlarge and fade.
    private void AnimateScoreMilestone()
    {
        if (activeMilestoneContainer == null) return;

        float elapsed = Time.unscaledTime - milestonePhaseStartedAt;
        switch (milestonePhase)
        {
            case MilestonePhase.Appearing:
            {
                const float duration = 0.28f;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                activeMilestoneContainer.localScale =
                    Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                activeMilestoneOpacity.alpha =
                    Mathf.Lerp(0f, MilestonePeakOpacity, eased);
                activeMilestoneGlow.color = WithAlpha(MutedDarkGold,
                    activeMilestoneOpacity.alpha * 0.15f);
                if (t >= 1f)
                {
                    milestonePhase = MilestonePhase.Holding;
                    milestonePhaseStartedAt = Time.unscaledTime;
                }
                break;
            }
            case MilestonePhase.Holding:
                activeMilestoneOpacity.alpha = MilestonePeakOpacity;
                activeMilestoneContainer.localScale = Vector3.one;
                activeMilestoneGlow.color =
                    WithAlpha(MutedDarkGold, MilestoneGlowOpacity);
                if (elapsed >= 0.72f)
                {
                    milestonePhase = MilestonePhase.Fading;
                    milestonePhaseStartedAt = Time.unscaledTime;
                }
                break;
            case MilestonePhase.Fading:
            {
                const float duration = 0.42f;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                activeMilestoneOpacity.alpha =
                    Mathf.Lerp(MilestonePeakOpacity, 0f, eased);
                activeMilestoneContainer.localScale =
                    Vector3.one * Mathf.Lerp(1f, 1.24f, eased);
                activeMilestoneGlow.color =
                    WithAlpha(MutedDarkGold,
                        Mathf.Lerp(MilestoneGlowOpacity, 0f, eased));
                if (t >= 1f)
                    ClearScoreMilestone();
                break;
            }
        }
    }

    // [Jinyang.Li] Removes the current milestone display at the end of its fade.
    private void ClearScoreMilestone()
    {
        if (activeMilestoneContainer != null)
            Destroy(activeMilestoneContainer.gameObject);
        activeMilestoneContainer = null;
        activeMilestoneLabel = null;
        activeMilestoneGlow = null;
        activeMilestoneOpacity = null;
        milestonePhase = MilestonePhase.None;
        displayedMilestone = 0;
    }

    // [Jinyang.Li] Loads a hard-edged system font for milestone digits.
    private Font GetMilestoneFont()
    {
        if (milestoneFont != null) return milestoneFont;

        Font osFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Bahnschrift SemiBold", "Arial Black", "Impact", "Arial" }, 590);
        if (osFont != null)
        {
            milestoneFont = osFont;
            milestoneFontOwned = true;
        }
        else
        {
            milestoneFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            milestoneFontOwned = false;
        }
        return milestoneFont;
    }

    // [Jinyang.Li] Reuses up to three concurrent instances of each effect type.
    // This avoids per-collision Instantiate/Destroy allocations.
    private GameObject AcquireSkillEffect(PassiveCardData.EffectType effectType,
        GameObject prefab)
    {
        if (!skillEffectPool.TryGetValue(effectType, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>(MaxConcurrentEffectsPerType);
            skillEffectPool.Add(effectType, pool);
        }

        if (pool.Count == 0)
        {
            int activeOfType = 0;
            int oldestIndex = -1;
            float oldestTime = float.PositiveInfinity;
            for (int i = 0; i < activeSkillEffects.Count; i++)
            {
                PlayingSkillEffect active = activeSkillEffects[i];
                if (active.effectType != effectType) continue;
                activeOfType++;
                if (active.expiresAt < oldestTime)
                {
                    oldestTime = active.expiresAt;
                    oldestIndex = i;
                }
            }

            if (activeOfType >= MaxConcurrentEffectsPerType && oldestIndex >= 0)
            {
                GameObject oldest = activeSkillEffects[oldestIndex].instance;
                activeSkillEffects.RemoveAt(oldestIndex);
                ReturnSkillEffect(effectType, oldest);
            }
        }

        GameObject instance = pool.Count > 0
            ? pool.Pop()
            : Instantiate(prefab);
        instance.name = "Effect_" + (int)effectType + " (" + effectType + ")";
        return instance;
    }

    // [Jinyang.Li] Returns expired triggered effects to their object pools.
    private void RecycleExpiredSkillEffects()
    {
        for (int i = activeSkillEffects.Count - 1; i >= 0; i--)
        {
            PlayingSkillEffect active = activeSkillEffects[i];
            if (Time.unscaledTime < active.expiresAt) continue;

            activeSkillEffects.RemoveAt(i);
            ReturnSkillEffect(active.effectType, active.instance);
        }
    }

    // [Jinyang.Li] Stops an effect and returns it to the matching pool.
    private void ReturnSkillEffect(PassiveCardData.EffectType effectType, GameObject instance)
    {
        if (instance == null) return;

        if (skillEffectSystems.TryGetValue(instance, out ParticleSystem[] systems))
        {
            foreach (ParticleSystem system in systems)
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        instance.SetActive(false);

        if (!skillEffectPool.TryGetValue(effectType, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>(MaxConcurrentEffectsPerType);
            skillEffectPool.Add(effectType, pool);
        }
        pool.Push(instance);
    }
}
