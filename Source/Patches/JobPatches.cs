using HarmonyLib;
using RimWorld;
using rjw; // Make sure rjw.dll is referenced!
using System;
using Verse;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(SexUtility), nameof(SexUtility.Aftersex))]
    public static class Patch_SexUtility_Aftersex
    {
        [HarmonyPostfix]
        public static void Postfix(SexProps props)
        {
            try
            {
                if (!LuxandraStorytellerCheck.IsActive())
                    return;

                // Validate that the properties packet isn't corrupted
                if (props == null || props.pawn == null)
                    return;

                Pawn actor = props.pawn;

                if (!actor.RaceProps.Humanlike)
                    return;

                bool isPlayerControlled = actor.Faction == Faction.OfPlayer ||    // Colonists & Mechs
                                          actor.IsPrisonerOfColony ||             // Prisoners
                                          actor.IsSlaveOfColony ||                // Slaves
                                          actor.IsQuestLodger();                  // Quest Guests / Refugees

                if (!isPlayerControlled)
                    return;

                // Fapping won't count, sorry. Nor will touching each other.
                bool isMasturbation = props.sexType == xxx.rjwSextype.Masturbation || props.sexType == xxx.rjwSextype.MutualMasturbation;
                if (isMasturbation)
                {
                    DebugActions_Luxandra.DebugLogMessage($"Masturbation detected for {actor.NameShortColored}: does not count as sex action.");
                    return;
                }

                GameComponent_LuxandraLust.Instance?.RegisterSexAction();
                DebugActions_Luxandra.DebugLogMessage($"Sex action detected for {actor.NameShortColored}");

                bool isImpureSex = props.sexType != xxx.rjwSextype.Vaginal;
                if (isImpureSex)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterImpureSexAction();
                    DebugActions_Luxandra.DebugLogMessage($"Impure sex action detected for {actor.NameShortColored}");
                }

                bool isRapeAct = props.isRape;

                if (isRapeAct)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterRapeSexAction();
                    DebugActions_Luxandra.DebugLogMessage($"Rape action detected involving {actor.NameShortColored}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Luxandra] Critical exception tracking inside SexUtility.Aftersex Postfix: {ex}");
            }
        }
    }
}