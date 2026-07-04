using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LuxandraLust
{
    public class IncidentWorker_AmazonStealthAmbush : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_AmazonStealthAmbush.defName))
            {
                return false;
            }

            // Rule 1: Only trigger at night time (between 21:00 and 04:00)
            Map map = (Map)parms.target;
            int hour = GenLocalDate.HourOfDay(map);
            if (hour > 4 && hour < 21) return false;

            // Verify faction exists
            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(LuxandraFactionDefOf.Luxandra_AmazonTribe);
            if (amazonFaction == null) return false;


            // Need at least 1 male adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p) && p.gender == Gender.Male);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction amazonFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Luxandra_AmazonTribe"));
            PawnKindDef infiltratorKind = PawnKindDef.Named("Luxandra_Amazon_Infiltrator");

            if (amazonFaction == null || infiltratorKind == null)
            {
                Log.Warning("[Luxandra Debug] Amazon Blood Trial incident failed: Missing faction or pawn kind definition.");
                return false;
            }

            // Rule 2: Find the priority target pawn
            Pawn targetPawn = FindTargetPawn(map);
            if (targetPawn == null) return false;

            // Find a valid spawn cell near the target pawn (within a 6-cell radius)
            // x.Roofed(map) == false ensures they drop into open night areas rather than inside solid mountains or bedroom walls
            if (!CellFinder.TryFindRandomCellNear(
                targetPawn.Position,
                map,
                6,
                x => x.Walkable(map) && !x.Fogged(map) && !x.Roofed(map),
                out IntVec3 spawnCell))
            {
                // Safe vanilla fallback: if they are deep inside a fully enclosed bunker, 
                // we bypass roof checks and just force the spawn nearby.
                if (!CellFinder.TryFindRandomCellNear(
                    targetPawn.Position,
                    map,
                    6,
                    x => x.Walkable(map) && !x.Fogged(map),
                    out spawnCell))
                {
                    spawnCell = targetPawn.Position; // Absolute ultimate fallback directly on the target's tile
                }
            }

            // Calculate scaling combat counts (1/3rd of standard power)
            float unitCost = infiltratorKind.combatPower;
            int normalRaidCount = Mathf.Max(1, Mathf.RoundToInt(parms.points / unitCost));
            int stealthSquadCount = Mathf.Max(1, Mathf.RoundToInt(normalRaidCount / 3f));

            List<Pawn> assassins = new List<Pawn>();

            for (int i = 0; i < stealthSquadCount; i++)
            {
                PawnGenerationRequest request = new PawnGenerationRequest(infiltratorKind, amazonFaction);
                Pawn assassin = PawnGenerator.GeneratePawn(request);

                // Rule 3: Max out their vital needs (Food, Rest, etc.) right before they drop
                if (assassin.needs != null)
                {
                    assassin.needs.SetInitialLevels(); // Fills basic necessities
                    foreach (Need need in assassin.needs.AllNeeds)
                    {
                        need.CurLevel = need.MaxLevel; // Force absolute maximum limits
                    }
                }

                GenSpawn.Spawn(assassin, spawnCell, map);
                assassins.Add(assassin);
            }

            // Rule 2 part B: Trigger visual puff of smoke at the spawn location
            FleckMaker.ThrowSmoke(spawnCell.ToVector3Shifted(), map, 2.5f);
            FleckMaker.ThrowMicroSparks(spawnCell.ToVector3Shifted(), map);

            // Rule 4: Immediately go into attack mode targeting the base core
            LordJob_AssaultColony assaultJob = new LordJob_AssaultColony(amazonFaction, canKidnap: false, canTimeoutOrFlee: false, canSteal: false);
            LordMaker.MakeNewLord(amazonFaction, assaultJob, map, assassins);

            string targetDescription = targetPawn.gender == Gender.Male ? "prime male specimen" : "colony leader";
            string groupDescription = stealthSquadCount == 1 ? "A lone shadow assassin" : $"A squad of {stealthSquadCount} shadow assassins";

            string flavorfulLetterText =
                $"Under the cover of the darkness, a synchronized puff of smoke clears to reveal a nightmare. " +
                $"{groupDescription} from {amazonFaction.Name} has bypassed your defensive perimeters!\n\n" +
                $"They have materialized directly inside your base, hunting down your {targetDescription}: {targetPawn.NameShortColored}.\n\n" +
                $"Defend them immediately—they have already drawn their weapons.";

            // 2. Fire the native red alert box
            Find.LetterStack.ReceiveLetter(
                "Ambush: Shadow Infiltration",
                flavorfulLetterText,
                LetterDefOf.ThreatBig,
                assassins[0]
            );

            return true;
        }

        private Pawn FindTargetPawn(Map map)
        {
            var playerPawns = map.mapPawns.FreeColonistsSpawned.Where(p => !p.Downed && !p.Dead).ToList();
            if (!playerPawns.Any()) return null;

            // Priority 1: Highest Title Royal (Any Gender - Rules are Rules!)
            if (ModsConfig.RoyaltyActive)
            {
                Pawn highestRoyal = playerPawns
                    .Where(p => p.royalty != null && p.royalty.AllTitlesInEffectForReading.Any())
                    .OrderByDescending(p => p.royalty.AllTitlesInEffectForReading.Max(t => t.def.seniority))
                    .FirstOrDefault();

                if (highestRoyal != null) return highestRoyal;
            }

            // Priority 2: Ideo Leader (Any Gender - Target the crown)
            if (ModsConfig.IdeologyActive)
            {
                Pawn ideoLeader = playerPawns
                    .FirstOrDefault(p => p.Ideo != null && p.Ideo.GetRole(p) != null && p.Ideo.GetRole(p).def.leaderRole);

                if (ideoLeader != null) return ideoLeader;
            }

            // Priority 3: Fallback strictly to the highest financial market value MALE
            Pawn highestValueMale = playerPawns
                .Where(p => p.gender == Gender.Male)
                .OrderByDescending(p => p.MarketValue)
                .FirstOrDefault();

            // Ultimate absolute fallback (just in case a male was somehow lost between CanFire and execution)
            return highestValueMale ?? playerPawns.OrderByDescending(p => p.MarketValue).FirstOrDefault();
        }
    }
}