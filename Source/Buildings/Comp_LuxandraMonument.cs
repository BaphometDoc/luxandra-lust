using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LuxandraLust
{
    public class Comp_LuxandraMonument : ThingComp
    {
        private GameComponent_LuxandraLust LuxandraComp => GameComponent_LuxandraLust.Instance;

        // This function tells RimWorld what to show when right-clicking the building
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.IsColonistPlayerControlled || selPawn.Downed || selPawn.Dead || (!LuxandraStorytellerCheck.IsActive()))
                yield break;

            yield return new FloatMenuOption("Commune with Luxandra", () =>
            {
                // If an interaction cell is defined, use it, otherwise target the building's center
                IntVec3 targetCell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
                Job job = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                if (selPawn.jobs.curDriver is Verse.AI.JobDriver_Goto driver)
                {
                    driver.AddFinishAction((Action) =>
                    {
                        // If the building center is too far, use the pawn instead
                        if (selPawn.Position.AdjacentTo8WayOrInside(parent) && !selPawn.Downed && !selPawn.Dead)
                        {
                            OpenRootDialogue(selPawn);
                        }
                    });
                }
            });
        }

        #region The root window
        private void OpenRootDialogue(Pawn pawn)
        {
            if (LuxandraComp == null) return;

            decimal currentFavor = LuxandraComp.colonyFavorPoints;
            string text = $"{pawn.LabelShort} kneels before the monument, opening their mind to Luxandra's influence.\n\n" +
                          $"Current Favor: {currentFavor} points.";

            DiaNode rootNode = new DiaNode(text);

            // Option: Pray for a blessing (open list)
            DiaOption prayOption = new DiaOption("Pray for a blessing");
            prayOption.action = () => OpenIncidentSelectionDialogue(pawn);
            rootNode.options.Add(prayOption);

            if (ModsConfig.IdeologyActive)
            {
                // Option: Summon a slaver (open list)
                DiaOption slaverOption = new DiaOption("Request the visit of a slaver");
                slaverOption.action = () => OpenRequestSlaverDialogue(pawn);
                rootNode.options.Add(slaverOption);
            }

            // Option: Leave
            DiaOption leaveOption = new DiaOption("Leave");
            leaveOption.resolveTree = true;
            rootNode.options.Add(leaveOption);

            ShowDialog(rootNode, "Communing with Luxandra");
        }
        #endregion

        #region Request Slave Traders
        private void OpenRequestSlaverDialogue(Pawn pawn)
        {
            var isEroTraderActive = LuxandraModChecks.IsEroTraderActive();

            var extraDialogue = isEroTraderActive ? "\n\nI know of some with a very interesting selection..." : "";
            DiaNode rootSelectionNode = new DiaNode($"<color=#D4AF37>Luxandra</color>:\n\nYou require the visit of a slave seller? Are you in need of more subjects, or you have more... interesting plans?{extraDialogue}");

            DiaOption slaverOption = new DiaOption("(10 Favor) Yes, please bless us with their visit.");
            if (LuxandraComp.colonyFavorPoints < 10)
            {
                slaverOption.Disable($"Requires {10} Favor (You have {LuxandraComp.colonyFavorPoints}).");
            }
            slaverOption.action = () => OpenSlaverConfirmationDialogue(pawn);
            rootSelectionNode.options.Add(slaverOption);

            // Add a back button inside the sub-menu to return to our main category listing
            DiaOption subMenuBack = new DiaOption("No, choose something else");
            subMenuBack.action = () => OpenRootDialogue(pawn);
            rootSelectionNode.options.Add(subMenuBack);

            ShowDialog(rootSelectionNode, "Request the visit of a slaver");
        }

        private void OpenSlaverConfirmationDialogue(Pawn pawn)
        {
            string text = $"Are you sure you want to spend 10 Favor to request the visit a Slave Trader?\n\nYou would have {LuxandraComp.colonyFavorPoints - 10} left after the request." +
                           "\n\n\n\n<i>Dev note: Summoning merchants can occasionally fail due to Rimworld innate issues. It is recommended to save the game if you don't want to risk to occasionally waste the points without any trader showing up.</i>";

            DiaNode confirmNode = new DiaNode(text);

            DiaOption yesOption = new DiaOption("Yes, request it");

            bool failedToFindTrader = false;

            // Prepare slaver parameters
            TraderKindDef slaveTraderDef = DefDatabase<TraderKindDef>.GetNamed("RJW_Lewd_Trader_Caravan", false);


            if (slaveTraderDef == null)
                slaveTraderDef = DefDatabase<TraderKindDef>.GetNamed("Caravan_Neolithic_Slaver", false);
            if (slaveTraderDef == null)
                slaveTraderDef = DefDatabase<TraderKindDef>.GetNamed("Caravan_Outlander_PirateMerchant", false);
            if (slaveTraderDef == null)
            {
                failedToFindTrader = true;
                LuxandraDebugActions.DebugLogMessage("No valid slave trader found for Luxandra's request.");
            }
            else
                LuxandraDebugActions.DebugLogMessage($"Found valid trader def: {slaveTraderDef.defName}.");

            Faction tradingFaction = Find.FactionManager.AllFactions
                    .Where(f => !f.def.permanentEnemy && !f.IsPlayer && !f.defeated && f.def.caravanTraderKinds.Any())
                    .RandomElementWithFallback(null);

            if (tradingFaction == null)
            {
                // Absolute safety fallback to any peaceful entity if specialized ones aren't active
                tradingFaction = Find.FactionManager.AllFactions
                    .Where(f => !f.def.permanentEnemy && !f.IsPlayer && !f.defeated)
                    .RandomElementWithFallback(null);
            }

            if (tradingFaction == null)
            {
                LuxandraDebugActions.DebugLogMessage("Could not find any valid faction to send a slave trader.");
                failedToFindTrader = true;
            }
            else
                LuxandraDebugActions.DebugLogMessage($"Found valid faction: {tradingFaction.NameColored}.");

            if (failedToFindTrader)
            {
                yesOption.Disable("None available right now.");
            }

            // Prepare the trader incident
            IncidentDef incidentDef = IncidentDefOf.TraderCaravanArrival;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, pawn.Map);
            parms.faction = tradingFaction;
            parms.traderKind = slaveTraderDef;
            parms.forced = true;

            yesOption.action = () =>
            {
                if (!incidentDef.Worker.TryExecute(parms))
                {
                    LuxandraDebugActions.DebugLogMessage("Caravan arrival failed due to save state corruption or map error. Falling back to Orbital Trade Ship.");

                    IncidentDef orbitalInc = IncidentDefOf.OrbitalTraderArrival;
                    IncidentParms orbitalParms = StorytellerUtility.DefaultParmsNow(orbitalInc.category, pawn.Map);
                    orbitalParms.traderKind = slaveTraderDef;
                    orbitalParms.forced = true;

                    orbitalInc.Worker.TryExecute(orbitalParms);
                }

                LuxandraComp.PayForLuxandraServices(10);
                Messages.Message($"Your request to Luxandra consumed 10 Favor to invoke a trader visit.", MessageTypeDefOf.TaskCompletion, false);
            };

            yesOption.resolveTree = true; // This finishes the interaction entirely
            confirmNode.options.Add(yesOption);

            DiaOption noOption = new DiaOption("No, choose something else");
            noOption.action = () => OpenRequestSlaverDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, "Confirm Slave Trader request");
        }

        #endregion

        #region Request incidents
        private string DetermineSectionFlavorText(LuxandraIncidentType incidentType)
        {
            switch (incidentType)
            {
                case LuxandraIncidentType.Positive:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a blessing?\n\nSome gifts, or maybe something more... exciting?";
                case LuxandraIncidentType.Neutral:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a mundane event?\n\nMaybe something to spice your boring days?";
                case LuxandraIncidentType.Negative:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a trial?\n\nAn exotic threat, for the arousing thrill of danger?";
                case LuxandraIncidentType.Raid:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a threat to your colony?\n\nReckless bravery, or another chance to acquire more subjects for your perversions?";
                case LuxandraIncidentType.Quest:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a mission?\n\nA demand of sort, to test your abilities?";
                default: // This should never show up but here just in case
                    return "Luxandra's gaze is drawn to the threads of fate, each shimmering with potential.";
            }
        }

        // --- THE INCIDENT SELECTION WINDOW ---
        private void OpenIncidentSelectionDialogue(Pawn pawn)
        {
            DiaNode rootSelectionNode = new DiaNode("Luxandra sifts through the tapestry of fates, waiting for your request.\n\nWhat twist of fate will you try to invoke?");

            // Define our categories mapping to your collections
            var categories = new List<(string Name, IEnumerable<LuxandraIncidentDefs> Incidents, string FlavorText)>
                                        {
                                            ("Positive Blessings", LuxandraDefsCollections.PositiveIncidents, DetermineSectionFlavorText(LuxandraIncidentType.Positive)),
                                            ("Neutral Alterations", LuxandraDefsCollections.NeutralIncidentsNoQuests, DetermineSectionFlavorText(LuxandraIncidentType.Neutral)),
                                            ("Negative Afflictions", LuxandraDefsCollections.NegativeIncidentsNoRaids, DetermineSectionFlavorText(LuxandraIncidentType.Negative)),
                                            ("Raids & Invasions", LuxandraDefsCollections.Raids, DetermineSectionFlavorText(LuxandraIncidentType.Raid)),
                                            ("Sacred Quests", LuxandraDefsCollections.Quests, DetermineSectionFlavorText(LuxandraIncidentType.Quest))
                                            };


            foreach (var category in categories)
            {
                // Filter out what belongs to this category and actually has a cost config
                var categoryIncidents = category.Incidents
                    .Where(p => LuxandraEventCheck.IsEnabled(p.IncidentDef.defName) && p.PointBaseCost != null)
                    .OrderBy(x => x.PointBaseCost)
                    .ToList();

                // If the XML collection is completely empty or no items have costs configured
                if (!categoryIncidents.Any())
                {
                    DiaOption emptyCategoryOption = new DiaOption(category.Name);
                    emptyCategoryOption.Disable("\nNone available at the moment");
                    rootSelectionNode.options.Add(emptyCategoryOption);
                    continue;
                }

                // Otherwise, proceed with creating the sub-menu node as normal.
                DiaNode subMenuNode = new DiaNode(category.FlavorText);
                bool anyEventAvailable = false;

                // Generate the submenu
                foreach (var luxIncident in categoryIncidents)
                {
                    // Calculation for base event costs: sexual threshold / 50
                    decimal masterThreshold = GameComponent_LuxandraLust.CalculateSexualRerollThreshold();
                    decimal multiplier = luxIncident.PointBaseCost.Value / 50m;
                    int cost = (int)Math.Ceiling(masterThreshold * multiplier);

                    string label = $"({cost} Favor) {luxIncident.IncidentDef.LabelCap}";
                    DiaOption incidentOption = new DiaOption(label);

                    var canFireNow = luxIncident.IncidentDef.Worker.CanFireNow(StorytellerUtility.DefaultParmsNow(luxIncident.IncidentDef.category, pawn.Map));

                    if (LuxandraComp.colonyFavorPoints < cost)
                    {
                        incidentOption.Disable($"Requires {cost} Favor (You have {LuxandraComp.colonyFavorPoints}).");
                    }
                    else if (!canFireNow)
                    {
                        incidentOption.Disable("Cannot be invoked right now.");
                    }
                    else
                    {
                        incidentOption.action = () => OpenIncidentConfirmationDialogue(pawn, luxIncident, cost);
                        anyEventAvailable = true; // Found at least one playable event!
                    }

                    subMenuNode.options.Add(incidentOption);
                }

                // Add a back button inside the sub-menu to return to our main category listing
                DiaOption subMenuBack = new DiaOption("Go Back");
                subMenuBack.action = () => ShowDialog(rootSelectionNode, "Select a Blessing");
                subMenuNode.options.Add(subMenuBack);

                // 2. Create the main menu option that links to this sub-node
                DiaOption categoryOption = new DiaOption(category.Name);

                if (!anyEventAvailable)
                {
                    categoryOption.Disable("No events in this category are currently available or affordable.");
                }
                else
                {
                    categoryOption.action = () => ShowDialog(subMenuNode, category.Name);
                }

                rootSelectionNode.options.Add(categoryOption);
            }

            // Main menu "Go Back" button
            DiaOption backOption = new DiaOption("Go Back");
            backOption.action = () => OpenRootDialogue(pawn);
            rootSelectionNode.options.Add(backOption);

            ShowDialog(rootSelectionNode, "Select a Category");
        }

        // --- STEP 3: THE CONFIRMATION WINDOW ---
        private void OpenIncidentConfirmationDialogue(Pawn pawn, LuxandraIncidentDefs luxIncident, int cost)
        {
            string text = $"Are you sure you want to spend {cost} Favor to invoke {luxIncident.IncidentDef.LabelCap}?\n\nYou would have {LuxandraComp.colonyFavorPoints - cost} left after the request.";

            DiaNode confirmNode = new DiaNode(text);

            DiaOption yesOption = new DiaOption("Yes, manifest it");
            yesOption.action = () =>
            {
                // Prepare incident parameters
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(luxIncident.IncidentDef.category, pawn.Map);
                parms.forced = true; // Force the incident to occur, bypassing the reroll from Luxandra

                // Immediately queue or fire the incident
                if (luxIncident.IncidentDef.Worker.CanFireNow(parms))
                {
                    luxIncident.IncidentDef.Worker.TryExecute(parms);
                }
                else
                {
                    // Fallback backup if something structural blocks it right this second (like bad weather for a trader)
                    Find.Storyteller.incidentQueue.Add(luxIncident.IncidentDef, Find.TickManager.TicksGame, parms);
                }

                LuxandraComp.PayForLuxandraServices(cost);
                Messages.Message($"Your request to Luxandra consumed {cost} Favor to manifest {luxIncident.IncidentDef.LabelCap}.", MessageTypeDefOf.TaskCompletion, false);
            };
            yesOption.resolveTree = true; // This finishes the interaction entirely
            confirmNode.options.Add(yesOption);

            DiaOption noOption = new DiaOption("No, choose something else");
            noOption.action = () => OpenIncidentSelectionDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, "Confirm Fate Selection");
        }

        // Simple helper to cut down on repeating WindowStack code
        private void ShowDialog(DiaNode node, string title)
        {
            Dialog_NodeTree window = new Dialog_NodeTree(node, delayInteractivity: false, radioMode: false, title: title);
            Find.WindowStack.Add(window);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // 1. Let vanilla components draw their own gizmos first (like deconstruction or minifying)
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // 2. Make sure our data component actually exists before drawing the button
            if (LuxandraComp != null)
            {
                // 3. Create a standard command button
                Command_Action favorGizmo = new Command_Action();

                favorGizmo.defaultLabel = "Check Favor";
                favorGizmo.defaultDesc = "Check how much Favor you currently have with Luxandra.";

                favorGizmo.icon = ContentFinder<Texture2D>.Get("UI/Icons/Rituals/BeseechLuxandra");

                favorGizmo.action = () =>
                {
                    decimal currentFavor = LuxandraComp.colonyFavorPoints;

                    Messages.Message($"Current Favor with Luxandra: {currentFavor} points.", MessageTypeDefOf.NeutralEvent);
                };

                yield return favorGizmo;
            }
        }
    }

    // 3. The properties class that allows RimWorld's XML parser to read it
    public class CompProperties_LuxandraMonument : CompProperties
    {
        public CompProperties_LuxandraMonument()
        {
            this.compClass = typeof(Comp_LuxandraMonument);
        }
    }
    #endregion
}