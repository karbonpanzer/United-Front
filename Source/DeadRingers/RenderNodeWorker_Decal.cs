using Verse;

namespace DeadRinger
{
    public class PawnRenderNodeWorkerApparel : PawnRenderNodeWorker
    {
        //Base check plus clothes visibility flag, no extra apparel scan
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            return parms.flags.FlagSet(PawnRenderFlags.Clothes);
        }
    }

    //Same as body apparel worker, just needs ApparelHead in the XML
    public class PawnRenderNodeWorkerHeadware : PawnRenderNodeWorkerApparel { }
}