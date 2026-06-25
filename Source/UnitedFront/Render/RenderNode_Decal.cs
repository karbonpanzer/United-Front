using RimWorld;
using UnitedFront.Decal;
using UnityEngine;
using Verse;

namespace UnitedFront.Render
{
    public class PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : PawnRenderNode(pawn, props, tree)
    {
        private readonly DecalSlot _slot = DetermineSlot(props);

        protected override string TexPathFor(Pawn pawn)
        {
            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            if (profile.Active && !profile.SymbolPath.NullOrEmpty())
                return profile.SymbolPath;
            return base.TexPathFor(pawn);
        }

        public override Color ColorFor(Pawn pawn)
        {
            DecalProfile profile = DecalUtil.ReadProfileFrom(pawn, _slot);
            if (profile.Active)
                return profile.SymbolColor * Props.colorRGBPostFactor;
            return base.ColorFor(pawn);
        }

        private static DecalSlot DetermineSlot(PawnRenderNodeProperties props)
        {
            if (props is PawnRenderNodePropertiesDecal { ExplicitSlot: not null } decalProps)
                return decalProps.ExplicitSlot.Value;

            if (props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead)
                return DecalSlot.Helmet;

            return DecalSlot.Armor;
        }
    }
}