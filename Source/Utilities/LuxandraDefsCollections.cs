using RimWorld;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // This file contains mostly the quick references to the defs, as well as the categorization of every
    // event compatible with the mod.

    // Defs for incidents so they can be referenced in code without hardcoding strings everywhere
    [DefOf]
    public static class LuxandraIncidentDefOf
    {
        // Auras, pulses and similar stuff
        public static IncidentDef Luxandra_Inc_HornyRushFemale;
        public static IncidentDef Luxandra_Inc_HornyRushMale;
        public static IncidentDef Luxandra_Inc_WhiteRain;

        public static IncidentDef Luxandra_Inc_LustfulFertilityPulse;
        public static IncidentDef Luxandra_Inc_FertilityPulseSite;
        public static IncidentDef Luxandra_Inc_FertilityPulseMechCluster;

        public static IncidentDef Luxandra_Inc_RapistBreak;

        // Raids
        public static IncidentDef Luxandra_Inc_HornyTribalRaid;
        public static IncidentDef Luxandra_Inc_DeviantHordeRaid;

        // Disease, illnesses
        public static IncidentDef Luxandra_Inc_AphrodisiacFever;

        // Body part messing
        public static IncidentDef Luxandra_Inc_MaleExpansion;
        public static IncidentDef Luxandra_Inc_FemaleExpansion;
        public static IncidentDef Luxandra_Inc_MaleReduction;
        public static IncidentDef Luxandra_Inc_FemaleReduction;

        // Boons
        public static IncidentDef Luxandra_Inc_LustfulSupplies;

        // Quests incidents
        public static IncidentDef Luxandra_Inc_BreakPrisonersContractQuest;

        static LuxandraIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LuxandraIncidentDefOf));
        }
    }

    public static class LuxandraDefsCollections
    {
        // List of all incidents managed by the mod
        // For usage in the rest of the mod
        private static readonly List<LuxandraIncidentDefs> _allIncidents = new List<LuxandraIncidentDefs>();

        /// <summary>
        /// All incidents available
        /// </summary>
        public static ReadOnlyCollection<LuxandraIncidentDefs> AllIncidents => _allIncidents.AsReadOnly();

        /// <summary>
        /// Incidents that provide benefits to the player
        /// </summary>
        public static IEnumerable<LuxandraIncidentDefs> PositiveIncidents => _allIncidents.Where(i => i.IsPositive);

        /// <summary>
        /// Incidents that harm or challenge the player
        /// </summary>
        public static IEnumerable<LuxandraIncidentDefs> NegativeIncidents => _allIncidents.Where(i => i.IsNegative);

        /// <summary>
        /// Incidents that are not strictly good nor bad, and are not quests
        /// </summary>
        public static IEnumerable<LuxandraIncidentDefs> NeutralIncidents => _allIncidents.Where(i => ((i.IsNegative && i.IsPositive) || (!i.IsNegative && !i.IsPositive)) && !i.IsQuest);

        /// <summary>
        /// Quests
        /// </summary>
        public static IEnumerable<LuxandraIncidentDefs> Quests => _allIncidents.Where(i => i.IsQuest);

        /// <summary>
        /// Incidents that cause raids
        /// </summary>
        public static IEnumerable<LuxandraIncidentDefs> Raids => _allIncidents.Where(i => i.IsRaid);

        public static void InizializeLuxandraIncidents()
        {
            #region Luxandra's base events
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushFemale,
                isRaid: false,
                isNegative: false,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushMale,
                isRaid: false,
                isNegative: false,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulFertilityPulse,
                isRaid: false,
                isNegative: true,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseSite,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseMechCluster,
                isRaid: true,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RapistBreak,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid,
                    isRaid: true,
                    isNegative: true,
                    isPositive: false
                ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid,
                isRaid: true,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion,
                isRaid: false,
                isNegative: false,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion,
                isRaid: false,
                isNegative: false,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction,
                isRaid: false,
                isNegative: true,
                isPositive: false
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulSupplies,
                isRaid: false,
                isNegative: false,
                isPositive: true
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BreakPrisonersContractQuest,
                isRaid: false,
                isNegative: false,
                isPositive: false,
                isQuest: true
            ));
            #endregion

            #region Royalty
            if (ModsConfig.RoyaltyActive)
            {
                IncidentDef royalLuxury = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_RoyalLuxury", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: royalLuxury,
                    isRaid: false,
                    isNegative: false,
                    isPositive: true,
                    requiresDLC: true
                ));

                IncidentDef royalDepravity = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_RoyalDepravity", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: royalDepravity,
                    isRaid: false,
                    isNegative: true,
                    isPositive: false,
                    requiresDLC: true
                ));
            }
            #endregion

            #region Ideology
            if (ModsConfig.IdeologyActive)
            {
                IncidentDef ideoLeaderBlessing = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_IdeoLeaderBlessing", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: ideoLeaderBlessing,
                    isRaid: false,
                    isNegative: false,
                    isPositive: true,
                    requiresDLC: true
                ));

                IncidentDef ideoLeaderDepravity = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_IdeoLeaderDepravity", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: ideoLeaderDepravity,
                    isRaid: false,
                    isNegative: true,
                    isPositive: false,
                    requiresDLC: true
                ));
            }
            #endregion

            #region RimJobWorld
            // This is a hard dependancy. If this breaks there's a valid reason for it to.
            IncidentDef nymphJoins = DefDatabase<IncidentDef>.GetNamed("NymphJoins", false);
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: nymphJoins,
                isRaid: false,
                isNegative: false,
                isPositive: true,
                requiresMod: true
            ));

            IncidentDef nymphVisitor = DefDatabase<IncidentDef>.GetNamed("NymphVisitor", false);
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: nymphVisitor,
                isRaid: false,
                isNegative: false,
                isPositive: false,
                requiresMod: true
            ));
            #endregion

            #region Brothel Colony Quests
            if (LuxandraCompatUtilities.IsBrothelColonyQuestsActive())
            {
                IncidentDef brothelQuest = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuest,
                   isRaid: false,
                   isNegative: false,
                   isPositive: false,
                   isQuest: true,
                   requiresMod: true
               ));

                IncidentDef brothelQuestBig = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Big", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestBig,
                   isRaid: false,
                   isNegative: false,
                   isPositive: false,
                   isQuest: true,
                   requiresMod: true
               ));

                IncidentDef brothelQuestExtreme = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Extreme", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestExtreme,
                   isRaid: false,
                   isNegative: false,
                   isPositive: false,
                   isQuest: true,
                   requiresMod: true
               ));
            }
            #endregion

            #region RJW Events
            if (LuxandraCompatUtilities.IsRJWEventsActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("PsychicArouse", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   isRaid: false,
                   isNegative: true,
                   isPositive: false,
                   requiresMod: true
               ));
            }
            #endregion

            #region RJW Genes
            if (LuxandraCompatUtilities.IsRJWGenesActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("SuccubusDreamVisit", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   isRaid: false,
                   isNegative: true,
                   isPositive: false,
                   requiresMod: true
               ));
            }
            #endregion

            #region Unleashed Bastards
            if (LuxandraCompatUtilities.IsUnleashedBastardsActive())
            {
                IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: unleashedBastardsRaid,
                   isRaid: true,
                   isNegative: true,
                   isPositive: false,
                   requiresMod: true
               ));
            }
            #endregion 

            // A final cleanup, just in case any def was wrong or corrupted
            var modsWithMissingDefs = _allIncidents.Where(i => i.IncidentDef == null || i.IncidentDef.defName == "");
            if (modsWithMissingDefs.Count() > 0)
            {
                Log.Warning($"[Luxandra Lust] Warning: {modsWithMissingDefs.Count()} events had missing defs. If this shown in your game, please contact the dev. He probably messed up.");
                foreach (var mod in modsWithMissingDefs)
                    _allIncidents.Remove(mod);
            }
        }
    }

    public class LuxandraIncidentDefs
    {
        /// <summary>
        /// The actual def
        /// </summary>
        public IncidentDef IncidentDef { get; set; }

        /// <summary>
        /// Is this a raid?
        /// </summary>
        public bool IsRaid { get; set; }

        /// <summary>
        /// Is this event harmful in some way? (can cohexist with positive)
        /// </summary>
        public bool IsNegative { get; set; }

        /// <summary>
        /// Is this event helpful in some way? (can cohexist with negative)
        /// </summary>
        public bool IsPositive { get; set; }

        /// <summary>
        /// Is this a quest?
        /// </summary>
        public bool IsQuest { get; set; }

        /// <summary>
        /// Does this require another a DLC to work?
        /// </summary>
        public bool RequiresDLC { get; set; }

        /// <summary>
        /// Does this require another mod to work?
        /// </summary>
        public bool RequiresMod { get; set; }

        /// <summary>
        /// Disabled by configs
        /// </summary>
        public bool IsDisabledByConfigs { get; set; }

        public LuxandraIncidentDefs(IncidentDef incidentDef, bool isRaid, bool isNegative, bool isPositive, bool isQuest = false, bool requiresDLC = false, bool requiresMod = false, bool isDisabledByConfigs = false)
        {
            this.IncidentDef = incidentDef;
            this.IsRaid = isRaid;
            this.IsNegative = isNegative;
            this.IsPositive = isPositive;
            this.IsQuest = isQuest;
            this.RequiresDLC = requiresDLC;
            this.RequiresMod = requiresMod;
            this.IsDisabledByConfigs = isDisabledByConfigs;
        }
    }
}
