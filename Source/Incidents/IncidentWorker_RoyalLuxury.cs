using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_RoyalLuxury : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Safety Gate: Block if Royalty DLC isn't active
            if (!ModsConfig.RoyaltyActive) return false;
            Map map = parms.target as Map ?? Find.CurrentMap;

            return map.mapPawns.FreeColonistsSpawned.Any(p =>
                LuxandraLustUtilities.IsAdult(p) && !p.Dead &&
                p.royalty != null && p.royalty.AllTitlesInEffectForReading.Count > 0
            );
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Pawn targetNoble = map.mapPawns.FreeColonistsSpawned
                .Where(p => LuxandraLustUtilities.IsAdult(p) && !p.Dead && p.royalty?.MostSeniorTitle != null)
                .OrderByDescending(p => p.royalty.MostSeniorTitle.def.seniority)
                .FirstOrDefault();

            DebugActions_Luxandra.DebugLogMessage($"Attempted to execute Royal Luxury for  {targetNoble.NameShortColored}.");

            if (targetNoble == null) return false;

            // They shall now become beautiful. If they aren't already, anyway
            TraitDef beautyTraitDef = DefDatabase<TraitDef>.GetNamed("Beauty", errorOnFail: false);
            if (beautyTraitDef != null && targetNoble.story?.traits != null)
            {
                Trait activeBeautyTrait = targetNoble.story.traits.allTraits
                    .FirstOrDefault(t => t.def == beautyTraitDef);

                if (activeBeautyTrait == null)
                {
                    // They don't have it, so give them degree 2 (Beautiful)
                    targetNoble.story.traits.GainTrait(new Trait(beautyTraitDef, 2, forced: true));
                }
                // If they have it, but it's only degree 1 (Pretty), upgrade it!
                else if (activeBeautyTrait.Degree == 1)
                {
                    // Remove the old "Pretty" instance first to prevent UI duplication bugs
                    targetNoble.story.traits.allTraits.Remove(activeBeautyTrait);

                    // Re-grant as degree 2 (Beautiful)
                    targetNoble.story.traits.GainTrait(new Trait(beautyTraitDef, 2, forced: true));
                }
            }

            // And not only that, but they're about to get thiccer. If they can.
            var sexParts = targetNoble.GetLewdParts();
            if (sexParts.Breasts != null && targetNoble.gender != Gender.Male && !sexParts.Breasts.EnumerableNullOrEmpty())
                EnlargeSexPart(targetNoble, sexParts.Breasts);
            if (sexParts.Penises != null && !sexParts.Penises.EnumerableNullOrEmpty())
                EnlargeSexPart(targetNoble, sexParts.Penises);

            ThoughtDef luxuryThought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_RoyalLuxuryMoodlet", errorOnFail: false);
            if (luxuryThought != null && targetNoble.needs?.mood?.thoughts?.memories != null)
            {
                targetNoble.needs.mood.thoughts.memories.TryGainMemory(luxuryThought);
            }

            string titleText = "Royal Luxury";
            string descText = $"Luxandra showers {targetNoble.LabelShort} with divine attention. As most important noble present, their mind fills with exceptional validation, for they are the most beautiful of all.";

            Find.LetterStack.ReceiveLetter(titleText, descText, LetterDefOf.PositiveEvent, targetNoble);
            return true;
        }

        private bool EnlargeSexPart(Pawn pawn, List<RJWLewdablePart> sexParts)
        {
            if (pawn == null || pawn.Dead || sexParts.EnumerableNullOrEmpty()) return false;

            bool anyChanged = false;

            foreach (var part in sexParts)
            {
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    float changeAmount = 0.5f;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + changeAmount, 3.0f);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, sync: false);

                        anyChanged = true;
                        DebugActions_Luxandra.DebugLogMessage($"Increased {part.SexPart.Def.defName} size for {pawn.NameShortColored}.");
                    }
                }
            }

            return anyChanged;
        }
    }
}