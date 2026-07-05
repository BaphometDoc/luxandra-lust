using HarmonyLib;
using LuxandraLust;
using System.Reflection;
using Verse;

namespace Luxandra_Menstruation_Integrations
{
    [StaticConstructorOnStartup]
    public static class MenstruationInitializer
    {
        static MenstruationInitializer()
        {
            MenstruationIntegration.IncreaseOvaryPower = (Pawn targetPawn) =>
            {
                WombManipulation.ReplenishOvaryPower(targetPawn);
            };

            MenstruationIntegration.ForceOvulation = (Pawn targetPawn) =>
            {
                WombManipulation.ForceOvulationIfPossible(targetPawn);
            };

            MenstruationIntegration.UpdateMenstruationWombGraphic = (Pawn targetPawn) =>
            {
                WombManipulation.UpdateMenstruationWombGraphic(targetPawn);
            };

            // ADD THIS: Automatically find and apply all [HarmonyPatch] classes in this namespace
            var harmony = new Harmony("luxandralust.menstruation.bridge");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}