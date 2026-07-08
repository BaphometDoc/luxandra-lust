using RimWorld;
using UnityEngine;
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

            // --- KINK RELATED
            Scribe_Values.Look(ref CurrentKink, "Luxandra_CurrentKinkPhase", StorytellerKink.None);
            Scribe_Values.Look(ref kinkPleasedCounter, "kinkPleasedCounter", 0);

            // --- GENERIC COUNTERS ---
            Scribe_Values.Look(ref sexActionCounter, "sexActionCounter", 0);
            Scribe_Values.Look(ref impureSexActionCounter, "impureSexActionCounter", 0);
            Scribe_Values.Look(ref rapeSexActionCounter, "rapeSexActionCounter", 0);
            Scribe_Values.Look(ref bestialitySexActionCounter, "bestialitySexActionCounter", 0);
            Scribe_Values.Look(ref necrophiliaSexActionCounter, "necrophiliaSexActionCounter", 0);

            // --- REROLL COUNTER ---
            Scribe_Values.Look(ref colonyFavorPoints, "colonyFavorPoints", 0);

            // --- CYCLE COUNTERS --- (TODO: Determine events in base of sexual skewing)
            Scribe_Values.Look(ref sexActionCounterForCycle, "sexActionCounterForCycle", 0);
            Scribe_Values.Look(ref impureSexActionCounterForCycle, "impureSexActionCounterForCycle", 0);
            Scribe_Values.Look(ref rapeSexActionCounterForCycle, "rapeSexActionCounterForCycle", 0);
            Scribe_Values.Look(ref bestialitySexActionCounterForCycle, "bestialitySexActionCounterForCycle", 0);
            Scribe_Values.Look(ref necrophiliaSexActionCounterForCycle, "necrophiliaSexActionCounterForCycle", 0);
        }


        /// <summary>
        /// Favor points owned by the colony (Former sexActionCounterForRerolls)
        /// </summary>
        public int colonyFavorPoints = 0;

        // Sex counters (these still do nothing atm)
        public int sexActionCounter = 0;
        public int sexActionCounterForCycle = 0;
        public int impureSexActionCounter = 0;
        public int impureSexActionCounterForCycle = 0;
        public int rapeSexActionCounter = 0;
        public int rapeSexActionCounterForCycle = 0;
        public int bestialitySexActionCounter = 0;
        public int bestialitySexActionCounterForCycle = 0;
        public int necrophiliaSexActionCounter = 0;
        public int necrophiliaSexActionCounterForCycle = 0;

        public int kinkPleasedCounter = 0;


        /// <summary>
        /// Luxandra's Kinks
        /// </summary>
        public enum StorytellerKink
        {
            None,
            Pregnancy,
            Anal,
            Oral,
            Bestiality,
            Rape,
            Masturbation,
            Necrophilia,
            Gay,
            Lesbian,
            Cum,
            Breasts,
            Incest,
            Implantation,
            Futa,
            Mechanophilia,
            Tentacles
        }

        /// <summary>
        /// Current active kink
        /// </summary>
        public static StorytellerKink CurrentKink = StorytellerKink.None;

        /// <summary>
        /// Subtracts the specified amount from the sex counter
        /// </summary>
        public void PayForLuxandraServices(int amountPaid)
        {
            colonyFavorPoints -= amountPaid;
        }

        public void AddToFavorCounter(int amount)
        {
            colonyFavorPoints += amount;
        }

        public void RegisterSexAction(bool satisfiedKink = false)
        {
            sexActionCounter++;
            colonyFavorPoints++;
            sexActionCounterForCycle++;

            if (satisfiedKink)
            {
                colonyFavorPoints++;
                kinkPleasedCounter++;
            }
        }
        public void RegisterImpureSexAction()
        {
            impureSexActionCounter++;
            impureSexActionCounterForCycle++;
        }
        public void RegisterRapeSexAction()
        {
            rapeSexActionCounter++;
            rapeSexActionCounterForCycle++;
        }
        public void RegisterBestialitySexAction()
        {
            bestialitySexActionCounter++;
            bestialitySexActionCounterForCycle++;
        }
        public void RegisterNecrophiliaSexAction()
        {
            necrophiliaSexActionCounter++;
            necrophiliaSexActionCounterForCycle++;
        }

        public void ResetSexCountersForRerolls()
        {
            colonyFavorPoints = 0;
            impureSexActionCounter = 0;
            rapeSexActionCounter = 0;
            bestialitySexActionCounter = 0;
            necrophiliaSexActionCounter = 0;

            LuxandraDebugActions.DebugLogMessage("Sex counters for event rerolls reset.");
        }

        public void ResetSexCountersForCycle()
        {
            sexActionCounterForCycle = 0;
            impureSexActionCounterForCycle = 0;
            rapeSexActionCounterForCycle = 0;
            bestialitySexActionCounterForCycle = 0;
            necrophiliaSexActionCounterForCycle = 0;

            LuxandraDebugActions.DebugLogMessage("Sex counters for storyteller cycle reset.");
        }

        /// <summary>
        /// Calculates the threshold to trigger Luxanna's event reroll (Configs included)
        /// </summary>
        public static int CalculateSexualRerollThreshold()
        {
            int activeAdults = 0;
            // Add adult colonists
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeAdultColonistsSpawned)
            {
                if (LuxandraUtilities.IsAdult(pawn) && !pawn.Dead)
                {
                    activeAdults++;
                }
            }
            // Add adult slaves
            foreach (Pawn pawn in Find.CurrentMap.mapPawns.SlavesOfColonySpawned)
            {
                if (LuxandraUtilities.IsAdult(pawn) && !pawn.Dead)
                {
                    activeAdults++;
                }
            }

            // Calculate potential pairs (e.g., 4 adults = 2 pairs; 5 adults = 3 potential mating vectors)
            int potentialPairs = Mathf.CeilToInt(activeAdults / 2f);

            // 13 points required per pair per 11 days guarantees coasting couples fail check 1
            float basePointsPerPair = 13f;
            float currentThreshold = potentialPairs * basePointsPerPair;

            // Apply Colony Age Scaling (1.0x at day 0 up to 1.5x at day 120)
            float colonyDays = (float)Find.TickManager.TicksGame / GenDate.TicksPerDay;
            float ageMultiplier = Mathf.Lerp(1.0f, 1.5f, Mathf.Min(colonyDays / 120f, 1.0f));
            currentThreshold *= ageMultiplier;

            // Enforce strict minimum/maximum boundaries
            int finalizedThreshold = Mathf.RoundToInt(currentThreshold);
            finalizedThreshold = Mathf.Clamp(finalizedThreshold, 13, 100);

            // Factor in user configuration settings slider
            return Mathf.RoundToInt(finalizedThreshold * LuxandraModSettings.eventRerollThresholdMultiplier);
        }
    }
}