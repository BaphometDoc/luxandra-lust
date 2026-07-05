using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class IncidentWorker_IntimateInfestation : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            if (!LuxandraEventCheck.IsEnabled(LuxandraIncidentDefOf.Luxandra_Inc_IntimateInfestation.defName))
                return false;

            Map map = (Map)parms.target;

            // At least 3 colonists must be available
            var colonistsAvailable = map.mapPawns.FreeAdultColonistsSpawned.Where(p => p.RaceProps.Humanlike && !p.Downed && !p.Dead && LuxandraUtilities.IsAdult(p));
            return colonistsAvailable.Any() && colonistsAvailable.Count() > 2;
        }

        #region Utility methods

        private PawnKindDef GenerateRandomInsectDef()
        {
            var maxRoll = ModsConfig.OdysseyActive ? 4 : 3;
            var roll = Rand.RangeInclusive(1, maxRoll);

            switch (roll)
            {
                default:
                case 1:
                    return PawnKindDefOf.Spelopede;
                case 2:
                    return PawnKindDefOf.Megascarab;
                case 3:
                    return PawnKindDefOf.Megaspider;
                case 4:
                    return PawnKindDefOf.Locust;
            }
        }

        private static bool IsPawnValidForInfestation(Pawn pawn)
        {
            LuxandraDebugActions.DebugLogMessage($"Examining if {pawn.NameShortColored} can be infested.");
            // Valid pawn
            if (pawn == null) return false;
            // Humanlike, adult, alive
            if (!pawn.RaceProps.Humanlike || !LuxandraUtilities.IsAdult(pawn) || pawn.Dead) return false;
            if (pawn.gender == Gender.Female)
            {
                if (LuxandraUtilities.IsPregnant(pawn))
                {
                    LuxandraDebugActions.DebugLogMessage($"Pregnant woman: checking if anus.");
                    return pawn.GetAnuses().Any();
                }
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"Non pregnant woman: checking if vagina.");
                    return pawn.GetVaginas().Any();
                }
            }
            else
            {
                LuxandraDebugActions.DebugLogMessage($"Male: checking if anus.");
                return pawn.GetAnuses().Any();
            }
        }

        private BodyPartRecord GetAppropriateBodyPart(Pawn pawn)
        {
            LuxandraDebugActions.DebugLogMessage($"Determining what bodypart to infest for {pawn.NameShortColored}.");
            if (pawn?.RaceProps?.body?.AllParts == null) return null;
            BodyPartRecord bodyPart = null;

            // Females: if not pregnant, get their vagina, else their anus
            if (pawn.gender == Gender.Female)
            {
                var anuses = pawn.GetAnuses();
                if (LuxandraUtilities.IsPregnant(pawn))
                {
                    LuxandraDebugActions.DebugLogMessage($"Pregnant woman: checking if anus.");
                    if (anuses.Any())
                        bodyPart = anuses.FirstOrDefault().BodyPart;
                }
                else
                {
                    LuxandraDebugActions.DebugLogMessage($"Non pregnant woman: checking if vagina.");
                    var vaginas = pawn.GetVaginas();
                    if (vaginas.Any())
                        bodyPart = vaginas.FirstOrDefault().BodyPart;
                    else if (anuses.Any())
                        bodyPart = anuses.FirstOrDefault().BodyPart;
                }
            }
            // Males: their anus
            else
            {
                LuxandraDebugActions.DebugLogMessage($"Male: checking if anus.");
                var anuses = pawn.GetAnuses();
                if (anuses.Any())
                    bodyPart = anuses.FirstOrDefault().BodyPart;
            }

            // Fallback, if somehow no vagina nor anus, the stomach
            if (bodyPart == null)
            {
                LuxandraDebugActions.DebugLogMessage($"Nothing valid was found, looking for stomach.");
                var stomach = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def.defName == "Stomach");
            }

            return bodyPart;
        }

        #endregion

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            List<Pawn> allAdults = map.mapPawns.FreeAdultColonistsSpawned
                .Where(p => IsPawnValidForInfestation(p))
                .ToList();

            List<Pawn> slaves = map.mapPawns.SlavesOfColonySpawned
                .Where(p => IsPawnValidForInfestation(p))
                .ToList();

            if (slaves.Count > 0)
                allAdults.AddRange(slaves);

            int totalAdultCount = allAdults.Count;
            if (totalAdultCount == 0 || totalAdultCount < 3) return false;

            // 1 pawn for every 5 adults
            int targetsToAffect = totalAdultCount / 5;
            if (targetsToAffect < 1) targetsToAffect = 1;

            // Filter pawns so they have a valid vagina or anus.

            // Hierarchical prioritization: Female Colonists -> Female Slaves -> Men
            List<Pawn> prioritizedPool = allAdults.OrderBy(p =>
            {
                if (p.gender == Gender.Female && p.IsColonist && !p.IsSlave) return 0;
                if (p.gender == Gender.Female && p.IsSlave) return 1;
                if (p.gender == Gender.Male) return 2;
                return 3;
            }).ToList();

            if (prioritizedPool.Count == 0)
            {
                LuxandraDebugActions.DebugLogMessage($"No valid pawn was found.");
                return false;
            }
            var victim = prioritizedPool.First();

            // Pick the insect to spawn
            PawnKindDef chosenInsect = GenerateRandomInsectDef();
            List<Pawn> bugs = new List<Pawn>();

            var validEggs = HediffComp_Ovipositor.PossibleEggs(chosenInsect.defName);

            if (!validEggs.Any())
            {
                LuxandraDebugActions.DebugLogMessage($"Failed to find valid eggs for {chosenInsect.defName} for the Intimate infestation");
                return false;
            }

            LuxandraDebugActions.DebugLogMessage($"Egg found for {chosenInsect.defName}.");

            // Try to find a valid spawn point near one of the targets
            if (!CellFinder.TryFindRandomCellNear(
                victim.Position,
                map,
                2,
                x => x.Walkable(map) && !x.Fogged(map) && !x.Roofed(map),
                out IntVec3 spawnCell))
            {
                // Safe vanilla fallback: if they are deep inside a fully enclosed bunker, 
                // we bypass roof checks and just force the spawn nearby.
                if (!CellFinder.TryFindRandomCellNear(
                    victim.Position,
                    map,
                    6,
                    x => x.Walkable(map) && !x.Fogged(map),
                    out spawnCell))
                {
                    spawnCell = victim.Position; // Absolute ultimate fallback directly on the target's tile
                }
            }

            // Spawn the actual bugs
            Faction insectFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Insect);
            PawnGenerationRequest request = new PawnGenerationRequest(kind: chosenInsect, fixedGender: Gender.Female, faction: insectFaction);
            Pawn originalBug = PawnGenerator.GeneratePawn(request);
            GenSpawn.Spawn(originalBug, spawnCell, map);
            bugs.Add(originalBug);

            // TODO: Sound

            // Spawn a few locusts or megascarabs
            for (int i = 0; i < 3; i++)
            {
                PawnKindDef helperInsectsDefs = PawnKindDefOf.Megascarab; ;
                if (ModsConfig.OdysseyActive)
                    helperInsectsDefs = PawnKindDefOf.Locust;

                PawnGenerationRequest requestHelpers = new PawnGenerationRequest(kind: helperInsectsDefs, fixedGender: Gender.Male, faction: insectFaction);
                Pawn helperBug = PawnGenerator.GeneratePawn(requestHelpers);
                GenSpawn.Spawn(helperBug, spawnCell, map);
                bugs.Add(helperBug);
            }

            // Exhaust the insects and tell them to ""defend"" their target
            if (bugs.Count > 0)
            {
                foreach (var bug in bugs)
                {
                    bug.mindState.enemyTarget = victim;

                    var debilitatingHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_ImplantationInducedLethargy", false);

                    if (debilitatingHediff != null)
                        bug.health.AddHediff(debilitatingHediff);
                }
            }

            var comp = new HediffComp_Ovipositor();

            LuxandraDebugActions.DebugLogMessage($"Attempting to infest {victim.NameShortColored} for the Intimate infestation");
            BodyPartRecord targetPart = GetAppropriateBodyPart(victim);
            LuxandraDebugActions.DebugLogMessage($"Bodypart targetted: ${targetPart.def}");

            // Immobilize the victim
            HediffDef cocoon = DefDatabase<HediffDef>.GetNamed("RJW_Cocoon", false);
            if (cocoon != null)
                victim.health.AddHediff(cocoon);

            // Give the victim a moodlet as well
            ThoughtDef customMoodlet = ThoughtDef.Named("Luxandra_InsectoidAssaultMood");
            if (LuxandraUtilities.IsMasochist(victim))
                customMoodlet = ThoughtDef.Named("Luxandra_InsectoidAssaultMoodMasochist");

            if (customMoodlet != null && victim.needs?.mood?.thoughts?.memories != null)
            {
                victim.needs.mood.thoughts.memories.TryGainMemory(customMoodlet);
            }

            string letterText = $"Out of nowhere, several insects burst out of the ground and assaulted {victim.Name}! " +
                               $"The insects have spun a cocoon on them immobilizing them, and proceeded to assault their orifices." +
                               "\n\nThe insects look exhausted and harmless, but the victim needs immediate help.";

            if (targetPart != null)
            {
                // Prepare the hediff
                HediffDef_InsectEgg egg = validEggs.First();
                LuxandraDebugActions.DebugLogMessage($"Attempting to implant {egg.defName} in {victim.NameShortColored}'s {targetPart.def}");
                egg.eggsize = 0.5f;
                egg.selffertilized = true;
                egg.parentDef = chosenInsect.defName;
                egg.childrenDefs.Add(chosenInsect.defName);

                int eggAmount = Rand.RangeInclusive(1, 5);
                int eggsSuccessfullyImplanted = 0;

                // Implant
                for (int i = 0; i < eggAmount; i++)
                {
                    var eggImplanted = AddEggHediff(egg, victim, targetPart, chosenInsect, insectFaction, originalBug);
                    if (eggImplanted != null)
                        eggsSuccessfullyImplanted++;
                }

                // Report result
                if (eggsSuccessfullyImplanted != 0)
                {
                    letterText = letterText + " And probably some medical attention for what horrible things the insects did to them.";
                }
            }

            Find.LetterStack.ReceiveLetter(
                "Insectoid Infestation Outbreak",
                letterText,
                LetterDefOf.NegativeEvent,
                prioritizedPool.FirstOrDefault()
            );


            if (CurrentKink == StorytellerKink.Implantation || CurrentKink == StorytellerKink.Bestiality)
            {
                Messages.Message($"Luxandra saw {victim.NameShortColored} being ravaged by the insectoids and loved it. She gifts you 5 Favor.", MessageTypeDefOf.NeutralEvent);
                GameComponent_LuxandraLust.Instance?.AddToFavorCounter(5);

            }
            return true;
        }

        private Hediff_InsectEgg AddEggHediff(HediffDef_InsectEgg egg, Pawn victim, BodyPartRecord targetPart, PawnKindDef chosenInsect, Faction insectFaction, Pawn originalBug)
        {
            var addedEgg = victim.health.AddHediff(egg, targetPart) as Hediff_InsectEgg;
            //addedEgg.InitImplanter(props.pawn);
            if (addedEgg != null)
            {
                float p_end_tick_mods = victim.RaceProps.gestationPeriodDays * GenDate.TicksPerDay;

                PawnGenerationRequest request = new PawnGenerationRequest(kind: chosenInsect, fixedGender: Gender.Female, faction: insectFaction);
                Pawn childbug = PawnGenerator.GeneratePawn(request);

                addedEgg.father = originalBug;
                addedEgg.implanter = originalBug;
                addedEgg.p_start_tick = Find.TickManager.TicksGame;
                addedEgg.p_end_tick = addedEgg.p_start_tick + (p_end_tick_mods / 10); // Eggs will hatch faster than normal
                addedEgg.lastTick = addedEgg.p_start_tick;
                LuxandraDebugActions.DebugLogMessage($"Implant successful.");
                addedEgg.Severity = 0.1f;

                return addedEgg;
            }
            else return null;
        }
    }
}