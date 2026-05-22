using Verse;

namespace EOTF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerApparel : PawnRenderNodeWorker
    {
        //Gate like VEF does it — base check plus clothes visibility flag
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            if (!parms.flags.FlagSet(PawnRenderFlags.Clothes)) return false;
            return DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }

    //Same as body apparel worker, just needs ApparelHead in the XML
    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerApparel { }
}