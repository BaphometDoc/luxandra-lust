using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_DivineConception : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_DivineConception.defName))
                return false;

            Map map = (Map)parms.target;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount < 2) return false;

            // Ensure there is at least one adult woman. Can be downed.
            var validWomen = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.RaceProps.Humanlike && p.GetVaginas().Any() && !LuxandraUtilities.IsPregnant(p) && !p.Dead && LuxandraUtilities.IsAdult(p)).ToList();

            return validWomen.Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var femalePawn = FindWomanTarget(map);

            if (femalePawn == null)
            {
                LuxandraDebugActions.DebugLogMessage("Attempted to generate a divine pregnancy but couldn't find any valid woman");
                return false;
            }

            LuxandraDebugActions.DebugLogMessage($"Valid target found: {femalePawn.NameShortColored}");

            // Biotech integration
            bool isPregnant = false;
            // Ensure she isn't already pregnant and is biologically capable
            // This may be porn but we have standards
            if (!LuxandraUtilities.IsPregnant(femalePawn) && LuxandraUtilities.IsAdult(femalePawn))
            {
                LuxandraDebugActions.DebugLogMessage("Pawn not pregnant, creating hediff...");
                if (RJWPregnancySettings.UseVanillaPregnancy)
                {
                    LuxandraDebugActions.DebugLogMessage("Attempting to create Biotech pregnancy.");
                    // Create the pregnancy
                    Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, femalePawn, null);
                    GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(femalePawn, femalePawn, out _);
                    hediff_Pregnant.SetParents(femalePawn, femalePawn, inheritedGeneSet);
                    if (hediff_Pregnant != null)
                    {
                        hediff_Pregnant.Severity = 0.05f;
                    }

                    isPregnant = true;
                    LuxandraDebugActions.DebugLogMessage("Biotech pregnancy created successfully.");
                }
                else
                {

                    LuxandraDebugActions.DebugLogMessage("Attempting to create RJW pregnancy.");
                    PregnancyHelper.AddPregnancyHediff(femalePawn, femalePawn);

                    if (LuxandraUtilities.IsPregnant(femalePawn))
                    {
                        isPregnant = true;
                        LuxandraDebugActions.DebugLogMessage("RJW pregnancy created successfully.");
                    }
                }

                if (isPregnant && (CurrentKink == StorytellerKink.Pregnancy))
                {
                    Messages.Message($"Luxandra smiles at the sudden pregnancy of {femalePawn.NameShortColored}. She gifts you 5 Favor.", new LookTargets(new List<Pawn> { femalePawn }), MessageTypeDefOf.NeutralEvent);
                    GameComponent_LuxandraLust.Instance?.AddToFavorCounter(5);
                }

                if (isPregnant && LuxandraModChecks.IsMenstruationActive())
                {
                    MenstruationIntegration.UpdateMenstruationWithNewPregnancy(femalePawn);
                }
            }

            string letterLabel = "Divine Conception";

            string letterText =
                $"A bizarre medical anomaly has sent shockwaves through the colony. During a routine check, " +
                $"it was discovered that {femalePawn.NameShortColored} is suddenly pregnant.\n\n" +
                $"{femalePawn.LabelShort} swears she has been completely chaste, yet an embryo thrives within her all the same.\n\n" +
                $"Some of the colonists whisper of ancient, unseen mechanisms—or perhaps a grand, mocking jest " +
                $"Luxandra, weaving life from nothingness simply to see what happens next. Whatever the truth, a new life is coming.";

            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterText,
                LetterDefOf.PositiveEvent,
                femalePawn
            );
            return true;
        }

        // Helper method to find a non pregnant pawn with a vagina
        private Pawn FindWomanTarget(Map map)
        {
            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.RaceProps.Humanlike && p.GetVaginas().Any() && !p.Dead && !LuxandraUtilities.IsPregnant(p) && LuxandraUtilities.IsAdult(p)).ToList();

            if (candidates.Any())
            {
                candidates.SortBy(p => p.ageTracker.AgeBiologicalYears);
                return candidates.First();
            }
            else
                return null;
        }
    }
}