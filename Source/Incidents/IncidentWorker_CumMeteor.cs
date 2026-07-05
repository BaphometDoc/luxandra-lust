using RimWorld;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_CumMeteor : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_CumMeteor.defName))
                return false;

            Map map = (Map)parms.target;

            return map != null && map.mapPawns.AllHumanlikeSpawned.Any(p => p.Spawned && !p.Dead && LuxandraUtilities.IsAdult(p) && p.Position.Roofed(map) == false);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Find a poor target pawn out in the open (no roof overhead)
            Pawn targetPawn = map.mapPawns.AllHumanlikeSpawned
                .Where(p => p.Spawned && !p.Dead && LuxandraUtilities.IsAdult(p) && !p.Position.Roofed(map))
                .InRandomOrder()
                .FirstOrDefault();

            if (targetPawn == null) return false;

            IntVec3 targetCell = targetPawn.Position;

            EffecterDefOf.Deflect_General.Spawn(targetCell, map).Cleanup();

            // The Filth: Spawn a massive ring of filth around the impact site
            int filthRadius = 3;
            CellRect splashZone = CellRect.CenteredOn(targetCell, filthRadius);

            // This one is cool but has the slight issue of oneshotting anything below it
            //SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, targetCell, map);

            // Look up Cumpilation's filth def, or fall back to vanilla slime if missing
            ThingDef cumFilthDef = DefDatabase<ThingDef>.GetNamed("FilthCum", false) ?? ThingDefOf.Filth_Slime;

            foreach (IntVec3 cell in splashZone.Cells)
            {
                if (cell.InBounds(map) && cell.Walkable(map) && !cell.Roofed(map))
                {
                    // 75% chance to coat every cell in the radius
                    if (Rand.Value < 0.75f)
                    {
                        FilthMaker.TryMakeFilth(cell, map, cumFilthDef, Rand.Range(5, 10));
                    }
                }
            }

            // If cumpilation is loaded, drench it
            if (LuxandraModChecks.IsCumpilationActive())
            {
                float totalCum = 40.0f;
                while (totalCum > 0)
                {
                    CumpilationIntegration.DrenchInCumFromNothing(targetPawn, 2.5f);
                    totalCum -= 2.5f;
                }

                // Was originally going to throw a bunch of cum buckets while at it
                // But I think spawning a bunch of buckets doesn't really look that well
                //ThingDef cumItemDef = DefDatabase<ThingDef>.GetNamed("Cum", false);
                //if (cumItemDef != null)
                //{
                //    int itemAmount = Rand.Range(15, 35);
                //    Thing cumStack = ThingMaker.MakeThing(cumItemDef);
                //    cumStack.stackCount = itemAmount;

                //    // Drop it safely right next to the target pawn
                //    GenPlace.TryPlaceThing(cumStack, targetCell, map, ThingPlaceMode.Near);
                //}
            }

            if (CurrentKink == StorytellerKink.Cum)
            {
                Messages.Message($"Luxandra had a massive laugh at {targetPawn.NameShortColored} being hopelessly covered in cum from head to feet. She thanks you with 5 Favor.", MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(5);
            }


            TaggedString letterLabel = "Orbital Payload Incoming";
            TaggedString letterText = $"A sudden disturbance in the upper atmosphere has coalesced into a dense atmospheric anomaly directly above {targetPawn.LabelShort}.\n\nBefore they could even react, an orbital cascade of warm fluids fell directly from the sky. They never saw it coming.";

            // 3. Send the letter alerting the player
            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterText,
                LetterDefOf.NegativeEvent,
                new TargetInfo(targetCell, map)
            );

            return true;
        }
    }
}