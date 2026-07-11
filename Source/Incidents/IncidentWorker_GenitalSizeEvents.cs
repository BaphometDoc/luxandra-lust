using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public abstract class IncidentWorker_GenitalSizeChange : IncidentWorker
    {
        protected abstract Gender TargetGender { get; }
        protected abstract string EventLogName { get; }
        protected abstract string AssociatedThoughtDefName { get; }
        protected abstract string IncidentDef { get; }

        protected bool ApplyBodyAlteringHediff(Pawn pawn, List<RJWLewdablePart> sexParts, bool isExpansion)
        {
            LuxandraDebugActions.DebugLogMessage($"Attempted to alter {pawn.def} part size. IsExpansion: {isExpansion.ToString()}");
            if (pawn == null || pawn.Dead || sexParts.EnumerableNullOrEmpty()) return false;

            string properHediffDef = isExpansion ? "Luxandra_IntimateGrowth" : "Luxandra_IntimateShrinking";

            LuxandraDebugActions.DebugLogMessage($"Pawn and sex part was valid.");
            HediffDef resizeHediffDef = DefDatabase<HediffDef>.GetNamed(properHediffDef, false);
            if (resizeHediffDef == null)
            {
                Log.Warning("[Luxandra Debug] Def for Luxandra_GenitalResizing not found in the database.");
                return false;
            }

            // Apply the tracking Hediff to the pawn
            HediffWithComps trackingHediff = HediffMaker.MakeHediff(resizeHediffDef, pawn, null) as HediffWithComps;

            pawn.health.AddHediff(trackingHediff, null, null, null);

            var disappearComp = trackingHediff.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed(AssociatedThoughtDefName, errorOnFail: false);

                int exactDurationTicks = disappearComp.ticksToDisappear;

                // Give the thought and match the duration
                pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
                Thought_Memory liveMemory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(thought);
                if (liveMemory != null)
                {
                    liveMemory.durationTicksOverride = exactDurationTicks;
                    liveMemory.age = 0;
                }
            }

            return true;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (IncidentDef != null && !LuxandraEventCheck.IsEnabled(IncidentDef))
            {
                return false;
            }

            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            // Check if there is at least one adult matching our target gender on the map
            return map.mapPawns.AllPawnsSpawned.Any(p => IsEligibleTarget(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            LuxandraDebugActions.DebugLogMessage("Attempted to launch a genitalia resize event.");
            LuxandraDebugActions.DebugLogMessage("Specific incident: " + EventLogName);
            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            // Find all matching adult targets on the map (colonists, slaves, guests, enemies, etc.)
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(p => IsEligibleTarget(p))
                .ToList();


            LuxandraDebugActions.DebugLogMessage("Candidates found: " + candidates.Count);

            if (candidates.Count == 0) return false;

            // Execute the specific RJW adjustment logic
            bool success = ApplySizeChange(candidates, map);

            if (success)
            {
                this.SendStandardLetter(this.def.letterLabel, this.def.letterText, this.def.letterDef, parms, null);

                LuxandraDebugActions.DebugLogMessage("Resizing event successful.");
            }

            return success;
        }

        private bool IsEligibleTarget(Pawn p)
        {
            // Check that they there is someone who hasnt got hit yet first
            HediffDef resizeHediffDef = DefDatabase<HediffDef>.GetNamed("Luxandra_GenitalResizing", false);

            return p != null
                   && !p.Dead
                   && p.RaceProps != null
                   && p.RaceProps.Humanlike
                   && LuxandraUtilities.IsAdult(p)
                   && p.gender == TargetGender
                   && !p.health.hediffSet.HasHediff(resizeHediffDef);
        }

        protected abstract bool ApplySizeChange(List<Pawn> targets, Map map);
    }


    // Male Expansion Event (Penises +0.3)
    public class IncidentWorker_MaleExpansion : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Male;
        protected override string EventLogName => "Male Expansion";
        protected override string AssociatedThoughtDefName => "Luxandra_MaleExpandedMood";
        protected override string IncidentDef => LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion.defName;

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Penises.EnumerableNullOrEmpty())
                {
                    if (ApplyBodyAlteringHediff(pawn, sexParts.Penises, isExpansion: true))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }

    // Male Reduction Event (Penises -0.3)
    public class IncidentWorker_MaleReduction : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Male;
        protected override string EventLogName => "Masculine growth";
        protected override string AssociatedThoughtDefName => "Luxandra_MaleReducedMood";
        protected override string IncidentDef => LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction.defName;

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Penises.EnumerableNullOrEmpty())
                {
                    if (ApplyBodyAlteringHediff(pawn, sexParts.Penises, isExpansion: false))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }

    // Female Expansion Event (Breasts +0.3)
    public class IncidentWorker_FemaleExpansion : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Female;
        protected override string EventLogName => "Feminine growth";
        protected override string AssociatedThoughtDefName => "Luxandra_FemaleExpandedMood";
        protected override string IncidentDef => LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion.defName;

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Breasts.EnumerableNullOrEmpty())
                {
                    if (ApplyBodyAlteringHediff(pawn, sexParts.Breasts, isExpansion: true))
                    {
                        executedSuccessfully = true;
                    }
                }
            }

            if (executedSuccessfully && CurrentKink == StorytellerKink.Breasts)
            {
                Messages.Message($"Luxandra loves your colonists' new oversized breasts! She gifts you 2 Favor.", MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(2);
            }

            return executedSuccessfully;
        }
    }

    // Female Reduction Event (Breasts -0.3)
    public class IncidentWorker_FemaleReduction : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Female;
        protected override string EventLogName => "Feminine shrinkage";
        protected override string AssociatedThoughtDefName => "Luxandra_FemaleReducedMood";
        protected override string IncidentDef => LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction.defName;

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Breasts.EnumerableNullOrEmpty())
                {
                    if (ApplyBodyAlteringHediff(pawn, sexParts.Breasts, isExpansion: false))
                    {
                        executedSuccessfully = true;
                    }
                }
            }

            if (executedSuccessfully && CurrentKink == StorytellerKink.Breasts)
            {
                Messages.Message($"Luxandra finds your colonists' flatter chests cute! She gifts you 1 Favor to cheer you up.", MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(1);
            }

            return executedSuccessfully;
        }
    }

    /// <summary>
    /// Hediff that handles the genitalia increase
    /// </summary>
    public class Hediff_IntimateGrowth : HediffWithComps
    {
        private const float ChangeAmount = 0.3f;
        private const float MaxSeverity = 5.0f;
        private const float MinSeverity = 0.01f;

        public override void PostAdd(DamageInfo? dinfo) // Increase by 0.3
        {
            base.PostAdd(dinfo);
            LuxandraDebugActions.DebugLogMessage($"Genital growth hediff applied to {pawn.NameShortColored} parts.");

            // Randomize duration between 3 days (180,000 ticks) and 7 days (420,000 ticks)
            int randomTicks = Rand.RangeInclusive(180000, 420000);

            var disappearComp = this.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = randomTicks;
            }

            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            // Target penises for males, breasts for females
            var partsToModify = (pawn.gender == Gender.Male) ? sexParts.Penises : sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + ChangeAmount, MaxSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Increased {pawn.NameShortColored}'s {naturalPart.def} from {currentSeverity} to {newSeverity}.");
                    }
                }
            }
        }

        // RESTORE OLD SIZES ON EXPIRATION (Decrease by 0.3)
        public override void PostRemoved()
        {
            base.PostRemoved();

            // Gather the appropriate RJW parts based on the gender
            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            // Target penises for males, breasts for females
            var partsToModify = (pawn.gender == Gender.Male) ? sexParts.Penises : sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Max(currentSeverity - ChangeAmount, MinSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Restored {pawn.NameShortColored}'s {naturalPart.def} to {newSeverity}.");
                    }
                }
            }

            var messageString = pawn.gender == Gender.Male ? "Penises" : "Breasts";
            Messages.Message($"{pawn.LabelShort}'s {messageString} have gone back to their previous size.", pawn, MessageTypeDefOf.NeutralEvent);
        }
    }

    /// <summary>
    /// Hediff that handles the genitalia decrease
    /// </summary>
    public class Hediff_IntimateShrinking : HediffWithComps
    {
        private const float ChangeAmount = 0.3f;
        private const float MaxSeverity = 5.0f;
        private const float MinSeverity = 0.01f;

        public override void PostAdd(DamageInfo? dinfo) // Decrease by 0.3
        {
            base.PostAdd(dinfo);
            LuxandraDebugActions.DebugLogMessage($"Genital growth hediff applied to {pawn.NameShortColored} parts.");

            // Randomize duration between 3 days (180,000 ticks) and 7 days (420,000 ticks)
            int randomTicks = Rand.RangeInclusive(180000, 420000);

            var disappearComp = this.TryGetComp<HediffComp_Disappears>();
            if (disappearComp != null)
            {
                disappearComp.ticksToDisappear = randomTicks;
            }

            // Gather the appropriate RJW parts based on the gender
            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            // Target penises for males, breasts for females
            var partsToModify = (pawn.gender == Gender.Male) ? sexParts.Penises : sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Max(currentSeverity - ChangeAmount, MinSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Decreased {pawn.NameShortColored}'s {naturalPart.def} from {currentSeverity} to {newSeverity}.");
                    }
                }
            }
        }

        // RESTORE OLD SIZES ON EXPIRATION (Increase by 0.3)
        public override void PostRemoved()
        {
            base.PostRemoved();

            // Gather the appropriate RJW parts based on the gender
            var sexParts = pawn.GetLewdParts();
            if (sexParts == null) return;

            // Target penises for males, breasts for females
            var partsToModify = (pawn.gender == Gender.Male) ? sexParts.Penises : sexParts.Breasts;
            if (partsToModify.EnumerableNullOrEmpty()) return;

            for (int i = 0; i < partsToModify.Count; i++)
            {
                var part = partsToModify[i];
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + ChangeAmount, MaxSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, false);

                        LuxandraDebugActions.DebugLogMessage($"Restored {pawn.NameShortColored}'s {naturalPart.def} to {newSeverity}.");
                    }
                }
            }

            var messageString = pawn.gender == Gender.Male ? "Penises" : "Breasts";
            Messages.Message($"{pawn.LabelShort}'s {messageString} have gone back to their previous size.", pawn, MessageTypeDefOf.NeutralEvent);
        }
    }
}