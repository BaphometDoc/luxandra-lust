using RimWorld;
using HarmonyLib;
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
}