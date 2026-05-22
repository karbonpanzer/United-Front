using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace EOTF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp 
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        // XML save/load for decal state, this is the fallback structure if the symboldefs shit the bed
        public override void PostExposeData()
        {
            base.PostExposeData();
            
            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "eotfDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "eotfDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "eotfDecalHelmetColor", Color.white);
            
            Scribe_Values.Look(ref ProfileSet.Armor.Active, "eotfDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "eotfDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "eotfDecalArmorColor", Color.white);
        }

        //Hook into the WorldComponent so pawn tracking doesn't break when gear changes
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                if (apparel.TryGetComp<CompEditDecalMarker>() != null) return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
    
    //Gizmo that opens the decal editing UI, still needs work
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class PatchPawnGetGizmosDecals
    {
        private static readonly Texture2D GizmoIcon =
            ContentFinder<Texture2D>.Get("UI/CustomizeDecal");

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Faction != Faction.OfPlayerSilentFail || 
                !DecalUtil.PawnHasAnyDecalApparel(__instance)) return;
        
            __result = AppendGizmo(__result, CreateDecalGizmo(__instance));
        }

        private static IEnumerable<Gizmo> AppendGizmo(IEnumerable<Gizmo> source, Gizmo gizmo)
        {
            foreach (var g in source) yield return g;
            yield return gizmo;
        }

        private static Gizmo CreateDecalGizmo(Pawn pawn)
        {
            return new Command_Action
            {
                defaultLabel = "EOTF_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc = "EOTF_StyleDecalsDesc".Translate(),
                icon = GizmoIcon,
                action = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };
        }
    }
    
}
