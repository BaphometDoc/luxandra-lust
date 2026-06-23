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

    // This class tracks the important events and cycles for the storyteller
    public class LuxandraNarrator
    {
        // Sex counters
        public int sexActionCounter = 0;
        public int impureSexActionCounter = 0;
        public int rapeSexActionCounter = 0;

        public void RegisterSexAction()
        {
            sexActionCounter++;
        }
        public void RegisterImpureSexAction()
        {
            impureSexActionCounter++;
        }
        public void RegisterRapeSexAction()
        {
            rapeSexActionCounter++;
        }

        public void ResetSexCounters()
        {
            sexActionCounter = 0;
            impureSexActionCounter = 0;
            rapeSexActionCounter = 0;
        }
    }

    // Defs for incidents so they can be referenced in code without hardcoding strings everywhere
    [DefOf]
    public static class LuxandraIncidentDefOf
    {
        public static IncidentDef Luxandra_Inc_HornyRushFemale;
        public static IncidentDef Luxandra_Inc_HornyRushMale;
        public static IncidentDef Luxandra_Inc_HornyTribalRaid;
        public static IncidentDef Luxandra_Inc_UnleashedBastardsRaid;

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

            Log.Message($"[LuxannaLust] Cached {cachedPositiveIncidents.Count} valid positive incidents for rerolls.");
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
            };

            // If these mods are present, those won't be null so can be added
            if (LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid != null)
                cachedSexRelatedIncidents.Add(LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid);
            if (LuxandraIncidentDefOf.Luxandra_Inc_UnleashedBastardsRaid != null)
                cachedSexRelatedIncidents.Add(LuxandraIncidentDefOf.Luxandra_Inc_UnleashedBastardsRaid);

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

            Log.Message($"[LuxannaLust] Cached {cachedSexRelatedIncidents.Count} valid sex incidents.");
            return cachedSexRelatedIncidents;
        }

        // Debug cleaning method in case i need it
        public static void ClearCache()
        {
            cachedPositiveIncidents = null;
        }
    }
}