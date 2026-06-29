using RimWorld;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_RoyalDepravity : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Safety Gate: Block if Royalty DLC isn't active
            if (!ModsConfig.RoyaltyActive) return false;
            Map map = parms.target as Map ?? Find.CurrentMap;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_RoyalDepravity.defName))
            {
                return false;
            }

            return map.mapPawns.FreeColonistsSpawned.Any(p =>
                LuxandraUtilities.IsAdult(p) && !p.Dead &&
                p.royalty != null && p.royalty.AllTitlesInEffectForReading.Count > 0
            );
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Locate the adult player pawn with the highest structural title seniority score
            Pawn targetNoble = map.mapPawns.FreeColonistsSpawned
                .Where(p => LuxandraUtilities.IsAdult(p) && !p.Dead && p.royalty?.MostSeniorTitle != null)
                .OrderByDescending(p => p.royalty.MostSeniorTitle.def.seniority)
                .FirstOrDefault();

            if (targetNoble == null) return false;

            LuxandraDebugActions.DebugLogMessage($"Attempted to execute Royal Depravity for  {targetNoble.NameShortColored}.");

            // Welp, now they're a nymphomaniac rapist. Unfortunate. Maybe they already were...
            if (targetNoble.story?.traits != null)
            {
                // Fetch Rapist Trait
                TraitDef rapistTraitDef = DefDatabase<TraitDef>.GetNamed("Rapist", errorOnFail: false);
                if (rapistTraitDef != null && !targetNoble.story.traits.HasTrait(rapistTraitDef))
                {
                    targetNoble.story.traits.GainTrait(new Trait(rapistTraitDef));
                }

                // Fetch Hypersexual (Nymphomaniac) Trait
                TraitDef nymphoTraitDef = DefDatabase<TraitDef>.GetNamed("Nymphomaniac", errorOnFail: false);
                if (nymphoTraitDef != null && !targetNoble.story.traits.HasTrait(nymphoTraitDef))
                {
                    targetNoble.story.traits.GainTrait(new Trait(nymphoTraitDef));
                }
            }

            ThoughtDef depravityThought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_RoyalDepravityMoodlet", errorOnFail: false);
            if (depravityThought != null && targetNoble.needs?.mood?.thoughts?.memories != null)
            {
                targetNoble.needs.mood.thoughts.memories.TryGainMemory(depravityThought);
            }

            // Also send them raping. They earned it (kinda)
            LuxandraUtilities.ForceRapistBreak(targetNoble, "Royal Depravity", true);

            string titleText = "Royal Depravity";
            string descText = $"Luxandra tests the limits of your nobility. {targetNoble.LabelShort} bends under the cosmic weight of their high station, twisting their personality into something much more abusive.";

            Find.LetterStack.ReceiveLetter(titleText, descText, LetterDefOf.NegativeEvent, targetNoble);
            return true;
        }
    }
}