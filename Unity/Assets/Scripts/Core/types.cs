using static Core.Copy;

namespace Core.Types
{
    /*public class Emotion : Copyable<Emotion>
    {
        public double Pos { get; set; }
        public double Neg { get; set; }
        public double Val { get; set; }
        public double Arousal { get; set; }
        public double Surprise { get; set; }
        public double Uncertainty { get; set; }

        public Emotion(double pos = 0, double neg = 0, double arousal = 0, double surprise = 0, double uncertainty = 0)
        {
            this.Pos = pos;
            this.Neg = neg;
            Val = pos - neg;
            this.Arousal = arousal;
            this.Surprise = surprise;
            this.Uncertainty = uncertainty;
        }

        public Emotion Copy() => new(Pos, Neg, Arousal, Surprise, Uncertainty);
        public static Emotion operator +(Emotion a, Emotion b) => new(a.Pos + b.Pos, a.Neg + b.Neg, a.Arousal + b.Arousal, a.Surprise + b.Surprise, a.Uncertainty + b.Uncertainty);
        public static Emotion operator *(Emotion a, double b) => new(a.Pos * b, a.Neg * b, a.Arousal * b, a.Surprise * b, a.Uncertainty * b);
        public static Emotion operator /(Emotion a, double b) => a * (1 / b);
    }

    public class EmotionSystem : Copyable<EmotionSystem>
    {
        public Emotion Felt { get; set; } // real emotion
        public Emotion VoluntaryPhysiological { get; set; } // not used for the moment, we don't yet have an example of voluntary physiological emotion
        public Emotion Physiological { get; set; } // (unvoluntary)
        public Emotion VoluntaryFacial { get; set; } // ex : fake smile
        public Emotion Facial { get; set; } // (unvoluntary)

        public EmotionSystem()
        {
            Felt = new Emotion();
            VoluntaryFacial = new Emotion();
            Facial = new Emotion();
            VoluntaryPhysiological = new Emotion();
            Physiological = new Emotion();
        }
        public EmotionSystem Copy() => new() { Felt = Felt.Copy(), VoluntaryPhysiological = VoluntaryPhysiological.Copy(), Physiological = Physiological.Copy(), VoluntaryFacial = VoluntaryFacial.Copy(), Facial = Facial.Copy() };
    }*/

    public enum ActionType
    {
        Walk,
        Rotate,
        Grab,
        LetGo,
        Smile,
        Grimace,
        Idle,
        Stare
    }
}