using UnityEngine;
using Verse;

namespace UnitedFront.Decal
{
    public sealed class DecalSymbol : Def
    {
        public readonly string Path = "";
        public readonly bool armorOnly = false;
        public readonly bool helmetOnly = false;
    }

    public enum DecalSlot { Helmet, Armor }

    public struct DecalProfile
    {
        public bool Active;
        public string SymbolPath;
        public Color SymbolColor;

        private DecalProfile(bool active, string path, Color color)
        {
            Active = active;
            SymbolPath = path;
            SymbolColor = color;
        }

        public static DecalProfile Default => new DecalProfile(false, "", Color.white);
    }

    public struct DecalProfileSet
    {
        public DecalProfile Helmet;
        public DecalProfile Armor;

        private DecalProfileSet(DecalProfile helmet, DecalProfile armor)
        {
            Helmet = helmet;
            Armor = armor;
        }

        public static DecalProfileSet Default => new DecalProfileSet(DecalProfile.Default, DecalProfile.Default);
    }
}