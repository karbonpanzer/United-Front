using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DeadRinger
{
    public sealed class CompEditDecalMarker : ThingComp
    {
        public DecalProfileSet ProfileSet = DecalProfileSet.Default;

        //Saves decal state per-item so it persists across saves
        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref ProfileSet.Helmet.Active, "DeadRingerDecalHelmetActive");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolPath, "DeadRingerDecalHelmetPath", "");
            Scribe_Values.Look(ref ProfileSet.Helmet.SymbolColor, "DeadRingerDecalHelmetColor", Color.white);

            Scribe_Values.Look(ref ProfileSet.Armor.Active, "DeadRingerDecalArmorActive");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolPath, "DeadRingerDecalArmorPath", "");
            Scribe_Values.Look(ref ProfileSet.Armor.SymbolColor, "DeadRingerDecalArmorColor", Color.white);
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

        //Applies PawnKindDef decal defaults, render node validates textures on main thread
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