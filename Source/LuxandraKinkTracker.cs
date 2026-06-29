using RimWorld;
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
                    flavorText = "Luxandra thinks that nature has a lot to offer. Expecially huge cocks beyond human sizes, soft pussyes with unfamiliar textures, and exciting knot action to be bound by.";
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
            }

            return $"Current Obsession: {phaseName}\n\n{flavorText}";
        }
    }
}