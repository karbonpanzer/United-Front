using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {

        private readonly DecalSlot _slot;

        private Graphic? _cachedGraphic;
        private string?  _cachedPath;
        private Color    _cachedColor;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props as PawnRenderNodePropertiesOmni);
        }

        //Graphic lookup with caching so we're not hammering GraphicDatabase every goddamn frame
        public override Graphic? GraphicFor(Pawn pawn)
        {
            var eotfProps = Props as PawnRenderNodePropertiesOmni;
            if (eotfProps == null) return null;

            DecalProfile profile   = DecalUtil.ReadProfileFrom(pawn, _slot);
            string       path      = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn, eotfProps);
            Color        finalColor = profile.Active ? profile.SymbolColor : eotfProps.Color;

            if (path.NullOrEmpty()) return null;

            if (_cachedPath == path && _cachedColor == finalColor)
                return _cachedGraphic;

            _cachedPath    = path;
            _cachedColor   = finalColor;
            _cachedGraphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, finalColor);

            return _cachedGraphic;
        }

        //Figures out if this is helmet or armor, defaults to armor if XML doesn't specify
        private static DecalSlot DetermineSlot(PawnRenderNodePropertiesOmni? eotfProps)
        {
            if (eotfProps?.ExplicitSlot.HasValue == true)
                return eotfProps.ExplicitSlot.Value;

            if (eotfProps?.parentTagDef != null)
            {
                string tagName = eotfProps.parentTagDef.defName;
                if (tagName.Contains("Head") || tagName.Contains("Headgear") || tagName.Contains("Helmet"))
                    return DecalSlot.Helmet;
            }

            return DecalSlot.Armor;
        }

        //Default texture path when nobody's picked a decal, falls back to whatever's in the XML texPaths
        private string GetDefaultPath(Pawn pawn, PawnRenderNodePropertiesOmni eotfProps)
        {
            if (eotfProps.texPaths != null && eotfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return eotfProps.texPaths[seed % eotfProps.texPaths.Count];
            }
            return "";
        }
    }
}