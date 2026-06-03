using RimWorld;
using Verse;

namespace ReplicantXenotype
{
    [DefOf]
    public static class ReplicantGenesDefOf
    {
        public static HediffDef? DRG_Replicant_FinalClarity;
        public static ThoughtDef? DRG_Replicant_FinalClarityThought;

        static ReplicantGenesDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ReplicantGenesDefOf));
        }
    }
}