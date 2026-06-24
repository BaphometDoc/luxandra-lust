using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

// Did you know Rimworld rituals are a fucking mess? No? Congratulations, now you do.

namespace LuxandraLust
{
    // Postfix that adds the new Gizmo. 
    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.GetGizmos))]
    public static class Patch_RitualTargetGizmos
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, ThingWithComps __instance)
        {
            foreach (Gizmo gizmo in __result) yield return gizmo;

            // Requirements: Storyteller active AND Player has Ideology
            if (!LuxandraStorytellerCheck.IsActive() || !ModsConfig.IdeologyActive)
                yield break;

            // Building Check (Ritual spot + altar are valid targets)
            if (__instance.def.defName == "RitualSpot" || (__instance.def.category == ThingCategory.Building && __instance.def.isAltar))
            {
                Map map = __instance.Map;

                Ideo primaryIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;

                Precept_Role leaderPrecept = primaryIdeo.PreceptsListForReading
                    .OfType<Precept_Role>()
                    .FirstOrDefault(p => p.def == PreceptDefOf.IdeoRole_Leader);

                Pawn leaderPawn = leaderPrecept?.ChosenPawns()?.FirstOrDefault();

                if (leaderPawn != null)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Commune with Luxandra",
                        defaultDesc = "Gather the colony to beseech Luxandra's blessing.",
                        icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BeseechLuxandra"),
                        action = delegate
                        {
                            StartLuxandraRitual(__instance, leaderPawn);
                        }
                    };
                }
            }
        }

        private static void StartLuxandraRitual(ThingWithComps target, Pawn leader)
        {
            // All this mess is preparing the "fake" precept that makes the ritual work.
            // It's a huge headache. Would not recommend.
            PreceptDef def = DefDatabase<PreceptDef>.GetNamed("Luxandra_CommunePrecept");
            Precept_Ritual ritualInstance = (Precept_Ritual)PreceptMaker.MakePrecept(def);

            if (Faction.OfPlayer?.ideos?.PrimaryIdeo != null)
            {
                ritualInstance.ideo = Faction.OfPlayer.ideos.PrimaryIdeo;
            }

            // Kind of a bad way to set the name but it's the only way I found
            HarmonyLib.AccessTools.Field(typeof(Precept), "name").SetValue(ritualInstance, def.label);
            ritualInstance.behavior = DefDatabase<RitualBehaviorDef>.GetNamed("LeaderSpeech").GetInstance();
            ritualInstance.outcomeEffect = DefDatabase<RitualOutcomeEffectDef>.GetNamed("AttendedFuneralNoCorpse").GetInstance();

            // Create the TargetInfo
            TargetInfo targetInfo = new TargetInfo(target.Position, target.Map);

            // This is the actual ritual action
            Dialog_BeginRitual.ActionCallback onStartClicked = new Dialog_BeginRitual.ActionCallback((assignments) =>
            {
                LordJob_Joinable_Speech lordJob = new LordJob_Joinable_Speech(
                    targetInfo,
                    leader,
                    ritualInstance,
                    DefDatabase<RitualBehaviorDef>.GetNamed("LeaderSpeech").stages,
                    assignments, // Hand off the custom setup from the UI
                    false
                );

                List<Pawn> chosenParticipants = assignments.Participants;

                // Clear current tasks so they march immediately
                foreach (var p in chosenParticipants)
                {
                    p.GetLord()?.Notify_PawnLost(p, PawnLostCondition.ForcedToJoinOtherLord);
                    p.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }

                LordMaker.MakeNewLord(leader.Faction, lordJob, target.Map, chosenParticipants);
                Messages.Message("The commune has begun.", MessageTypeDefOf.PositiveEvent);

                return true;
            });

            // Ritual window
            Dialog_BeginRitual dialog = new Dialog_BeginRitual(
                "Commune with Luxandra",
                ritualInstance,
                targetInfo,
                target.Map,
                onStartClicked,
                leader,
                null
            );

            Find.WindowStack.Add(dialog);
        }
    }
}