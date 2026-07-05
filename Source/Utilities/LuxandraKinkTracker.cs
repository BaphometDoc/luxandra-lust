using RimWorld;
using rjw;
using System.Collections.Generic;
using Verse;
using static LuxandraLust.GameComponent_LuxandraLust;

namespace LuxandraLust
{
    public class LuxandraKinkTracker : Alert
    {
        public LuxandraKinkTracker()
        {
            this.defaultLabel = "Luxandra's current kink: None";

            this.defaultPriority = AlertPriority.Medium;
        }

        public override AlertReport GetReport()
        {
            // If Luxandra is not active, immediately hide the alert from the sidebar
            if (!LuxandraStorytellerCheck.IsActive())
                return false;

            // Otherwise always display the alert
            return true;
        }
        public override string GetLabel()
        {
            string phaseName = CurrentKink.ToString();
            return $"Luxandra's current kink: {phaseName}";
        }

        public static void TriggerKinkShift()
        {
            // Roll a new random phase
            RerollKink();

            // Roll a random day counter between 1 and 7 days
            int randomDays = Rand.RangeInclusive(1, 7);

            // Convert days to internal ticks (1 day = 60,000 ticks)
            GameComponent_LuxandraCyclicEvents.Instance.ticksUntilKinkChange = randomDays * GenDate.TicksPerDay;

            if (LuxandraModSettings.enableLogging)
            {
                LuxandraDebugActions.DebugLogMessage($"Storyteller shifted phase to {CurrentKink}. Next shift scheduled in {randomDays} days ({GameComponent_LuxandraCyclicEvents.Instance.ticksUntilKinkChange} ticks).");
            }
        }

        public override TaggedString GetExplanation()
        {
            string phaseName = CurrentKink.ToString();
            string flavorText = "The storyteller's current obsession. Will you try to give her what she wants to watch?";

            switch (CurrentKink)
            {
                case StorytellerKink.None:
                default:
                    flavorText = "Luxandra is enjoying watching whatever comes by. Everything is good to our goddess, as long as it's sex.";
                    break;
                case StorytellerKink.Pregnancy:
                    flavorText = "Luxandra loves watching raw vaginal sex, as well as seeing already pregnant women still embark in lewd activities despite the new life they carry in them.";
                    break;
                case StorytellerKink.Anal:
                    flavorText = "Luxandra is enjoying watching sexy backdoor access. Fearless raw anal penetration and fisting truly reward who can stand the heat.";
                    break;
                case StorytellerKink.Oral:
                    flavorText = "Luxandra wants to see some good head action. Masterful tongue strokes, gentle sucking, and warm fluid exchanges, she craves it all.";
                    break;
                case StorytellerKink.Bestiality:
                    flavorText = "Luxandra thinks that nature has a lot to offer. Huge cocks beyond human sizes, soft pussies with unfamiliar textures, and exciting knot action to be bound by.";
                    break;
                case StorytellerKink.Rape:
                    flavorText = "Luxandra is bored of consensuality. She wants to see people taken by force. She loves when the other person cannot say no...";
                    break;
                case StorytellerKink.Masturbation:
                    flavorText = "Luxandra often enjoys some alone time, and wants to see people do the same. After all, you know how to please yourself better than anyone else.";
                    break;
                case StorytellerKink.Necrophilia:
                    flavorText = "Luxandra loves when something is stiff. Or moist. Why use sex toys when corpses always provide a new, unexpected sensation?";
                    break;
                case StorytellerKink.Gay:
                    flavorText = "Luxandra loves manliness, but loves even more when two hot men join in a sensual union of bodies. The muscles, the sweat, the cum, that is what she lives for.";
                    break;
                case StorytellerKink.Lesbian:
                    flavorText = "Luxandra craves seeing other women together. Without the limits of male genitals, women can embrace into virtually infinite exchanges, with their soft breasts pressed against each other in a marvelous view.";
                    break;
                case StorytellerKink.Cum:
                    flavorText = "Luxandra craves the warm, sticky product of male pleasure. Anything that involves making a man cum will be pure ecstasy for her.";
                    break;
                case StorytellerKink.Breasts:
                    flavorText = "Luxandra enjoys looking the curves of other women. Small, big or gargantuan, all are proud symbols of fertility, and strong instruments of seduction.";
                    break;
                case StorytellerKink.Incest:
                    flavorText = "Luxandra loves the forbidden. She wants to see people who are not supposed to be together, together. The more taboo, the better.";
                    break;
                case StorytellerKink.Implantation:
                    flavorText = "Luxandra has seen parasitic insectoids use humans as seed bed and found it arousing. She wants to see more of them, witness eggs implanted into people and watch those unsettling births come toghether.";
                    break;
                case StorytellerKink.Futa:
                    flavorText = "Luxandra loves the best of both worlds. She enjoys seeing women with massive dicks, and men with enormous breasts hiding their vagina behind their throbbing cocks. The combination of both worlds is a true delight for her.";
                    break;
            }

            return $"Current Obsession: {phaseName}\n\n{flavorText}";
        }

        /// <summary>
        /// Gets the kinks that are enabled based on the RJW settings and potential submods
        /// </summary>
        public static List<StorytellerKink> GetEnabledKinks()
        {
            List<StorytellerKink> validKinks = new List<StorytellerKink>();

            validKinks.Add(StorytellerKink.None);

            validKinks.Add(StorytellerKink.Pregnancy);
            validKinks.Add(StorytellerKink.Anal);
            validKinks.Add(StorytellerKink.Oral);
            validKinks.Add(StorytellerKink.Masturbation);
            validKinks.Add(StorytellerKink.Gay);
            validKinks.Add(StorytellerKink.Lesbian);
            validKinks.Add(StorytellerKink.Cum);
            validKinks.Add(StorytellerKink.Breasts);

            if (IsBestialityEnabled())
                validKinks.Add(StorytellerKink.Bestiality);

            if (IsRapeEnabled())
                validKinks.Add(StorytellerKink.Rape);

            if (IsNecrophiliaEnabled())
                validKinks.Add(StorytellerKink.Necrophilia);

            if (IsImplantationEnabled())
                validKinks.Add(StorytellerKink.Implantation);

            if (IsIncestEnabled())
                validKinks.Add(StorytellerKink.Incest);

            if (IsFutaEnabled())
                validKinks.Add(StorytellerKink.Futa);

            return validKinks;
        }


        /// <summary>
        /// Rerolls Luxandra's current kink (optionally specify which to get)
        /// </summary>
        public static void RerollKink(bool forceSpecific = false, StorytellerKink forcedType = StorytellerKink.None)
        {
            if (forceSpecific)
                CurrentKink = forcedType;

            var validKinks = GetEnabledKinks();

            CurrentKink = validKinks.RandomElement();

            if (CurrentKink != StorytellerKink.None)
                Messages.Message($"Luxandra's whims have shifted: She is now into {CurrentKink}.", MessageTypeDefOf.CautionInput, false);
            else
                Messages.Message($"Luxandra's whims have shifted: She is not into anything specific at the moment.", MessageTypeDefOf.CautionInput, false);
        }

        /// <summary>
        /// Valid condition: RJW bestiality option enabled
        /// </summary>
        public static bool IsBestialityEnabled()
        {
            if (RJWSettings.bestiality_enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: RJW rape option enabled
        /// </summary>
        public static bool IsRapeEnabled()
        {
            if (RJWSettings.rape_enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: RJW necrophilia option enabled
        /// </summary>
        public static bool IsNecrophiliaEnabled()
        {
            if (RJWSettings.necrophilia_enabled)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: any RJW egg pregnancy option enabled or RJW Insects loaded
        /// </summary>
        public static bool IsImplantationEnabled()
        {
            if (RJWPregnancySettings.insect_pregnancy_enabled || RJWPregnancySettings.insect_anal_pregnancy_enabled ||
                RJWPregnancySettings.insect_oral_pregnancy_enabled || LuxandraModChecks.IsRJWInsectsActive())
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: option enabled or Sexperience-Ideology loaded
        /// </summary>
        public static bool IsIncestEnabled()
        {
            if (LuxandraModChecks.IsSexperienceIdeologyActive() || LuxandraModSettings.removeRomanceRestrictions)
                return true;

            return false;
        }

        /// <summary>
        /// Valid condition: any RJW futa spawning option enabled
        /// </summary>
        public static bool IsFutaEnabled()
        {
            if (RJWSettings.futa_natives_chance > 0 || RJWSettings.futa_nymph_chance > 0 || RJWSettings.futa_spacers_chance > 0 ||
                RJWSettings.FemaleFuta || RJWSettings.GenderlessAsFuta)
                return true;

            return false;
        }

    }
}