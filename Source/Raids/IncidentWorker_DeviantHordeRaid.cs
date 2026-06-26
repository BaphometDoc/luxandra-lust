using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace LuxandraLust
{
    // These totally arent basically RJW nymphs
    // No, really, this used to be a Nymph horde, but RJW just fucks (he he) anything that has "nymph" in their name so... deviants it is.
    public class IncidentWorker_DeviantHordeRaid : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            LuxandraDebugActions.DebugLogMessage("Attempting to generate a Carnal Deviant raid.");
            Faction deviantFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Luxandra_DeviantHordeFaction", false));

            // Failsafe 1: Fallback to a hostile tribal faction
            if (deviantFaction == null)
            {
                Log.Warning("[LuxandraLust] Carnal Deviantfaction not found, searching for hostile tribal fallback.");

                deviantFaction = Find.FactionManager.AllFactionsListForReading
                    .FirstOrDefault(f => !f.IsPlayer && f.HostileTo(Faction.OfPlayer) && f.def.techLevel == TechLevel.Neolithic);
            }

            //  Failsafe 2: Abort if absolutely no hostile tribal faction exists
            if (deviantFaction == null)
            {
                Log.Error("[LuxandraLust] No Carnal Deviant faction OR hostile tribal faction found. Aborting Carnal Deviant raid.");
                return false;
            }

            parms.faction = deviantFaction;

            if (parms.points <= 0f)
            {
                parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
            }

            PawnKindDef deviantKind = DefDatabase<PawnKindDef>.GetNamed("Luxandra_CarnalDeviantStriker", false);
            if (deviantKind == null)
            {
                Log.Error("[LuxandraLust] Missing Luxandra_CarnalDeviantStriker PawnKindDef!");
                return false;
            }

            List<Pawn> list = new List<Pawn>();
            float currentPoints = 0f;

            // Naked pawns are cheap (combatPower 30), so this will scale up a massive horde naturally
            // Save your anus...
            while (currentPoints < parms.points)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    deviantKind,
                    deviantFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true,
                    1f
                )
                {
                    // Force the age range here
                    BiologicalAgeRange = new FloatRange(19f, 30f) // Keep them synced
                };

                Pawn pawn = PawnGenerator.GeneratePawn(request);
                list.Add(pawn);
                currentPoints += deviantKind.combatPower;

                // Safety anchor to prevent infinite loops if something breaks
                if (list.Count > 100) break;
            }

            if (list.Count == 0) return false;

            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 loc, map, CellFinder.EdgeRoadChance_Hostile))
            {
                return false;
            }

            // The hediff Luxandra_RapistRage is applied by the raid AI now
            HediffDef clawHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_EnthrallingTouch", false);
            if (clawHediff == null)
            {
                Log.Error("[LuxandraLust] Missing Luxandra_EnthrallingTouch HediffDef!");
                return true;
            }

            // Spawn them and send them on rapist frenzy
            for (int i = 0; i < list.Count; i++)
            {
                Pawn deviant = list[i];
                IntVec3 loc2 = CellFinder.RandomClosewalkCellNear(loc, map, 5);
                GenSpawn.Spawn(deviant, loc2, map, WipeMode.Vanish);

                // Nuke their gear in case they spawn with some
                // (As I learned, the game can in fact give them gear if the colony wealth is high enough)
                List<Apparel> apparelList = deviant.apparel.WornApparel;
                for (int j = apparelList.Count - 1; j >= 0; j--)
                {
                    deviant.apparel.Remove(apparelList[j]);
                }
                deviant.equipment?.DestroyAllEquipment();

                // Apply the as the anesthetic touch
                if (clawHediff != null && deviant.health != null)
                {
                    // Search for either Left or Right hand explicitly
                    IEnumerable<BodyPartRecord> strikeParts = deviant.RaceProps.body.AllParts.Where(p => p.def.defName.ToLower().Contains("hand"));

                    // Fallback to arms if missing hands
                    if (!strikeParts.Any())
                        strikeParts = deviant.RaceProps.body.AllParts.Where(p => p.def == BodyPartDefOf.Arm);

                    // Fallback to torso if all else fails
                    if (!strikeParts.Any())
                        strikeParts.AddItem(deviant.RaceProps.body.corePart);

                    if (strikeParts.Any())
                    {
                        foreach (var strikePart in strikeParts)
                        {
                            Hediff enthrallingTouch = HediffMaker.MakeHediff(clawHediff, deviant, strikePart);
                            deviant.health.AddHediff(enthrallingTouch);
                        }
                    }
                }
            }

            // Calling it "massive crowd" when it's like 3 of them seems pretty dumb
            string crowdDefinition = list.Count > 15 ? "massive crowd" : list.Count < 5 ? "handful" : "group";

            Find.LetterStack.ReceiveLetter(
                "Raid (Deviant Horde)",
                $"A {crowdDefinition} of completely naked primal humans are entering the area from the map edge! Driven by a carnal frenzy, they are advancing directly onto your colony. Run, or be ready for what is to come...",
                LetterDefOf.ThreatBig,
                list[0]);

            LordMaker.MakeNewLord(deviantFaction, new LordJob_RapePillageAssault(deviantFaction, true, true), map, list);

            return true;
        }
    }
}