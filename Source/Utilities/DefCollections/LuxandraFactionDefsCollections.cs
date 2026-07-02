using RimWorld;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
