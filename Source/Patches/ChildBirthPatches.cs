using HarmonyLib;
using RimWorld;
using rjw;
using System.Collections.Generic;
using Verse;

namespace LuxandraLust
{
    /// Holds all childbirth hooks for easy browsing. 
    public static class ChildbirthEventPatches
    {
        // ==========================================
        // BIOTECH / VANILLA POSTFIX 
        // ==========================================        
        // This will throw a harmless error if the player doesn't have Biotech (who doesn't have Biotech nowadays?)        
        [HarmonyPatch("RimWorld.PregnancyUtility", "DoBirth")]
        [HarmonyPostfix]
        public static void BiotechBirthPostfix(Pawn mother, Pawn baby)
        {
            if (!ShouldExecute(mother)) return;

            LuxandraDebugActions.DebugLogMessage($"Intercepted Biotech birth event. Mother: {mother.NameShortColored}, Baby: {baby.NameShortColored}");

            if (mother.Faction != null && (mother.Faction.IsPlayer || mother.IsPrisonerOfColony || mother.IsSlaveOfColony))
            {
                ExecutePreceptBirthJudgement(mother, baby.GetFather(), baby);
            }
        }

        // ==========================================
        // RJW POSTFIX
        // ==========================================
        [HarmonyPatch(typeof(Hediff_BasePregnancy), nameof(Hediff_BasePregnancy.PostBirth))]
        public static class RJW_Patch_PostBirth
        {
            public static void Postfix(Hediff_BasePregnancy __instance, Pawn mother, Pawn father, Pawn baby)
            {
                if (!ShouldExecute(mother)) return;

                LuxandraDebugActions.DebugLogMessage($"Intercepted birth event. Mother: {mother.NameShortColored}, Father: {father?.NameShortColored}, Baby: {baby.NameShortColored}");

                if (mother.Faction != null && (mother.Faction.IsPlayer || mother.IsPrisonerOfColony || mother.IsSlaveOfColony))
                {
                    ExecutePreceptBirthJudgement(mother, father, baby);
                }
            }
        }

        private static bool ShouldExecute(Pawn mother)
        {
            // Only enable for Luxandra
            if (!LuxandraStorytellerCheck.IsActive()) return false;

            // Don't run the postfix if the mother is invalid or if the map is null 
            if (mother == null || mother?.Map == null) return false;

            // Don't run the postfix if the mother is an animal and the player has disabled tracking for animals
            if (mother.IsAnimal() && !LuxandraModSettings.trackChildbirthAppraisalForAnimals) return false;

            // Finally check if the setting is enabled in first place
            return LuxandraModSettings.enableChildbirthAppraisal;
        }

        private static void ExecutePreceptBirthJudgement(Pawn mother, Pawn father, Pawn baby)
        {
            Map map = mother.Map;

            if (father == null)
            {
                Log.Message("[Luxandra Debug] intercepted a childbirth with null father. Please contact the mod author about it with the details so he can fix this edge case.");
                return;
            }

            bool isMotherPlayerOwned = mother.Faction.IsPlayer || mother.IsSlaveOfColony;
            bool isMotherWhore = rjw.xxx.is_whore(mother);
            bool isFatherPlayerOwned = father != null && (father.Faction.IsPlayer || father.IsSlaveOfColony);

            bool childFromZoophilia = (mother.IsHumanLike() && father.IsAnimal()) || (mother.IsAnimal() && baby.IsHumanLike() && LuxandraModSettings.trackChildbirthAppraisalForAnimals);
            bool childFromRape = !(isMotherPlayerOwned && isFatherPlayerOwned) && !isMotherWhore;
            bool childFromProstitution = LuxandraModChecks.IsBrothelColonyActive() && isMotherWhore && !isFatherPlayerOwned;
            bool childFromIncest = LuxandraUtilities.ArePawnsBloodRelated(mother, father);

            DepravitySupportLevel bestialitySupport = LuxandraUtilities.DetermineBestialitySupport(map);
            DepravitySupportLevel rapeSupport = LuxandraUtilities.DetermineRapistSupport(map);
            DepravitySupportLevel repopulationSupport = LuxandraUtilities.DetermineRepopulationSupport(map);
            DepravitySupportLevel incestSupport = LuxandraUtilities.DetermineIncestSupport(map);

            // Tracks whether the birth broke any rules or fulfilled requirements
            bool brokeHatedRule = false;
            bool fulfilledAnyRequired = false;
            bool missingARequiredPrecept = false;

            // Determine if any required types are set across the colony
            bool bestialityRequired = bestialitySupport == DepravitySupportLevel.Required;
            bool rapeRequired = rapeSupport == DepravitySupportLevel.Required;
            bool prostitutionRequired = repopulationSupport == DepravitySupportLevel.Required;
            bool incestRequired = incestSupport == DepravitySupportLevel.Required;
            bool anyPreceptRequired = bestialityRequired || rapeRequired || prostitutionRequired || incestRequired;

            // Evaluate Bestiality Precept
            if (bestialitySupport == DepravitySupportLevel.Hated && childFromZoophilia) brokeHatedRule = true;
            if (bestialityRequired && childFromZoophilia) fulfilledAnyRequired = true;

            // Evaluate Rape Precept
            if (rapeSupport == DepravitySupportLevel.Hated && childFromRape) brokeHatedRule = true;
            if (rapeRequired && childFromRape) fulfilledAnyRequired = true;

            // Evaluate Prostitution Precept
            if (repopulationSupport == DepravitySupportLevel.Hated && childFromProstitution) brokeHatedRule = true;
            if (prostitutionRequired && childFromProstitution) fulfilledAnyRequired = true;

            // Evaluate Incest Precept
            if (incestSupport == DepravitySupportLevel.Hated && childFromIncest) brokeHatedRule = true;
            if (incestRequired && childFromIncest) fulfilledAnyRequired = true;

            // If the colony demands specific types of births, but this birth didn't match ANY of them
            if (anyPreceptRequired && !fulfilledAnyRequired)
            {
                missingARequiredPrecept = true;
            }

            // ===============================================================================
            //                                  Logic execution 
            // ===============================================================================

            // PRIORITY 1: Direct Hated Violations
            if (brokeHatedRule)
            {
                if (childFromZoophilia && bestialitySupport == DepravitySupportLevel.Hated)
                {
                    ApplyDegradationPunishment(map,
                        "Luxandra_Letter_PrimalContamination_Title".Translate(),
                        "Luxandra_Letter_PrimalContamination_HumanMother_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromProstitution && repopulationSupport == DepravitySupportLevel.Hated)
                {
                    TriggerIncidentPunishment(map,
                        LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever.defName,
                        "Luxandra_Letter_UntrackedContagion_Title".Translate(),
                        "Luxandra_Letter_UntrackedContagion_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromRape && rapeSupport == DepravitySupportLevel.Hated)
                {
                    TriggerIncidentPunishment(map,
                        LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain.defName,
                        "Luxandra_Letter_FracturedOrder_Title".Translate(),
                        "Luxandra_Letter_FracturedOrder_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromIncest && incestSupport == DepravitySupportLevel.Hated)
                {
                    ApplyBrokenDesirePunishment(map,
                        "Luxandra_Letter_ForbiddenBlood_Title".Translate(),
                        "Luxandra_Letter_ForbiddenBlood_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // PRIORITY 2: Required Fulfillments (Blessings)
            if (fulfilledAnyRequired)
            {
                if (childFromZoophilia && bestialityRequired)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_FeralDevotion_Title".Translate(),
                        "Luxandra_Letter_FeralDevotion_HumanMother_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromProstitution && prostitutionRequired)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_CommercialFertility_Title".Translate(),
                        "Luxandra_Letter_CommercialFertility_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromRape && rapeRequired)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_SovereignDominion_Title".Translate(),
                        "Luxandra_Letter_SovereignDominion_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (childFromIncest && incestRequired)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_SacredLineage_Title".Translate(), //TODO
                        "Luxandra_Letter_SacredLineage_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // PRIORITY 3: Failed Requirements (Punishments for ordinary births when a depravity was required)
            if (missingARequiredPrecept)
            {
                if (bestialityRequired)
                {
                    TriggerManhunterPunishment(map,
                        "Luxandra_Letter_DomesticDefiance_Title".Translate(),
                        "Luxandra_Letter_DomesticDefiance_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (prostitutionRequired)
                {
                    TriggerIncidentPunishment(map,
                        LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid.defName,
                        "Luxandra_Letter_InsularHoarding_Title".Translate(),
                        "Luxandra_Letter_InsularHoarding_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (rapeRequired)
                {
                    TriggerIncidentPunishment(map,
                        LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid.defName,
                        "Luxandra_Letter_InsipidConsent_Title".Translate(),
                        "Luxandra_Letter_InsipidConsent_Desc".Translate(mother.LabelShort));
                    return;
                }
                if (incestRequired)
                {
                    ApplyForcedDesirePunishment(map,
                        "Luxandra_Letter_ExogamousBetrayal_Title".Translate(),
                        "Luxandra_Letter_ExogamousBetrayal_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // PRIORITY 4: Pure Colony Success
            bool isColonyPure = (bestialitySupport == DepravitySupportLevel.Hated &&
                                       rapeSupport == DepravitySupportLevel.Hated &&
                               repopulationSupport == DepravitySupportLevel.Hated &&
                                     incestSupport == DepravitySupportLevel.Hated);

            if (isColonyPure && !brokeHatedRule)
            {
                ApplyPleasureBlessing(map, mother, father, "Luxandra_Letter_SanctifiedUnion_Title".Translate(), "Luxandra_Letter_SanctifiedUnion_Desc".Translate(mother.LabelShort));
                return;
            }
        }

        // ==========================================
        // INCIDENTS
        // ==========================================

        private static void ApplyPleasureBlessing(Map map, Pawn mother, Pawn father, string title, string text)
        {
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, mother);

            HediffDef globalBlessing = DefDatabase<HediffDef>.GetNamed("Luxandra_ColonyPleasureBlessing", false);
            HediffDef parentalBlessing = DefDatabase<HediffDef>.GetNamed("Luxandra_ParentalFertilityBlessing", false);

            List<Pawn> adults = map.mapPawns.FreeColonists;
            for (int i = 0; i < adults.Count; i++)
            {
                Pawn p = adults[i];
                // Strict Filter: Only Adult Colonists (No Slaves, Prisoners, or Babes/Kids)
                if (p != null && !p.Dead && LuxandraUtilities.IsAdult(p) && !p.IsSlave)
                {
                    p.health.AddHediff(globalBlessing);
                }
            }

            // Target Parents for the massive buffs
            if (mother != null && !mother.Dead && mother.IsHumanLike())
                mother.health.AddHediff(parentalBlessing);
            if (father != null && !father.Dead && father.IsHumanLike() && father.Map == map)
                father.health.AddHediff(parentalBlessing);

            string parentNames = mother.NameShortColored;
            if (father != null)
                parentNames = parentNames + " and " + father.NameShortColored;

            Messages.Message($"The actions of {parentNames} have pleased Luxandra. You gained 10 Favor with her.", MessageTypeDefOf.PositiveEvent);
            GameComponent_LuxandraLust.Instance?.AddToFavorCounter(10);
        }

        private static void ApplyDegradationPunishment(Map map, string title, string text)
        {
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent);
            HediffDef punishmentHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_BestialDegradation", false);

            List<Pawn> adults = map.mapPawns.FreeColonists;
            for (int i = 0; i < adults.Count; i++)
            {
                Pawn p = adults[i];
                if (p != null && !p.Dead && p.DevelopmentalStage.Adult() && !p.IsSlave)
                {
                    p.health.AddHediff(punishmentHediff);
                }
            }
        }

        private static void ApplyBrokenDesirePunishment(Map map, string title, string text)
        {
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent);
            HediffDef punishmentHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_ColdRevulsion", false);

            List<Pawn> adults = map.mapPawns.FreeColonists;
            for (int i = 0; i < adults.Count; i++)
            {
                Pawn p = adults[i];
                if (p != null && !p.Dead && p.DevelopmentalStage.Adult() && !p.IsSlave)
                {
                    p.health.AddHediff(punishmentHediff);
                }
            }
        }

        private static void ApplyForcedDesirePunishment(Map map, string title, string text)
        {
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent);
            HediffDef punishmentHediff = DefDatabase<HediffDef>.GetNamed("Luxandra_WhippingGaze", false);

            List<Pawn> adults = map.mapPawns.FreeColonists;
            for (int i = 0; i < adults.Count; i++)
            {
                Pawn p = adults[i];
                if (p != null && !p.Dead && p.DevelopmentalStage.Adult() && !p.IsSlave)
                {
                    p.health.AddHediff(punishmentHediff);
                }
            }
        }

        private static void TriggerManhunterPunishment(Map map, string title, string text)
        {
            IncidentDef manhunter = IncidentDefOf.ManhunterPack;
            if (manhunter == null) return;

            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatBig);

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(manhunter.category, map);
            parms.target = map;

            parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
            parms.forced = true;

            manhunter.Worker.TryExecute(parms);
        }

        private static void TriggerIncidentPunishment(Map map, string incidentDefName, string title, string text)
        {
            IncidentDef incident = DefDatabase<IncidentDef>.GetNamed(incidentDefName, false);
            if (incident == null)
            {
                Log.Error($"[Luxandra Debug] Could not find incident definition named: {incidentDefName}");
                return;
            }

            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatBig);

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
            parms.target = map;
            parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
            parms.forced = true;

            incident.Worker.TryExecute(parms);
        }
    }
}