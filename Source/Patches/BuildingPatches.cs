using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LuxandraLust
{
    [HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
    public static class Patch_Designator_Build_Visible
    {
        public static void Postfix(Designator_Build __instance, ref bool __result)
        {
            // If the button is already hidden by vanilla logic, don't waste time checking
            if (!__result) return;

            // Check if the thing being built is Luxandra's specific monument
            if (__instance.PlacingDef?.defName == "Luxandra_SacredMonument")
            {
                // Only show the building if Luxandra is the active storyteller
                if (!LuxandraStorytellerCheck.IsActive())
                {
                    __result = false;
                }
            }
        }
    }

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
                IntVec3 targetCell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
                Job job = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

                if (selPawn.jobs.curDriver is Verse.AI.JobDriver_Goto driver)
                {
                    driver.AddFinishAction((Action) =>
                    {
                        if (selPawn.Position.AdjacentTo8WayOrInside(parent.Position) && !selPawn.Downed && !selPawn.Dead)
                        {
                            OpenRootDialogue(selPawn);
                        }
                    });
                }
            });
        }

        // --- STEP 1: THE ROOT WINDOW ---
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

            // Option: Leave
            DiaOption leaveOption = new DiaOption("Leave");
            leaveOption.resolveTree = true;
            rootNode.options.Add(leaveOption);

            ShowDialog(rootNode, "Communing with Luxandra");
        }

        private string DetermineSectionFlavorText(LuxandraIncidentType incidentType)
        {
            switch (incidentType)
            {
                case LuxandraIncidentType.Positive:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a blessing? Some gifts, or maybe something more... exciting?";
                case LuxandraIncidentType.Neutral:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a mundane event? Maybe something to spice your boring days?";
                case LuxandraIncidentType.Negative:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a trial? An exotic threat, for the arousing thrill of danger?";
                case LuxandraIncidentType.Raid:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a threat to your colony? Reckless bravery, or another chance to acquire more subjects for your perversions?";
                case LuxandraIncidentType.Quest:
                    return "<color=#D4AF37>Luxandra</color>:\n\nYou would request a mission? A demand of sort, to test your abilities?";
                default: // This should never show up but here just in case
                    return "Luxandra's gaze is drawn to the threads of fate, each shimmering with potential.";
            }
        }

        // --- STEP 2: THE INCIDENT SELECTION WINDOW ---
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
                        incidentOption.action = () => OpenConfirmationDialogue(pawn, luxIncident, cost);
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
        private void OpenConfirmationDialogue(Pawn pawn, LuxandraIncidentDefs luxIncident, int cost)
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
}