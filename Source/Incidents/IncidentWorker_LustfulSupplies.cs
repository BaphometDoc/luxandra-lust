using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LuxandraLust
{
    public class IncidentWorker_LustfulSupplies : IncidentWorker
    {
        private static readonly SimpleCurve WealthToBudgetCurve = new SimpleCurve
        {
            new CurvePoint(0f, 150f),        // Under 30k (starts flat at 150)
            new CurvePoint(30000f, 150f),   // 30k wealth = 150 budget
            new CurvePoint(80000f, 400f),   // 80k wealth = 400 budget
            new CurvePoint(150000f, 1000f), // 150k wealth = 1000 budget
            new CurvePoint(300000f, 2500f)  // 300k wealth = 2500 budget (capped max)
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            // Standard safety check
            return base.CanFireNowSub(parms) && parms.target is Map;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            float rawColonyWealth = map.wealthWatcher.WealthTotal;
            float currentAverageSexNeed = LuxandraLustUtilities.GetAverageColonySexNeed(map);

            // This scales up the budget if they are satisfied, or tanks it down if they are horny
            float hornyMultiplier = 1;
            if (currentAverageSexNeed > 0.75f)
                hornyMultiplier = 1.25f;
            else if (currentAverageSexNeed < 0.25f)
                hornyMultiplier = 0.75f;
            float modifiedWealth = rawColonyWealth * hornyMultiplier;

            float totalMarketValueBudget = WealthToBudgetCurve.Evaluate(modifiedWealth);
            DebugActions_Luxandra.DebugLogMessage($"Attempted to create a lustful drop pod for wealth equal to {rawColonyWealth}. (Horny modifier: {hornyMultiplier})");

            // Possible drops and their values
            List<ThingCountDef> possibleDrops = new List<ThingCountDef>();
            possibleDrops.Add(new ThingCountDef(ThingDefOf.Beer.defName, 20f));
            possibleDrops.Add(new ThingCountDef(ThingDefOf.MedicineHerbal.defName, 20f));

            // RJW stuff (RimJobWorld is required by this mod, it can't not be on)
            possibleDrops.Add(new ThingCountDef("Condom", 20f)); // Condoms!
            possibleDrops.Add(new ThingCountDef("UsedCondom", 15f)); // Used condoms! yummy!
            possibleDrops.Add(new ThingCountDef("Aphrodisiac", 50f)); // Aphrodisiacs
            possibleDrops.Add(new ThingCountDef("HumpShroom", 35f)); // Our beloved humpshrooms
            possibleDrops.Add(new ThingCountDef("RJW_FertPill", 55f)); // Fertility pills
            possibleDrops.Add(new ThingCountDef("RJW_Contraceptive", 55f)); // And antifertility pills

            // Menstruation
            if (ModsConfig.IsActive("rjw.menstruation"))
            {
                possibleDrops.Add(new ThingCountDef("OvaryRegenerationPill", 250f)); // Ovary regeneration pills are not cheap
                possibleDrops.Add(new ThingCountDef("SuperovulationInducingAgent", 250f)); // Superovulation inducing agents are also not cheap
                possibleDrops.Add(new ThingCountDef("PainReliever", 50f)); // Painkillers however are
                possibleDrops.Add(new ThingCountDef("Cyclosporine", 50f)); // Do people even use those? I don't know, but they exist in the mod so we can drop them too
                possibleDrops.Add(new ThingCountDef("Absorber_Tampon", 50f)); // Tampons!
                possibleDrops.Add(new ThingCountDef("Absorber_Tampon_Dirty", 20f)); // ...used Tampons!
                possibleDrops.Add(new ThingCountDef("Absorber_Pad", 75f)); // Sanitary pads!
                possibleDrops.Add(new ThingCountDef("Absorber_Pad_Dirty", 25f)); // ...and used ones!
            }

            // RJW Genes
            if (ModsConfig.IsActive("Vegapnk.rjw.genes"))
            {
                // Should add some of the bionics once I find the recipes
            }

            List<Thing> finalDropList = new List<Thing>();
            float spentBudget = 0f;
            int safetyTimeout = 0;

            while (spentBudget < totalMarketValueBudget && safetyTimeout < 50)
            {
                safetyTimeout++;
                var validSelection = possibleDrops
                    .Where(d => d.marketValue <= (totalMarketValueBudget - spentBudget))
                    .InRandomOrder()
                    .FirstOrDefault();

                if (validSelection.defName == null) break;

                ThingDef foundDef = DefDatabase<ThingDef>.GetNamed(validSelection.defName, errorOnFail: false);
                if (foundDef != null)
                {
                    Thing itemInstance;

                    // Check if the item requires a material to be made out of
                    if (foundDef.MadeFromStuff)
                    {
                        ThingDef defaultStuff = GenStuff.DefaultStuffFor(foundDef);
                        itemInstance = ThingMaker.MakeThing(foundDef, defaultStuff);
                    }
                    else
                    {
                        itemInstance = ThingMaker.MakeThing(foundDef);
                    }

                    int maxAffordable = Mathf.FloorToInt((totalMarketValueBudget - spentBudget) / validSelection.marketValue);
                    int stackCount = Rand.RangeInclusive(1, Mathf.Min(maxAffordable, foundDef.stackLimit));

                    itemInstance.stackCount = stackCount;
                    finalDropList.Add(itemInstance);

                    spentBudget += (validSelection.marketValue * stackCount);
                }
            }

            if (finalDropList.Count == 0) return false;

            // Define the drop cell and drop it
            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 dropSpot, false, false, false))
            {
                dropSpot = DropCellFinder.RandomDropSpot(map);
            }

            DropPodUtility.DropThingsNear(dropSpot, map, finalDropList, canRoofPunch: true, forbid: false);
            DebugActions_Luxandra.DebugLogMessage($"Drop pod created and sent!");

            string label = this.def.letterLabel;
            string text = this.def.letterText;

            Find.LetterStack.ReceiveLetter(label, text, this.def.letterDef, new TargetInfo(dropSpot, map));
            return true;
        }
    }


    public struct ThingCountDef
    {
        public string defName;
        public float marketValue;

        public ThingCountDef(string defName, float marketValue)
        {
            this.defName = defName;
            this.marketValue = marketValue;
        }
    }
}