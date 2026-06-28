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
                case StorytellerKinkPhase.None:
                default:
                    flavorText = "Luxandra is enjoying watching whatever comes by. Everything is good to our goddess, as long as it's sexual.";
                    break;
                case StorytellerKinkPhase.Pregnancy:
                    flavorText = "Luxandra loves watching raw vaginal sex, as well as seeing already pregnant women still embark in lewd activities.";
                    break;
                case StorytellerKinkPhase.Anal:
                    flavorText = "Luxandra is enjoying watching sexy backdoor access. Fearless raw anal penetration and fisting truly reward who can stand the heat.";
                    break;
                case StorytellerKinkPhase.Oral:
                    flavorText = "Luxandra wants to see some good head action. Masterful tongue strokes, gentle sucking, and warm fluid exchanges, she wants it all.";
                    break;
                case StorytellerKinkPhase.Bestiality:
                    flavorText = "Luxandra thinks that nature has a lot to offer. Expecially huge cocks beyond human ones, soft pussyes with unfamiliar textures, and exciting knot action to be bound by.";
                    break;
                case StorytellerKinkPhase.Rape:
                    flavorText = "Luxandra is bored of consensuality. She wants to see people taken by force. She loves when the other person cannot say no...";
                    break;
                case StorytellerKinkPhase.Masturbation:
                    flavorText = "Luxandra often enjoys some alone time, and wants to see people do the same. After all, only you know how to best please yourself.";
                    break;
                case StorytellerKinkPhase.Necrophilia:
                    flavorText = "Luxandra loves when something is stiff. Or moist. Why use sex toys when corpses always provide a new sensation?";
                    break;
                case StorytellerKinkPhase.Gay:
                    flavorText = "Luxandra loves manliness, but loves even more when two hot men join in a sensual union of bodies. The muscles, the sweat, the cum, that is what she lives for.";
                    break;
                case StorytellerKinkPhase.Lesbian:
                    flavorText = "Luxandra craves to see other women toghether. Without the limits of male genitals, women can embrace into virtually infinite exchanges, with their soft breasts bouncing toghether all the time in a marvelous symphony.";
                    break;
                case StorytellerKinkPhase.Cum:
                    flavorText = "Luxandra craves the warm, sticky product of male pleasure. Anything that involves making a man cum will be pure ecstasy for her.";
                    break;
                case StorytellerKinkPhase.Breasts:
                    flavorText = "Luxandra enjoys looking the curves of other women. Small, big or gargantuan, all are proud symbols of fertility, and strong instruments of seduction.";
                    break;
            }

            return $"Current Obsession: {phaseName}\n\n{flavorText}";
        }
    }
}