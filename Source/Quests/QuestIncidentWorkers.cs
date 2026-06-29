using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_BreakPrisonersContractQuest : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Standard safety check: Make sure the game can naturally generate a quest right now
            if (!base.CanFireNowSub(parms))
            {
                return false;
            }

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_BreakPrisonersContractQuest.defName))
            {
                return false;
            }

            // Find quest definition by name
            QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamed("Luxandra_BreakPrisonersContractQuest", errorOnFail: false);
            if (questDef == null)
            {
                return false;
            }

            // In case i dont want duplicates
            // return !Find.QuestManager.QuestsListForReading.Any(q => q.def == questDef && q.State == QuestState.Ongoing);
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // Pull your target quest script definition
            QuestScriptDef questDef = DefDatabase<QuestScriptDef>.GetNamed("Luxandra_BreakPrisonersContractQuest", errorOnFail: false);
            if (questDef == null) return false;

            // Resolve your map target (usually the player's home base colony map)
            Map map = (Map)parms.target;

            // Generate the dynamic story points scale using the map's current wealth/difficulty
            float points = StorytellerUtility.DefaultThreatPointsNow(map);

            // Package the points up cleanly into a slate object
            Slate slate = new Slate();
            slate.Set("points", points);

            // Generate and activate the quest cleanly, entirely bypassing the XML check!
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);

            // Send the notification letter so the player knows the event happened
            Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest);

            return true;
        }
    }
}