using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_FertilityPulseSite : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            PlanetTile playerPlanetTile = new PlanetTile(map.Tile);

            // Tightened distance check for the radar finder sub-routine
            return TileFinder.TryFindNewSiteTile(out _, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            PlanetTile playerPlanetTile = new PlanetTile(map.Tile);

            if (!TileFinder.TryFindNewSiteTile(out PlanetTile targetTile, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near))
            {
                return false;
            }

            SitePartDef customPart = DefDatabase<SitePartDef>.GetNamed("Luxandra_FertilityPulseSitePart", false);
            if (customPart == null) return false;

            Site site = SiteMaker.MakeSite(customPart, targetTile, parms.faction);
            if (site == null) return false;

            int durationDays = Rand.RangeInclusive(15, 30);
            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                timeoutComp.StartTimeout(durationDays * 60000);
            }

            Find.WorldObjects.Add(site);

            string letterLabel = "Fertility Overload Transmitter Activated";
            string letterText = $"Long-range sensors have detected an active mechanical transmitter nearby. It has begun broadcasting an intense localized psychic ripple, overloading the reproductive instincts of all adults in the area.\n\nIt will continue to plague your colony until you send a caravan to destroy it. According to energy signatures, its internal power matrix will deplete and shut down naturally in {durationDays} days if left alone.";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, site);

            return true;
        }
    }
}