using System;
using Verse;

namespace LuxandraLust
{
    public static class MenstruationIntegration
    {
        public static Action<Pawn> IncreaseOvaryPower = null;

        public static Action<Pawn> ForceOvulation = null;

        /// <summary>
        /// Induces ovulation state if possible and restores a decent
        /// amount of eggs in the ovaries
        /// </summary>
        public static void InduceOvulationAndRestoreOvaryPower(Pawn pawn)
        {
            if (pawn == null)
                return;

            if (IncreaseOvaryPower != null)
            {
                IncreaseOvaryPower.Invoke(pawn);
            }

            if (ForceOvulation != null)
            {
                ForceOvulation.Invoke(pawn);
            }
        }
    }
}