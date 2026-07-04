using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
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
        private static bool _isInitialized = false;

        /// <summary>
        // List of all factions managed by the mod
        // For usage in the rest of the mod
        /// </summary>
        private static readonly List<LuxandraFactionDefs> _allFactions = new List<LuxandraFactionDefs>();
        private static List<LuxandraFactionDefs> _allRaidingFactions = new List<LuxandraFactionDefs>();

        /// <summary>
        /// All factions available
        /// </summary>
        public static List<LuxandraFactionDefs> AllFactions
        {
            get
            {
                if (!_isInitialized)
                {
                    Log.Error("[Luxandra Lust] Notice: A system tried to read the faction def database before initialization finished. If the game is still booting up, DO NOT PANIC—this will resolve itself automatically once loading completes. Please share this full log with the dev.");
                }
                return _allFactions;
            }
        }

        /// <summary>
        /// All factions available capable of raiding. Instant lookup, zero allocations!
        /// </summary>
        public static List<LuxandraFactionDefs> AllRaidingFactions => _allRaidingFactions;

        public static void InizializeLuxandraFactions()
        {
            // Prevent double-initialization just in case
            if (_isInitialized) return;

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

            _allRaidingFactions = _allFactions.Where(f => f.CanSendRaids).ToList();

            // Initialization successful
            _isInitialized = true;
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
