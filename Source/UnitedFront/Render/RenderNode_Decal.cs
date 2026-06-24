using RimWorld;
using UnitedFront.Decal;
using UnityEngine;
using Verse;

namespace UnitedFront.Render
{
    public class PawnRenderNodeDecal : PawnRenderNode
    {
        private readonly DecalSlot _slot;

        public PawnRenderNodeDecal(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree)
        {
            _slot = DetermineSlot(props);
        }

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
            if (props is PawnRenderNodePropertiesDecal decalProps && decalProps.ExplicitSlot.HasValue)
                return decalProps.ExplicitSlot.Value;

            if (props.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead)
                return DecalSlot.Helmet;

            return DecalSlot.Armor;
        }
    }
}