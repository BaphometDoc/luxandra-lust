using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{

    // Ugly bastard raid. They're here for your ass...
    public class IncidentWorker_BastardRaid : IncidentWorker_RaidEnemy
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // 1. Always call the base game rules first (checks population minimums, etc.)
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled("Luxandra_Inc_UnleashedBastardsRaid"))
            {
                return false;
            }

            // Unleashed Bastards must be installed & the faction must actually be in the game
            if (!LuxandraModChecks.IsUnleashedBastardsActive())
                return false;

            Faction targetFactionInstance = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == "RJW_Unleashed_BastardFaction");
            if (targetFactionInstance == null)
                return false;

            return true;
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
            Faction targetFactionInstance = Find.FactionManager.AllFactions
                .FirstOrDefault(f => f.def.defName == "RJW_Unleashed_BastardFaction");

            if (targetFactionInstance != null)
            {
                parms.faction = targetFactionInstance;
            }
            else
            {
                Log.Warning("[LuxandraLust] IncidentWorker_BastardRaid could not find active faction. Defaulting routing.");
            }

            RaidStrategyDef customStrategy = DefDatabase<RaidStrategyDef>.GetNamed("Luxandra_RapeAndPillageAssault", false);
            if (customStrategy != null)
            {
                parms.raidStrategy = customStrategy;
            }
            else
            {
                parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
            }

            return base.TryExecuteWorker(parms);
        }
    }
}