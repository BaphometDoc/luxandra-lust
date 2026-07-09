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

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderDepravity.defName))
            {
                return false;
            }

            // Ensure the player faction has a primary ideoligion and an assigned leader pawn
            Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (primaryIdeo == null) return false;

            Precept_Role leaderPrecept = primaryIdeo.PreceptsListForReading
                .OfType<Precept_Role>()
                .FirstOrDefault(p => p.def == PreceptDefOf.IdeoRole_Leader);

            Pawn leaderPawn = leaderPrecept?.ChosenPawns()?.FirstOrDefault();

            return leaderPawn != null &&
                   !leaderPawn.Dead &&
                   !leaderPawn.Downed &&
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
            if (leaderPawn == null || leaderPawn.Map != map || leaderPawn.Dead || leaderPawn.Downed) return false;


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

            // Tank their sex need
            if (leaderPawn.needs != null)
            {
                var sexNeed = LuxandraUtilities.GetSexNeed(leaderPawn);
                if (sexNeed != null)
                {
                    sexNeed.CurLevel = 0f;
                }
            }

            // Add the hediff
            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamed("Luxandra_HubristicGlow", false);
            if (hediffDef != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(hediffDef, leaderPawn, null);
                HediffComp_Disappears disappearComp = hediff.TryGetComp<HediffComp_Disappears>();
                if (disappearComp != null)
                {
                    disappearComp.ticksToDisappear = 120000;
                }
                leaderPawn.health.AddHediff(hediff, null, null, null);
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