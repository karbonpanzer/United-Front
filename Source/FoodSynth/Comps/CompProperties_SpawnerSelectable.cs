using System.Collections.Generic;
using Verse;

namespace FoodSynthesizer
{
    public class CompProperties_SpawnerSelectable : CompProperties
    {
        //My addition which replaces the single thingToSpawn with a selectable list
        public List<ThingDefCountClass> spawnOptions = null!;

        //Carryover from CompProperties_Spawner
        public IntRange spawnIntervalRange = new IntRange(600000, 600000);
        public int spawnMaxAdjacent = -1;
        public bool spawnForbidden;
        public bool requiresPower = true;
        public bool writeTimeLeftToSpawn = true;
        public bool showMessageIfOwned = true;
        public bool inheritFaction;
        [NoTranslate]
        public string saveKeysPrefix = null!;

        public CompProperties_SpawnerSelectable()
        {
            compClass = typeof(CompSpawnerSelectable);
        }

        //Validate that spawnOptions isn't empty so I don't get null refs at runtime
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (spawnOptions.NullOrEmpty())
            {
                yield return "CompProperties_SpawnerSelectable has no spawnOptions defined on "
                             + parentDef.defName;
            }
        }
    }
}