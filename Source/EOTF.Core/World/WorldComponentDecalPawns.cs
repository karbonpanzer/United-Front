using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public class WorldComponentDecalPawns : WorldComponent
    {
        public static WorldComponentDecalPawns? Instance { get; private set; }

        private HashSet<Pawn> _pawns = new HashSet<Pawn>();

        private static readonly List<Pawn> TMPToRemove = new List<Pawn>();

        //Tracks which pawns have decal gear, persists across saves
        public WorldComponentDecalPawns(World world) : base(world) => Instance = this;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _pawns, "eotfDecalPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (_pawns == null)
                {
                    _pawns = new HashSet<Pawn>();
                }
                if (_pawns.RemoveWhere(x => x == null) != 0)
                {
                    Log.Error("[EOTF] Some decal pawns were null after loading.");
                }
                if (_pawns.RemoveWhere(x => x.def == null || x.kindDef == null) != 0)
                {
                    Log.Error("[EOTF] Some decal pawns had null def after loading.");
                }
            }
        }

        //Purge dead or destroyed pawns so the set doesn't grow forever
        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 10000 != 0) return;

            TMPToRemove.Clear();
            foreach (var pawn in _pawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.apparel == null)
                {
                    if (pawn != null) TMPToRemove.Add(pawn);
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
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.TryGetComp<CompEditDecalMarker>();
                if (comp != null) return comp;
            }
            return null;
        }
    }
}