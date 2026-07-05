using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

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

        public static IncidentDef Luxandra_Inc_WetDreamsPulse;
        public static IncidentDef Luxandra_Inc_WetDreamsPulseSite;
        public static IncidentDef Luxandra_Inc_WetDreamsPulseMechCluster;

        public static IncidentDef Luxandra_Inc_RapistBreak;

        // Raids
        public static IncidentDef Luxandra_Inc_HornyTribalRaid;
        public static IncidentDef Luxandra_Inc_DeviantHordeRaid;
        public static IncidentDef Luxandra_Inc_DeviantHungerSwarm;
        public static IncidentDef Luxandra_Inc_AmazonStealthAmbush;
        public static IncidentDef Luxandra_Inc_AmazonBloodTrial;
        [MayRequireRoyalty]
        public static IncidentDef Luxandra_Inc_InquisitionPurgeSappers;

        public static IncidentDef Luxandra_Inc_TheMilkGame;

        // Disease, illnesses
        public static IncidentDef Luxandra_Inc_AphrodisiacFever;
        public static IncidentDef Luxandra_Inc_IntimateInfestation;

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
        public static IncidentDef Luxandra_Inc_BustyCurse;

        // Quests incidents
        public static IncidentDef Luxandra_Inc_BreakPrisonersContractQuest;

        // Other random stuff
        public static IncidentDef Luxandra_Inc_ForbiddenLove;
        public static IncidentDef Luxandra_Inc_WetDreamIncident;
        public static IncidentDef Luxandra_Inc_StripQuake;

        static LuxandraIncidentDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LuxandraIncidentDefOf));
        }
    }

    public static class LuxandraDefsCollections
    {
        public static bool _isInitialized = false;

        // Cached list of all incidents managed by the mod
        // For usage in the rest of the mod
        private static readonly List<LuxandraIncidentDefs> _allIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _positiveIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _negativeIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _negativeIncidentsNoRaids = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _neutralIncidents = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _neutralIncidentsNoQuests = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _quests = new List<LuxandraIncidentDefs>();
        private static List<LuxandraIncidentDefs> _raids = new List<LuxandraIncidentDefs>();

        /// <summary>
        /// All incidents available. Safe-checked to ensure data is loaded.
        /// </summary>
        public static List<LuxandraIncidentDefs> AllIncidents
        {
            get
            {
                if (!_isInitialized)
                {
                    Log.Error("[Luxandra Lust] Notice: A system tried to read the incident database before initialization finished. If the game is still booting up, DO NOT PANIC—this will resolve itself automatically once loading completes. Please share this full log with the dev.");
                }
                return _allIncidents;
            }
        }

        /// <summary>
        /// Incidents that provide benefits to the player
        /// </summary>
        public static List<LuxandraIncidentDefs> PositiveIncidents => _positiveIncidents;

        /// <summary>
        /// Incidents that harm or challenge the player (including raids)
        /// </summary>
        public static List<LuxandraIncidentDefs> NegativeIncidents => _negativeIncidents;

        /// <summary>
        /// Incidents that harm or challenge the player (excluding raids)
        /// </summary>
        public static List<LuxandraIncidentDefs> NegativeIncidentsNoRaids => _negativeIncidentsNoRaids;

        /// <summary>
        /// Incidents that are not strictly good nor bad (including quests)
        /// </summary>
        public static List<LuxandraIncidentDefs> NeutralIncidents => _neutralIncidents;

        /// <summary>
        /// Incidents that are not strictly good nor bad (excluding quests)
        /// </summary>
        public static List<LuxandraIncidentDefs> NeutralIncidentsNoQuests => _neutralIncidentsNoQuests;

        /// <summary>
        /// Quests
        /// </summary>
        public static List<LuxandraIncidentDefs> Quests => _quests;

        /// <summary>
        /// Incidents that cause raids
        /// </summary>
        public static List<LuxandraIncidentDefs> Raids => _raids;

        #region Debug stuff
        /// <summary>
        /// Reset the event pool and recalculate it
        /// </summary>
        public static void ReinitializeIncidentPool()
        {
            // 1. Flip the flag so systems know we are in a mutating state
            _isInitialized = false;

            // 2. Clear the master list contents (handles the readonly constraint safely)
            _allIncidents.Clear();

            // 3. Wipe out the cached sub-lists by allocating empty lists
            _positiveIncidents = new List<LuxandraIncidentDefs>();
            _negativeIncidents = new List<LuxandraIncidentDefs>();
            _negativeIncidentsNoRaids = new List<LuxandraIncidentDefs>();
            _neutralIncidents = new List<LuxandraIncidentDefs>();
            _neutralIncidentsNoQuests = new List<LuxandraIncidentDefs>();
            _quests = new List<LuxandraIncidentDefs>();
            _raids = new List<LuxandraIncidentDefs>();

            InizializeLuxandraIncidents();
            Log.Message($"[Luxandra Debug] Manual cache invalidation triggered. Active incident pools have been rebuilt based on current kink preferences.");
            PrintLuxandraIncidentTotals();

            Messages.Message("Luxandra incident pools successfully recalculated!", MessageTypeDefOf.TaskCompletion, false);
        }

        /// <summary>
        /// Prints the amount of available incident per type
        /// </summary>
        public static void PrintLuxandraIncidentTotals()
        {
            Log.Message($"[Luxandra Lust] found {AllIncidents.Count} lustful events.");

            LuxandraDebugActions.DebugLogMessage($"Positive incidents: {PositiveIncidents.Count}");
            LuxandraDebugActions.DebugLogMessage($"Neutral incidents: {NeutralIncidents.Count} (including {Quests.Count} quests)");
            LuxandraDebugActions.DebugLogMessage($"Negative incidents: {NegativeIncidents.Count} (including {Raids.Count} raids)");
            LuxandraDebugActions.DebugLogMessage($"TOTAL: {AllIncidents.Count} incidents available.");
        }
        #endregion

        public static void InizializeLuxandraIncidents()
        {
            // Prevent double-initialization just in case
            if (_isInitialized) return;

            #region Luxandra's base events
            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushFemale,
                incidentType: LuxandraIncidentType.Positive,
                description: "Causes a surge of speed and libido in female colonists.",
                pointBaseCost: 15
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyRushMale,
                incidentType: LuxandraIncidentType.Positive,
                description: "Causes a surge of speed and libido in male colonists.",
                pointBaseCost: 15
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain,
                incidentType: LuxandraIncidentType.Negative,
                description: "It's warm. It's sticky. You probably shouldn't go outside today.",
                pointBaseCost: 40,
                kinks: new[] { StorytellerKink.Cum }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulFertilityPulse,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A somewhat long lasting condition that will massively increase the fertility and libido of every human in the map.",
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseSite,
                incidentType: LuxandraIncidentType.Negative,
                description: "A remote transmitter will cause a Fertility Pulse on your map until you disable it",
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FertilityPulseMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                description: "A mech cluster will cause a Fertility Pulse on your map until you destroy it",
                pointBaseCost: 45,
                kinks: new[] { StorytellerKink.Pregnancy }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulse,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A average lasting condition that will occasionally wake up your colonists in a pleasurable way.",
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Cum }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulseSite,
                incidentType: LuxandraIncidentType.Negative,
                description: "A remote transmitter will cause a Wet Dreams Pulse on your map until you disable it",
                kinks: new[] { StorytellerKink.Cum }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamsPulseMechCluster,
                incidentType: LuxandraIncidentType.Raid,
                description: "A mech cluster will cause a Wet Dreams Pulse on your map until you destroy it",
                pointBaseCost: 55,
                kinks: new[] { StorytellerKink.Cum }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RapistBreak,
                incidentType: LuxandraIncidentType.Negative,
                description: "One or more pawns go on a Random Rape mental break",
                kinks: new[] { StorytellerKink.Rape }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid,
                incidentType: LuxandraIncidentType.Raid,
                description: "A tribal raid with increased speed and sex drive",
                pointBaseCost: 35,
                kinks: new[] { StorytellerKink.Rape }
             ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid,
                incidentType: LuxandraIncidentType.Raid,
                description: "A Carnal Deviant that will try to rape your colonists rather than kill them",
                pointBaseCost: 35,
                kinks: new[] { StorytellerKink.Rape }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_DeviantHungerSwarm,
                incidentType: LuxandraIncidentType.Raid,
                description: "A horde of hungry Deviants is approaching your colony to find food",
                pointBaseCost: 35
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AmazonStealthAmbush,
                incidentType: LuxandraIncidentType.Raid,
                description: "An elite assassin squad of Amazons attempts to kill your most valuable pawn... or male."
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AmazonBloodTrial,
                incidentType: LuxandraIncidentType.Raid,
                description: "Young amazons are sent on a trial to prove their worth. They'll have your heads or die trying.",
                pointBaseCost: 35
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_TheMilkGame,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A gift of milk, but someone will come for the newly fed cattles...",
                pointBaseCost: 40
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever,
                incidentType: LuxandraIncidentType.Negative,
                description: "A disease that heals only via sex.",
                pointBaseCost: 15
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                description: "Male colonist penises will grow for a while.",
                pointBaseCost: 25
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleExpansion,
                incidentType: LuxandraIncidentType.Positive,
                description: "Female colonist breasts will grow for a while.",
                pointBaseCost: 25,
                kinks: new[] { StorytellerKink.Breasts }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_MaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                description: "Male colonist penises will shrink for a while.",
                pointBaseCost: 15
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_FemaleReduction,
                incidentType: LuxandraIncidentType.Negative,
                description: "Female colonist breasts will shrink for a while.",
                pointBaseCost: 15,
                kinks: new[] { StorytellerKink.Breasts }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_LustfulSupplies,
                incidentType: LuxandraIncidentType.Positive,
                description: "A drop of supplies for your more exotic needs.",
                pointBaseCost: 55
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BustyCurse,
                incidentType: LuxandraIncidentType.Positive,
                description: "One of your pawns wanted to touch tits. The monkey paw's finger curls...",
                pointBaseCost: 15,
                kinks: new[] { StorytellerKink.Breasts, StorytellerKink.Futa }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_BreakPrisonersContractQuest,
                incidentType: LuxandraIncidentType.Quest,
                description: "A request to 'break' some prisoners.",
                pointBaseCost: 40,
                kinks: new[] { StorytellerKink.Rape }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_ForbiddenLove,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A couple of close relatives developed a forbidden relationship.",
                pointBaseCost: 50,
                kinks: new[] { StorytellerKink.Incest, StorytellerKink.Pregnancy }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_WetDreamIncident,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A pawn will have an amazing dream.",
                pointBaseCost: 5,
                kinks: new[] { StorytellerKink.Cum }
            ));

            _allIncidents.Add(new LuxandraIncidentDefs(
                incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_StripQuake,
                incidentType: LuxandraIncidentType.Neutral,
                description: "A strong quake will strip every pawn on the map.",
                pointBaseCost: 100
            ));

            #endregion

            #region Royalty
            if (ModsConfig.RoyaltyActive)
            {
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalLuxury,
                    incidentType: LuxandraIncidentType.Positive,
                    description: "Your royal colonist will gain several permanent sex related boons.",
                    requiresMod: true,
                    modRequired: "Royalty",
                    pointBaseCost: 35
                ));

                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_RoyalDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    description: "Your royal colonist will gain several permanent sex related penalties and go on a rapist rage.",
                    requiresMod: true,
                    modRequired: "Royalty",
                    kinks: new[] { StorytellerKink.Rape }
                ));

                // Crusader events require Royalty
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_InquisitionPurgeSappers,
                    incidentType: LuxandraIncidentType.Raid,
                    description: "A group of crusaders took the 'burn the infidels' motto very literally.",
                    requiresMod: true,
                    modRequired: "Royalty"
                ));
            }
            #endregion

            #region Ideology
            if (ModsConfig.IdeologyActive)
            {
                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderBlessing,
                    incidentType: LuxandraIncidentType.Positive,
                    description: "Your ideology leader will gain several permanent sex related boons.",
                    requiresMod: true,
                    modRequired: "Ideology",
                    pointBaseCost: 35
                ));

                _allIncidents.Add(new LuxandraIncidentDefs(
                    incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IdeoLeaderDepravity,
                    incidentType: LuxandraIncidentType.Negative,
                    description: "Your ideology leader will gain several permanent sex related penalties and go on a rapist rage.",
                    requiresMod: true,
                    modRequired: "Ideology",
                    kinks: new[] { StorytellerKink.Rape }
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
                pointBaseCost: 35
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
            if (LuxandraModChecks.IsBrothelColonyQuestsActive())
            {
                IncidentDef brothelQuest = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuest,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 45
               ));

                IncidentDef brothelQuestBig = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Big", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestBig,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 55
               ));

                IncidentDef brothelQuestExtreme = DefDatabase<IncidentDef>.GetNamed("RJWBCQ_GiveQuest_BrothelCustomer_Extreme", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: brothelQuestExtreme,
                   incidentType: LuxandraIncidentType.Quest,
                   requiresMod: true,
                   modRequired: "Brothel Colony Quests",
                   pointBaseCost: 65
               ));
            }
            #endregion

            #region RJW Events
            if (LuxandraModChecks.IsRJWEventsActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("PsychicArouse", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Negative,
                   requiresMod: true,
                   modRequired: "RJW Events",
                   pointBaseCost: 25
               ));
            }
            #endregion

            #region RJW Genes
            if (LuxandraModChecks.IsRJWGenesActive())
            {
                IncidentDef psychicArouse = DefDatabase<IncidentDef>.GetNamed("SuccubusDreamVisit", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: psychicArouse,
                   incidentType: LuxandraIncidentType.Neutral,
                   requiresMod: true,
                   modRequired: "RJW Genes"
               ));
            }
            #endregion

            #region RJW Insects
            if (LuxandraModChecks.IsRJWInsectsActive())
            {
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: LuxandraIncidentDefOf.Luxandra_Inc_IntimateInfestation,
                   incidentType: LuxandraIncidentType.Negative,
                   description: "Insectoids will assault a pawn and attempted to use them as seedbed.",
                   requiresMod: true,
                   pointBaseCost: 35,
                   kinks: new[] { StorytellerKink.Implantation }
               ));
            }
            #endregion

            #region Unleashed Bastards
            if (LuxandraModChecks.IsUnleashedBastardsActive())
            {
                IncidentDef unleashedBastardsRaid = DefDatabase<IncidentDef>.GetNamed("Luxandra_Inc_UnleashedBastardsRaid", false);
                _allIncidents.Add(new LuxandraIncidentDefs(
                   incidentDef: unleashedBastardsRaid,
                   incidentType: LuxandraIncidentType.Raid,
                   description: "A stronger raid which will more easily try to rape your colonists.",
                   requiresMod: true,
                   kinks: new[] { StorytellerKink.Rape }
               ));
            }
            #endregion

            #region Anomaly

            if (ModsConfig.AnomalyActive)
            {
                #region Forbidden Anomalies
                if (LuxandraModChecks.IsForbiddenAnomaliesActive())
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
                       pointBaseCost: 35
                   ));
                }
                #endregion
            }
            #endregion

            // A final cleanup, just in case any def was wrong or corrupted
            var incidentWithMissingDefs = _allIncidents.Where(i => i.IncidentDef == null || i.IncidentDef.defName == "");
            if (incidentWithMissingDefs.Count() > 0)
            {
                Log.Warning($"[Luxandra Debug] Warning: {incidentWithMissingDefs.Count()} events had missing defs. If this shown in your game, please contact the dev. He probably messed up.");
                foreach (var incident in incidentWithMissingDefs)
                    _allIncidents.Remove(incident);
            }

            // Finally, remove all incidents with tags incompatible with the enabled kinks
            foreach (var incident in _allIncidents)
            {
                int incidentsDisabledCauseKinks = 0;
                bool shouldBeEnabled = AreRelatedKinksEnabled(incident);
                if (!shouldBeEnabled)
                {
                    LuxandraDebugActions.DebugLogMessage($"Disabling {incident.IncidentDef.defName} since the necessary conditions aren't enabled");
                    _allIncidents.Remove(incident);
                    incidentsDisabledCauseKinks++;
                }

                if (incidentsDisabledCauseKinks > 0)
                    Log.Message($"Disabled {incidentsDisabledCauseKinks} events due to detected kink conditions not found");
            }

            // Populate the remaining caches
            _positiveIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Positive).ToList();
            _negativeIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Negative || i.IncidentType == LuxandraIncidentType.Raid).ToList();
            _negativeIncidentsNoRaids = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Negative).ToList();
            _neutralIncidents = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Neutral || i.IncidentType == LuxandraIncidentType.Quest).ToList();
            _neutralIncidentsNoQuests = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Neutral).ToList();
            _quests = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Quest).ToList();
            _raids = _allIncidents.Where(i => i.IncidentType == LuxandraIncidentType.Raid).ToList();

            // Initialization successful
            _isInitialized = true;
        }

        public static bool AreRelatedKinksEnabled(LuxandraIncidentDefs incident)
        {
            var tags = incident.AssociatedKinks;

            if (tags.Contains(StorytellerKink.Bestiality) && !LuxandraKinkTracker.IsBestialityEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Rape) && !LuxandraKinkTracker.IsRapeEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Necrophilia) && !LuxandraKinkTracker.IsNecrophiliaEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Implantation) && !LuxandraKinkTracker.IsImplantationEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Incest) && !LuxandraKinkTracker.IsIncestEnabled())
                return false;

            if (tags.Contains(StorytellerKink.Futa) && !LuxandraKinkTracker.IsFutaEnabled())
                return false;

            return true;
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
        /// Short description of the event
        /// </summary>
        public string ShortDescription { get; set; }

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

        /// <summary>
        /// What kinks are associated with the event
        /// </summary>
        public StorytellerKink[] AssociatedKinks { get; }

        public LuxandraIncidentDefs(IncidentDef incidentDef, LuxandraIncidentType incidentType, string description = "", decimal? pointBaseCost = null, bool requiresMod = false, string modRequired = "", StorytellerKink[] kinks = null)
        {
            this.IncidentDef = incidentDef;
            this.IncidentType = incidentType;
            this.ShortDescription = description;
            this.PointBaseCost = pointBaseCost;
            this.RequiresMod = requiresMod;
            this.ModRequired = modRequired;
            this.AssociatedKinks = kinks ?? new StorytellerKink[0];
        }
    }
}
