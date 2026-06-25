using RimWorld;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_IdeologyLeaderDepravity : IncidentWorker
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
                   LuxandraUtilities.IsAdult(leaderPawn);
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

            LuxandraDebugActions.DebugLogMessage($"Attempted to execute Spiritual Depravity for {leaderPawn.NameShortColored}.");

            // Fallback check
            if (leaderPawn == null || leaderPawn.Map != map || leaderPawn.Dead) return false;

            if (leaderPawn.story?.traits != null)
            {
                TraitDef rapistTraitDef = DefDatabase<TraitDef>.GetNamed("Rapist", errorOnFail: false);
                if (rapistTraitDef != null && !leaderPawn.story.traits.HasTrait(rapistTraitDef))
                {
                    leaderPawn.story.traits.GainTrait(new Trait(rapistTraitDef));
                }

                TraitDef nymphoTraitDef = DefDatabase<TraitDef>.GetNamed("Nymphomaniac", errorOnFail: false);
                if (nymphoTraitDef != null && !leaderPawn.story.traits.HasTrait(nymphoTraitDef))
                {
                    leaderPawn.story.traits.GainTrait(new Trait(nymphoTraitDef));
                }
            }

            ThoughtDef depravityThought = DefDatabase<ThoughtDef>.GetNamed("Luxandra_IdeologyDepravityMoodlet", errorOnFail: false);
            if (depravityThought != null && leaderPawn.needs?.mood?.thoughts?.memories != null)
            {
                leaderPawn.needs.mood.thoughts.memories.TryGainMemory(depravityThought);
            }

            // Tank their sex need
            if (leaderPawn.needs != null)
            {
                var sexNeed = leaderPawn.needs.TryGetNeed<rjw.Need_Sex>();
                if (sexNeed != null)
                {
                    sexNeed.CurLevel = 0f;
                }
            }

            // Also send them raping. They earned it (kinda)
            LuxandraUtilities.ForceRapistBreak(leaderPawn, "Spiritual Depravity", true);

            string label = "Spiritual Depravity";
            string text = $"{leaderPawn.LabelShort}, the ultimate guide of your belief system has cracked. Under the quiet whispers of Luxandra, they have warped their moral compass, collapsing into an unhinged state of obsession.";

            Find.LetterStack.ReceiveLetter(label, text, this.def.letterDef ?? LetterDefOf.NegativeEvent, leaderPawn);
            return true;
        }
    }
}