using RimWorld;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_ThrallPodCrash : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map ?? Find.CurrentMap;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_ThrallPodCrash.defName))
            {
                return false;
            }

            return map != null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            Gender randomGender = (Rand.Value > 0.5f) ? Gender.Male : Gender.Female;

            string targetDefName = (randomGender == Gender.Male)
                ? "Luxandra_Submissive_Male"
                : "Luxandra_Submissive_Female";

            PawnKindDef submissiveKind = DefDatabase<PawnKindDef>.GetNamed(targetDefName, false);
            if (submissiveKind == null)
            {
                Log.Error($"[Luxandra] Incident failed: {targetDefName} could not be found.");
                return false;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(
                submissiveKind,
                faction: null,
                context: PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true,
                fixedGender: randomGender,
                allowAddictions: false,
                canGeneratePawnRelations: false
            );

            Pawn generatedPawn = PawnGenerator.GeneratePawn(request);
            generatedPawn.apparel?.DestroyAll();

            HediffDef anestheticDef = DefDatabase<HediffDef>.GetNamedSilentFail("Anesthetic");
            if (anestheticDef != null)
            {
                generatedPawn.health.AddHediff(anestheticDef, null, null);
            }

            if (ModsConfig.AnomalyActive)
            {
                HediffDef blissLobotomyDef = DefDatabase<HediffDef>.GetNamedSilentFail("BlissLobotomy");
                if (blissLobotomyDef != null)
                {
                    BodyPartRecord brain = generatedPawn.health.hediffSet.GetBrain();
                    if (brain != null)
                    {
                        generatedPawn.health.AddHediff(blissLobotomyDef, brain, null);
                    }
                }
            }

            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 dropSpot, false, false, false))
            {
                dropSpot = DropCellFinder.RandomDropSpot(map);
            }


            ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
            activeTransporterInfo.innerContainer.TryAdd(generatedPawn);
            DropPodUtility.MakeDropPodAt(dropSpot, map, activeTransporterInfo);

            string letterLabel = "Thrall Pod Crash";

            string letterText = "An orbital cargo transport seems to have jettisoned some excess baggage. A sedated, naked thrall has crashed on the surface in a survival pod.\n\nSince they hold no faction allegiance, you can capture or rescue them as you see fit.";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, new LookTargets(dropSpot, map));

            return true;
        }
    }
}