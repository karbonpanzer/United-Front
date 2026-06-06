using UnityEngine;
using Verse;

namespace DeadRinger
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
            var DeadRingerProps = Props as PawnRenderNodePropertiesOmni;

            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            string path = profile.Active ? profile.SymbolPath : GetDefaultPath(pawn);
            Color color = profile.Active ? profile.SymbolColor : (DeadRingerProps?.Color ?? new Color(0.2f, 0.2f, 0.2f));

            if (path.NullOrEmpty()) return null;

            //Body type path concat like VEF
            if (DeadRingerProps?.autoBodyTypePaths == true && pawn.story?.bodyType != null)
                path = path + "_" + pawn.story.bodyType.defName;

            return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, Vector2.one, color);
        }

        //Figures out if this is helmet or armor, defaults to armor if XML doesn't specify
        private static DecalSlot DetermineSlot(PawnRenderNodeProperties props)
        {
            if (props is PawnRenderNodePropertiesOmni DeadRingerProps && DeadRingerProps.ExplicitSlot.HasValue)
                return DeadRingerProps.ExplicitSlot.Value;

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
            if (Props is PawnRenderNodePropertiesOmni DeadRingerProps && DeadRingerProps.texPaths != null && DeadRingerProps.texPaths.Count > 0)
            {
                int seed = pawn.Faction?.loadID ?? pawn.thingIDNumber;
                return DeadRingerProps.texPaths[seed % DeadRingerProps.texPaths.Count];
            }
            return "";
        }
    }
}