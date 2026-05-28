using System.Collections.Generic;
using RimWorld;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public static class DecalUtil
    {
        public static DecalProfileSet ReadProfileSetFrom(Pawn pawn)
        {
            var comp = GetMarker(pawn);
            return comp != null ? comp.ProfileSet : DecalProfileSet.Default;
        }

        public static DecalProfile ReadProfileFrom(Pawn pawn, DecalSlot slot)
        {
            var comp = GetMarker(pawn);
            if (comp == null) return DecalProfile.Default;
            return slot == DecalSlot.Helmet ? comp.ProfileSet.Helmet : comp.ProfileSet.Armor;
        }

        private static void WriteProfileSetTo(Pawn pawn, DecalProfileSet profileSet)
        {
            var comp = GetMarker(pawn);
            if (comp != null) comp.ProfileSet = profileSet;
        }

        //Live preview so you can see changes without closing the damn dialog
        public static void SetLiveEditFull(Pawn pawn, DecalProfileSet profileSet)
        {
            WriteProfileSetTo(pawn, profileSet);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        public static void EndLiveEdit(Pawn pawn, bool commit, DecalProfileSet original)
        {
            if (!commit)
                WriteProfileSetTo(pawn, original);
            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }

        //Tries WorldComponent cache first, falls back to brute force apparel scan if that's fucked
        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;

            var registry = WorldComponentDecalPawns.Instance;
            if (registry != null)
            {
                var cached = registry.GetComp(pawn);
                if (cached != null) return cached;
                if (registry.HasDecalApparel(pawn)) registry.Unregister(pawn);
            }

            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                var comp = wornApparel[i].TryGetComp<CompEditDecalMarker>();
                if (comp != null)
                {
                    registry?.Register(pawn);
                    return comp;
                }
            }
            return null;
        }

        public static List<DecalSymbolDef> AllSymbols() => DefDatabase<DecalSymbolDef>.AllDefsListForReading;

        //Filters symbols by slot — armorOnly decals hidden from helmet tab, helmetOnly hidden from armor tab
        public static List<DecalSymbolDef> SymbolsForSlot(DecalSlot slot)
        {
            var all = AllSymbols();
            var filtered = new List<DecalSymbolDef>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                if (slot == DecalSlot.Armor && all[i].helmetOnly) continue;
                if (slot == DecalSlot.Helmet && all[i].armorOnly) continue;
                filtered.Add(all[i]);
            }
            return filtered;
        }

        public static bool PawnHasAnyDecalApparel(Pawn pawn) => GetMarker(pawn) != null;
    }
}