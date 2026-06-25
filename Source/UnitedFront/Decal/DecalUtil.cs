using System.Collections.Generic;
using RimWorld;
using Verse;

namespace UnitedFront.Decal
{
    public static class DecalUtil
    {
        private static List<DecalSymbol>? _cachedArmorSymbols;
        private static List<DecalSymbol>? _cachedHelmetSymbols;

        public static DecalProfileSet ReadProfileSetFrom(Pawn pawn)
        {
            var comp = GetMarker(pawn);
            return comp?.ProfileSet ?? DecalProfileSet.Default;
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

        private static CompEditDecalMarker? GetMarker(Pawn? pawn)
        {
            if (pawn?.apparel == null) return null;

            List<Apparel> worn = pawn.apparel.WornApparel;
            foreach (var t in worn)
            {
                var comp = t.TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }

        private static List<DecalSymbol> AllSymbols() => DefDatabase<DecalSymbol>.AllDefsListForReading;

        public static List<DecalSymbol> SymbolsForSlot(DecalSlot slot)
        {
            if (slot == DecalSlot.Armor)
            {
                if (_cachedArmorSymbols == null)
                {
                    var all = AllSymbols();
                    _cachedArmorSymbols = new List<DecalSymbol>(all.Count);
                    foreach (var t in all)
                    {
                        if (!t.helmetOnly)
                            _cachedArmorSymbols.Add(t);
                    }
                }
                return _cachedArmorSymbols;
            }
            else
            {
                if (_cachedHelmetSymbols == null)
                {
                    var all = AllSymbols();
                    _cachedHelmetSymbols = new List<DecalSymbol>(all.Count);
                    foreach (var t in all)
                    {
                        if (!t.armorOnly)
                            _cachedHelmetSymbols.Add(t);
                    }
                }
                return _cachedHelmetSymbols;
            }
        }

        public static bool PawnHasAnyDecalApparel(Pawn pawn) => GetMarker(pawn) != null;
    }
}