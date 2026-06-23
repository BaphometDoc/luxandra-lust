using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace LuxandraLust
{
    // Horny tribal raid. They're horny, and so is the player now
    public class IncidentWorker_HornyTribalRaid : IncidentWorker_RaidEnemy
    {
        #region Overrides for the event letter
        protected override string GetLetterLabel(IncidentParms parms)
        {
            if (!this.def.letterLabel.NullOrEmpty())
            {
                return this.def.letterLabel.Formatted(parms.faction.NameColored).Resolve();
            }
            return base.GetLetterLabel(parms);
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> outPawns)
        {
            if (!this.def.letterText.NullOrEmpty())
            {
                return this.def.letterText.Formatted(parms.faction.NameColored).Resolve();
            }
            return base.GetLetterText(parms, outPawns);
        }

        protected override LetterDef GetLetterDef()
        {
            if (this.def.letterDef != null)
            {
                return this.def.letterDef;
            }
            return base.GetLetterDef();
        }
        #endregion

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            // If Fantasy Race is loaded and there is the hostile faction, use that faction for the raid. Otherwise, default to a tribal raid.
            Faction targetFactionInstance = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == "EFR_FantasyTribeHostile");

            if (targetFactionInstance != null)
            {
                parms.faction = targetFactionInstance;
            }
            else
            {
                Faction fallbackFaction = Find.FactionManager.AllFactions
                    .FirstOrDefault(f => f.HostileTo(Faction.OfPlayer) &&
                                         (int)f.def.techLevel < (int)TechLevel.Neolithic &&
                                         !f.Hidden);

                if (fallbackFaction != null)
                {
                    parms.faction = fallbackFaction;
                    DebugActions_Luxandra.DebugLogMessage("'EFR_FantasyTribeHostile' not found. Falling back to active hostile low tech faction: " + fallbackFaction.Name);
                }
                else
                {
                    // If absolutely no hostile tribals exist, let vanilla pick a random hostile faction so the raid doesn't break
                    Log.Warning("[Luxandra Debug] Could not find 'EFR_FantasyTribeHostile' or any fallback hostile low tech faction. Allowing vanilla faction assignment.");
                }
            }

            bool raidSuccessful = base.TryExecuteWorker(parms);

            if (!raidSuccessful)
            {
                return false;
            }

            try
            {
                Map map = (Map)parms.target;

                // All player controlled pawns become horny
                List<Pawn> playerControlledPawns = map.mapPawns.FreeColonistsSpawned
                    .Concat(map.mapPawns.SlavesOfColonySpawned)
                    .ToList();

                ThoughtDef customMoodlet = ThoughtDef.Named("Luxandra_WarCry_Panic");

                foreach (Pawn pawn in playerControlledPawns)
                {
                    if (pawn == null || pawn.Dead) continue;

                    if (pawn.needs != null)
                    {
                        var sexNeed = pawn.needs.TryGetNeed<rjw.Need_Sex>();
                        if (sexNeed != null)
                        {
                            sexNeed.CurLevel = 0f;
                        }
                    }

                    if (customMoodlet != null && pawn.needs?.mood?.thoughts?.memories != null)
                    {
                        pawn.needs.mood.thoughts.memories.TryGainMemory(customMoodlet);
                    }
                }

                // All raiders become rapists
                List<Pawn> incomingRaiders = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.Faction == parms.faction && !p.Dead && LuxandraLustUtilities.IsAdult(p))
                    .ToList();
                TraitDef targetTrait = TraitDef.Named("Rapist");

                if (targetTrait != null)
                {
                    foreach (Pawn raider in incomingRaiders)
                    {
                        if (raider.story?.traits != null && !raider.story.traits.HasTrait(targetTrait))
                        {
                            Trait newTrait = new Trait(targetTrait, 0, forced: true);
                            raider.story.traits.GainTrait(newTrait);
                        }
                    }
                }
                else
                {
                    Log.Warning("[LuxandraLust] Could not find the specified TraitDef to apply to raiders.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[LuxandraLust] Error executing custom raid condition: {ex}");
            }

            // Return true because the raid successfully happened
            return true;
        }
    }

    // Ugly bastard raid. They're here for your ass...
    public class IncidentWorker_BastardRaid : IncidentWorker_RaidEnemy
    {
        #region Overrides for the event letter
        protected override string GetLetterLabel(IncidentParms parms)
        {
            if (!this.def.letterLabel.NullOrEmpty())
            {
                return this.def.letterLabel.Formatted(parms.faction.NameColored).Resolve();
            }
            return base.GetLetterLabel(parms);
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> outPawns)
        {
            if (!this.def.letterText.NullOrEmpty())
            {
                return this.def.letterText.Formatted(parms.faction.NameColored).Resolve();
            }
            return base.GetLetterText(parms, outPawns);
        }

        protected override LetterDef GetLetterDef()
        {
            if (this.def.letterDef != null)
            {
                return this.def.letterDef;
            }
            return base.GetLetterDef();
        }
        #endregion

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction targetFactionInstance = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == "RJW_Unleashed_BastardFaction");

            if (targetFactionInstance != null)
            {
                parms.faction = targetFactionInstance;
            }
            else
            {
                Log.Warning("[LuxandraLust] IncidentWorker_BastardRaid could not find an active faction with defName 'RJW_Unleashed_BastardFaction' in this save file. Defaulting to standard enemy faction routing.");
            }

            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;

            bool raidSuccessful = base.TryExecuteWorker(parms);
            if (!raidSuccessful) return false;

            try
            {
                Map map = (Map)parms.target;
                if (map == null) return true;

                // Grab the newly spawned raiders
                List<Pawn> bastards = map.mapPawns.AllPawnsSpawned
                    .Where(p => p.Faction == parms.faction && !p.Dead && p.RaceProps.Humanlike)
                    .ToList();

                // Set their starting duty directly to assault colony so they go knock people out
                foreach (Pawn bastard in bastards)
                {
                    if (bastard.mindState != null)
                    {
                        bastard.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    }
                }

                // Spawn our global invisible map watcher to coordinate the kidnapping lines
                UnityEngine.GameObject mapWatcher = new UnityEngine.GameObject("BastardRaidWatcher");
                var script = mapWatcher.AddComponent<BastardRaidMapWatcher>();
                script.Initialize(map, parms.faction);
            }
            catch (Exception ex)
            {
                Log.Error($"[LuxandraLust] Error initializing Bastard Raid behaviors: {ex}");
            }

            return true;
        }
    }

    // A small runtime engine that watches the map for downed pawns and forces kidnapping. Save your poopers...
    public class BastardRaidMapWatcher : UnityEngine.MonoBehaviour
    {
        private Map map;
        private Faction raidFaction;
        private int tickInterval = 60; // Check the battlefield once every second (60 ticks)

        public void Initialize(Map targetMap, Faction faction)
        {
            this.map = targetMap;
            this.raidFaction = faction;
        }

        void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing || map == null || raidFaction == null)
            {
                UnityEngine.Object.Destroy(this.gameObject);
                return;
            }

            if (Find.TickManager.TicksGame % tickInterval == 0)
            {
                ManageKidnappingLogic();
            }
        }

        private void ManageKidnappingLogic()
        {
            List<Pawn> activeRaiders = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction == raidFaction && !p.Dead && p.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                .ToList();

            // If all raiders are dead, incapacitated, or left the map, kill the watcher script
            if (!activeRaiders.Any())
            {
                UnityEngine.Object.Destroy(this.gameObject);
                return;
            }

            // Find any downed player-controlled pawns left exposed on the floor and kidnap them
            List<Pawn> downedTargets = map.mapPawns.FreeAdultColonistsSpawned
                .Where(p => p.Downed && !p.Dead && !IsBeingKidnapped(p))
                .ToList();

            foreach (Pawn raider in activeRaiders)
            {
                if (raider.mindState.duty?.def == DutyDefOf.Kidnap)
                {
                    continue;
                }

                if (downedTargets.Any())
                {
                    // Snatch an available downed target and switch duty immediately
                    Pawn target = downedTargets.First();
                    downedTargets.Remove(target);

                    raider.mindState.duty = new PawnDuty(DutyDefOf.Kidnap);
                    raider.jobs?.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
                else
                {
                    // No downed targets available? Keep hunting and downing active colonists!
                    if (raider.mindState.duty?.def != DutyDefOf.AssaultColony)
                    {
                        raider.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    }
                }
            }
        }

        private bool IsBeingKidnapped(Pawn pawn)
        {
            return map.mapPawns.AllPawnsSpawned
                .Any(p => p.Faction == raidFaction && p.CurJob != null && p.CurJob.targetA.Thing == pawn && p.CurJob.def == JobDefOf.Kidnap);
        }
    }
}