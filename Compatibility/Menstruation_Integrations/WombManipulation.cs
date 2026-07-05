using LuxandraLust;
using RJW_Menstruation;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Luxandra_Menstruation_Integrations
{
    internal class WombManipulation
    {
        public static void ReplenishOvaryPower(Pawn pawn)
        {
            if (pawn == null)
            {
                LuxandraDebugActions.DebugLogMessage($"Attempted to increase ovary power of null pawn.");
                return;
            }

            var menstruationParts = pawn.GetMenstruationComps();

            if (!menstruationParts.Any())
            {
                LuxandraDebugActions.DebugLogMessage($"Attempted to increase ovary power of {pawn.Name}, but they don't have any vagina.");
                return;
            }

            foreach (HediffComp_Menstruation comp in menstruationParts)
            {
                var currentEggCount = comp.ovarypower;
                LuxandraDebugActions.DebugLogMessage($"Attempting to increase ovary power of {pawn.Name}'s {comp.Def.defName}. Current egg count: {currentEggCount}");

                if (currentEggCount < 500)
                {
                    comp.ovarypower += 50;
                    LuxandraDebugActions.DebugLogMessage($"{pawn.Name}'s {comp.Def.defName} egg count increased by 50.");
                    comp.RecoverOvary();
                }
            }
        }


        public static void ForceOvulationIfPossible(Pawn pawn)
        {
            if (pawn == null)
            {
                LuxandraDebugActions.DebugLogMessage($"Attempted to force ovulation in a null pawn.");
                return;
            }

            var menstruationParts = pawn.GetMenstruationComps();

            if (!menstruationParts.Any())
            {
                LuxandraDebugActions.DebugLogMessage($"Attempted to force ovulation in {pawn.Name}, but they don't have any vagina.");
                return;
            }

            foreach (HediffComp_Menstruation comp in menstruationParts)
            {
                var currentVaginaState = comp.curStage;
                LuxandraDebugActions.DebugLogMessage($"Attempting to force ovulation of {pawn.Name}'s {comp.Def.defName}.");

                if (currentVaginaState == HediffComp_Menstruation.Stage.Pregnant)
                {
                    LuxandraDebugActions.DebugLogMessage($"{pawn.Name}'s {comp.Def.defName} was in pregnant state. No change was done.");
                    return;
                }
                else if (currentVaginaState == HediffComp_Menstruation.Stage.Ovulatory)
                {
                    LuxandraDebugActions.DebugLogMessage($"{pawn.Name}'s {comp.Def.defName} was in already in ovulatory state.");
                    return;
                }
                else if (!comp.IsEggExist)
                {
                    int coinflip = Rand.RangeInclusive(1, 2);
                    if (coinflip == 2)
                        comp.eggstack++;

                    comp.GoNextStage(HediffComp_Menstruation.Stage.Ovulatory);
                    LuxandraDebugActions.DebugLogMessage($"{pawn.Name}'s {comp.Def.defName} was forced to ovulate with {coinflip} eggs.");
                }
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"{pawn.Name}'s already had eggs in her womb.");
                }
            }
        }

        public static void UpdateMenstruationWombGraphic(Pawn mother)
        {
            if (mother == null)
            {
                LuxandraDebugActions.DebugLogMessage($"Attempted to recalculate womb graphic in a null pawn.");
                return;
            }

            IEnumerable<HediffComp_Menstruation> menstruationParts = mother.GetMenstruationComps();
            foreach (HediffComp_Menstruation comp in menstruationParts)
            {
                _ = comp.Pregnancy;
                comp.TakeLoosePregnancy();
            }
        }
    }
}
