using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace DeadRinger
{
    // Ability effect that draws charges from the CompApparelReloadable on the apparel granting this ability, then launches the grenade projectile.
    public sealed class CompAbilityEffectApparelReloadable : CompAbilityEffect
    {
        private new CompPropertiesAbilityEffectApparelReloadable Props =>
            (CompPropertiesAbilityEffectApparelReloadable)props;

        private CompApparelReloadable? _cachedReloadable;
        private Apparel? _cachedApparel;

        // Resolves (and caches) the worn apparel that grants this ability and its reloadable comp.
        private CompApparelReloadable? Reloadable
        {
            get
            {
                if (_cachedReloadable != null && _cachedApparel != null &&
                    parent.pawn.apparel.WornApparel.Contains(_cachedApparel))
                {
                    return _cachedReloadable;
                }

                ResolveLinkedApparel();
                return _cachedReloadable;
            }
        }

        private void ResolveLinkedApparel()
        {
            _cachedReloadable = null;
            _cachedApparel = null;

            Pawn_ApparelTracker apparelTracker = parent.pawn.apparel;
            if (apparelTracker == null) return;

            foreach (Apparel apparel in apparelTracker.WornApparel)
            {
                List<AbilityDef>? granted = apparel.def.apparel?.abilities;
                if (granted == null || !granted.Contains(parent.def)) continue;

                CompApparelReloadable reloadable = apparel.TryGetComp<CompApparelReloadable>();
                if (reloadable != null)
                {
                    _cachedReloadable = reloadable;
                    _cachedApparel = apparel;
                    return;
                }
            }
        }

        public override void Initialize(AbilityCompProperties properties)
        {
            base.Initialize(properties);
            ResolveLinkedApparel();
        }

        // Consumes one charge, then launches the projectile from the caster.
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            CompApparelReloadable? reloadable = Reloadable;
            if (reloadable == null)
            {
                Log.Error($"[DeadRinger] {parent.def.defName}: no reloadable apparel grants this ability.");
                return;
            }

            if (reloadable.RemainingCharges <= 0)
            {
                Log.Warning($"[DeadRinger] {parent.def.defName}: no charges left in {_cachedApparel?.LabelCap}.");
                return;
            }

            reloadable.UsedOnce();
            LaunchProjectile(target);
            base.Apply(target, dest);
        }

        // Spawns and launches the grenade projectile, replicating the landing scatter the old grenade verb produced via forcedMissRadius.
        private void LaunchProjectile(LocalTargetInfo target)
        {
            Pawn caster = parent.pawn;
            if (caster.Map == null || Props.ProjectileDef == null) return;

            LocalTargetInfo landing = target;
            if (Props.ForcedMissRadius > 0.5f && target.Cell.IsValid)
            {
                int cellCount = GenRadial.NumCellsInRadius(Props.ForcedMissRadius);
                IntVec3 candidate = target.Cell + GenRadial.RadialPattern[Rand.Range(0, cellCount)];
                if (candidate.InBounds(caster.Map))
                {
                    landing = candidate;
                }
            }

            Projectile projectile = (Projectile)GenSpawn.Spawn(Props.ProjectileDef, caster.Position, caster.Map);
            projectile.Launch(caster, caster.DrawPos, landing, target, ProjectileHitFlags.All);

            if (Props.SoundOnCast != null)
            {
                Props.SoundOnCast.PlayOneShot(SoundInfo.InMap(new TargetInfo(caster.Position, caster.Map)));
            }
        }

        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
        {
            CompApparelReloadable? reloadable = Reloadable;
            if (reloadable == null || reloadable.RemainingCharges <= 0) return false;
            if (!reloadable.CanBeUsed(out _)) return false;
            return base.CanApplyOn(target, dest);
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            CompApparelReloadable? reloadable = Reloadable;
            if (reloadable == null)
            {
                if (throwMessages && Props.ShowWarningIfMissing)
                {
                    Messages.Message("This ability requires reloadable apparel that provides it.",
                        MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            if (reloadable.RemainingCharges <= 0)
            {
                if (throwMessages)
                {
                    Messages.Message(reloadable.DisabledReason(1, 1),
                        MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            return base.Valid(target, throwMessages);
        }

        public override bool GizmoDisabled(out string reason)
        {
            CompApparelReloadable? reloadable = Reloadable;
            if (reloadable == null)
            {
                reason = "No reloadable apparel with this ability equipped.";
                return true;
            }

            if (reloadable.RemainingCharges <= 0)
            {
                reason = reloadable.DisabledReason(1, 1);
                return true;
            }

            return base.GizmoDisabled(out reason);
        }

        public override string ExtraTooltipPart()
        {
            CompApparelReloadable? reloadable = Reloadable;
            if (reloadable != null && _cachedApparel != null)
            {
                return $"Requires: {_cachedApparel.LabelCap}\n" +
                       $"Charges: {reloadable.RemainingCharges} / {reloadable.MaxCharges}";
            }
            return "Requires reloadable apparel that provides this ability.";
        }
    }

    public sealed class CompPropertiesAbilityEffectApparelReloadable : CompProperties_AbilityEffect
    {
        // The grenade projectile this ability throws.
        public ThingDef? ProjectileDef;

        // Landing scatter, equivalent to the old verb's forcedMissRadius (grenades used 1.9).
        public float ForcedMissRadius;

        // Throw sound, equivalent to the old verb's soundCast (e.g. ThrowGrenade).
        public SoundDef? SoundOnCast;

        public bool ShowWarningIfMissing = true;

        public CompPropertiesAbilityEffectApparelReloadable()
        {
            compClass = typeof(CompAbilityEffectApparelReloadable);
        }
    }
}