using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // Horny tribal raid. They're horny, and so is the player now
    public class IncidentWorker_HornyTribalRaid : IncidentWorker_RaidEnemy
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_HornyTribalRaid.defName))
            {
                return false;
            }

            Map map = (Map)parms.target;
            // Need at least 1 humanlike adult pawn to even try
            return map.mapPawns.FreeColonistsAndPrisonersSpawned.Any(p => p.RaceProps.Humanlike && LuxandraUtilities.IsAdult(p));
        }

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
                                         (int)f.def.techLevel < (int)TechLevel.Industrial &&
                                         !f.Hidden);

                if (fallbackFaction != null)
                {
                    parms.faction = fallbackFaction;
                    LuxandraDebugActions.DebugLogMessage("'EFR_FantasyTribeHostile' not found. Falling back to active hostile low tech faction: " + fallbackFaction.Name);
                }
                else
                {
                    // If absolutely no hostile tribals exist, let vanilla pick a random hostile faction so the raid doesn't break
                    Log.Warning("[Luxandra Debug] Could not find 'EFR_FantasyTribeHostile' or any fallback hostile low tech faction. Allowing vanilla faction assignment.");
                }
            }

            // Assign a raid strategy
            if (parms.raidStrategy == null)
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            }
            if (parms.raidArrivalMode == null)
            {
                parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
            }

            bool raidSuccessful = base.TryExecuteWorker(parms);

            if (!raidSuccessful)
            {
                LuxandraDebugActions.DebugLogMessage("Failed to generate a Horny raid for the faction " + parms.faction.Name);
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
                        var sexNeed = LuxandraUtilities.GetSexNeed(pawn);
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
                    .Where(p => p.Faction == parms.faction && !p.Dead && LuxandraUtilities.IsAdult(p))
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
                    Log.Warning("[LuxandraLust] Could not find the specified Rapist TraitDef to apply to raiders.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[LuxandraLust] Error executing rapist raid condition: {ex}");
            }

            // Return true because the raid successfully happened
            LuxandraDebugActions.DebugLogMessage("Horny raid successfully started for the faction " + parms.faction.Name);
            return true;
        }
    }
}