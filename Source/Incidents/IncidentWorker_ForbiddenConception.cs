using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_ForbiddenConception : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_ForbiddenConception.defName))
                return false;

            // Can only really make this work with RJW pregnancy, Biotech pregnancy almost guaranteed to break something
            if (RJWPregnancySettings.UseVanillaPregnancy)
                return false;

            // Only triggers for anti-bestiality colonies. That's the whole point
            if (DoesColonyIdeologySupportBestiality())
                return false;

            Map map = (Map)parms.target;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount < 2) return false;

            // Ensure there is at least one adult woman. Can be downed.
            var validWomen = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.RaceProps.Humanlike && p.GetVaginas().Any() && !LuxandraUtilities.IsPregnant(p) && !p.Dead && LuxandraUtilities.IsAdult(p)).ToList();

            var validAnimals = map.mapPawns.ColonyAnimals.Where(p => p.GetPenises().Any());

            return validWomen.Any() && validAnimals.Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var femalePawn = FindWomanTarget(map);
            var animals = map.mapPawns.ColonyAnimals.Where(p => p.GetPenises().Any());

            if (femalePawn == null)
            {
                LuxandraDebugActions.DebugLogMessage("Attempted to generate a divine pregnancy but couldn't find any valid woman");
                return false;
            }

            if (!animals.Any())
            {
                LuxandraDebugActions.DebugLogMessage("Attempted to generate a divine pregnancy but there is no valid animals");
                return false;
            }

            var chosenAnimal = animals.RandomElement();

            LuxandraDebugActions.DebugLogMessage($"Valid targets found: {femalePawn.NameShortColored} and {chosenAnimal.NameShortColored}");

            // Biotech integration
            bool isPregnant = false;
            // Ensure she isn't already pregnant and is biologically capable, and recheck if bestiality is enabled
            // This may be porn but we have standards
            if (!LuxandraUtilities.IsPregnant(femalePawn) && LuxandraUtilities.IsAdult(femalePawn) && LuxandraKinkTracker.IsBestialityEnabled())
            {
                LuxandraDebugActions.DebugLogMessage("Pawn not pregnant, creating hediff...");

                LuxandraDebugActions.DebugLogMessage("Attempting to create RJW pregnancy.");
                PregnancyHelper.AddPregnancyHediff(femalePawn, chosenAnimal);

                if (LuxandraUtilities.IsPregnant(femalePawn))
                {
                    isPregnant = true;
                    LuxandraDebugActions.DebugLogMessage("RJW pregnancy created successfully.");
                }
            }

            if (isPregnant && (CurrentKink == StorytellerKink.Pregnancy || CurrentKink == StorytellerKink.Bestiality))
            {
                Messages.Message($"Luxandra giggles at the sudden pregnancy of {femalePawn.NameShortColored}. She gifts you 5 Favor.", new LookTargets(new List<Pawn> { femalePawn }), MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(5);
            }

            if (isPregnant && LuxandraModChecks.IsMenstruationActive())
            {
                MenstruationIntegration.UpdateMenstruationWithNewPregnancy(femalePawn);
            }

            string letterLabel = "Forbidden Conception";

            // 2. Fetch animal name safely (e.g., "Muffalo 1" or its custom nickname)
            string animalName = chosenAnimal != null ? chosenAnimal.NameShortColored : "a colony animal";

            // 3. Format the story text
            string letterText =
                $"A sickening and scandalous secret has come to light within the colony. {femalePawn.NameShortColored} " +
                $"has been confirmed pregnant, but the genetic markers are deeply disturbing.\n\n" +
                $"The sire is not human—it matches {animalName}. It seems that during a moment of profound depravity, away " +
                $"from watchful eyes, an unspeakable act of forbidden intimacy occurred.\n\n" +
                $"{femalePawn.LabelShort} must now carry the consequences " +
                $"of this feral transgression to term, a living testament to a moment where human dignity completely collapsed.";

            // 4. Dispatch the letter (Uses NegativeEvent / ThreatSmall color palette to match the dark/scandal theme)
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterText,
                LetterDefOf.NegativeEvent,
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

        // Check if the player ideology exists and allows bestiality
        private bool DoesColonyIdeologySupportBestiality()
        {
            if (ModsConfig.IdeologyActive)
            {
                MemeDef zoophileMeme = DefDatabase<MemeDef>.GetNamed("Zoophile", false);
                PreceptDef bestialityAccepted = DefDatabase<PreceptDef>.GetNamed("Bestiality_Acceptable", false);
                PreceptDef bestialityVenerated = DefDatabase<PreceptDef>.GetNamed("Bestiality_OnlyVenerated", false);
                PreceptDef bestialityBonded = DefDatabase<PreceptDef>.GetNamed("Bestiality_BondOnly", false);
                PreceptDef bestialityHonorable = DefDatabase<PreceptDef>.GetNamed("Bestiality_Honorable", false);

                if (LuxandraUtilities.PlayerFactionHasMeme(zoophileMeme) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityAccepted) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityVenerated) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityBonded) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityHonorable))
                {
                    return true;
                }
            }

            return false;
        }
    }
}