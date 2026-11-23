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

The same prefabs are also used for ambient gameplay feedback:

- every valid peg hit randomly plays one of `Effect_1` through `Effect_5` at
  the contact point;
- every ball entering a score slot randomly plays `Effect_6` or `Effect_7`;
- `Effect_8` is always played when a ball enters a score slot.

These triggers subscribe to `GameManager.PinballHitPeg` and
`GameManager.PinballEnteredSlot`, so they do not require changes in the ball or
slot scripts. Ambient effects use a cached object pool (at most three active
instances per effect type) instead of instantiating and destroying a prefab on
every collision.

`Effect_Glow.mat` and `CardEffectParticleGlow.shader` provide additive blending
for the common glow look. The existing firework burst in `GameFeedback.Burst()`
also uses this material when available.

At runtime, `GameFeedback.PlaySkillEffect()` also remaps every loaded effect to
a low-saturation white, pale gold and muted gold-red palette, switches particle
renderers to flat billboards, and adds category-specific feedback:

- gold-gain effects emit small falling coin particles;
- extra-ball effects show a bright `+1` flash;
- split-ball effects show an infinity sigil above the split position.
- multiplier and score effects send a small burst of rotating triangle-line
  particles outward;
- state, growth and bounce effects open a rotating six-petal flower ring with
  a few tiny petal sparks.

The common burst uses small, softly feathered procedural star and diamond
textures. Its brightness comes from additive blending plus higher foreground
opacity, so it remains legible without depending on Bloom or a blur pass.
Signature effects use saturated white-gold accents while staying within the
existing effect footprint.

Once the target score is reached, the screen-edge flow starts a long-lived
stream of small white-gold glow particles travelling toward the centre. Both
the edge flow and the inward particle stream stop when `RoundEnded` fires.
The inward stream is capped at 96 pooled UI images so the celebration remains
visible without updating hundreds of transforms every frame.

The prefabs currently stop after their particle lifetime. A safety cleanup also
removes any instance after eight seconds.
