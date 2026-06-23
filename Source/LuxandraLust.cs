using RimWorld;
using rjw;
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