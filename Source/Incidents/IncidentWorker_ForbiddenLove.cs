using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_ForbiddenLove : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_ForbiddenLove.defName))
                return false;

            Map map = (Map)parms.target;
            if (map == null || map.mapPawns.FreeColonistsSpawnedCount < 2) return false;

            // Check Ideology: Event should only fire if the ideology hates incest.
            if (ModsConfig.IdeologyActive)
            {
                Ideo playerIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;
                if (playerIdeo != null)
                {
                    // Scan for incest precepts
                    bool allowsIncest = playerIdeo.PreceptsListForReading.Any(p =>
                        p.def.defName == "Incestuos_IncestOnly" || p.def.defName == "Incestuos_Free");

                    if (allowsIncest) return false;
                }
            }
            // ... or if ideology / sexperience-ideology isn't active as then they hate incest by default

            // Ensure there is at least one pair of spawned, alive, close blood relatives
            return FindValidIncestuousPair(map, out _, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            if (!FindValidIncestuousPair(map, out Pawn malePawn, out Pawn femalePawn))
            {
                LuxandraDebugActions.DebugLogMessage("Attempted to generate a forbidden incest but couldn't find any valid couple");
                return false;
            }

            string relationName = malePawn.GetRelations(femalePawn).FirstOrDefault()?.defName ?? "relative";

            // Leaving this here in case i decide to remove other lovers - but I think it's funny if it's a threesome or more
            // Ruin those couples bois
            //malePawn.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, malePawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover));
            //femalePawn.relations.RemoveDirectRelation(PawnRelationDefOf.Lover, femalePawn.relations.GetFirstDirectRelationPawn(PawnRelationDefOf.Lover));

            LuxandraDebugActions.DebugLogMessage($"Valid couple found: {malePawn.NameShortColored} + {femalePawn.NameShortColored} ({relationName})");
            malePawn.relations.AddDirectRelation(PawnRelationDefOf.Lover, femalePawn);

            // Biotech integration
            bool isPregnant = false;
            // Ensure she isn't already pregnant and both are biologically capable
            // This may be porn but we have standards
            if (!LuxandraUtilities.IsPregnant(femalePawn) && LuxandraUtilities.IsAdult(femalePawn) && LuxandraUtilities.IsAdult(malePawn))
            {
                LuxandraDebugActions.DebugLogMessage("Pawn not pregnant, creating hediff...");
                if (RJWPregnancySettings.UseVanillaPregnancy)
                {
                    LuxandraDebugActions.DebugLogMessage("Attempting to create Biotech pregnancy.");
                    // Create the pregnancy
                    Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, femalePawn, null);
                    GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(malePawn, femalePawn, out _);
                    hediff_Pregnant.SetParents(femalePawn, malePawn, inheritedGeneSet);
                    if (hediff_Pregnant != null)
                    {
                        hediff_Pregnant.Severity = 0.05f;
                    }

                    isPregnant = true;
                    LuxandraDebugActions.DebugLogMessage("Biotech pregnancy created successfully.");
                }
                else if (PregnancyHelper.isFertile(femalePawn) && PregnancyHelper.isFertile(malePawn))
                {
                    LuxandraDebugActions.DebugLogMessage("Attempting to create RJW pregnancy.");
                    PregnancyHelper.AddPregnancyHediff(femalePawn, malePawn);

                    if (LuxandraUtilities.IsPregnant(femalePawn))
                    {
                        isPregnant = true;
                        LuxandraDebugActions.DebugLogMessage("RJW pregnancy created successfully.");
                        // TODO: proper menstruation integration
                    }
                }
            }

            string letterText = $"A scandalous secret has unraveled. It turns out {malePawn.NameShortColored} and his {relationName}, {femalePawn.NameShortColored}, have been harboring a forbidden romantic obsession with one another.\n\n";

            if (isPregnant)
            {
                letterText += $"But that's not all! Rumors are flying because {femalePawn.NameShortColored} is already visibly pregnant from their secret rendezvous!";
            }

            Find.LetterStack.ReceiveLetter("Forbidden Romance!", letterText, LetterDefOf.NeutralEvent, new LookTargets(new List<Pawn> { malePawn, femalePawn }));

            return true;
        }

        // Helper method to find close blood relatives
        private bool FindValidIncestuousPair(Map map, out Pawn male, out Pawn female)
        {
            male = null;
            female = null;

            var candidates = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.RaceProps.Humanlike && !p.Dead && LuxandraUtilities.IsAdult(p)).ToList();

            foreach (var pA in candidates.InRandomOrder())
            {
                foreach (var pB in candidates)
                {
                    if (pA == pB) continue;

                    // Enforce heterosexual pairing, it's funnier when you add the pregnancy
                    if (pA.gender == Gender.Male && pB.gender == Gender.Female)
                    {
                        // Check blood relation using vanilla's deep tree helper
                        if (pA.relations.FamilyByBlood.Contains(pB))
                        {
                            // Ensure they aren't already lovers
                            if (!pA.relations.DirectRelationExists(PawnRelationDefOf.Lover, pB))
                            {
                                male = pA;
                                female = pB;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}