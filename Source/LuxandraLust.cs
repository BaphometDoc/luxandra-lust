using RimWorld;
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

            // --- REROLL COUNTERS ---
            Scribe_Values.Look(ref sexActionCounterForRerolls, "sexActionCounterForRerolls", 0);
            Scribe_Values.Look(ref impureSexActionCounterForRerolls, "impureSexActionCounterForRerolls", 0);
            Scribe_Values.Look(ref rapeSexActionCounterForRerolls, "rapeSexActionCounterForRerolls", 0);
            Scribe_Values.Look(ref bestialitySexActionCounterForRerolls, "bestialitySexActionCounterForRerolls", 0);
            Scribe_Values.Look(ref necrophiliaSexActionCounterForRerolls, "necrophiliaSexActionCounterForRerolls", 0);

            // --- CYCLE COUNTERS ---
            Scribe_Values.Look(ref sexActionCounterForCycle, "sexActionCounterForCycle", 0);
            Scribe_Values.Look(ref impureSexActionCounterForCycle, "impureSexActionCounterForCycle", 0);
            Scribe_Values.Look(ref rapeSexActionCounterForCycle, "rapeSexActionCounterForCycle", 0);
            Scribe_Values.Look(ref bestialitySexActionCounterForCycle, "bestialitySexActionCounterForCycle", 0);
            Scribe_Values.Look(ref necrophiliaSexActionCounterForCycle, "necrophiliaSexActionCounterForCycle", 0);
        }


        // Sex counters
        public int sexActionCounterForRerolls = 0;
        public int sexActionCounterForCycle = 0;
        public int impureSexActionCounterForRerolls = 0;
        public int impureSexActionCounterForCycle = 0;
        public int rapeSexActionCounterForRerolls = 0;
        public int rapeSexActionCounterForCycle = 0;
        public int bestialitySexActionCounterForRerolls = 0;
        public int bestialitySexActionCounterForCycle = 0;
        public int necrophiliaSexActionCounterForRerolls = 0;
        public int necrophiliaSexActionCounterForCycle = 0;

        public void RegisterSexAction()
        {
            sexActionCounterForRerolls++;
            sexActionCounterForCycle++;
        }
        public void RegisterImpureSexAction()
        {
            impureSexActionCounterForRerolls++;
            impureSexActionCounterForCycle++;
        }
        public void RegisterRapeSexAction()
        {
            rapeSexActionCounterForRerolls++;
            rapeSexActionCounterForCycle++;
        }
        public void RegisterBestialitySexAction()
        {
            bestialitySexActionCounterForRerolls++;
            bestialitySexActionCounterForCycle++;
        }
        public void RegisterNecrophiliaSexAction()
        {
            necrophiliaSexActionCounterForRerolls++;
            necrophiliaSexActionCounterForCycle++;
        }

        public void ResetSexCountersForRerolls()
        {
            sexActionCounterForRerolls = 0;
            impureSexActionCounterForRerolls = 0;
            rapeSexActionCounterForRerolls = 0;
            bestialitySexActionCounterForRerolls = 0;
            necrophiliaSexActionCounterForRerolls = 0;

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
    }
}