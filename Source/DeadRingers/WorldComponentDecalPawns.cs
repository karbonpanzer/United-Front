using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace DeadRinger
{
    public class WorldComponentDecalPawns : WorldComponent
    {
        public static WorldComponentDecalPawns? Instance { get; private set; }

        private HashSet<Pawn> _pawns = new HashSet<Pawn>();

        private static readonly List<Pawn> TMPToRemove = new List<Pawn>();

        //Tracks which pawns have decal gear, persists across saves
        public WorldComponentDecalPawns(World world) : base(world) => Instance = this;

        //Vanilla WorldPawns pattern — strip nulls and corrupted defs on load
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _pawns, "DeadRingerDecalPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_pawns == null)
                {
                    _pawns = new HashSet<Pawn>();
                }
                if (_pawns.RemoveWhere(x => x == null) != 0)
                {
                    Log.Error("[DeadRinger] Some decal pawns were null after loading.");
                }
                if (_pawns.RemoveWhere(x => x.def == null || x.kindDef == null) != 0)
                {
                    Log.Error("[DeadRinger] Some decal pawns had null def after loading.");
                }
            }
        }

        //Purge dead or destroyed pawns so the set doesn't grow forever
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (_pawns.Count == 0 || Find.TickManager.TicksGame % 10000 != 0) return;

            TMPToRemove.Clear();
            foreach (var pawn in _pawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.apparel == null)
                {
                    if (pawn != null)
                        TMPToRemove.Add(pawn);
                }
            }
            for (int i = 0; i < TMPToRemove.Count; i++)
            {
                _pawns.Remove(TMPToRemove[i]);
            }
            TMPToRemove.Clear();
        }

        public void Register(Pawn pawn) => _pawns.Add(pawn);

        public void Unregister(Pawn pawn) => _pawns.Remove(pawn);

        public bool HasDecalApparel(Pawn pawn) => _pawns.Contains(pawn);

        public CompEditDecalMarker? GetComp(Pawn pawn)
        {
            if (!_pawns.Contains(pawn) || pawn.apparel == null) return null;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                var comp = wornApparel[i].TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}