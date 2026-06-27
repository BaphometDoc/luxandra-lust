using LuxandraLust;
using Verse;

namespace Luxandra_Menstruation_Integrations
{
    [StaticConstructorOnStartup]
    public static class MenstruationInitializer
    {
        static MenstruationInitializer()
        {
            // We assign an anonymous function that accepts the incoming pawn
            MenstruationIntegration.IncreaseOvaryPower = (Pawn targetPawn) =>
            {
                WombManipulation.ReplenishOvaryPower(targetPawn);
            };

            MenstruationIntegration.ForceOvulation = (Pawn targetPawn) =>
            {
                WombManipulation.ForceOvulationIfPossible(targetPawn);
            };
        }
    }
}