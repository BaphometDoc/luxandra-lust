using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // This class tracks the selected storyteller
    public static class LuxandraStorytellerCheck
    {
        public static bool IsActive()
        {
            return Find.Storyteller?.def.defName == "LuxandraLust";
        }
    }

    // Defs for incidents so they can be referenced in code without hardcoding strings everywhere
    [DefOf]
    public static class LuxandraIncidentDefOf
    {
        public static IncidentDef Luxandra_Inc_HornyRushFemale;
        public static IncidentDef Luxandra_Inc_HornyRushMale;
        public static IncidentDef Luxandra_Inc_HornyTribalRaid;
        public static IncidentDef Luxandra_Inc_RapistBreak;

        static LuxandraIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LuxandraIncidentDefOf));
        }
    }

    // This class manages the pool of events that Luxandra should consider for her triggers
    public static class LuxandraEventPool
    {
        private static List<IncidentDef> cachedPositiveIncidents = null;
        private static List<IncidentDef> cachedSexRelatedIncidents = null;

        public static List<IncidentDef> GetPositiveIncidents()
        {
            if (cachedPositiveIncidents != null)
            {
                return cachedPositiveIncidents;
            }

            cachedPositiveIncidents = new List<IncidentDef>();

            cachedPositiveIncidents = DefDatabase<IncidentDef>.AllDefs
                         .Where(x => x.category == IncidentCategoryDefOf.Misc &&
                                     x.letterDef == LetterDefOf.PositiveEvent)
                         .ToList();

            DebugActions_Luxandra.DebugLogMessage($"Cached {cachedPositiveIncidents.Count} valid positive incidents for rerolls.");
            return cachedPositiveIncidents;
        }

        public static List<IncidentDef> GetSexRelatedIncidents()
        {
            if (cachedSexRelatedIncidents != null)
            {
                return cachedSexRelatedIncidents;
            }

            // Add events managed by this mod
            cachedSexRelatedIncidents = new List<IncidentDef>
            {
                LuxandraIncidentDefOf.Luxandra_Inc_HornyRushFemale,
                LuxandraIncidentDefOf.Luxandra_Inc_HornyRushMale,
                LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid,
                LuxandraIncidentDefOf.Luxandra_Inc_RapistBreak,
            };

            // If these mods are present, those won't be null so can be added
            // Unleashed Bastards
            IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
            if (unleashedBastardsRaid != null)
                cachedSexRelatedIncidents.Add(unleashedBastardsRaid);

            // Other events from other mods
            var sexDefNames = new List<string>
            {
                // Base RJW
                "NymphJoins",
                "NymphVisitor",
                "NymphVisitorGroupEasy", // Not adding the hard one as these are tribals on steroids

                // Brothel colony quests
                "RJWBCQ_GiveQuest_BrothelCustomer",
                "RJWBCQ_GiveQuest_BrothelCustomer_Big",
                "RJWBCQ_GiveQuest_BrothelCustomer_Extreme",

                // RJW Events
                "PsychicArouse",

                // RJW Genes
                "SuccubusDreamVisit",
            };

            // Cycle through the list and add the ones it finds
            // This should on paper not hard-require the mods and not break if they get updated/removed
            foreach (string defName in sexDefNames)
            {
                IncidentDef def = DefDatabase<IncidentDef>.GetNamed(defName, false);

                if (def != null)
                {
                    cachedSexRelatedIncidents.Add(def);
                }
            }

            DebugActions_Luxandra.DebugLogMessage($"Cached {cachedSexRelatedIncidents.Count} valid sex incidents.");
            return cachedSexRelatedIncidents;
        }

        // Debug cleaning method in case i need it
        public static void ClearCache()
        {
            cachedPositiveIncidents = null;
        }
    }
}