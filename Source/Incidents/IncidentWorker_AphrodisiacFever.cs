using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_AphrodisiacFever : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever.defName))
            {
                return false;
            }

            Map map = (Map)parms.target;
            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            var potentialTargets = map.mapPawns.FreeColonistsAndPrisonersSpawned
                .Where(p => p.RaceProps.Humanlike && !p.Dead && p.health != null && LuxandraUtilities.IsAdult(p))
                .ToList();

            if (potentialTargets.Count == 0) return false;

            // Decide how many pawns to infect based on population size
            int minInfectTarget = Math.Max(1, potentialTargets.Count / 5);
            int maxInfectTarget = Math.Max(1, potentialTargets.Count / 2);
            // Safety net in case rounding does weird shit on low amount of colonists
            if (maxInfectTarget < minInfectTarget) maxInfectTarget = minInfectTarget;
            int countToInfect = Rand.RangeInclusive(minInfectTarget, maxInfectTarget);
            potentialTargets.Shuffle(); // Randomizes the list order

            List<Pawn> infectedPawns = new List<Pawn>();

            for (int i = 0; i < countToInfect; i++)
            {
                Pawn targetPawn = potentialTargets[i];

                int partToInfect = Rand.RangeInclusive(1, 3);

                BodyPartRecord targetPart = null;

                // Roll for a body part first
                switch (partToInfect)
                {
                    case 1:
                        targetPart = targetPawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def.defName == "Genitals");
                        break;
                    case 2:
                        targetPart = targetPawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def.defName == "Anus");
                        break;
                    case 3:
                    default:
                        targetPart = targetPawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def.defName == "Mouth" || p.def.defName == "Stomach");
                        break;
                }

                // Check if the rolled part is missing
                if (targetPart == null)
                {
                    // If missing, attempt to infect the first available one
                    targetPart = targetPawn.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(p => p.def.defName == "Genitals" ||
                                         p.def.defName == "Anus" ||
                                         p.def.defName == "Mouth");
                }

                if (targetPart == null)
                    LuxandraDebugActions.DebugLogMessage($"Failed to find a eligible bodypart to infect {targetPawn.NameFullColored}.");

                if (targetPart != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(HediffDef.Named("Luxandra_AphrodisiacFever"), targetPawn, targetPart);
                    targetPawn.health.AddHediff(hediff);
                    infectedPawns.Add(targetPawn);
                }
            }

            if (infectedPawns.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage($"Failed to roll any eligible colonist to infect.");
                return false;
            }
            ;

            // Build the Custom Multi-Pawn Letter text dynamically
            StringBuilder textBuilder = new StringBuilder();

            if (infectedPawns.Count == 1)
            {
                textBuilder.AppendLine($"A sudden, overwhelming heat has overcome {infectedPawns[0].NameShortColored}.");
            }
            else
            {
                textBuilder.AppendLine("A sudden wave of overwhelming heat has swept through the colony, affecting the following pawns:");
                foreach (Pawn p in infectedPawns)
                {
                    textBuilder.AppendLine($" - {p.NameShortColored}");
                }
            }

            textBuilder.AppendLine();
            textBuilder.AppendLine("They are suffering from an acute Aphrodisiac Fever that is dangerously affecting their ability to focus and rest.");
            textBuilder.AppendLine();
            textBuilder.AppendLine("It will not clear on its own, and the sensory overload is severely draining their consciousness. The fever is known to only cool and dissipate through direct contact with freshly produced vaginal fluids or semen, physically quenching the heat during the act.");

            LookTargets lookTargets = new LookTargets(infectedPawns);

            string letterLabel = infectedPawns.Count == 1 ? "Feverish Flare" : "Fever Outbreak";
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                textBuilder.ToString().TrimEnd(),
                LetterDefOf.NegativeEvent,
                lookTargets
            );

            return true;
        }
    }
}