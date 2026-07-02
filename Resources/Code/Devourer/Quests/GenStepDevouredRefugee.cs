using RimWorld;
using Verse;

namespace UnitedFront.Quests
{
    public class GenStepDevouredRefugee : GenStep_Scatterer
    {
        public override int SeedPart => 743920185;

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            if (base.CanScatterAt(c, map) && c.Standable(map))
            {
                return !c.Fogged(map);
            }
            return false;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int stackCount = 1)
        {
            Pawn refugee;
            if (parms.sitePart is { things.Any: true })
            {
                refugee = (Pawn)parms.sitePart.things.Take(parms.sitePart.things[0]);
            }
            else
            {
                refugee = DownedRefugeeQuestUtility.GenerateRefugee(map.Tile);
            }
            HealthUtility.DamageUntilDowned(refugee, allowBleedingWounds: false);
            HealthUtility.DamageLegsUntilIncapableOfMoving(refugee, allowBleedingWounds: false);
            GenSpawn.Spawn(refugee, loc, map);
            refugee.mindState.WillJoinColonyIfRescued = true;
            if (!CellFinder.TryFindRandomCellNear(loc, map, 3, c => c.Standable(map) && !c.Fogged(map), out IntVec3 devourerLoc))
            {
                devourerLoc = loc;
            }
            Pawn devourer = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDef.Named("UFR_GorgedDevourer"),
                Faction.OfEntities,
                PawnGenerationContext.NonPlayer,
                map.Tile,
                forceGenerateNewPawn: true));
            GenSpawn.Spawn(devourer, devourerLoc, map);
            devourer.GetComp<CompDevourer>().StartDigesting(loc, refugee);
            MapGenerator.SetVar("RectOfInterest", CellRect.CenteredOn(devourerLoc, 1, 1));
        }
    }
}