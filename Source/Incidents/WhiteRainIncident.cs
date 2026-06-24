using RimWorld;
using rjw;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_WhiteRain : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Don't fire if the weather or condition is already active on this map
            if (map.gameConditionManager.ConditionIsActive(GameConditionDef.Named("Luxandra_WhiteRain")))
            {
                return false;
            }

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            GameConditionDef conditionDef = GameConditionDef.Named("Luxandra_WhiteRain");
            WeatherDef weatherDef = WeatherDef.Named("Luxandra_Rain_White");

            // Random duration for the storm (2 to 5 days)
            int duration = Rand.Range(120000, 300000);

            GameCondition gameCondition = GameConditionMaker.MakeCondition(conditionDef, duration);
            map.gameConditionManager.RegisterCondition(gameCondition);

            map.weatherManager.TransitionTo(weatherDef);

            if (this.def.letterDef != null)
            {
                string letterText = string.Format(this.def.letterText, map.Parent.Label).CapitalizeFirst();
                Find.LetterStack.ReceiveLetter(this.def.letterLabel, letterText, this.def.letterDef);
            }

            return true;
        }
    }

    public class GameCondition_WhiteRain : GameCondition_ForceWeather
    {
        private const int TickBuildupInterval = 300;
        private List<Pawn> tempPawnList = new List<Pawn>();

        public override WeatherDef ForcedWeather()
        {
            return WeatherDef.Named("Luxandra_Rain_White");
        }

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            // --- Apply Hediff Buildup (Every 5s) ---
            if (Find.TickManager.TicksGame % TickBuildupInterval == 0)
            {
                Map map = this.SingleMap;
                if (map != null)
                {
                    ApplyBuildupToMap(map);
                }
            }
        }
        public override void End()
        {
            base.End();

            // Clear out the permanent weather by forcing the map to pick a new, normal vanilla weather pattern
            if (this.SingleMap != null)
            {
                this.SingleMap.weatherDecider.StartNextWeather();
            }
        }

        private void ApplyBuildupToMap(Map map)
        {
            tempPawnList.Clear();
            tempPawnList.AddRange(map.mapPawns.AllPawnsSpawned);

            HediffDef buildupDef = HediffDef.Named("Luxandra_WhiteRainBuildup");
            ThingDef filthDef = DefDatabase<ThingDef>.GetNamedSilentFail("Luxandra_FilthCumRain");

            // Skip this step for non-humans and non-colonyanimals if there's too many animals on the map to save on performance
            bool skipAnimals = tempPawnList.Where(p => !p.IsHumanLike() && !p.IsColonyAnimal).Count() > 50;

            for (int i = tempPawnList.Count - 1; i >= 0; i--)
            {
                Pawn pawn = tempPawnList[i];

                // Check if they are outdoors and alive
                if (pawn != null && !pawn.Dead && pawn.Spawned && !pawn.Position.Roofed(map))
                {
                    bool skipCurrentPawn = !pawn.IsHumanLike() && !pawn.IsColonyAnimal && skipAnimals;
                    // Drop a mess right under their feet as they walk in the rain!
                    if (!skipCurrentPawn && filthDef != null && pawn.Position.InBounds(map) && pawn.Position.WalkableByAny(map))
                    {
                        Filth existingFilth = pawn.Position.GetThingList(map).FirstOrDefault(t => t.def == filthDef) as Filth;
                        if (existingFilth != null)
                        {
                            if (existingFilth.CanBeThickened) existingFilth.ThickenFilth();
                        }
                        else
                        {
                            Filth newFilth = (Filth)ThingMaker.MakeThing(filthDef);
                            GenSpawn.Spawn(newFilth, pawn.Position, map);
                        }
                    }

                    // Only tick the hediff on humanlikes
                    if (pawn.RaceProps.Humanlike)
                    {
                        // Apply the slow hediff buildup
                        HealthUtility.AdjustSeverity(pawn, buildupDef, 0.015f);

                        // Check for the 100% mental break snap
                        Hediff activeHediff = pawn.health.hediffSet.GetFirstHediffOfDef(buildupDef);
                        if (activeHediff != null && activeHediff.Severity >= 1.0f)
                        {
                            TriggerMentalBreak(pawn, activeHediff);
                        }
                    }
                }
            }

            tempPawnList.Clear();
        }

        private void TriggerMentalBreak(Pawn pawn, Hediff hediff)
        {
            // Only adults should actually trigger the primary target break
            if (LuxandraLustUtilities.IsAdult(pawn))
            {
                MentalStateDef rapistBreak = DefDatabase<MentalStateDef>.GetNamed("RandomRape", false);
                if (rapistBreak != null && pawn.mindState.mentalStateHandler.TryStartMentalState(rapistBreak, null, true))
                {
                    Messages.Message(pawn.LabelCap + " has snapped from exposure to the white rain!", pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(hediff); // Clear it so it stops checking
                }
            }
            // Kids go hide in their room instead
            else
            {
                if (pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Wander_OwnRoom, null, true))
                {
                    Messages.Message(pawn.LabelCap + " is grossed out by the white rain and has locked themselves in their bedroom.", pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }

    // Keeping this around to avoid XML issues
    public class Hediff_WhiteRainBuildup : HediffWithComps
    {
    }
}