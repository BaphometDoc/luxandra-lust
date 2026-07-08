using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class Comp_LuxandraMonument : ThingComp
    {
        private GameComponent_LuxandraLust LuxandraComp => GameComponent_LuxandraLust.Instance;
        public Dictionary<StorytellerKink, Graphic> graphicCache = new Dictionary<StorytellerKink, Graphic>();

        /// <summary>
        /// Lets the statue know the kink is changed so must change look
        /// </summary>
        public void Notify_KinkChanged()
        {
            if (this.parent == null) return;

            // Clear RimWorld's internal cached graphic reference for this specific building instance
            // This forces it to evaluate the Graphic property getter fresh next frame
            this.parent.Notify_ColorChanged();

            // Tell the map renderer that the pixels on this tile are dirty and must be redrawn
            if (this.parent.Spawned)
            {
                this.parent.Map.mapDrawer.MapMeshDirty(this.parent.Position, MapMeshFlagDefOf.Things);

                // TODO Visual flare to hide the sudden snap that I need to get working at some point
                //MoteMaker.MakeStaticMote(this.parent.Position, this.parent.Map, ThingDefOf.Mote_PowerBeam);
            }
        }

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

        #region Cum management

        /// <summary>
        /// Finds the offering attachment
        /// </summary>
        private Thing GetOfferingBuilding()
        {
            CompAffectedByFacilities facilityComp = this.parent.GetComp<CompAffectedByFacilities>();
            if (facilityComp == null)
            {
                LuxandraDebugActions.DebugLogMessage("Failed to find altar comps. Impossible to calculate cum stored.");
                return null;
            }

            List<Thing> linkedBuildings = facilityComp.LinkedFacilitiesListForReading;
            if (linkedBuildings == null || linkedBuildings.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage("No altar connected. Impossible to calculate cum stored.");
                return null;
            }

            // Currently only a single offering building can be linked
            return linkedBuildings[0];
        }

        /// <summary>
        /// Finds the attached tribute pedestal, checks its current sacrifice amount, and drains a specified portion.
        /// </summary>
        public bool TryConsumeTribute(Thing building, float amountToDrain)
        {
            //if (building.def.defName == "Luxandra_TributePedestal")
            //{
            // Grab the fuel system component
            CompRefuelable fuelComp = building.TryGetComp<CompRefuelable>();
            if (fuelComp != null)
            {
                // Current level check (e.g., how much total fluid is stored)
                float currentTributeLevel = fuelComp.Fuel;

                // Verify if there is enough to fulfill the blessing cost
                if (currentTributeLevel >= amountToDrain)
                {
                    // "Drain" the item count out of existence
                    fuelComp.ConsumeFuel(amountToDrain);

                    // Throw a nice visual text notification over the pedestal showing the drain
                    MoteMaker.ThrowText(building.DrawPos, building.Map, $"-{amountToDrain} Cum", 3f);

                    return true; // Success!
                }
            }
            // }

            return false; // Not enough tribute found, or pedestal missing
        }

        public float CheckCurrentAvailableCum(Thing building)
        {
            // Grab the fuel system component
            CompRefuelable fuelComp = building.TryGetComp<CompRefuelable>();
            if (fuelComp != null)
                return fuelComp.Fuel;

            return 0f;
        }
        #endregion

        #region The root window
        private void OpenRootDialogue(Pawn pawn)
        {
            if (LuxandraComp == null) return;

            decimal currentFavor = LuxandraComp.colonyFavorPoints;
            string text = $"{pawn.LabelShort} kneels before the monument, opening their mind to Luxandra's influence.\n\n" +
                          $"Current Favor: {currentFavor} points.";

            DiaNode rootNode = new DiaNode(text);

            // Option: Request a blessing (open list)
            DiaOption prayOption = new DiaOption("Pray for a blessing.");
            prayOption.action = () => OpenBlessingSelectionDialogue(pawn);
            rootNode.options.Add(prayOption);

            // Option: Request a event (open list)
            DiaOption eventOption = new DiaOption("Request a event.");
            eventOption.action = () => OpenIncidentSelectionDialogue(pawn);
            rootNode.options.Add(eventOption);

            // Option: Request more people (open list)
            DiaOption peopleOption = new DiaOption("Request more people.");
            peopleOption.action = () => OpenPeopleRequestDialogue(pawn);
            rootNode.options.Add(peopleOption);

            var tributeBuilding = GetOfferingBuilding();
            if (tributeBuilding != null)
            {
                // Option: Offer a tribute (open list)
                DiaOption tributeOption = new DiaOption("Offer a tribute.");
                tributeOption.action = () => OfferTributeDialogue(pawn, tributeBuilding);
                rootNode.options.Add(tributeOption);
            }

            // Option: Leave
            DiaOption leaveOption = new DiaOption("Leave.");
            leaveOption.resolveTree = true;
            rootNode.options.Add(leaveOption);

            ShowDialog(rootNode, "Communing with Luxandra");
        }
        #endregion

        #region Cum Tribute
        private void OfferTributeDialogue(Pawn pawn, Thing tributeBuilding)
        {
            DiaNode rootSelectionNode = new DiaNode("<color=#D4AF37>Luxandra</color>:\n\nYou want to offer me a tribute? Material offerings, to prove your devotion?");

            DiaOption cumForFavorOption = new DiaOption("(Gain Favor) I will offer our fertile fluids to gain your divine favor.");

            var fluidTotal = CheckCurrentAvailableCum(tributeBuilding);
            if (fluidTotal < 100)
            {
                cumForFavorOption.Disable($"\nRequires at least {100} Cum (You have {fluidTotal}).");
            }
            cumForFavorOption.action = () => OpenCumForFavorDialogue(pawn, tributeBuilding);
            rootSelectionNode.options.Add(cumForFavorOption);

            DiaOption noOption = new DiaOption("No, choose something else.");
            noOption.action = () => OpenRootDialogue(pawn);
            rootSelectionNode.options.Add(noOption);

            ShowDialog(rootSelectionNode, $"Offer a tribute");
        }

        private void OpenCumForFavorDialogue(Pawn pawn, Thing tributeBuilding)
        {
            int fluidTotalBase100 = (int)(CheckCurrentAvailableCum(tributeBuilding) / 100) * 100;
            int favorGranted = fluidTotalBase100 / 10;
            string text = $"Offering {fluidTotalBase100} will grant you {favorGranted} Favor.\n\nYou would have {LuxandraComp.colonyFavorPoints + favorGranted} after the offering.";

            DiaNode confirmNode = new DiaNode(text);

            if (fluidTotalBase100 >= 200)
            {
                DiaOption offer100CumOption = new DiaOption("Offer 100 Cum for 10 Favor.");

                offer100CumOption.action = () =>
                {
                    TryConsumeTribute(tributeBuilding, 100);
                    LuxandraComp.AddToFavorCounter(10);
                    Messages.Message($"Luxandra accepts your offering and blesses you with {10} Favor. New total: {LuxandraComp.colonyFavorPoints}", MessageTypeDefOf.NeutralEvent, false);
                };

                offer100CumOption.resolveTree = true; // This finishes the interaction entirely
                confirmNode.options.Add(offer100CumOption);
            }

            DiaOption allOfItOption = new DiaOption($"Offer all the Cum she will take ({fluidTotalBase100} for {favorGranted} Favor).");

            allOfItOption.action = () =>
            {
                TryConsumeTribute(tributeBuilding, favorGranted * 10);
                LuxandraComp.AddToFavorCounter(favorGranted);
                Messages.Message($"Luxandra accepts your offering and blesses you with {favorGranted} Favor. New total: {LuxandraComp.colonyFavorPoints}", MessageTypeDefOf.NeutralEvent, false);
            };

            allOfItOption.resolveTree = true; // This finishes the interaction entirely
            confirmNode.options.Add(allOfItOption);

            DiaOption noOption = new DiaOption("No, choose something else.");
            noOption.action = () => OpenRequestSexSlaveDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, $"Offer Cum for Favor");
        }
        #endregion

        #region Request Blessings
        private void OpenBlessingSelectionDialogue(Pawn pawn)
        {
            DiaNode rootSelectionNode = new DiaNode($"<color=#D4AF37>Luxandra</color>:\n\nYou request my aid? Are you facing dire challenges, or are you just feeling lonely?");

            bool enableHealing = true;
            HealthUtility.TryGetWorstHealthCondition(pawn, out var hediff, out var bodyPart);
            if (hediff == null && bodyPart == null)
                enableHealing = false;

            DiaOption healingOption = new DiaOption("(50 Favor) Request healing.");
            if (LuxandraComp.colonyFavorPoints < 50)
            {
                healingOption.Disable($"Requires {50} Favor (You have {LuxandraComp.colonyFavorPoints}).");
            }
            else if (enableHealing == false)
            {
                healingOption.Disable($"{pawn.NameShortColored} is not injured.");
            }
            healingOption.action = () => OpenHealingConfirmationDialogue(pawn);
            rootSelectionNode.options.Add(healingOption);

            // Add a back button inside the sub-menu to people request section
            DiaOption subMenuBack = new DiaOption("No, choose something else.");
            subMenuBack.action = () => OpenPeopleRequestDialogue(pawn);
            rootSelectionNode.options.Add(subMenuBack);

            ShowDialog(rootSelectionNode, "Pray for a blessing");
        }

        private void OpenHealingConfirmationDialogue(Pawn pawn)
        {
            HealthUtility.TryGetWorstHealthCondition(pawn, out var hediff, out var bodyPart);
            string injuryName = hediff != null ? hediff.Label : bodyPart != null ? bodyPart.Label : "";
            string text = $"Are you sure you want to spend 50 Favor to request healing {injuryName} for {pawn.NameFullColored}?\n\nYou would have {LuxandraComp.colonyFavorPoints - 50} left after the request.";

            DiaNode confirmNode = new DiaNode(text);

            DiaOption yesOption = new DiaOption("Yes, request it.");

            yesOption.action = () =>
            {
                TaggedString taggedString = HealthUtility.FixWorstHealthCondition(pawn);

                // 5. Add a nice thematic feedback message and sound effect
                SoundDefOf.MechSerumUsed.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));

                MoteMaker.MakeStaticMote(pawn.Position, pawn.Map, ThingDefOf.Mote_Bestow);

                LuxandraComp.PayForLuxandraServices(50);
                Messages.Message($"{pawn.NameShortColored} has received Luxandra's healing embrace. Their {injuryName} is cured in exchange for 50 Favor.", pawn, MessageTypeDefOf.PositiveEvent);
            };

            yesOption.resolveTree = true; // This finishes the interaction entirely
            confirmNode.options.Add(yesOption);

            DiaOption noOption = new DiaOption("No, choose something else.");
            noOption.action = () => OpenBlessingSelectionDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, $"Request healing");
        }
        #endregion

        #region Request people

        private void OpenPeopleRequestDialogue(Pawn pawn)
        {
            if (LuxandraComp == null) return;
            string text = $"\"<color=#D4AF37>Luxandra</color>:\n\nYou require more people? Are you feeling lonely, or do you have more... spicy plans?\"";

            DiaNode rootSelectionNode = new DiaNode(text);

            // Option: Request a sex slave
            DiaOption prayOption = new DiaOption("Request a obedient servant.");
            prayOption.action = () => OpenRequestSexSlaveDialogue(pawn);
            rootSelectionNode.options.Add(prayOption);

            if (ModsConfig.IdeologyActive)
            {
                // Option: Summon a slaver (open list)
                DiaOption slaverOption = new DiaOption("Request the visit of a slaver.");
                slaverOption.action = () => OpenRequestSlaverDialogue(pawn);
                rootSelectionNode.options.Add(slaverOption);
            }

            // Option: Leave
            // Add a back button inside the sub-menu to return to our main category listing
            DiaOption subMenuBack = new DiaOption("No, choose something else.");
            subMenuBack.action = () => OpenRootDialogue(pawn);
            rootSelectionNode.options.Add(subMenuBack);

            ShowDialog(rootSelectionNode, "Request more people");
        }

        #region Sex Slave Request
        private void OpenRequestSexSlaveDialogue(Pawn pawn)
        {
            DiaNode rootSelectionNode = new DiaNode("<color=#D4AF37>Luxandra</color>:\n\nYou require an obedient thrall? Oh my, you naughty person, I wonder what <i>incredible things</i> you have in store for them...");

            DiaOption maleSlaveOption = new DiaOption("(50 Favor) Yes, I would like a male servant.");
            if (LuxandraComp.colonyFavorPoints < 50)
            {
                maleSlaveOption.Disable($"Requires {50} Favor (You have {LuxandraComp.colonyFavorPoints}).");
            }
            maleSlaveOption.action = () => OpenSexSlaveConfirmationDialogue(pawn, Gender.Male);
            rootSelectionNode.options.Add(maleSlaveOption);

            DiaOption femaleSlaveOption = new DiaOption("(50 Favor) Yes, I would like a female servant.");
            if (LuxandraComp.colonyFavorPoints < 50)
            {
                femaleSlaveOption.Disable($"Requires {50} Favor (You have {LuxandraComp.colonyFavorPoints}).");
            }
            femaleSlaveOption.action = () => OpenSexSlaveConfirmationDialogue(pawn, Gender.Female);
            rootSelectionNode.options.Add(femaleSlaveOption);

            // Add a back button inside the sub-menu to people request section
            DiaOption subMenuBack = new DiaOption("No, choose something else.");
            subMenuBack.action = () => OpenPeopleRequestDialogue(pawn);
            rootSelectionNode.options.Add(subMenuBack);

            ShowDialog(rootSelectionNode, "Request a servant");
        }

        private void OpenSexSlaveConfirmationDialogue(Pawn pawn, Gender gender)
        {
            string genderText = gender == Gender.Male ? "male" : "female";
            string text = $"Are you sure you want to spend 50 Favor to request a obedient {genderText} servant?\n\nYou would have {LuxandraComp.colonyFavorPoints - 50} left after the request.";

            DiaNode confirmNode = new DiaNode(text);

            DiaOption yesOption = new DiaOption("Yes, request it.");

            yesOption.action = () =>
            {
                // 1. Point to the specific gendered PawnKindDef you set up in XML
                string targetDefName = (gender == Gender.Male)
                    ? "Luxandra_Submissive_Male"
                    : "Luxandra_Submissive_Female";

                PawnKindDef submissiveKind = DefDatabase<PawnKindDef>.GetNamed(targetDefName, false);

                if (submissiveKind == null)
                {
                    Log.Error($"[Luxandra] Failed to execute request: {targetDefName} could not be found in the database.");
                    return;
                }

                var map = pawn.Map ?? Find.CurrentMap;

                LuxandraDebugActions.DebugLogMessage($"Attempting to generate {gender} sex slave.");

                // 2. Draft the generation parameters. 
                // Setting the faction to OfPlayer ensures they join the colony instantly on landing.
                PawnGenerationRequest request = new PawnGenerationRequest(
                    submissiveKind,
                    faction: null,
                    context: PawnGenerationContext.NonPlayer,
                    forceGenerateNewPawn: true
                );

                // Generate the pawn, purge his gear, and stun them
                Pawn generatedPawn = PawnGenerator.GeneratePawn(request);
                generatedPawn.apparel?.DestroyAll();

                // Clear any health issue the pawn may have. Only the best for our followers
                for (int i = generatedPawn.health.hediffSet.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = generatedPawn.health.hediffSet.hediffs[i];

                    // Identify chronic diseases, injuries, addictions, and toxicities
                    if (h is Hediff_Injury || h is Hediff_Addiction ||
                        h.def.isBad || h.def.makesSickThought)
                    {
                        // Double check it's not a vital modded added part
                        if (!(h is Hediff_AddedPart))
                        {
                            generatedPawn.health.RemoveHediff(h);
                        }
                    }
                }

                // Apply Anesthetic (Whole body/Null part)
                HediffDef anestheticDef = HediffDefOf.Anesthetic;
                if (anestheticDef != null)
                    generatedPawn.health.AddHediff(anestheticDef, null, null);

                // Apply Bliss Lobotomy if Anomaly is running
                if (ModsConfig.AnomalyActive)
                {
                    HediffDef blissLobotomyDef = HediffDefOf.BlissLobotomy;
                    if (blissLobotomyDef != null)
                    {
                        LuxandraDebugActions.DebugLogMessage($"Lobotomizing the sex slave.");
                        BodyPartRecord brain = generatedPawn.health.hediffSet.GetBrain();
                        if (brain != null)
                            generatedPawn.health.AddHediff(blissLobotomyDef, brain, null);
                    }
                }

                // Define the drop cell and drop it
                if (!DropCellFinder.TryFindDropSpotNear(pawn.Position, map, out IntVec3 dropSpot, false, false, false))
                {
                    dropSpot = DropCellFinder.RandomDropSpot(map);
                }

                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAdd(generatedPawn);
                DropPodUtility.MakeDropPodAt(dropSpot, map, activeTransporterInfo);

                // With sexperience they're always virgin even if for any reason they wouldn't be
                if (LuxandraModChecks.IsSexperienceActive())
                {
                    TraitDef virginTraitDef = DefDatabase<TraitDef>.GetNamedSilentFail("Virgin");
                    if (virginTraitDef != null)
                    {
                        // Add the trait if they don't have it
                        if (!generatedPawn.story.traits.HasTrait(virginTraitDef))
                        {
                            Trait virginTrait = new Trait(virginTraitDef, 0, forced: true);
                            generatedPawn.story.traits.GainTrait(virginTrait);
                        }
                    }
                }

                LuxandraComp.PayForLuxandraServices(50);
                Messages.Message($"Your request to Luxandra consumed 50 Favor to summon a {genderText} slave.", MessageTypeDefOf.TaskCompletion, false);
            };

            yesOption.resolveTree = true; // This finishes the interaction entirely
            confirmNode.options.Add(yesOption);

            DiaOption noOption = new DiaOption("No, choose something else.");
            noOption.action = () => OpenRequestSexSlaveDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, $"Confirm {genderText} thrall request");
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

            // Add a back button inside the sub-menu to people request section
            DiaOption subMenuBack = new DiaOption("No, choose something else.");
            subMenuBack.action = () => OpenPeopleRequestDialogue(pawn);
            rootSelectionNode.options.Add(subMenuBack);

            ShowDialog(rootSelectionNode, "Request the visit of a slaver");
        }

        private void OpenSlaverConfirmationDialogue(Pawn pawn)
        {
            string text = $"Are you sure you want to spend 10 Favor to request the visit a Slave Trader?\n\nYou would have {LuxandraComp.colonyFavorPoints - 10} left after the request." +
                           "\n\n\n\n<i>Dev note: Summoning merchants can occasionally fail due to Rimworld innate issues. It is recommended to save the game if you don't want to risk to occasionally waste the points without any trader showing up.</i>";

            DiaNode confirmNode = new DiaNode(text);

            DiaOption yesOption = new DiaOption("Yes, request it.");

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

            DiaOption noOption = new DiaOption("No, choose something else.");
            noOption.action = () => OpenRequestSlaverDialogue(pawn);
            confirmNode.options.Add(noOption);

            ShowDialog(confirmNode, "Confirm Slave Trader request");
        }

        #endregion

        #endregion Request people

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

                // Create the main menu option that links to this sub-node
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

            DiaOption noOption = new DiaOption("No, choose something else.");
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
            // Let vanilla components draw their own gizmos first (like deconstruction or minifying)
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            // Make sure component actually exists before drawing the button
            if (LuxandraComp != null)
            {
                // Create a standard command button
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