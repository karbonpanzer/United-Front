using UnityEngine;
using Verse;

namespace DeadRinger
{
    public sealed class DecalSymbol : Def
    {
        public string Path = "";

        //Slot restrictions, both false means available everywhere
        public bool armorOnly = false;
        public bool helmetOnly = false;
    }

    public enum DecalSlot { Helmet, Armor }

    public struct DecalProfile
    {
        public bool Active;
        public string SymbolPath;
        public Color SymbolColor;

        public DecalProfile(bool active, string path, Color color)
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

        public DecalProfileSet(DecalProfile helmet, DecalProfile armor)
        {
            Helmet = helmet;
            Armor = armor;
        }

        public static DecalProfileSet Default => new DecalProfileSet(DecalProfile.Default, DecalProfile.Default);
    }
}