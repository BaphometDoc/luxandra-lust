using HarmonyLib;
using RimWorld;
using rjw;
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

                // Fapping won't count, sorry. Nor will touching each other. Get those dicks out already
                bool isMasturbation = props.sexType == xxx.rjwSextype.Masturbation || props.sexType == xxx.rjwSextype.MutualMasturbation;
                if (isMasturbation)
                {
                    LuxandraDebugActions.DebugLogMessage($"Masturbation detected for {actor.NameShortColored}: does not count as sex action.");
                    return;
                }

                // Will always register the sex action happening regardless of the type
                GameComponent_LuxandraLust.Instance?.RegisterSexAction();
                LuxandraDebugActions.DebugLogMessage($"Sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");

                // If necrophilia is detected I won't register the type. I don't care which hole you're using, it's moist all the same.
                bool isNecrophilia = props.partner != null && props.partner.Dead;
                if (isNecrophilia)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterNecrophiliaSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Necrophilia detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                    return;
                }

                // Mechs aren't animals, sorry.
                bool isBestialityAct = props.partner != null && props.partner.RaceProps.Humanlike == false && !props.partner.IsColonyMech;
                if (isBestialityAct)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterBestialitySexAction();
                    LuxandraDebugActions.DebugLogMessage($"Bestiality sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                    return;
                }

                // Only proper vaginal sex is pure... right... right? I mean, anal is fun too, but it's not pure. And oral is just gross.
                bool isImpureSex = props.sexType != xxx.rjwSextype.Vaginal;
                if (isImpureSex)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterImpureSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Impure sex action detected for {actor.NameShortColored} with {props.partner?.NameShortColored}");
                }

                // Shouldn't be counting animals and corpses. It's only rape if they can't actually say no.
                bool isRapeAct = props.isRape;
                if (isRapeAct)
                {
                    GameComponent_LuxandraLust.Instance?.RegisterRapeSexAction();
                    LuxandraDebugActions.DebugLogMessage($"Rape action detected involving {actor.NameShortColored} forcing themvelves on {props.partner?.NameShortColored}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Luxandra] Critical exception tracking inside SexUtility.Aftersex Postfix: {ex}");
            }
        }
    }
}