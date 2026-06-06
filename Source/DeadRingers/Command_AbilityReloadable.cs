using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeadRinger
{
    // Ability gizmo that paints the linked apparel's charge count in the top-right corner of the button, the way the old verb gizmo did.
    public sealed class CommandAbilityReloadable(Ability ability, Pawn pawn)
        : Command_Ability(ability, pawn)
    {
        public override string TopRightLabel
        {
            get
            {
                CompApparelReloadable? reloadable = FindReloadable();
                if (reloadable != null)
                {
                    return $"{reloadable.RemainingCharges} / {reloadable.MaxCharges}";
                }

                return base.TopRightLabel;
            }
        }

        // Finds the worn apparel that grants this ability and returns its reloadable comp, mirroring how the ability effect comp links them.
        private CompApparelReloadable? FindReloadable()
        {
            if (Pawn?.apparel == null)
            {
                return null;
            }

            foreach (Apparel apparel in Pawn.apparel.WornApparel)
            {
                List<AbilityDef>? granted = apparel.def.apparel?.abilities;
                if (granted == null || !granted.Contains(Ability.def))
                {
                    continue;
                }

                CompApparelReloadable comp = apparel.TryGetComp<CompApparelReloadable>();
                if (comp != null)
                {
                    return comp;
                }
            }

            return null;
        }
    }
}