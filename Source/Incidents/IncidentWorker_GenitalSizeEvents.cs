using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public abstract class IncidentWorker_GenitalSizeChange : IncidentWorker
    {
        private const float MaxSeverity = 3.0f;
        private const float MinSeverity = 0.01f;

        protected abstract Gender TargetGender { get; }
        protected abstract string EventLogName { get; }
        protected abstract string AssociatedThoughtDefName { get; }

        protected bool AlterPartSize(Pawn pawn, List<RJWLewdablePart> sexParts, bool isExpansion)
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
                    float newSeverity = isExpansion
                        ? UnityEngine.Mathf.Min(currentSeverity + changeAmount, MaxSeverity)
                        : UnityEngine.Mathf.Max(currentSeverity - changeAmount, MinSeverity);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, sync: false);

                        anyChanged = true;
                    }
                }
            }

            // If their body size changed successfully, attempt to push the mood memory assignment
            if (anyChanged && pawn.needs?.mood?.thoughts?.memories != null)
            {
                ThoughtDef thought = DefDatabase<ThoughtDef>.GetNamed(AssociatedThoughtDefName, errorOnFail: false);

                if (thought != null)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thought);
                }
                else
                {
                    Log.Error($"[{EventLogName}] Moodlet Failed: Could not find any ThoughtDef named '{AssociatedThoughtDefName}' in the database!");
                }
            }

            return anyChanged;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            // Check if there is at least one adult matching our target gender on the map
            return map.mapPawns.AllPawnsSpawned.Any(p => IsEligibleTarget(p));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            DebugActions_Luxandra.DebugLogMessage("Attempted to launch a genitalia resize event.");
            DebugActions_Luxandra.DebugLogMessage("Specific incident: " + EventLogName);
            Map map = parms.target as Map ?? Find.CurrentMap;
            if (map == null) return false;

            // Find all matching adult targets on the map (colonists, slaves, guests, enemies, etc.)
            List<Pawn> candidates = map.mapPawns.AllPawnsSpawned
                .Where(p => IsEligibleTarget(p))
                .ToList();


            DebugActions_Luxandra.DebugLogMessage("Candidates found: " + candidates.Count);

            if (candidates.Count == 0) return false;

            // Execute the specific RJW adjustment logic
            bool success = ApplySizeChange(candidates, map);

            if (success)
            {
                this.SendStandardLetter(this.def.letterLabel, this.def.letterText, this.def.letterDef, parms, null);

                DebugActions_Luxandra.DebugLogMessage("Resizing event successful.");
            }

            return success;
        }

        private bool IsEligibleTarget(Pawn p)
        {
            return p != null
                   && !p.Dead
                   && p.RaceProps != null
                   && p.RaceProps.Humanlike
                   && p.DevelopmentalStage == DevelopmentalStage.Adult
                   && p.gender == TargetGender;
        }

        protected abstract bool ApplySizeChange(List<Pawn> targets, Map map);
    }


    // Male Expansion Event (Penises +0.1)
    public class IncidentWorker_MaleExpansion : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Male;
        protected override string EventLogName => "Male Expansion";
        protected override string AssociatedThoughtDefName => "Luxandra_MaleExpandedMood";

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Penises.EnumerableNullOrEmpty())
                {
                    if (AlterPartSize(pawn, sexParts.Penises, isExpansion: true))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }

    // Male Reduction Event (Penises -0.1)
    public class IncidentWorker_MaleReduction : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Male;
        protected override string EventLogName => "Masculine growth";
        protected override string AssociatedThoughtDefName => "Luxandra_MaleReducedMood";

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Penises.EnumerableNullOrEmpty())
                {
                    if (AlterPartSize(pawn, sexParts.Penises, isExpansion: false))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }

    // Female Expansion Event (Breasts +0.1)
    public class IncidentWorker_FemaleExpansion : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Female;
        protected override string EventLogName => "Feminine growth";
        protected override string AssociatedThoughtDefName => "Luxandra_FemaleExpandedMood";

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Breasts.EnumerableNullOrEmpty())
                {
                    if (AlterPartSize(pawn, sexParts.Breasts, isExpansion: true))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }

    // Female Reduction Event (Breasts -0.1)
    public class IncidentWorker_FemaleReduction : IncidentWorker_GenitalSizeChange
    {
        protected override Gender TargetGender => Gender.Female;
        protected override string EventLogName => "Feminine shrinkage";
        protected override string AssociatedThoughtDefName => "Luxandra_FemaleReducedMood";

        protected override bool ApplySizeChange(List<Pawn> targets, Map map)
        {
            bool executedSuccessfully = false;
            foreach (Pawn pawn in targets)
            {
                var sexParts = pawn.GetLewdParts();
                if (sexParts != null && !sexParts.Breasts.EnumerableNullOrEmpty())
                {
                    if (AlterPartSize(pawn, sexParts.Breasts, isExpansion: false))
                    {
                        executedSuccessfully = true;
                    }
                }
            }
            return executedSuccessfully;
        }
    }
}