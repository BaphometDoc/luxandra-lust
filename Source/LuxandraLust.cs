using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace LuxandraLust
{
    public class LuxandraLust : Storyteller
    {
        public LuxandraLust() { }
    }

    public class GameComponent_LuxandraLust : GameComponent
    {
        public static GameComponent_LuxandraLust Instance
        {
            get
            {
                if (Current.Game == null) return null;
                return Current.Game.GetComponent<GameComponent_LuxandraLust>();
            }
        }

        public GameComponent_LuxandraLust(Game game) : base()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref sexActionCounter, "sexActionCounter", 0);
            Scribe_Values.Look(ref impureSexActionCounter, "impureSexActionCounter", 0);
            Scribe_Values.Look(ref rapeSexActionCounter, "rapeSexActionCounter", 0);
        }


        // Sex counters
        public int sexActionCounter = 0;
        public int impureSexActionCounter = 0;
        public int rapeSexActionCounter = 0;

        public void RegisterSexAction()
        {
            sexActionCounter++;
        }
        public void RegisterImpureSexAction()
        {
            impureSexActionCounter++;
        }
        public void RegisterRapeSexAction()
        {
            rapeSexActionCounter++;
        }

        public void ResetSexCounters()
        {
            sexActionCounter = 0;
            impureSexActionCounter = 0;
            rapeSexActionCounter = 0;

            DebugActions_Luxandra.DebugLogMessage("Sex counters reset.");
        }
    }

    public class LuxandraLustUtilities
    {
        /// <summary>
        /// Determines the average sex need of all the adult colonists and slaves in the colony
        /// </summary>
        public static float GetAverageColonySexNeed(Map map)
        {
            if (map == null) return 0.5f; // Safe fallback

            var eligiblePawns = map.mapPawns.AllPawnsSpawned.Where(p =>
                p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                (p.IsColonist || p.IsSlave) &&
                LuxandraLustUtilities.IsAdult(p)
            );

            float totalSexNeed = 0f;
            int countWithNeed = 0;

            foreach (Pawn pawn in eligiblePawns)
            {
                var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                if (sexNeed != null)
                {
                    totalSexNeed += sexNeed.CurLevelPercentage;
                    countWithNeed++;
                }
            }

            // Divide by countwithneed rather than actual total of pawns to avoid pawns with no sex need (es, Androids)
            return countWithNeed > 0 ? (totalSexNeed / countWithNeed) : 0.5f;
        }

        /// <summary>
        /// Enlarges a sex part, within a certain limit
        /// </summary>
        public static bool EnlargeSexPart(Pawn pawn, List<RJWLewdablePart> sexParts)
        {
            if (pawn == null || pawn.Dead || sexParts.EnumerableNullOrEmpty()) return false;

            bool anyChanged = false;

            foreach (var part in sexParts)
            {
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    float changeAmount = 0.5f;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + changeAmount, 3.0f);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, sync: false);

                        anyChanged = true;
                        DebugActions_Luxandra.DebugLogMessage($"Increased {part.SexPart.Def.defName} size for {pawn.NameShortColored}.");
                    }
                }
            }

            return anyChanged;
        }

        /// <summary>
        /// Forces a RandomRape mental state on the specified pawn
        /// </summary>
        public static void ForceRapistBreak(Pawn pawn, string reasonString, bool tankSexNeed = false)
        {
            if (IsAdult(pawn) == false)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} is not adult.");
                return;
            }

            if (pawn?.mindState?.mentalStateHandler == null)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} or mentalStateHandler is null.");
                return;
            }

            MentalStateDef rjwBreakDef = DefDatabase<MentalStateDef>.GetNamed("RandomRape", errorOnFail: false);

            if (rjwBreakDef == null)
            {
                Log.Warning("[Luxandra Debug] Could not trigger Rape mental break: RJW 'RandomRape' MentalStateDef was not found in the game database.");
            }
            else
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    rjwBreakDef,
                    reason: reasonString,
                    forced: true,
                    forceWake: true // Ensures the pawn snaps out of bed instantly to execute the event
                );

                // If enabled, set their sex need to 0 to ensure they are ready to act on the mental state
                if (tankSexNeed && pawn.needs != null)
                {
                    var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the pawn is adult (or youth if RJW has the check enabled)
        /// </summary>
        public static bool IsAdult(Pawn pawn)
        {
            if (pawn == null)
                return false;

            var allowYouth = rjw.RJWSettings.AllowYouthSex;

            AgeCategory ageCategory = pawn.GetAgeCategory();

            if (ageCategory == AgeCategory.Adult)
                return true;
            else if (ageCategory == AgeCategory.Youth && allowYouth)
                return true;
            else
                return false;
        }
    }
}