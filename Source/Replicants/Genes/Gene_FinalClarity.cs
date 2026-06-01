using RimWorld;
using Verse;

namespace ReplicantXenotype
{
    public class Gene_FinalClarity : Gene
    {
        private bool _clarityTriggered = false;

        private const float TriggerBiologicalAge = 40f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _clarityTriggered, "clarityTriggered", false);
        }

        public override void Tick()
        {
            base.Tick();

            if (_clarityTriggered) { return; }
            if (!pawn.Spawned) { return; }
            if (Find.TickManager.TicksAbs % 600 != 0) { return; }

            if (pawn.ageTracker.AgeBiologicalYearsFloat >= TriggerBiologicalAge)
            {
                TriggerClarity();
            }
        }

        private void TriggerClarity()
        {
            _clarityTriggered = true;

            Hediff clarity = HediffMaker.MakeHediff(
                ReplicantGenesDefOf.DRG_Replicant_FinalClarity, pawn);
            pawn.health.AddHediff(clarity);

            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                ReplicantGenesDefOf.DRG_Replicant_FinalClarityThought);

            if (pawn.Spawned)
            {
                Messages.Message(
                    "Replicant_FinalClarity_Message".Translate(pawn.LabelShortCap),
                    pawn,
                    MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}