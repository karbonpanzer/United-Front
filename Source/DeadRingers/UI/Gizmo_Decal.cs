using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeadRinger
{
    [StaticConstructorOnStartup]
    public static class DecalBootstrap
    {
        //Harmony bootstrap, mainly here so I can tell if this shit actually loaded
        static DecalBootstrap()
        {
            try
            {
                new Harmony("DeadRinger.Decals").PatchAll();
                Log.Message("[DeadRinger] loaded successfully.");
            }
            catch (Exception e)
            {
                Log.Error("[DeadRinger] Decal System failed to load:\n" + e);
            }
        }
    }

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
                defaultLabel = "DeadRinger_StyleDecalsGizmo".Translate(pawn.LabelCap),
                defaultDesc = "DeadRinger_StyleDecalsDesc".Translate(),
                icon = GizmoIcon,
                action = () => Find.WindowStack.Add(new DialogEditDecals(pawn))
            };
        }
    }
}