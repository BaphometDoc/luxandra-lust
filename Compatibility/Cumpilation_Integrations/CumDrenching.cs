using Cumpilation;
using Cumpilation.Bukkake;
using LuxandraLust;
using RimWorld;
using rjw.Modules.Interactions;
using System.Linq;
using Verse;

namespace Luxandra_Cumpilation_Integrations
{
    internal class CumDrenching
    {
        public static void TryCumOnPawn(Pawn receiver, HediffDef splashDef, BodyPartRecord bodyPart, float severity, Pawn giver)
        {
            LuxandraDebugActions.DebugLogMessage($"Attempting to splash cum on {receiver.Name}.");
            if (!Settings.EnableBukkake) return;
            if (!BukkakeUtility.CanBeCovered(receiver)) return;
            LuxandraDebugActions.DebugLogMessage($"Bukkake is enabled and pawn can be cummed on, proceeding...");

            BukkakeUtility.CumOn(receiver, splashDef, bodyPart, severity, giver);
        }

        public static void GenerateCumFromNothing(Pawn receiver, float severity)
        {
            LuxandraDebugActions.DebugLogMessage($"Attempting to generate cum on {receiver.Name}.");
            if (!Settings.EnableBukkake) return;
            if (!BukkakeUtility.CanBeCovered(receiver)) return;
            LuxandraDebugActions.DebugLogMessage($"Bukkake is enabled and pawn can be cummed on, proceeding...");

            var splashDef = DefDatabase<HediffDef>.GetNamed("Cumpilation_Hediff_Cum", false);

            var validBodyParts = receiver.health.hediffSet.GetNotMissingParts().Where(h => h.def == BodyPartDefOf.Torso || h.def == BodyPartDefOf.Arm ||
                                                                                           h.def.defName == "Breasts" || h.def == BodyPartDefOf.Head);

            if (validBodyParts.Any())
            {
                var bodyPart = validBodyParts.RandomElement();
                BukkakeUtility.CumOn(receiver, splashDef, bodyPart, severity, null);
            }
        }

        public static void MakePawnCumItself(Pawn pawn, float severity)
        {
            LuxandraDebugActions.DebugLogMessage($"Attempting to make {pawn.Name} cum on themselves.");
            if (pawn == null) return;

            if (!Settings.EnableBukkake) return;
            LuxandraDebugActions.DebugLogMessage($"Bukkake is enabled and pawn can be cummed on, proceeding...");

            BodyPartRecord bodyPart = null;

            if (pawn.GetPenises().Any())
                bodyPart = pawn.GetPenises().FirstOrDefault().BodyPart;
            else if (pawn.GetVaginas().Any())
                bodyPart = pawn.GetVaginas().FirstOrDefault().BodyPart;
            else if (pawn.GetBreasts().Any())
                bodyPart = pawn.GetBreasts().FirstOrDefault().BodyPart;

            if (bodyPart == null)
            {
                LuxandraDebugActions.DebugLogMessage($"No valid body part found.");
                return;
            }
            LuxandraDebugActions.DebugLogMessage($"Using {bodyPart.def} to cause the self cum.");

            var splashDef = DefDatabase<HediffDef>.GetNamed("Cumpilation_Hediff_Cum", false);

            BukkakeUtility.CumOn(pawn, splashDef, bodyPart, severity, pawn);
        }
    }
}
