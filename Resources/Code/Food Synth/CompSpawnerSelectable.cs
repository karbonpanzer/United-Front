using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FoodSynthesizer
{
    public class CompSpawnerSelectable : ThingComp
    {
        private int ticksUntilSpawn;
        private int selectedIndex;

        public CompProperties_SpawnerSelectable Props
            => (CompProperties_SpawnerSelectable)props;

        private ThingDefCountClass CurrentOption
            => Props.spawnOptions[selectedIndex];

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                ResetTimer();
            }
        }

        private void ResetTimer()
        {
            ticksUntilSpawn = Props.spawnIntervalRange.RandomInRange;
        }

        public override void CompTick()
        {
            if (Props.requiresPower)
            {
                CompPowerTrader power = parent.TryGetComp<CompPowerTrader>();
                if (power != null && !power.PowerOn)
                {
                    return;
                }
            }

            ticksUntilSpawn--;
            if (ticksUntilSpawn <= 0)
            {
                TryDoSpawn();
                ResetTimer();
            }
        }

        private bool TryDoSpawn()
        {
            ThingDefCountClass option = CurrentOption;

            if (Props.spawnMaxAdjacent >= 0)
            {
                int adjacent = 0;
                foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(parent))
                {
                    if (cell.InBounds(parent.Map))
                    {
                        List<Thing> thingsAt = cell.GetThingList(parent.Map);
                        for (int i = 0; i < thingsAt.Count; i++)
                        {
                            if (thingsAt[i].def == option.thingDef)
                            {
                                adjacent += thingsAt[i].stackCount;
                            }
                        }
                    }
                }
                if (adjacent >= Props.spawnMaxAdjacent)
                {
                    return false;
                }
            }

            Thing thing = ThingMaker.MakeThing(option.thingDef);
            thing.stackCount = option.count;

            if (Props.inheritFaction && parent.Faction != null)
            {
                thing.SetFaction(parent.Faction);
            }

            if (Props.spawnForbidden)
            {
                thing.SetForbidden(true, false);
            }

            if (!GenPlace.TryPlaceThing(thing, parent.Position, parent.Map,
                ThingPlaceMode.Near))
            {
                thing.Destroy();
                return false;
            }

            if (Props.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
            {
                Messages.Message(
                    "MessageCompSpawnerSpawnedItem".Translate(
                        option.thingDef.LabelCap),
                    thing,
                    MessageTypeDefOf.PositiveEvent);
            }

            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            ThingDefCountClass option = CurrentOption;

            yield return new Command_Action
            {
                defaultLabel = "FoodSynth_GizmoLabel".Translate(
                    option.thingDef.LabelCap),
                defaultDesc = "FoodSynth_GizmoDesc".Translate(
                    option.thingDef.LabelCap,
                    option.count),
                icon = GetOptionIcon(option),
                action = delegate
                {
                    List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                    for (int i = 0; i < Props.spawnOptions.Count; i++)
                    {
                        int localIndex = i;
                        ThingDefCountClass opt = Props.spawnOptions[i];
                        string label;
                        if (i == selectedIndex)
                        {
                            label = "FoodSynth_CurrentMarker".Translate(
                                opt.thingDef.LabelCap,
                                opt.count);
                        }
                        else
                        {
                            label = "FoodSynth_MenuOption".Translate(
                                opt.thingDef.LabelCap,
                                opt.count);
                        }
                        menuOptions.Add(new FloatMenuOption(label, delegate
                        {
                            selectedIndex = localIndex;
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(menuOptions));
                }
            };
        }

        private static Texture2D GetOptionIcon(ThingDefCountClass option)
        {
            if (option.thingDef.uiIcon != null)
            {
                return option.thingDef.uiIcon;
            }
            return BaseContent.BadTex;
        }

        public override string CompInspectStringExtra()
        {
            ThingDefCountClass option = CurrentOption;
            string text = "FoodSynth_Producing".Translate(
                option.thingDef.LabelCap,
                option.count);

            if (Props.writeTimeLeftToSpawn)
            {
                text += "\n" + "NextSpawnedItemIn".Translate(
                    GenDate.ToStringTicksToPeriod(ticksUntilSpawn));
            }

            return text;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            string prefix = Props.saveKeysPrefix ?? "selectable";
            Scribe_Values.Look(ref ticksUntilSpawn,
                prefix + "_ticksUntilSpawn", 0);
            Scribe_Values.Look(ref selectedIndex,
                prefix + "_selectedIndex", 0);
        }
    }
}