using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_TheMilkGame : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // Full Stomachs & Spontaneous Lactation ---
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer)
                {
                    // Satisfy hunger completely
                    if (pawn.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
                    }

                    // Biotech-specific lactation hook
                    if (ModsConfig.BiotechActive && pawn.gender == Gender.Female)
                    {
                        HediffDef lactationDef = HediffDefOf.Lactating;
                        if (lactationDef != null && !pawn.health.hediffSet.HasHediff(lactationDef))
                        {
                            pawn.health.AddHediff(lactationDef);
                        }
                    }
                }
            }

            // Crop-to-Milk Transmutation because it's very funny
            List<Thing> cropsInStorage = map.listerThings.AllThings.Where(t =>
                t.def.category == ThingCategory.Item &&
                !t.Position.Fogged(map) &&
                (t.def == ThingDef.Named("RawCorn") || t.def == ThingDef.Named("RawRice") || t.def == ThingDef.Named("RawPotatoes"))
            ).ToList();

            foreach (Thing cropStack in cropsInStorage)
            {
                int stackCount = cropStack.stackCount;
                int amountToConvert = Rand.RangeInclusive(stackCount / 3, stackCount * 2 / 3);

                if (amountToConvert > 0)
                {
                    IntVec3 position = cropStack.Position;

                    if (amountToConvert >= stackCount)
                    {
                        cropStack.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        cropStack.SplitOff(amountToConvert).Destroy(DestroyMode.Vanish);
                    }

                    Thing milk = ThingMaker.MakeThing(ThingDef.Named("Milk"));
                    milk.stackCount = amountToConvert;
                    GenSpawn.Spawn(milk, position, map);
                }
            }

            // Wolf Pack Ambush
            float threatPoints = StorytellerUtility.DefaultThreatPointsNow(map) * 0.85f;

            IncidentParms wolfParms = new IncidentParms
            {
                target = map,
                points = threatPoints,
                pawnKind = DefDatabase<PawnKindDef>.GetNamed("Warg")
            };

            IncidentDef manhunterIncident = IncidentDefOf.ManhunterPack;

            if (manhunterIncident.Worker.CanFireNow(wolfParms))
            {
                manhunterIncident.Worker.TryExecute(wolfParms);
            }

            // Send a letter to the player explaining the event
            Find.LetterStack.ReceiveLetter(
                "Lactose Cataclysm",
                "Luxandra has decided to play a game with your colony! Stomachs are full, lactation has been spontaneously induced across your women, and parts of your granaries have been turned into fresh milk.\n\nBut beware—she has invited some hungry guests to the banquet...",
                LetterDefOf.NegativeEvent,
                new TargetInfo(map.mapPawns.FreeColonistsSpawned.RandomElement().Position, map)
            );

            return true;
        }
    }
}