using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class QuestPart_BreakPrisoners : QuestPart
    {
        public string inSignal;
        public string outSignalSuccess;
        public string outSignalPerfectSuccess;
        public string outSignalFail;

        // The script passes the tracking prisoners into this list when the quest starts
        public List<Pawn> prisoners = new List<Pawn>();

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            LuxandraDebugActions.DebugLogMessage($"Quest signal received: {signal}");
            // When the countdown ends and the shuttle arrives to fetch them
            if (signal.tag == inSignal)
            {
                EvaluatePrisonerCompliance();
            }
        }

        private void EvaluatePrisonerCompliance()
        {
            if (prisoners.NullOrEmpty())
            {
                Find.SignalManager.SendSignal(new Signal(outSignalFail));
                return;
            }

            LuxandraDebugActions.DebugLogMessage($"Prisoner evaluation commencing.");
            int totalPrisoners = prisoners.Count;
            int standardBrokenCount = 0;
            int fullyBrokenCount = 0;

            foreach (Pawn p in prisoners)
            {
                // Look up the exact RJW defName for the broken state
                var brokenHediff = p.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def.defName == "FeelingBroken");

                if (brokenHediff != null)
                {
                    float severity = brokenHediff.Severity;

                    // 0.5+ maps directly to the "Extremely broken" stage
                    if (severity >= 0.5f)
                    {
                        fullyBrokenCount++;
                        standardBrokenCount++; // Counts toward baseline completion as well
                    }
                    // 0.3+ maps directly to the "broken" stage
                    else if (severity >= 0.3f)
                    {
                        standardBrokenCount++;
                    }
                }
            }

            // --- EVALUATE OUTCOME TIERS ---

            // Tier 1: PERFECT SUCCESS (Every single prisoner is fully broken at 0.5+)
            if (fullyBrokenCount == totalPrisoners)
            {
                LuxandraDebugActions.DebugLogMessage($"Quest complete: All {fullyBrokenCount}/{totalPrisoners} prisoners perfectly broken.");
                Find.SignalManager.SendSignal(new Signal(outSignalPerfectSuccess));
            }
            // Tier 2: STANDARD SUCCESS (Every single survivor met the baseline 0.3+ severity check)
            else if (standardBrokenCount == totalPrisoners)
            {
                LuxandraDebugActions.DebugLogMessage($"Quest complete: All {standardBrokenCount}/{totalPrisoners} prisoners met the baseline requirements.");
                Find.SignalManager.SendSignal(new Signal(outSignalSuccess));
            }
            // Tier 3: FAILURE (One or more failed to hit the severity floor, or some died)
            else
            {
                LuxandraDebugActions.DebugLogMessage($"Quest failed: Only {standardBrokenCount}/{totalPrisoners} met the threshold.");
                Find.SignalManager.SendSignal(new Signal(outSignalFail));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Values.Look(ref outSignalSuccess, "outSignalSuccess");
            Scribe_Values.Look(ref outSignalPerfectSuccess, "outSignalPerfectSuccess");
            Scribe_Values.Look(ref outSignalFail, "outSignalFail");
            Scribe_Collections.Look(ref prisoners, "prisoners", LookMode.Reference);
        }
    }

    public class QuestNode_BreakPrisonersContract : QuestNode
    {
        public SlateRef<string> inSignal;
        public SlateRef<string> outSignalSuccess;
        public SlateRef<string> outSignalPerfectSuccess;
        public SlateRef<string> outSignalFail;
        public SlateRef<string> prisonersPawnList;

        protected override bool TestRunInt(Slate slate)
        {
            // fallback points value if dev mode hasn't initialized it yet
            if (!slate.Exists("points"))
            {
                slate.Set("points", 0f);
            }
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;

            // Instantiating our custom quest evaluation part safely
            var questPart = new QuestPart_BreakPrisoners();

            // Resolve strings securely using the active slate context
            string inSig = inSignal.GetValue(slate) ?? "ShuttleTimerExpired";
            string successSig = outSignalSuccess.GetValue(slate) ?? "QuestSuccessBaseline";
            string perfectSig = outSignalPerfectSuccess.GetValue(slate) ?? "QuestSuccessPerfect";
            string failSig = outSignalFail.GetValue(slate) ?? "QuestFailedCompliance";
            string listVarName = prisonersPawnList.GetValue(slate) ?? "lodgers";

            questPart.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSig);
            questPart.outSignalSuccess = QuestGenUtility.HardcodedSignalWithQuestID(successSig);
            questPart.outSignalPerfectSuccess = QuestGenUtility.HardcodedSignalWithQuestID(perfectSig);
            questPart.outSignalFail = QuestGenUtility.HardcodedSignalWithQuestID(failSig);

            // Retrieve the list safely without risking a null crash
            if (slate.Exists(listVarName))
            {
                var pawns = slate.Get<List<Pawn>>(listVarName);
                if (pawns != null)
                {
                    questPart.prisoners.AddRange(pawns);
                }
            }

            // Hand the part off to the active quest instance
            QuestGen.quest.AddPart(questPart);
        }
    }
}