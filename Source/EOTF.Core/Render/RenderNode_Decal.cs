using UnityEngine;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {
        private readonly DecalSlot _slot;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props);
        }

        //Let GraphicDatabase handle caching and error logging like VEF does
        public override Graphic? GraphicFor(Pawn pawn)
        {
            var eotfProps = Props as PawnRenderNodePropertiesOmni;

            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            Color color = profile.Active ? profile.SymbolColor : (eotfProps?.Color ?? new Color(0.2f, 0.2f, 0.2f));

            if (path.NullOrEmpty()) return null;

            //Body type path concat like VEF
            if (eotfProps?.autoBodyTypePaths == true && pawn.story?.bodyType != null)
                path = path + "_" + pawn.story.bodyType.defName;

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, color);
        }

        //Figures out if this is helmet or armor, defaults to armor if XML doesn't specify
        private static DecalSlot DetermineSlot(PawnRenderNodeProperties props)
        {
            if (props is PawnRenderNodePropertiesOmni eotfProps && eotfProps.ExplicitSlot.HasValue)
                return eotfProps.ExplicitSlot.Value;

            if (props.parentTagDef != null)
            {
                string tagName = props.parentTagDef.defName;
                if (tagName.Contains("Head") || tagName.Contains("Headgear") || tagName.Contains("Helmet"))
                    return DecalSlot.Helmet;
            }

            return DecalSlot.Armor;
        }

        //Falls back to whatever's in the XML texPaths if nobody picked a decal
        private string GetDefaultPath(Pawn pawn)
        {
            if (Props is PawnRenderNodePropertiesOmni eotfProps && eotfProps.texPaths != null && eotfProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return eotfProps.texPaths[seed % eotfProps.texPaths.Count];
            }
            return "";
        }
    }
}