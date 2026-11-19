# Skill Effect Prefabs

`Effect_1.prefab` through `Effect_20.prefab` are independent Particle System
prefabs. They are intentionally simple placeholders so each card effect can be
edited without changing the other cards.

The number is the integer value of `CardData.EffectType`:

| File | EffectType |
| --- | --- |
| Effect_1 | AddOnePinball |
| Effect_2 | AddPinballScore |
| Effect_3 | HalfSidesCenter075 |
| Effect_4 | Hit40AddOnePinball |
| Effect_5 | SlotMultiplier15 |
| Effect_6 | FirstPinballTenTimes |
| Effect_7 | PegHitScore |
| Effect_8 | RandomDoubleSlot |
| Effect_9 | DoubleGoldTargetUp |
| Effect_10 | RethrowPinball |
| Effect_11 | ThirdSlotGrowth |
| Effect_12 | RoundEndGoldChance |
| Effect_13 | PegHitExtraPinball |
| Effect_14 | Spawn16AddOneGold |
| Effect_15 | AllScoreMultiplier15 |
| Effect_16 | FirstSlotAddOnePinball |
| Effect_17 | PegHitSplitPinball |
| Effect_18 | RandomLeftBounceChance |
| Effect_19 | UnusedThrowBonus |
| Effect_20 | DuplicateShopSeventhSlot |

When `PassiveCard.Flash()` runs, `GameFeedback.PlaySkillEffect()` loads the
matching prefab from `Resources/SkillEffects`, instantiates it at the existing
effect position, and plays every Particle System in its hierarchy. Effects that
already have a positional burst use that exact position; card-only feedback
uses the card image position.

`Effect_Glow.mat` and `CardEffectParticleGlow.shader` provide additive blending
for the common glow look. The existing firework burst in `GameFeedback.Burst()`
also uses this material when available.

The prefabs currently stop after their particle lifetime. A safety cleanup also
removes any instance after eight seconds.
