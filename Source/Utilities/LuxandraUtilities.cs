using RimWorld;
using rjw;
using rjw.Modules.Interactions;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LuxandraLust
{
    // Various utilities
    public static class LuxandraUtilities
    {
        /// <summary>
        /// Extracts all incident defs from a the sexual incident collections
        /// </summary>
        public static List<IncidentDef> ExtractIncidentsFromCollection(IEnumerable<LuxandraIncidentDefs> incidentCollection)
        {
            if (incidentCollection == null || incidentCollection.Count() == 0)
                return new List<IncidentDef>();

            return incidentCollection.Select(i => i.IncidentDef).ToList();
        }

        /// <summary>
        /// Determines the average sex need of all the adult colonists and slaves in the colony
        /// </summary>
        public static float GetAverageColonySexNeed(Map map)
        {
            if (map == null) return 0.5f; // Safe fallback

            var eligiblePawns = map.mapPawns.AllPawnsSpawned.Where(p =>
                p.RaceProps != null && p.RaceProps.Humanlike && !p.Dead &&
                (p.IsColonist || p.IsSlave) &&
                LuxandraUtilities.IsAdult(p)
            );

            float totalSexNeed = 0f;
            int countWithNeed = 0;

            foreach (Pawn pawn in eligiblePawns)
            {
                var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                if (sexNeed != null)
                {
                    totalSexNeed += sexNeed.CurLevelPercentage;
                    countWithNeed++;
                }
            }

            // Divide by countwithneed rather than actual total of pawns to avoid pawns with no sex need (es, Androids)
            return countWithNeed > 0 ? (totalSexNeed / countWithNeed) : 0.5f;
        }

        /// <summary>
        /// Determines if there's at least 2 player controlled conscious adults on the map
        /// </summary>
        public static bool HasMultipleAdultColonists(Map map)
        {
            if (map == null) return false;

            int freeAdultCount = map.mapPawns.FreeColonistsSpawned.Count(pawn =>
                !pawn.Dead &&
                IsAdult(pawn)
            );

            int slaveAdultCount = map.mapPawns.SlavesOfColonySpawned.Count(pawn =>
                !pawn.Dead &&
                IsAdult(pawn)
            );

            return freeAdultCount + slaveAdultCount > 1;
        }

        /// <summary>
        /// Enlarges a sex part, within a certain limit
        /// </summary>
        public static bool EnlargeSexPart(Pawn pawn, List<RJWLewdablePart> sexParts)
        {
            if (pawn == null || pawn.Dead || sexParts.EnumerableNullOrEmpty()) return false;

            bool anyChanged = false;

            foreach (var part in sexParts)
            {
                if (part?.SexPart is Hediff_NaturalSexPart naturalPart)
                {
                    float currentSeverity = naturalPart.Severity;
                    float changeAmount = 0.5f;

                    // Calculates adjustments trying to not exceed the severity limits
                    float newSeverity = UnityEngine.Mathf.Min(currentSeverity + changeAmount, 3.0f);

                    if (newSeverity != currentSeverity)
                    {
                        naturalPart.Severity = newSeverity;

                        var comp = part.SexPart.GetPartComp();
                        comp?.SetSeverity(newSeverity, sync: false);

                        anyChanged = true;
                        LuxandraDebugActions.DebugLogMessage($"Increased {part.SexPart.Def.defName} size for {pawn.NameShortColored}.");
                    }
                }
            }

            return anyChanged;
        }

        /// <summary>
        /// Forces a RandomRape mental state on the specified pawn
        /// </summary>
        public static void ForceRapistBreak(Pawn pawn, string reasonString, bool tankSexNeed = false)
        {
            if (IsAdult(pawn) == false)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} is not adult.");
                return;
            }

            if (pawn?.mindState?.mentalStateHandler == null)
            {
                Log.Warning($"[Luxandra Debug] ForceRapistBreak aborted: {pawn.NameShortColored} or mentalStateHandler is null.");
                return;
            }

            MentalStateDef rjwBreakDef = DefDatabase<MentalStateDef>.GetNamed("RandomRape", errorOnFail: false);

            if (rjwBreakDef == null)
            {
                Log.Warning("[Luxandra Debug] Could not trigger Rape mental break: RJW 'RandomRape' MentalStateDef was not found in the game database.");
            }
            else
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(
                    rjwBreakDef,
                    reason: reasonString,
                    forced: true,
                    forceWake: true // Ensures the pawn snaps out of bed instantly to execute the event
                );

                // If enabled, set their sex need to 0 to ensure they are ready to act on the mental state
                if (tankSexNeed && pawn.needs != null)
                {
                    var sexNeed = pawn.needs.TryGetNeed<Need_Sex>();
                    if (sexNeed != null)
                    {
                        sexNeed.CurLevel = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the pawn is adult (or youth if RJW has the check enabled)
        /// </summary>
        public static bool IsAdult(Pawn pawn)
        {
            if (pawn == null)
                return false;

            var allowYouth = rjw.RJWSettings.AllowYouthSex;

            AgeCategory ageCategory = pawn.GetAgeCategory();

            if (ageCategory == AgeCategory.Adult)
                return true;
            else if (ageCategory == AgeCategory.Youth && allowYouth)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Checks if the pawn is pregnant. Should catch modded pregnancies too
        /// </summary>
        public static bool IsPregnant(Pawn pawn)
        {
            if (pawn.health?.hediffSet == null) return false;

            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h?.def == null) continue;

                string name = h.def.defName.ToLower();
                // Catches Vanilla, Biotech, RJW, and most common pregnancy mod variants
                if (name.Contains("pregnant") || name.Contains("pregnancy"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Counts how many living free colonists on the specified map possess a specific Trait.
        /// </summary>
        public static int CountColonistsWithTraitOnMap(Map map, TraitDef traitDef)
        {
            if (map == null || traitDef == null) return 0;

            int count = 0;
            var localColonists = map.mapPawns.FreeColonists;

            for (int i = 0; i < localColonists.Count; i++)
            {
                Pawn pawn = localColonists[i];
                if (pawn != null && !pawn.Dead && pawn.story?.traits?.HasTrait(traitDef) == true)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Counts how many living free colonists on the specified map possess a specific Gene.
        /// Returns 0 if the Biotech DLC is not active.
        /// </summary>
        public static int CountColonistsWithGeneOnMap(Map map, GeneDef geneDef)
        {
            if (!ModsConfig.BiotechActive || map == null || geneDef == null) return 0;

            int count = 0;
            var localColonists = map.mapPawns.FreeColonists;

            for (int i = 0; i < localColonists.Count; i++)
            {
                Pawn pawn = localColonists[i];
                if (pawn != null && !pawn.Dead && pawn.genes?.HasActiveGene(geneDef) == true)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Checks if the player's primary Ideology possesses a specific Precept.
        /// Returns false if the Ideology DLC is not active.
        /// </summary>
        public static bool PlayerFactionHasPrecept(PreceptDef preceptDef)
        {
            if (!ModsConfig.IdeologyActive || preceptDef == null) return false;

            Ideo playerIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (playerIdeo == null) return false;

            var precepts = playerIdeo.PreceptsListForReading;
            for (int i = 0; i < precepts.Count; i++)
            {
                if (precepts[i].def == preceptDef)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the player's primary Ideology possesses a specific Meme.
        /// Returns false if the Ideology DLC is not active.
        /// </summary>
        public static bool PlayerFactionHasMeme(MemeDef memeDef)
        {
            // Guard clause: If Ideology isn't active or def is null, bypass completely
            if (!ModsConfig.IdeologyActive || memeDef == null) return false;

            // Get the primary ideology of the player faction
            Ideo playerIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
            if (playerIdeo == null) return false;

            // Use RimWorld's built-in rapid list tracker method to check for the meme
            return playerIdeo.HasMeme(memeDef);
        }
    }

    // This class tracks the selected storyteller
    public static class LuxandraStorytellerCheck
    {
        /// <summary>
        /// Verifies that Luxandra is active
        /// </summary>
        public static bool IsActive()
        {
            return Find.Storyteller?.def.defName == "LuxandraLust";
        }
    }
}