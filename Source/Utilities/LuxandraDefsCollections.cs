using RimWorld;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    /// <summary>
    /// Defines the type of incident, for categorization and filtering purposes.
    /// </summary>
    public enum LuxandraIncidentType
    {
        Any = 0, // used as default value
        Positive,
        Negative,
        Neutral,
        Quest,
        Raid
    }

    // This file contains mostly the quick references to the defs, as well as the categorization of every
    // event compatible with the mod.

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

        public static IncidentDef Luxandra_Inc_TheMilkGame;

        // Disease, illnesses
        public static IncidentDef Luxandra_Inc_AphrodisiacFever;

        // Body part messing
        public static IncidentDef Luxandra_Inc_MaleExpansion;
        public static IncidentDef Luxandra_Inc_FemaleExpansion;
        public static IncidentDef Luxandra_Inc_MaleReduction;
        public static IncidentDef Luxandra_Inc_FemaleReduction;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_RoyalLuxury;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_RoyalDepravity;
        [MayRequireIdeology]
        public static IncidentDef Luxandra_Inc_IdeoLeaderBlessing;
        [MayRequireIdeology]
        public static IncidentDef Luxandra_Inc_IdeoLeaderDepravity;

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

        /// <summary>
        // List of all incidents managed by the mod
        // For usage in the rest of the mod
        /// </summary>
        private static readonly List<LuxandraIncidentDefs> _allIncidents = new List<LuxandraIncidentDefs>();

        /// <summary>
        /// All incidents available
        /// </summary>
        public static List<LuxandraIncidentDefs> AllIncidents => _allIncidents;

        /// <summary>
        /// Incidents that provide benefits to the player
        /// </summary>
        public static List<LuxandraIncidentDefs> PositiveIncidents => _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Positive).ToList();

        /// <summary>
        /// Incidents that harm or challenge the player
        /// </summary>
        public static List<LuxandraIncidentDefs> NegativeIncidents => _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Negative || i.IncidentType == LuxandraIncidentType.Raid).ToList();

        /// <summary>
        /// Incidents that are not strictly good nor bad, and are not quests
        /// </summary>
        public static List<LuxandraIncidentDefs> NeutralIncidents => _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Neutral || i.IncidentType == LuxandraIncidentType.Quest).ToList();

        /// <summary>
        /// Quests
        /// </summary>
        public static List<LuxandraIncidentDefs> Quests => _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Quest).ToList();

        /// <summary>
        /// Incidents that cause raids
        /// </summary>
        public static List<LuxandraIncidentDefs> Raids => _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Raid).ToList();

        public static void InizializeLuxandraIncidents()
        {
            #region Luxandra's base events
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushFemale,
                incidentType: LuxandraIncidentType.Positive,
                pointBaseCost: 1
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushMale,
                incidentType: LuxandraIncidentType.Positive,
                pointBaseCost: 1
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain,
                incidentType: LuxandraIncidentType.Negative,
                pointBaseCost: 5
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulFertilityPulse,
                incidentType: LuxandraIncidentType.Neutral,
                pointBaseCost: 5
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseSite,
                incidentType: LuxandraIncidentType.Negative
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                pointBaseCost: 7.5m
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RapistBreak,
                incidentType: LuxandraIncidentType.Negative
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid,
                incidentType: LuxandraIncidentType.Raid,
                pointBaseCost: 1
             ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid,
                incidentType: LuxandraIncidentType.Raid,
                pointBaseCost: 2
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_TheMilkGame,
                incidentType: LuxandraIncidentType.Neutral,
                pointBaseCost: 5
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever,
                incidentType: LuxandraIncidentType.Negative,
                pointBaseCost: 2
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                pointBaseCost: 1.5m
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                pointBaseCost: 1.5m
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                pointBaseCost: 1.5m
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                pointBaseCost: 1.5m
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulSupplies,
                incidentType: LuxandraIncidentType.Positive,
                pointBaseCost: 5
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BreakPrisonersContractQuest,
                incidentType: LuxandraIncidentType.Quest,
                pointBaseCost: 2.5m
            ));
            #endregion

            #region Royalty
            if (ModsConfig.RoyaltyActive)
            {
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalLuxury,
                    incidentType: LuxandraIncidentType.Positive,
                    requiresMod: true,
                    modRequired: "Royalty",
                    pointBaseCost: 5
                ));

                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    requiresMod: true,
                    modRequired: "Royalty",
                    pointBaseCost: 5
                ));
            }
            #endregion

            #region Ideology
            if (ModsConfig.IdeologyActive)
            {
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderBlessing,
                    incidentType: LuxandraIncidentType.Positive,
                    requiresMod: true,
                    modRequired: "Ideology",
                    pointBaseCost: 5
                ));

                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    requiresMod: true,
                    modRequired: "Ideology",
                    pointBaseCost: 5
                ));
            }
            #endregion

            #region RimJobWorld
            // This is a hard dependancy. If this breaks there's a valid reason for it to.
            IncidentDef nymphJoins = DefDatabase<IncidentDef>.GetNamed("NymphJoins", false);
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: nymphJoins,
                incidentType: LuxandraIncidentType.Positive,
                requiresMod: true,
                modRequired: "RimJobWorld",
                pointBaseCost: 2.5m
            ));

            IncidentDef nymphVisitor = DefDatabase<IncidentDef>.GetNamed("NymphVisitor", false);
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: nymphVisitor,
                incidentType: LuxandraIncidentType.Neutral,
                requiresMod: true,
                modRequired: "RimJobWorld"
            ));
            #endregion

            #region Brothel Colony Quests
            if (LuxandraCompatUtilities.IsBrothelColonyQuestsActive())
            {
                IncidentDef brothelQuest = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuest,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 2
               ));

                IncidentDef brothelQuestBig = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Big", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestBig,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 5
               ));

                IncidentDef brothelQuestExtreme = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Extreme", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestExtreme,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 5
               ));
            }
            #endregion

            #region RJW Events
            if (LuxandraCompatUtilities.IsRJWEventsActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("PsychicArouse", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Negative,
                   requiresMod: true,
                   modRequired: "RJW Events",
                   pointBaseCost: 1.5m
               ));
            }
            #endregion

            #region RJW Genes
            if (LuxandraCompatUtilities.IsRJWGenesActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("SuccubusDreamVisit", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Neutral,
                   requiresMod: true,
                   modRequired: "RJW Genes",
                   pointBaseCost: 2.5m
               ));
            }
            #endregion

            #region Unleashed Bastards
            if (LuxandraCompatUtilities.IsUnleashedBastardsActive())
            {
                IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: unleashedBastardsRaid,
                   incidentType: LuxandraIncidentType.Raid,
                   requiresMod: true,
                   pointBaseCost: 2
               ));
            }
            #endregion

            #region Anomaly

            if (ModsConfig.AnomalyActive)
            {
                #region Forbidden Anomalies
                if (LuxandraCompatUtilities.IsForbiddenAnomaliesActive())
                {
                    IncidentDef rapenant = DefDatabase<IncidentDef>.GetNamed("FA_RapenantIncident", false);
                    _allIncidents.Add(new LuxandraIncidentDefs(
                       incidentDef: rapenant,
                       incidentType: LuxandraIncidentType.Negative,
                       requiresMod: true,
                       modRequired: "Forbidden Anomalies"
                   ));

                    IncidentDef graspBloom = DefDatabase<IncidentDef>.GetNamed("FA_GraspbloomSpawn", false);
                    _allIncidents.Add(new LuxandraIncidentDefs(
                       incidentDef: graspBloom,
                       incidentType: LuxandraIncidentType.Negative,
                       requiresMod: true,
                       modRequired: "Forbidden Anomalies",
                       pointBaseCost: 2.5m
                   ));
                }
                #endregion
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
        /// Type of incident
        /// </summary>
        public LuxandraIncidentType IncidentType { get; set; }

        /// <summary>
        /// Base cost to be bought from event selectors (null = can't be bought)
        /// </summary>
        public decimal? PointBaseCost { get; set; }

        /// <summary>
        /// Does this require another mod to work?
        /// </summary>
        public bool RequiresMod { get; set; }

        /// <summary>
        /// Mod the event is from
        /// </summary>
        public string ModRequired { get; set; }

        public LuxandraIncidentDefs(IncidentDef incidentDef, LuxandraIncidentType incidentType, decimal? pointBaseCost = null, bool requiresMod = false, string modRequired = "")
        {
            this.IncidentDef = incidentDef;
            this.IncidentType = incidentType;
            this.PointBaseCost = pointBaseCost;
            this.RequiresMod = requiresMod;
            this.ModRequired = modRequired;
        }
    }



    [DefOf]
    public static class LuxandraFactionDefOf
    {
        /// <summary>
        /// Feral Amazons
        /// </summary>
        public static FactionDef Luxandra_AmazonTribe;

        /// <summary>
        /// The Crusade
        /// </summary>
        [MayRequireRoyalty]
        public static FactionDef Luxandra_PuritanCrusaders;

        /// <summary>
        /// Carnal Deviants
        /// </summary>
        public static FactionDef Luxandra_DeviantHordeFaction;

        static LuxandraFactionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LuxandraFactionDefOf));
        }
    }

    public static class LuxandraFactionDefsCollections
    {
        /// <summary>
        // List of all factions managed by the mod
        // For usage in the rest of the mod
        /// </summary>
        private static readonly List<LuxandraFactionDefs> _allFactions = new List<LuxandraFactionDefs>();

        /// <summary>
        /// All factions available
        /// </summary>
        public static ReadOnlyCollection<LuxandraFactionDefs> AllFactions => _allFactions.AsReadOnly();

        /// <summary>
        /// All factions available capable of raiding
        /// </summary>
        public static IEnumerable<LuxandraFactionDefs> AllRaidingFactions => _allFactions.Where(f => f.CanSendRaids);

        public static void InizializeLuxandraFactions()
        {

            _allFactions.Add(new LuxandraFactionDefs(
                factionDef: LuxandraFactionDefOf.Luxandra_AmazonTribe,
                canSendRaids: true
            ));

            _allFactions.Add(new LuxandraFactionDefs(
                factionDef: LuxandraFactionDefOf.Luxandra_DeviantHordeFaction,
                canSendRaids: true
            ));

            if (ModsConfig.RoyaltyActive)
            {
                _allFactions.Add(new LuxandraFactionDefs(
                    factionDef: LuxandraFactionDefOf.Luxandra_PuritanCrusaders,
                    canSendRaids: true
                ));
            }
        }

        public class LuxandraFactionDefs
        {
            /// <summary>
            /// The actual def
            /// </summary>
            public FactionDef FactionDef { get; set; }

            /// <summary>
            /// Determines if the faction can send raids
            /// </summary>
            public bool CanSendRaids { get; set; }

            public LuxandraFactionDefs(FactionDef factionDef, bool canSendRaids)
            {
                this.FactionDef = factionDef;
                this.CanSendRaids = canSendRaids;
            }
        }
    }
}
