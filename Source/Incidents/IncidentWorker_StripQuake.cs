using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_StripQuake : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_StripQuake.defName))
                return false;

            Map map = (Map)parms.target;
            // Safety check: Ensure the map exists and has colonists
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount == 0)
                return false;

            // 2. Find any spawned humans who are NOT player-controlled and NOT hostile to the player
            // This should catch friendly traders, visitors, allies, and neutral guests.
            var friendlyGuestsOnMap = map.mapPawns.AllHumanlikeSpawned.Where(p =>
                !p.IsPlayerControlled &&
                !p.HostileTo(Faction.OfPlayer) &&
                !p.IsPrisoner // Excludes prisoners so they don't block the event
            );

            return !friendlyGuestsOnMap.Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Trigger the visual/audio screen shake so the player feels the "quake"
            Find.CameraDriver.shaker.DoShake(2.5f); // Medium-strong screen shake
            // SoundDefOf.FlashstormAmbience.PlayOneShotOnCamera(map);

            List<Pawn> colonists = map.mapPawns.AllPawnsSpawned.ToList();

            // Loop through EVERY spawned pawn on the map
            for (int i = colonists.Count - 1; i >= 0; i--)
            {
                Pawn pawn = colonists[i];

                // Target absolutely all humanlikes (Colonists, Raiders, Strangers, Prisoners alike)
                if (pawn != null && pawn.Spawned && pawn.RaceProps.Humanlike && !pawn.IsColonyMech)
                {
                    if (pawn.apparel != null && pawn.apparel.WornApparelCount > 0)
                    {
                        pawn.apparel.DropAll(pawn.Position, forbid: false);
                    }
                }
            }

            return true;
        }
    }
}