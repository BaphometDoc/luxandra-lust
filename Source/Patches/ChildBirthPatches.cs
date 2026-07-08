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

            bool childFromProstitution = LuxandraModChecks.IsBrothelColonyActive() && isMotherWhore && !isFatherPlayerOwned;
            bool childFromZoophilia = mother.IsHumanLike() && father.IsAnimal();
            bool childFromRape = !(isMotherPlayerOwned && isFatherPlayerOwned) && !isMotherWhore;

            bool isColonyRepopulating = DetermineRepopulationSupport(map);
            bool isColonyZoophile = DetermineBestialitySupport(map);
            bool isColonyRapist = DetermineRapistSupport(map);

            // ===============================================================================
            //                      Logic for blessings and punishment.
            // ===============================================================================

            // Is the human child from an animal mother and the setting is enabled
            if (mother.IsAnimal() && baby.IsHumanLike() && LuxandraModSettings.trackChildbirthAppraisalForAnimals)
            {
                if (isColonyZoophile)
                {
                    ApplyPleasureBlessing(map, mother, father,
                         "Luxandra_Letter_FeralDevotion_Title".Translate(),
                         "Luxandra_Letter_FeralDevotion_AnimalMother_Desc".Translate(mother.LabelShortCap));
                    return;
                }
                else
                {
                    ApplyDegradationPunishment(map,
                          "Luxandra_Letter_PrimalContamination_Title".Translate(),
                          "Luxandra_Letter_PrimalContamination_Desc".Translate(mother.LabelShortCap));
                    return;
                }
            }

            // Is the child from animals
            if (childFromZoophilia)
            {
                if (isColonyZoophile)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_FeralDevotion_Title".Translate(),
                        "Luxandra_Letter_FeralDevotion_HumanMother_Desc".Translate(mother.LabelShort));
                    return;
                }
                else
                {
                    ApplyDegradationPunishment(map,
                        "Luxandra_Letter_PrimalContamination_Title".Translate(),
                        "Luxandra_Letter_PrimalContamination_HumanMother_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // Colony is zoophile and the child was a normal child
            if (isColonyZoophile && !childFromZoophilia)
            {
                TriggerManhunterPunishment(map,
                             "Luxandra_Letter_DomesticDefiance_Title".Translate(),
                             "Luxandra_Letter_DomesticDefiance_Desc".Translate(mother.LabelShort));
                return;
            }

            // Is the child from a whore
            if (isMotherWhore)
            {
                // Colony is repopulationist and the child is from prostitution
                if (isColonyRepopulating && childFromProstitution)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_CommercialFertility_Title".Translate(),
                        "Luxandra_Letter_CommercialFertility_Desc".Translate(mother.LabelShort));
                    return;
                }
                // Colony is repopulationist and the child is not from prostitution
                else if (isColonyRepopulating)
                {
                    TriggerIncidentPunishment(map, LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid.defName,
                        "Luxandra_Letter_InsularHoarding_Title".Translate(),
                        "Luxandra_Letter_InsularHoarding_Desc".Translate(mother.LabelShort));
                    return;
                }
                // Child was from prostitution, and colony is not repopulationist
                else if (childFromProstitution)
                {
                    TriggerIncidentPunishment(map, LuxandraIncidentDefOf.Luxandra_Inc_AphrodisiacFever.defName,
                        "Luxandra_Letter_UntrackedContagion_Title".Translate(),
                        "Luxandra_Letter_UntrackedContagion_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // The child was from rape
            if (childFromRape)
            {
                if (isColonyRapist)
                {
                    ApplyPleasureBlessing(map, mother, father,
                        "Luxandra_Letter_SovereignDominion_Title".Translate(),
                        "Luxandra_Letter_SovereignDominion_Desc".Translate(mother.LabelShort));
                    return;
                }
                else
                {
                    TriggerIncidentPunishment(map, LuxandraIncidentDefOf.Luxandra_Inc_WhiteRain.defName,
                        "Luxandra_Letter_FracturedOrder_Title".Translate(),
                        "Luxandra_Letter_FracturedOrder_Desc".Translate(mother.LabelShort));
                    return;
                }
            }

            // Colony is rapist, but the child was agreed between colonists
            if (isColonyRapist && !childFromRape)
            {
                TriggerIncidentPunishment(map, LuxandraIncidentDefOf.Luxandra_Inc_DeviantHordeRaid.defName,
                     "Luxandra_Letter_InsipidConsent_Title".Translate(),
                     "Luxandra_Letter_InsipidConsent_Desc".Translate(mother.LabelShort));
                return;
            }

            // The colony has no special affinity and the child was agreed between colonists
            ApplyPleasureBlessing(map, mother, father,
                "Luxandra_Letter_SanctifiedUnion_Title".Translate(),
                "Luxandra_Letter_SanctifiedUnion_Desc".Translate(mother.LabelShort));
            return;
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
            if (mother != null && !mother.Dead && mother.IsHumanLike()) mother.health.AddHediff(parentalBlessing);
            if (father != null && !father.Dead && father.IsHumanLike() && father.Map == map)
            {
                father.health.AddHediff(parentalBlessing);
            }

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

        private static void TriggerManhunterPunishment(Map map, string title, string text)
        {
            IncidentDef manhunter = IncidentDefOf.ManhunterPack;
            if (manhunter == null) return;

            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.ThreatBig);

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(manhunter.category, map);
            parms.target = map;

            parms.points = StorytellerUtility.DefaultThreatPointsNow(map);

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

            incident.Worker.TryExecute(parms);
        }


        // ==========================================
        // UTILITIES
        // ==========================================

        private static bool DetermineBestialitySupport(Map map)
        {
            // Ideology - tecnically this would require sexperience-ideology, but those are going to
            // just be false if it's not loaded anyway. Manages to catch similar mods as well if someone
            // is degenerate enough
            if (ModsConfig.IdeologyActive)
            {
                MemeDef zoophileMeme = DefDatabase<MemeDef>.GetNamed("Zoophile", false);
                PreceptDef bestialityAccepted = DefDatabase<PreceptDef>.GetNamed("Bestiality_Acceptable", false);
                PreceptDef bestialityVenerated = DefDatabase<PreceptDef>.GetNamed("Bestiality_OnlyVenerated", false);
                PreceptDef bestialityBonded = DefDatabase<PreceptDef>.GetNamed("Bestiality_BondOnly", false);
                PreceptDef bestialityHonorable = DefDatabase<PreceptDef>.GetNamed("Bestiality_Honorable", false);

                if (LuxandraUtilities.PlayerFactionHasMeme(zoophileMeme) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityAccepted) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityVenerated) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityBonded) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(bestialityHonorable))
                {
                    return true;
                }
            }

            var localColonists = map.mapPawns.FreeColonists;

            // Biotech - same as above but for RJW genes
            if (ModsConfig.BiotechActive)
            {
                GeneDef zoophileGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_zoophile", false);
                int zoophileGeneColonists = LuxandraUtilities.CountColonistsWithGeneOnMap(map, zoophileGene);

                if (zoophileGeneColonists > localColonists.Count * 2 / 3)
                    return true;
            }

            // Traits

            TraitDef zoophileTrait = DefDatabase<TraitDef>.GetNamed("Zoophile", false);
            int zoophileColonists = LuxandraUtilities.CountColonistsWithTraitOnMap(map, zoophileTrait);

            if (zoophileColonists > localColonists.Count * 2 / 3)
                return true;

            return false;
        }

        private static bool DetermineRapistSupport(Map map)
        {
            // Ideology - tecnically this would require sexperience-ideology, but those are going to
            // just be false if it's not loaded anyway. Manages to catch similar mods as well if someone
            // is degenerate enough
            if (ModsConfig.IdeologyActive)
            {
                MemeDef rapistMeme = DefDatabase<MemeDef>.GetNamed("Rapist", false);
                PreceptDef rapeAccepted = DefDatabase<PreceptDef>.GetNamed("Rape_Acceptable", false);
                PreceptDef rapeHonorable = DefDatabase<PreceptDef>.GetNamed("Rape_Honorable", false);

                if (LuxandraUtilities.PlayerFactionHasMeme(rapistMeme) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(rapeAccepted) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(rapeHonorable))
                {
                    return true;
                }
            }

            var localColonists = map.mapPawns.FreeColonists;

            // Biotech - same as above but for RJW genes
            if (ModsConfig.BiotechActive)
            {
                GeneDef rapistGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_rapist", false);
                int rapistGeneColonists = LuxandraUtilities.CountColonistsWithGeneOnMap(map, rapistGene);

                if (rapistGeneColonists > localColonists.Count * 2 / 3)
                    return true;
            }

            // Traits

            TraitDef rapistTrait = DefDatabase<TraitDef>.GetNamed("Rapist", false);
            int rapistColonists = LuxandraUtilities.CountColonistsWithTraitOnMap(map, rapistTrait);

            if (rapistColonists > localColonists.Count * 2 / 3)
                return true;

            return false;
        }

        // Brothel Colony Repopulation - Player has the meme and one of Kindness, Greed or Duty
        private static bool DetermineRepopulationSupport(Map map)
        {
            // Ideology + Brothel Colony
            if (ModsConfig.IdeologyActive && LuxandraModChecks.IsBrothelColonyActive())
            {
                MemeDef repopulationMeme = DefDatabase<MemeDef>.GetNamed("CB_Repopulationist", false);
                PreceptDef repopulationKindness = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Kindness", false);
                PreceptDef repopulationGreed = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Greed", false);
                PreceptDef repopulationDuty = DefDatabase<PreceptDef>.GetNamed("CB_Repopulation_Duty", false);

                if (LuxandraUtilities.PlayerFactionHasMeme(repopulationMeme) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(repopulationKindness) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(repopulationGreed) ||
                   LuxandraUtilities.PlayerFactionHasPrecept(repopulationDuty))
                {
                    return true;
                }
            }

            var localColonists = map.mapPawns.FreeColonists;

            // Biotech - same as above but for RJW genes
            if (ModsConfig.BiotechActive)
            {
                GeneDef zoophileGene = DefDatabase<GeneDef>.GetNamed("rjw_genes_zoophile", false);
                int zoophileGeneColonists = LuxandraUtilities.CountColonistsWithGeneOnMap(map, zoophileGene);

                if (zoophileGeneColonists > localColonists.Count * 2 / 3)
                    return true;
            }

            // Traits

            TraitDef zoophileTrait = DefDatabase<TraitDef>.GetNamed("Zoophile", false);
            int zoophileColonists = LuxandraUtilities.CountColonistsWithTraitOnMap(map, zoophileTrait);

            if (zoophileColonists > localColonists.Count * 2 / 3)
                return true;

            return false;
        }
    }
}