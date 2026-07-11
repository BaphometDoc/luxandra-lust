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
                LuxandraUtilities.IsAdult(p) && !p.Dead && !p.Downed &&
                p.royalty != null && p.royalty.AllTitlesInEffectForReading.Count > 0
            );
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Locate the adult player pawn with the highest structural title seniority score
            Pawn targetNoble = map.mapPawns.FreeColonistsSpawned
                .Where(p => LuxandraUtilities.IsAdult(p) && !p.Dead && !p.Downed && p.royalty?.MostSeniorTitle != null)
                .OrderByDescending(p => p.royalty.MostSeniorTitle.def.seniority)
                .FirstOrDefault();

            if (targetNoble == null) return false;

            LuxandraDebugActions.DebugLogMessage($"Attempted to execute Royal Depravity for  {targetNoble.NameShortColored}.");

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

            // Tank their sex need
            if (targetNoble.needs != null)
            {
                var sexNeed = LuxandraUtilities.GetSexNeed(targetNoble);
                if (sexNeed != null)
                {
                    sexNeed.CurLevel = 0f;
                }
            }

            // Add the hediff
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("Luxandra_HubristicGlow", false);
            if (hediffDef != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, targetNoble, null);
                HediffComp_Disappears disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappearComp != null)
                {
                    disappearComp.ticksToDisappear = 120000;
                }
                targetNoble.health.AddHediff(hediff, null, null, null);
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