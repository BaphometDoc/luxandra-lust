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
        // This will throw a harmless error if the player doesn't have Biotech
        // EDIT: Might actually not be necessary, if I didn't fuck up the one below should also catch the vanilla one now.
        //[HarmonyPatch("RimWorld.PregnancyUtility", "DoBirth")]
        //[HarmonyPostfix]
        //public static void BiotechBirthPostfix(Pawn mother, Pawn baby)
        //{
        //    LuxandraDebugActions.DebugLogMessage($"Intercepted Biotech birth event. Mother: {mother.NameShortColored}, Baby: {baby.NameShortColored}");
        //    if (baby == null || mother?.Map == null) return;

        //    if (mother.Faction != null && (mother.Faction.IsPlayer || mother.IsPrisonerOfColony || mother.IsSlaveOfColony))
        //    {
        //        ExecutePreceptBirthJudgement(mother, baby.GetFather());
        //    }
        //}

        // ==========================================
        // RJW POSTFIX
        // ==========================================
        [HarmonyPatch(typeof(Hediff_BasePregnancy), nameof(Hediff_BasePregnancy.PostBirth))]
        public static class RJW_Patch_PostBirth
        {
            public static void Postfix(Hediff_BasePregnancy __instance, Pawn mother, Pawn father, Pawn baby)
            {
                LuxandraDebugActions.DebugLogMessage($"Intercepted birth event. Mother: {mother.NameShortColored}, Father: {father?.NameShortColored}, Baby: {baby.NameShortColored}");
                if (mother?.Map == null) return;

                if (mother.Faction != null && (mother.Faction.IsPlayer || mother.IsPrisonerOfColony || mother.IsSlaveOfColony))
                {
                    ExecutePreceptBirthJudgement(mother, father);
                }
            }
        }

        private static void ExecutePreceptBirthJudgement(Pawn mother, Pawn father)
        {
            Map map = mother.Map;

            if (father == null)
            {
                Log.Message("[Luxandra Debug] intercepted a childbirth with null father. Please contact the mod author about it with the details so he can fix this edge case.");
                return;
            }

            // Base header tag for all letters
            string letterBase = "Luxandra's Appraisal";

            // TODO: Whoring part

            // Bestiality checks - Are you being faithful to your feral desires?
            bool isColonyZoophile = DetermineBestialitySupport(map);
            bool childFromZoophilia = mother.IsHumanLike() && father.IsAnimal();

            if (isColonyZoophile)
            {
                if (childFromZoophilia)
                {
                    // Committed bestiality - Luxandra is pleased
                    ApplyPleasureBlessing(map, mother, father,
                        $"{letterBase}: Feral Devotion",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn. By embracing the wild, beastial lineage of the father, your colony has proven true to its carnal doctrine. She rewards your raw fidelity.");
                }
                else
                {
                    // Did not commit bestiality - Luxandra is angry
                    TriggerManhunterPunishment(map,
                        $"{letterBase}: Domestic Defiance",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn and finds it lacking. You have defaulted to sterile, civilized normalcy. To break your domestic complacency, she commands the wild to correct your path.");
                }
                return;
            }

            // Rapist check - Are you forcing others to please you?
            bool isColonyRapist = DetermineRapistSupport(map);
            bool isMotherPlayerOwned = mother.Faction.IsPlayer;
            bool isFatherPlayerOwned = father != null && father.Faction.IsPlayer;
            bool childFromRape = !(isMotherPlayerOwned && isFatherPlayerOwned);

            if (isColonyRapist)
            {
                if (childFromRape && !childFromZoophilia)
                {
                    // The child was forced - Luxandra is pleased
                    ApplyPleasureBlessing(map, mother, father,
                        $"{letterBase}: Sovereign Dominion",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn. The product of absolute subjection and forceful conquest validates your creed of total authority. Your dominance is rewarded.");
                }
                else
                {
                    // The child was agreed between colonists - Luxandra is angry
                    TriggerIncidentPunishment(map, "Luxandra_Inc_DeviantHordeRaid",
                        $"{letterBase}: Insipid Consent",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn and rejects it. Your colonists chose soft, mutual compromise over absolute dominion. She sends a horde of true deviants to ruthlessly remind what your duty was.");
                }
                return;
            }

            // Lawful good check - Are you being faithful to your colonists?
            if (!isColonyZoophile && !isColonyRapist)
            {
                if (childFromZoophilia)
                {
                    // The child was from zoophilia - Luxandra is angry
                    ApplyDegradationPunishment(map, "Luxandra_BestialDegradation",
                        $"{letterBase}: Primal Contamination",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn with profound disdain. An animalistic violation has stained your community. A heavy fog descends upon your minds, dragging your consciousness down to match the beast you so eagerly defiled.");
                }
                else if (childFromRape)
                {
                    // The child was not from zoophilia but was forced - Luxandra is still angry
                    TriggerIncidentPunishment(map, "Luxandra_Inc_WhiteRain",
                        $"{letterBase}: Fractured Order",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn. A violent, undisciplined acts has fractured your civilized vows. She unleashes the White Rain to show you what it truly means to lose control over your instincts.");
                }
                else
                {
                    // The child was agreed between colonists - Luxandra is pleased
                    ApplyPleasureBlessing(map, mother, father,
                        $"{letterBase}: Sanctified Union",
                        $"Luxandra has appraised the birth of {mother.LabelShort}'s newborn. Your commitment to your community's continuity is recognized. She blesses your dedication to a pure and disciplined order.");
                }
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
            if (mother != null && !mother.Dead) mother.health.AddHediff(parentalBlessing);
            if (father != null && !father.Dead && father.IsHumanLike() && father.Map == map)
            {
                father.health.AddHediff(parentalBlessing);
            }
        }

        private static void ApplyDegradationPunishment(Map map, string hediffDefName, string title, string text)
        {
            Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.NegativeEvent);
            HediffDef punishmentHediff = DefDatabase<HediffDef>.GetNamed(hediffDefName, false);

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
                Log.Error($"[LuxandraLust] Could not find incident definition named: {incidentDefName}");
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
    }
}