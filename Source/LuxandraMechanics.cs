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

        public static IncidentDef Luxandra_Inc_MaleExpansion;
        public static IncidentDef Luxandra_Inc_FemaleExpansion;
        public static IncidentDef Luxandra_Inc_MaleReduction;
        public static IncidentDef Luxandra_Inc_FemaleReduction;

        public static IncidentDef Luxandra_Inc_LustfulSupplies;

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

        /// <summary>
        /// This is used as failsafe for the event reroll system, if it can't find a sexual event, it tries to roll a positive one
        /// </summary>
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

        /// <summary>
        /// Gets the list of events that are either from this mod or from RJW related mods
        /// </summary>
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
                LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion,
                LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion,
                LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction,
                LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction,
                LuxandraIncidentDefOf.Luxandra_Inc_LustfulSupplies
            };

            // Add the Royalty events
            if (ModsConfig.RoyaltyActive)
            {
                IncidentDef royalLuxury = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_RoyalLuxury", false);
                if (royalLuxury != null)
                    cachedSexRelatedIncidents.Add(royalLuxury);

                IncidentDef royalDepravity = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_RoyalDepravity", false);
                if (royalDepravity != null)
                    cachedSexRelatedIncidents.Add(royalDepravity);
            }

            // Add the Ideology events
            if (ModsConfig.IdeologyActive)
            {
                IncidentDef ideoLeaderBlessing = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_IdeoLeaderBlessing", false);
                if (ideoLeaderBlessing != null)
                    cachedSexRelatedIncidents.Add(ideoLeaderBlessing);

                IncidentDef ideoLeaderDepravity = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_IdeoLeaderDepravity", false);
                if (ideoLeaderDepravity != null)
                    cachedSexRelatedIncidents.Add(ideoLeaderDepravity);
            }

            // If these mods are present, those won't be null so can be added
            // Unleashed Bastards
            IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
            if (unleashedBastardsRaid != null)
                cachedSexRelatedIncidents.Add(unleashedBastardsRaid);

            // Other events from other RJW based mods
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