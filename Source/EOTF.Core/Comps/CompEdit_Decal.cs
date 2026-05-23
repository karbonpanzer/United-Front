using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        //Saves decal state per-item so it persists across saves
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

        //Register with WorldComponent and apply PawnKindDef defaults if they exist
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            WorldComponentDecalPawns.Instance?.Register(pawn);
            TryApplyKindDefaults(pawn);
        }

        //Only unregister if no other decal gear is still on the pawn
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (wornApparel[i].def.HasComp<CompEditDecalMarker>())
                    return;
            }
            WorldComponentDecalPawns.Instance?.Unregister(pawn);
        }

        //Checks PawnKindDef for DecalKindExtension, validates textures exist, applies defaults
        private void TryApplyKindDefaults(Pawn pawn)
        {
            if (pawn.kindDef == null) return;

            var ext = pawn.kindDef.GetModExtension<DecalKindExtension>();
            if (ext == null) return;

            bool armorEmpty = ProfileSet.Armor.SymbolPath.NullOrEmpty();
            bool helmetEmpty = ProfileSet.Helmet.SymbolPath.NullOrEmpty();

            if (!ext.armorDecalPath.NullOrEmpty() && (armorEmpty || ext.overrideSaved))
            {
                if (!ValidateTexturePath(ext.armorDecalPath, pawn.kindDef.defName, "armor"))
                    return;
                ProfileSet.Armor.Active = true;
                ProfileSet.Armor.SymbolPath = ext.armorDecalPath;
                ProfileSet.Armor.SymbolColor = ext.armorDecalColor;
            }

            if (!ext.helmetDecalPath.NullOrEmpty() && (helmetEmpty || ext.overrideSaved))
            {
                if (!ValidateTexturePath(ext.helmetDecalPath, pawn.kindDef.defName, "helmet"))
                    return;
                ProfileSet.Helmet.Active = true;
                ProfileSet.Helmet.SymbolPath = ext.helmetDecalPath;
                ProfileSet.Helmet.SymbolColor = ext.helmetDecalColor;
            }
        }

        //Logs an error if the texture path from a PawnKindDef extension doesn't resolve
        private static bool ValidateTexturePath(string path, string kindDefName, string slot)
        {
            if (ContentFinder<Texture2D>.Get(path + "_south", false) != null) return true;
            if (ContentFinder<Texture2D>.Get(path, false) != null) return true;

            Log.ErrorOnce("[EOTF] DecalKindExtension on " + kindDefName + " has missing " + slot
                + " texture: " + path, path.GetHashCode() ^ kindDefName.GetHashCode());
            return false;
        }
    }

    public sealed class CompPropertiesEditDecalMarker : CompProperties
    {
        public CompPropertiesEditDecalMarker() => compClass = typeof(CompEditDecalMarker);
    }
}
