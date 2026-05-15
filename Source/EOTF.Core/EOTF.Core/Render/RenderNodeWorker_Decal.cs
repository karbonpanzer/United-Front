using Verse;

namespace EOTF.Core.DecalSystem
{
    public class PawnRenderNodeWorkerApparel : PawnRenderNodeWorker
    {
        //Gate so decals don't fucking render on pawns without the right gear
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            return base.CanDrawNow(node, parms) && DecalUtil.PawnHasAnyDecalApparel(parms.pawn);
        }
    }

    // Same shit as body apparel worker, just needs ApparelHead in the XML
    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerApparel { }
}