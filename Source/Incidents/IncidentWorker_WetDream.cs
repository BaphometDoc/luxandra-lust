using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_WetDream : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_WetDreamIncident.defName)) return false;

            Map map = (Map)parms.target;
            if (map == null) return false;

            // Check if there is at least one sleeping pawn with a low sex need
            return GetValidCandidates(map).Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Grab all valid sleeping, frustrated pawns
            List<Pawn> candidates = GetValidCandidates(map);
            if (!candidates.Any()) return false;

            // Pick a random lucky (or unlucky) target
            Pawn targetPawn = candidates.RandomElement();

            LuxandraUtilities.CauseWetDream(targetPawn, map);

            // Send a silent/minor notification (Message instead of a giant aggressive Red Letter)
            Messages.Message(
                $"{targetPawn.NameShortColored} woke up abruptly from an incredibly vivid dream. The sheets are ruined.",
                targetPawn,
                MessageTypeDefOf.NeutralEvent,
                false
            );

            return true;
        }

        // Helper to cleanly filter out viable targets on the map
        private List<Pawn> GetValidCandidates(Map map)
        {
            return map.mapPawns.FreeAdultColonistsSpawned
                .Where(p => p.RaceProps.Humanlike
                            && !p.Dead
                            && p.CurJob != null
                            && p.jobs.curJob.def == JobDefOf.LayDown // Explicitly sleeping/resting
                            && !p.Awake()
                            && LuxandraUtilities.IsAdult(p))
                .Where(p =>
                {
                    // Dynamic check for the RJW Sex Need falling under 50%
                    Need sexNeed = p.needs.AllNeeds.FirstOrDefault(n => n.def.defName == "Sex");
                    return sexNeed != null && sexNeed.CurLevelPercentage < 0.50f;
                })
                .ToList();
        }
    }
}