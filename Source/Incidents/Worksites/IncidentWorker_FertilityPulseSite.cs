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

            return TileFinder.TryFindNewSiteTile(out _, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null || !map.IsPlayerHome) return false;

            PlanetTile playerPlanetTile = new PlanetTile(map.Tile);

            // 1. Find the target location
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile targetTile, playerPlanetTile, 3, 8, false, null, 0.5f, true, TileFinderMode.Near))
            {
                return false;
            }

            int tileIndex = targetTile.tileId;

            // 2. Grab the vanilla Psychic Droner SitePartDef
            // Note: If SitePartDefOf.PsychicDroner isn't in your framework, use DefDatabase<SitePartDef>.GetNamed("PsychicDroner")
            SitePartDef vanillaDronePart = DefDatabase<SitePartDef>.GetNamed("PsychicDroner");
            if (vanillaDronePart == null) return false;

            // 3. Make the site using the vanilla standard maker signature
            Site site = SiteMaker.MakeSite(vanillaDronePart, targetTile, parms.faction);
            if (site == null) return false;

            // 4. Add the standard expiration timer
            int durationDays = Rand.RangeInclusive(15, 30);
            TimeoutComp timeoutComp = site.GetComponent<TimeoutComp>();
            if (timeoutComp != null)
            {
                timeoutComp.StartTimeout(durationDays * 60000);
            }

            // 5. Spawn it on the World Map
            Find.WorldObjects.Add(site);

            // 6. Send the Letter
            string letterLabel = "TEST Psychic Drone Site Detected";
            string letterText = $"TEST Long-range sensors have detected a vanilla psychic droner nearby. It will plague your colony for {durationDays} days or until destroyed.";

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, site);

            return true;
        }
    }
}