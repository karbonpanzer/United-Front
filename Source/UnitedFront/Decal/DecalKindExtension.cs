using UnityEngine;
using Verse;

namespace UnitedFront.Decal
{
    public class DecalKindExtension : DefModExtension
    {
        public readonly string armorDecalPath = "";
        public Color armorDecalColor = new Color(0.2f, 0.2f, 0.2f);

        public readonly string helmetDecalPath = "";
        public Color helmetDecalColor = new Color(0.2f, 0.2f, 0.2f);

        public readonly bool overrideSaved = false;
    }
}