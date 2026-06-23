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
        }
    }
}