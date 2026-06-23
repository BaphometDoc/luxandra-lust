using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class IncidentWorker_HornyRushFemale : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Incident can fire only if there is at least one living, free female colonist
            return map.mapPawns.FreeAdultColonistsSpawned.Any(p => p.gender == Gender.Female && !p.Dead);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Pawn> femalePawns = map.mapPawns.AllPawnsSpawned
                            .Where(p => p.RaceProps.Humanlike && p.gender == Gender.Female && !p.Dead && p.DevelopmentalStage.Adult())
                            .ToList();

            if (femalePawns.Count == 0) return false;

            ThoughtDef moodDef = ThoughtDef.Named("Luxandra_HornyRush_Mood");
            HediffDef buffDef = HediffDef.Named("Luxandra_HornyRush_Buff");

            SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(null);
            foreach (Pawn pawn in femalePawns)
            {
                // Drop the target 'Sex' need to 0%
                if (pawn.needs != null)
                {
                    var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }

                // Give custom moodlet
                if (moodDef != null)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(moodDef);
                }

                // Apply custom Hediff buff (+Consciousness, +Movement, +Decay Multiplier)
                if (buffDef != null)
                {
                    pawn.health.AddHediff(HediffMaker.MakeHediff(buffDef, pawn, null));
                }
            }

            // Fire standard UI alert notification letter targeting the first affected unit
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, femalePawns.First());

            Log.Message($"[LuxandraLust] Horny Rush (female) successfully processed for {femalePawns.Count} targets.");
            return true;
        }
    }

    public class IncidentWorker_HornyRushMale : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Incident can fire only if there is at least one living, free male colonist
            return map.mapPawns.FreeAdultColonistsSpawned.Any(p => p.gender == Gender.Male && !p.Dead);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            List<Pawn> malePawns = map.mapPawns.AllPawnsSpawned
                            .Where(p => p.RaceProps.Humanlike && p.gender == Gender.Male && !p.Dead && p.DevelopmentalStage.Adult())
                            .ToList();

            if (malePawns.Count == 0) return false;

            ThoughtDef moodDef = ThoughtDef.Named("Luxandra_HornyRush_Mood");
            HediffDef buffDef = HediffDef.Named("Luxandra_HornyRush_Buff");

            SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(null);
            foreach (Pawn pawn in malePawns)
            {
                // Drop the target 'Sex' need to 0%
                if (pawn.needs != null)
                {
                    var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }

                // Give custom moodlet
                if (moodDef != null)
                {
                    pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(moodDef);
                }

                // Apply custom Hediff buff (+Consciousness, +Movement, +Decay Multiplier)
                if (buffDef != null)
                {
                    pawn.health.AddHediff(HediffMaker.MakeHediff(buffDef, pawn, null));
                }
            }

            // Fire standard UI alert notification letter targeting the first affected unit
            SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, malePawns.First());

            Log.Message($"[LuxandraLust] Horny Rush (male) successfully processed for {malePawns.Count} targets.");
            return true;
        }
    }
}