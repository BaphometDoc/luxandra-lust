using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // Your colonists decided it's time to fuck, whenever you like it or not.
    public class IncidentWorker_RapistBreak : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            return map.mapPawns.FreeColonistsSpawned.Any(p => !p.Downed && !p.InMentalState);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            DebugActions_Luxandra.DebugLogMessage("Attempting to trigger Rapist Break incident.");
            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            MentalStateDef rjwBreakDef = DefDatabase<MentalStateDef>.GetNamed("RandomRape", errorOnFail: false);
            if (rjwBreakDef == null)
            {
                Log.Warning("[Luxandra Debug] Could not fire Rapist Break incident: RJW 'RandomRape' MentalStateDef was not found in the game database.");
                return false;
            }

            HediffDef customHediffDef = DefDatabase<HediffDef>.GetNamed("Luxandra_PerverseCalling_Hediff", errorOnFail: false);

            int adultColonists = map.mapPawns.FreeColonistsSpawned.Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);
            int adultSlaves = map.mapPawns.SlavesOfColonySpawned.Count(p => p.DevelopmentalStage == DevelopmentalStage.Adult);

            int totalWorkforce = adultColonists + adultSlaves;
            int targetsToSelect = System.Math.Max(1, totalWorkforce / 4);

            List<Pawn> candidates = map.mapPawns.FreeColonistsSpawned
                .Where(p => !p.Downed && !p.Dead && p.skills != null && !p.InMentalState)
                .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Melee).Level) // Sort by highest Melee
                .ToList();

            if (candidates.Count == 0) return false;

            if (candidates.Count > targetsToSelect)
                candidates = candidates.Take(targetsToSelect).ToList();

            bool anySucceeded = false;
            List<Pawn> affectedPawns = new List<Pawn>();
            DebugActions_Luxandra.DebugLogMessage($"{candidates.Count} valid candidates found.");

            foreach (Pawn targetPawn in candidates)
            {
                // Send them raping (or try to)
                targetPawn.mindState.mentalStateHandler.TryStartMentalState(rjwBreakDef, "Rapist Break", forced: true);

                if (targetPawn.InMentalState && targetPawn.MentalStateDef == rjwBreakDef)
                {
                    affectedPawns.Add(targetPawn);
                    anySucceeded = true;

                    // Make them horny
                    if (targetPawn.needs != null)
                    {
                        var sexNeed = targetPawn.needs.TryGetNeed<Need_Sex>();
                        if (sexNeed != null)
                        {
                            sexNeed.CurLevel = 0f;
                        }
                    }

                    // Make them fast, for there is no escape
                    if (customHediffDef != null)
                    {
                        BodyPartRecord brainPart = targetPawn.RaceProps.body.AllParts
                                            .FirstOrDefault(p => p.def.tags != null && p.def.tags.Contains(RimWorld.BodyPartTagDefOf.ConsciousnessSource));

                        if (brainPart != null)
                        {
                            targetPawn.health.AddHediff(customHediffDef, brainPart, null);
                        }
                        else
                        {
                            // Fallback in case it can't find the brain
                            targetPawn.health.AddHediff(customHediffDef, null, null);
                        }
                    }
                }
            }

            if (anySucceeded)
            {
                string pawnNames = string.Join(", ", affectedPawns.Select(p => p.LabelShortCap));

                TaggedString letterLabel = "Rapist Break: " + (affectedPawns.Count > 1 ? "Multiple Colonists" : affectedPawns[0].LabelShortCap);
                TaggedString letterText = $"The dark influence of Luxandra has gripped your colony.\n\n" +
                                          $"<b>Affected: {pawnNames}</b>\n\n" +
                                          $"They crave sex, and they will not take 'no' for an answer.";

                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, affectedPawns);
                return true;
            }
            else
                Log.Warning("[Luxandra Debug] Failed to find apply mental state to pawns for Rapist Break incident.");

            return false;
        }
    }
}