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

The trigger mapping and object pool live in
`Assets/Script/UI/Manager/GameFeedback.SkillEffectTriggers.cs`, separate from
the main HUD/result implementation. Every dynamically created ParticleSystem is
stopped with `StopEmittingAndClear` before its duration or playback settings are
changed, which avoids Unity's "Setting the duration while system is still
playing" warning.

Additional gameplay feedback:

- peg-hit effects `Effect_1`..`Effect_5` use reduced overall scale, particle
  scale and opacity while retaining their prefab gravity settings;
- score-slot effects `Effect_6`..`Effect_8` use a stronger scale;
- `Effect_8` is converted at runtime into equal-sized streamline particles that
  rotate outward from the slot center;
- split-ball feedback adds a larger low-saturation red rotating flower bloom
  centered on the peg, with six round petals and no outer orbit ring;
- reaching 100, 200, 500, 1000, 1500 and then every additional 500 points shows
  a centered, slightly raised muted dark-gold milestone number in three stages:
  appear, hold, then enlarge and fade. The number uses a hard-edged dynamic
  system font and a single state-driven display, so a newer milestone replaces
  the value immediately and resets the hold phase instead of waiting for the
  old number;
- a slot that accumulates at least 150 points within 1.2 seconds flashes one of
  the `icon_0`..`icon_20` tarot sprites, with a short cooldown. The icon uses a
  high-priority nested canvas, a warm gold tint and a larger fade enlargement.

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

The particle prefab preview uses the prefab's original round particle texture.
During play, `NormalizeParticlePalette()` replaces that texture with procedural
star/diamond textures, switches renderers to 2D billboards, and applies the
white-gold palette. That is why the same prefab can look like a soft 3D round
particle in the Inspector but appear as a hard 2D geometric shape in game.

Once the target score is reached, the screen-edge flow starts a long-lived
stream of small white-gold glow particles travelling toward the centre. Both
the edge flow and the inward particle stream stop when `RoundEnded` fires.
The inward stream is capped at 96 pooled UI images so the celebration remains
visible without updating hundreds of transforms every frame.

The prefabs currently stop after their particle lifetime. A safety cleanup also
removes any instance after eight seconds.
