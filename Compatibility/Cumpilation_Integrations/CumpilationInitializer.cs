using LuxandraLust;
using Verse;

namespace Luxandra_Cumpilation_Integrations
{
    [StaticConstructorOnStartup]
    public static class CumpilationInitializer
    {
        static CumpilationInitializer()
        {
            CumpilationIntegration.CumOnAction = (Pawn receiver, HediffDef splashDef, BodyPartRecord bodyPart, float severity, Pawn giver) =>
            {
                CumDrenching.TryCumOnPawn(receiver, splashDef, bodyPart, severity, giver);
            };

            CumpilationIntegration.SplashCumFromNothing = (Pawn receiver, float severity) =>
            {
                CumDrenching.GenerateCumFromNothing(receiver, severity);
            };

            CumpilationIntegration.CumOnSelf = (Pawn receiver, float severity) =>
            {
                CumDrenching.MakePawnCumItself(receiver, severity);
            };

            // Automatically find and apply all [HarmonyPatch] classes in this namespace
            //var harmony = new Harmony("luxandralust.cumpilation.bridge");
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}