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
        public LuxandraNarrator narrator = new LuxandraNarrator(); 
        public static GameComponent_LuxandraLust Instance;

        public GameComponent_LuxandraLust(Game game)
        {
            Instance = this;
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
    }
}