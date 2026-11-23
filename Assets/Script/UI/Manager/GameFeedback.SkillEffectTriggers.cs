using System.Collections.Generic;
using UnityEngine;

// [Jinyang.Li] Skill-effect trigger/mapping/pooling.
// This file deliberately owns only effect spawning so it stays separate from
// the leader's card gameplay and the general UI feedback implementation.
public partial class GameFeedback
{
    private const int MaxConcurrentEffectsPerType = 3;
    private const float SkillEffectLifetime = 1.25f;

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

    private struct PlayingSkillEffect
    {
        public PassiveCardData.EffectType effectType;
        public GameObject instance;
        public float expiresAt;
    }

    // [Jinyang.Li] Loads Resources/SkillEffects/Effect_N and plays it at a world
    // position. The optional signature burst is disabled for collision effects to
    // avoid spawning several extra particle systems on every peg or slot hit.
    public void PlaySkillEffect(PassiveCardData.EffectType effectType, Vector3 position,
        bool playSignature = true)
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
        instance.SetActive(true);

        bool isNewInstance = !skillEffectSystems.TryGetValue(instance, out ParticleSystem[] systems);
        if (isNewInstance)
        {
            systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            skillEffectSystems.Add(instance, systems);
        }

        foreach (ParticleSystem system in systems)
        {
            if (isNewInstance)
                NormalizeParticlePalette(system, effectType);

            // [Jinyang.Li] Fixes Unity's "Setting duration while system is still
            // playing" warning. Always stop/clear before Play/particle settings.
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
        PlaySkillEffect(effect, hitPoint, false);
    }

    // [Jinyang.Li] Score-slot entry randomly chooses Effect_6/Effect_7 and always
    // adds Effect_8 (RandomDoubleSlot).
    private void OnPinballEnteredSlot(ScoreSlot slot)
    {
        if (slot == null) return;

        Vector3 position = slot.transform.position;
        PassiveCardData.EffectType effect =
            SlotEntryEffectPool[Random.Range(0, SlotEntryEffectPool.Length)];
        PlaySkillEffect(effect, position, false);
        PlaySkillEffect(PassiveCardData.EffectType.RandomDoubleSlot, position, false);
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
