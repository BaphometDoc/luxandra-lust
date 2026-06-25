using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace LuxandraLust
{
    public class GameCondition_FertilityPulse : GameCondition
    {
        private const int ScanInterval = 2500;

        public override void GameConditionTick()
        {
            base.GameConditionTick();

            if (Find.TickManager.TicksGame % ScanInterval == 0)
            {
                Map map = this.SingleMap;
                if (map != null)
                {
                    ManagePulseEffects(map);
                }
            }
        }

        public override void Init()
        {
            base.Init();

            // Play the low, echoing psychic hum across the map when it triggers
            if (SoundDefOf.PsychicSootheGlobal != null)
            {
                // Finds the primary map affected by this condition and plays it globally for the player
                Map map = this.SingleMap;
                if (map != null)
                {
                    SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(map);
                }
            }
        }

        private void ManagePulseEffects(Map map)
        {
            HediffDef childDef = HediffDef.Named("Luxandra_PulseChildConfusion");
            HediffDef maleDef = HediffDef.Named("Luxandra_PulseAdultMale");
            HediffDef femaleDef = HediffDef.Named("Luxandra_PulseAdultFemale");
            HediffDef pregnantDef = HediffDef.Named("Luxandra_PulsePregnantMaternal");

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || !pawn.RaceProps.Humanlike) continue;

                // Ensure they have the correct hediff depending on age/gender
                if (!LuxandraLustUtilities.IsAdult(pawn))
                {
                    EnsureHediff(pawn, childDef);
                }
                else if (pawn.gender == Gender.Male)
                {
                    EnsureHediff(pawn, maleDef);
                    if (pawn.IsColonist || pawn.IsSlave || pawn.IsPrisoner) ClearFertilitySuppressants(pawn);
                }
                else if (pawn.gender == Gender.Female)
                {
                    if (LuxandraLustUtilities.IsPregnant(pawn))
                    {
                        EnsureHediff(pawn, pregnantDef);

                        // Remove the previous hediff if they get pregnant during the pulse
                        if (pawn.health.hediffSet.HasHediff(femaleDef))
                        {
                            Hediff oldHediff = pawn.health.hediffSet.GetFirstHediffOfDef(femaleDef);
                            pawn.health.RemoveHediff(oldHediff);
                        }
                    }
                    else
                    {
                        EnsureHediff(pawn, femaleDef);
                        if (pawn.IsColonist || pawn.IsSlave || pawn.IsPrisoner) ClearFertilitySuppressants(pawn);

                        // Clean up the pregnant hediff if a pregnancy somehow ended while the pulse is active
                        if (pawn.health.hediffSet.HasHediff(pregnantDef))
                        {
                            Hediff oldPregnantHediff = pawn.health.hediffSet.GetFirstHediffOfDef(pregnantDef);
                            pawn.health.RemoveHediff(oldPregnantHediff);
                        }
                    }
                }
            }
        }

        private void EnsureHediff(Pawn pawn, HediffDef def)
        {
            if (!pawn.health.hediffSet.HasHediff(def))
            {
                pawn.health.AddHediff(def);
            }
        }

        private void ClearFertilitySuppressants(Pawn pawn)
        {
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;

            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                Hediff h = hediffs[i];
                if (h?.def == null) continue;

                // Skip surgeries, implants, or permanent injuries
                if (h is Hediff_AddedPart || h is Hediff_Implant || h.def.isBad) continue;

                // Don't remove the biotech lactating hediff either as that one does tank fertility
                // but we don't want the babies to starve. Modded ones usually do not interfere
                if (ModsConfig.BiotechActive)
                {
                    if (HediffDefOf.Lactating != null && h.def == HediffDefOf.Lactating)
                        continue;
                }

                bool suppressesFertility = false;

                if (h.CurStage != null && EvaluateStageForContraception(h.CurStage))
                {
                    suppressesFertility = true;
                }
                //  Fallback text filter just in case
                else
                {
                    string name = h.def.defName.ToLower();
                    string label = h.def.label?.ToLower() ?? "";

                    if (name.Contains("contraceptive") || name.Contains("birthcontrol") ||
                        name.Contains("anti_fertility") || label.Contains("contraceptive"))
                    {
                        suppressesFertility = true;
                    }
                }

                // If caught, vaporize it from their health tracker
                if (suppressesFertility)
                {
                    pawn.health.RemoveHediff(h);
                    Messages.Message($"{pawn.LabelShort}'s contraceptive protection was faded away due to the fertility pulse!", pawn, MessageTypeDefOf.CautionInput, false);
                }
            }
        }

        private bool EvaluateStageForContraception(HediffStage stage)
        {
            if (stage == null) return false;
            // NOTE: I have to use a word check since RJW stuff targets RJW_Fertility and Biotech targets Fertility
            // and I want to catch both

            if (stage.capMods != null)
            {
                foreach (PawnCapacityModifier capMod in stage.capMods)
                {
                    if (capMod.capacity != null)
                    {
                        string capName = capMod.capacity.defName.ToLower();

                        // If the capacity is a fertility stat and it enforces a maximum cap under 50%
                        if (capName.Contains("fertility") && capMod.setMax < 0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            if (stage.statOffsets != null)
            {
                foreach (StatModifier modifier in stage.statOffsets)
                {
                    if (modifier.stat != null)
                    {
                        string statName = modifier.stat.defName.ToLower();
                        if (statName.Contains("fertility") && modifier.value <= -0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            if (stage.statFactors != null)
            {
                foreach (StatModifier factor in stage.statFactors)
                {
                    if (factor.stat != null)
                    {
                        string statName = factor.stat.defName.ToLower();
                        if (statName.Contains("fertility") && factor.value <= 0.5f)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // Clean up everything when the condition ends
        public override void End()
        {
            Map map = this.SingleMap;
            if (map != null)
            {
                HediffDef childDef = HediffDef.Named("Luxandra_PulseChildConfusion");
                HediffDef maleDef = HediffDef.Named("Luxandra_PulseAdultMale");
                HediffDef femaleDef = HediffDef.Named("Luxandra_PulseAdultFemale");

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn == null || !pawn.RaceProps.Humanlike) continue;

                    Hediff cH = pawn.health.hediffSet.GetFirstHediffOfDef(childDef);
                    Hediff mH = pawn.health.hediffSet.GetFirstHediffOfDef(maleDef);
                    Hediff fH = pawn.health.hediffSet.GetFirstHediffOfDef(femaleDef);

                    if (cH != null) pawn.health.RemoveHediff(cH);
                    if (mH != null) pawn.health.RemoveHediff(mH);
                    if (fH != null) pawn.health.RemoveHediff(fH);
                }
            }
            base.End();
        }
    }
}