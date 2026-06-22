using HarmonyLib;
using RimWorld;
using rjw;
using Verse;
using Verse.AI;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(JobDriver), "EndJobWith")]
    public static class Patch_JobDriver_EndJobWith
    {
        public static void Prefix(JobDriver __instance, JobCondition condition)
        {
            if (!LuxandraStorytellerCheck.IsActive())
                return;

            if (condition != JobCondition.Succeeded)
                return;

            Pawn pawn = __instance.pawn;

            if (pawn == null || pawn.Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike)
                return;

            JobDriver driver = __instance.pawn?.jobs?.curDriver;

            if (driver == null)
                return;

            bool isSex = driver is JobDriver_Sex;

            // If the sex act was completed, add it to the counter
            if (isSex)
            {
                GameComponent_LuxandraLust.Instance?.RegisterSexAction();
                Log.Message("[Luxandra] Sex action counted");

                JobDriver_Sex jobDriverSex = (JobDriver_Sex)driver;
                bool isRape = jobDriverSex is JobDriver_Rape || jobDriverSex.Partner?.jobs?.curDriver is JobDriver_SexBaseRecieverRaped;
                if(isRape)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterImpureSexAction();
                    Log.Message("[Luxandra] Rape action counted");
                }
            }
        }
    }
}