using UnityEngine;
using Verse;

namespace DeadRinger
{
    public class PawnRenderNodePropertiesOmni : PawnRenderNodeProperties
    {
        //Default decal color if nothing is picked
        public Color Color = new Color(0.2f, 0.2f, 0.2f);

        //Explicit slot override, otherwise determined by parentTagDef
        public DecalSlot? ExplicitSlot = null;

        //Auto-concat body type to texture path like VEF's autoBodyTypePaths
        public bool autoBodyTypePaths = false;

        public PawnRenderNodePropertiesOmni()
        {
            nodeClass = typeof(PawnRenderNodeDecal);
            workerClass = typeof(PawnRenderNodeWorkerApparel);
        }
    }
}