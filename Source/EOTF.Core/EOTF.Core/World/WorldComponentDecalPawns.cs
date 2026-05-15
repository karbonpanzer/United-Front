using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace EOTF.Core.DecalSystem
{
    public class WorldComponentDecalPawns : WorldComponent
    {
        public static WorldComponentDecalPawns? Instance { get; private set; }

        private HashSet<Pawn> _pawns = new HashSet<Pawn>();

        // WorldComponent to track which pawns have decal gear
        // Persists the pawn set across saves so it doesn't lose track of who has what
        public WorldComponentDecalPawns(World world) : base(world) => Instance = this;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _pawns, "eotfDecalPawns", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                _pawns ??= new HashSet<Pawn>();
        }

        // HashSet of pawns known to have decal apparel on them
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