using RimWorld;
using rjw.Modules.Interactions;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_IdeologyLeaderBlessing : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Safety Gate: Block if Ideology DLC isn't active
            if (!ModsConfig.IdeologyActive) return false;
            Map map = parms.target as Map ?? Find.CurrentMap;

            // Ensure the player faction has a primary ideoligion and an assigned leader pawn
            Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (primaryIdeo == null) return false;

            Precept_Role leaderPrecept = primaryIdeo.PreceptsListForReading
                .OfType<Precept_Role>()
                .FirstOrDefault(p => p.def == PreceptDefOf.IdeoRole_Leader);

            Pawn leaderPawn = leaderPrecept?.ChosenPawns()?.FirstOrDefault();

            return leaderPawn != null &&
                   !leaderPawn.Dead &&
                   leaderPawn.Spawned &&
                   leaderPawn.Map == map &&
                   LuxandraLustUtilities.IsAdult(leaderPawn);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (primaryIdeo == null) return false;

            Precept_Role leaderPrecept = primaryIdeo.PreceptsListForReading
                .OfType<Precept_Role>()
                .FirstOrDefault(p => p.def == PreceptDefOf.IdeoRole_Leader);

            Pawn leaderPawn = leaderPrecept?.ChosenPawns()?.FirstOrDefault();

            DebugActions_Luxandra.DebugLogMessage($"Attempted to execute Spiritual Radiance for {leaderPawn.NameShortColored}.");

            // Fallback
            if (leaderPawn == null || leaderPawn.Map != map || leaderPawn.Dead) return false;

            if (leaderPawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef blessingThought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_IdeologyBlessingMoodlet", errorOnFail: false);
                if (blessingThought != null)
                {
                    leaderPawn.needs.mood.thoughts.memories.TryGainMemory(blessingThought);
                }
            }

            // They shall now become beautiful. If they aren't already, anyway
            TraitDef beautyTraitDef = DefDatabase<TraitDef>.GetNamed("Beauty", errorOnFail: false);
            if (beautyTraitDef != null && leaderPawn.story?.traits != null)
            {
                Trait activeBeautyTrait = leaderPawn.story.traits.allTraits
                    .FirstOrDefault(t => t.def == beautyTraitDef);

                if (activeBeautyTrait == null)
                {
                    // They don't have it, so give them degree 2 (Beautiful)
                    leaderPawn.story.traits.GainTrait(new Trait(beautyTraitDef, 2, forced: true));
                }
                // If they have it, but it's only degree 1 (Pretty), upgrade it!
                else if (activeBeautyTrait.Degree == 1)
                {
                    // Remove the old "Pretty" instance first to prevent UI duplication bugs
                    leaderPawn.story.traits.allTraits.Remove(activeBeautyTrait);

                    // Re-grant as degree 2 (Beautiful)
                    leaderPawn.story.traits.GainTrait(new Trait(beautyTraitDef, 2, forced: true));
                }
            }

            // And not only that, but they're about to get thiccer. If they can.
            var sexParts = leaderPawn.GetLewdParts();
            if (sexParts.Breasts != null && leaderPawn.gender != Gender.Male && !sexParts.Breasts.EnumerableNullOrEmpty())
                LuxandraLustUtilities.EnlargeSexPart(leaderPawn, sexParts.Breasts);
            if (sexParts.Penises != null && !sexParts.Penises.EnumerableNullOrEmpty())
                LuxandraLustUtilities.EnlargeSexPart(leaderPawn, sexParts.Penises);

            string label = "Spiritual Radiance";
            string text = $"Luxandra has reached out to touch the mind (and not only that!) of your guide! {leaderPawn.LabelShort} has experienced a surge of euphoric clarity, transforming their vision of the faith into a passionate beacon of bliss and devotion.";

            Find.LetterStack.ReceiveLetter(label, text, this.def.letterDef ?? LetterDefOf.PositiveEvent, leaderPawn);
            return true;
        }
    }
}