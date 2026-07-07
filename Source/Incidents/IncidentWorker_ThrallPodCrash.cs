using RimWorld;
using System.Linq;
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

            // The ones from transport pods are never virgin, they got thrown out after being used
            if (LuxandraModChecks.IsSexperienceActive())
            {
                TraitDef virginTraitDef = DefDatabase<TraitDef>.GetNamedSilentFail("Virgin");
                if (virginTraitDef != null)
                {
                    // Remove it
                    if (generatedPawn.story.traits.HasTrait(virginTraitDef))
                    {
                        generatedPawn.story.traits.RemoveTrait(generatedPawn.story.traits.GetTrait(virginTraitDef));
                    }
                }
            }

            // They'll also come down severely broken due to the abuse
            // (if RJW Genes is active, this would be removed by the Unbreakable gene, so no add some bruises instead)
            if (LuxandraModChecks.IsRJWGenesActive())
            {
                HediffDef bruiseDef = HediffDef.Named("Bruise");
                int bruiseCount = Rand.RangeInclusive(2, 5);

                if (bruiseDef != null)
                {
                    for (int i = 0; i < bruiseCount; i++)
                    {
                        try
                        {
                            // Select a random external body part
                            if (generatedPawn.health.hediffSet.GetNotMissingParts()
                                .Where(p => p.depth == BodyPartDepth.Outside)
                                .TryRandomElement(out BodyPartRecord randomPart))
                            {
                                // Make the bruise injury and specify a realistic severity
                                Hediff_Injury bruise = (Hediff_Injury)HediffMaker.MakeHediff(bruiseDef, generatedPawn, randomPart);
                                bruise.Severity = Rand.Range(3f, 10f);

                                generatedPawn.health.AddHediff(bruise, randomPart, null);
                            }
                        }
                        catch
                        {
                            LuxandraDebugActions.DebugLogMessage($"Failed to generate superficial bruises on {generatedPawn.LabelShort}.");
                        }
                    }
                    LuxandraDebugActions.DebugLogMessage($"Generated {bruiseCount} superficial bruises on {generatedPawn.LabelShort}.");
                }
            }
            else
            {
                HediffDef brokenDef = DefDatabase<HediffDef>.GetNamedSilentFail("FeelingBroken");
                if (brokenDef != null)
                {
                    // Pass null for body part to apply it to the whole body/mind framework
                    Hediff brokenHediff = HediffMaker.MakeHediff(brokenDef, generatedPawn, null);
                    brokenHediff.Severity = 0.55f;

                    generatedPawn.health.AddHediff(brokenHediff, null, null);
                    Log.Message($"[Luxandra Debug] Applied FeelingBroken at severity 0.55 to {generatedPawn.LabelShort}.");
                }
            }

            ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
            activeTransporterInfo.innerContainer.TryAdd(generatedPawn);
            DropPodUtility.MakeDropPodAt(dropSpot, map, activeTransporterInfo);

            string letterLabel = "Thrall Pod Crash";

            string letterText = "An orbital cargo transport seems to have jettisoned some excess baggage. A sedated, naked thrall has crashed on the surface in a survival pod.\n\nThey shown visible signs of abuse, but since they appear to be just a discarded slave, you can probably do with them what you please without any diplomatic consequence.";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, new LookTargets(dropSpot, map));

            return true;
        }
    }
}