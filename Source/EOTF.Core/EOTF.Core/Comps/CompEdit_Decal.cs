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
}