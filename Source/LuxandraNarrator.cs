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

    // This class tracks ticks and cycles
    public class LuxandraNarrator
    {
        private int cycleCounter = 0;

        public void TickDaily()
        {
            cycleCounter++;
        }

        public int GetCycle()
        {
            return cycleCounter;
        }

        public void ResetCycle()
        {
            cycleCounter = 0;
        }
    }
}