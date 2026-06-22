using RimWorld;
using HarmonyLib;
using Verse;


namespace LuxandraLust
{
    public class LuxandraLust : Storyteller
    {
        public LuxandraLust() { }
    }

    public class GameComponent_LuxandraLust : GameComponent
    {
        private int lastCheckedDay = -1;
        private bool initialized = false;
        private LuxandraNarrator narrator = new LuxandraNarrator(); 
        public static GameComponent_LuxandraLust Instance;

        public GameComponent_LuxandraLust(Game game)
        {
            Instance = this;
        }
        public override void GameComponentTick()
        {
            if (!LuxandraStorytellerCheck.IsActive())
                return;

            int currentDay = GenDate.DaysPassed;

            // First-time initialization
            if (!initialized)
            {
                lastCheckedDay = currentDay;
                initialized = true;
                return;
            }

            if (currentDay != lastCheckedDay)
            {
                lastCheckedDay = currentDay;

                narrator.TickDaily();

                Log.Message($"[Luxandra] Cycle: {narrator.GetCycle()}");
            }
        }

        #region iteration methods
        public void RegisterSexAction()
        {
            narrator.RegisterSexAction();
        }

        public void RegisterImpureSexAction()
        {
            narrator.RegisterImpureSexAction();
        }

        public void RegisterRapeSexAction()
        {
            narrator.RegisterRapeSexAction();
        }
        #endregion

        #region debug stuff
        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastCheckedDay, "lastCheckedDay", -1);
            Scribe_Values.Look(ref initialized, "initialized", false);
        }
        #endregion
    }
}