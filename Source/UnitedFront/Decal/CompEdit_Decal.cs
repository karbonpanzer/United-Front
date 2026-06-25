using System.Collections.Generic;
using RimWorld;
using UnitedFront.UI;
using UnityEngine;
using Verse;

namespace UnitedFront.Decal
{
    [StaticConstructorOnStartup]
    public sealed class CompEditDecalMarker : ThingComp
    {
        private static readonly Texture2D GizmoIcon;

        static CompEditDecalMarker()
        {
            GizmoIcon = ContentFinder<Texture2D>.Get("UI/CustomizeDecal");
        }

        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "UnitedFrontDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "UnitedFrontDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "UnitedFrontDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "UnitedFrontDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "UnitedFrontDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "UnitedFrontDecalArmorColor", Color.white);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
            TryApplyKindDefaults(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);

            List<Apparel> worn = pawn.apparel.WornApparel;
            foreach (var t in worn)
            {
                if (t == parent) continue;
                if (t.def.HasComp<CompEditDecalMarker>())
                    return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Apparel? apparel = parent as Apparel;
            Pawn? pawn = apparel?.Wearer;
            if (pawn == null) yield break;
            if (pawn.Faction != Faction.OfPlayerSilentFail) yield break;

            List<Apparel> worn = pawn.apparel.WornApparel;
            foreach (var t in worn)
            {
                var other = t.TryGetComp<CompEditDecalMarker>();
                if (other == null) continue;
                if (other != this) yield break;
                break;
            }

            yield return new Command_Action
            {
                defaultLabel = "UnitedFront_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc  = "UnitedFront_StyleDecalsDesc".Translate(),
                icon         = GizmoIcon,
                action       = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };
        }

        private void TryApplyKindDefaults(Pawn pawn)
        {
            if (pawn.kindDef == null) return;

            var ext = pawn.kindDef.GetModExtension<DecalKindExtension>();
            if (ext == null) return;

            bool armorEmpty = ProfileSet.Armor.SymbolPath.NullOrEmpty();
            bool helmetEmpty = ProfileSet.Helmet.SymbolPath.NullOrEmpty();

            if (!ext.armorDecalPath.NullOrEmpty() && (armorEmpty || ext.overrideSaved))
            {
                ProfileSet.Armor.Active = true;
                ProfileSet.Armor.SymbolPath = ext.armorDecalPath;
                ProfileSet.Armor.SymbolColor = ext.armorDecalColor;
            }

            if (!ext.helmetDecalPath.NullOrEmpty() && (helmetEmpty || ext.overrideSaved))
            {
                ProfileSet.Helmet.Active = true;
                ProfileSet.Helmet.SymbolPath = ext.helmetDecalPath;
                ProfileSet.Helmet.SymbolColor = ext.helmetDecalColor;
            }
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}