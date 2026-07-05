using System;
using Verse;

namespace LuxandraLust
{
    public static class CumpilationIntegration
    {
        public static Action<Pawn, HediffDef, BodyPartRecord, float, Pawn> CumOnAction = null;

        public static Action<Pawn, float> SplashCumFromNothing = null;

        public static Action<Pawn, float> CumOnSelf = null;

        /// <summary>
        /// Splashes the pawn's top half with cum out of nowhere
        /// </summary>
        public static void DrenchInCumFromNothing(Pawn receiver, float severity)
        {
            if (receiver == null)
                return;

            if (SplashCumFromNothing != null)
                SplashCumFromNothing.Invoke(receiver, severity);
        }

        /// <summary>
        /// Causes the pawn to wet themselves
        /// </summary>
        public static void CauseSelfCum(Pawn receiver, float severity)
        {
            if (receiver == null)
                return;

            if (CumOnSelf != null)
                CumOnSelf.Invoke(receiver, severity);
        }
    }
}