using Verse;

namespace LuxandraLust
{
    // Various utilities related to mod compatibility
    public static class LuxandraCompatUtilities
    {
        // This mainly exists cause i'm lazy and cba checking the mod packages every time I want 
        // to add compatibility. Also lets me quickly fix every instance in case I end up needing
        // to due to a mod being discontinued and replaced by a fork
        #region Mod Checks

        public static bool IsCumpilationActive()
        {
            return ModsConfig.IsActive("vegapnk.cumpilation") || ModsConfig.IsActive("parciwal.cumpliationlite");
        }

        public static bool IsMenstruationActive()
        {
            return ModsConfig.IsActive("rjw.menstruation");
        }

        public static bool IsRJWGenesActive()
        {
            return ModsConfig.IsActive("Vegapnk.rjw.genes");
        }

        public static bool IsRJWEventsActive()
        {
            return ModsConfig.IsActive("c0ffee.rjw.events");
        }

        public static bool IsBrothelColonyActive()
        {
            return ModsConfig.IsActive("calamabanana.rjw.brothelcolony");
        }

        public static bool IsBrothelColonyQuestsActive()
        {
            return ModsConfig.IsActive("bep.rjw.brothelcolony.quest");
        }

        public static bool IsUnleashedBastardsActive()
        {
            return ModsConfig.IsActive("archersaiter.rjw.unleashed.bastard");
        }

        public static bool IsForbiddenAnomaliesActive()
        {
            return ModsConfig.IsActive("forbidden.anomalies");
        }

        #endregion
    }
}