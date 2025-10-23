using static PCM.Core.Utils.Copy;

namespace PCM.Core.Types
{
    public class WorldState : ICopyable<WorldState>
    {
        public SceneObjects.ObjectBody[] Positions { get; set; }

        public EmotionSystem[] Emotions { get; set; }

        public Dictionary<int, int> Interactions = new();

        public bool NewTextInput { get; set; }

        public WorldState Copy() => new()
        {
            Emotions = CopyArray(Emotions),
            Positions = CopyArray(Positions),
            Interactions = new Dictionary<int, int>(Interactions)
        };

    }

    public class Emotion : ICopyable<Emotion>
    {
        public double Pos { get; set; }
        public double Neg { get; set; }
        public double Val { get; set; }
        public double Arousal { get; set; }
        public double Surprise { get; set; }
        public double Uncertainty { get; set; }

        public Emotion(double pos = 0, double neg = 0, double arousal = 0, double surprise = 0, double uncertainty = 0)
        {
            Pos = pos;
            Neg = neg;
            Val = pos - neg;
            Arousal = arousal;
            Surprise = surprise;
            Uncertainty = uncertainty;
        }

        public Emotion Copy() => new(Pos, Neg, Arousal, Surprise, Uncertainty);
        public static Emotion operator +(Emotion a, Emotion b) => new(a.Pos + b.Pos, a.Neg + b.Neg, a.Arousal + b.Arousal, a.Surprise + b.Surprise, a.Uncertainty + b.Uncertainty);
        public static Emotion operator *(Emotion a, double b) => new(a.Pos * b, a.Neg * b, a.Arousal * b, a.Surprise * b, a.Uncertainty * b);
        public static Emotion operator /(Emotion a, double b) => a * (1 / b);


    }

    public class EmotionSystem : ICopyable<EmotionSystem>
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
    }
}