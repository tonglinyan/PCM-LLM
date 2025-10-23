using Core.Types;
using System.Collections.Generic;
using System.Numerics;

namespace Core
{
    public static class Interfacing
    {
        /// <summary>
        /// Input is the data fed back to the PCM from the GE (game engine)
        /// </summary>
        public class Input
        {
            public long TimeStamp;
            public Dictionary<int, AgentInput> Agents;
            public Dictionary<int, Entity> Objects;
            /// to add public Dictionary<int, > Perceptions;

            public PlayerEmotion PlayerEmotion;
        }
        /// <summary>
        /// Output is the data sent from the PCM to the GE (game engine)
        /// </summary>
        public class Output
        {
            public long TimeStamp;
            //For each agent, the value is an array of desired states which are the result of an action
            //The first value in the array is the action to do right now, the other values are just predictions 
            //and shouldn't be used in Unity
            public Dictionary<int, AgentOutput[]> AgentStates;
        }

        /// <summary>
        /// valence is the valence expressed by the player,
        /// valenceForce is a dictionary of <entityId, "force" of valence (0 ...1)>
        /// </summary>
        public class PlayerEmotion{
            public double valence;
            public Dictionary<int, double> valenceFactor;
        }

        public class Entity
        {
            public Body Body;
            public int Id;
        }

        public class Body
        {
            public double Width;
            public double Height;
            public double Depth;
            public Vector3 Center;
            public Vector3 OrientationOrigin;
            public Vector3 Orientation;
        }

        public class Emotion
        {
            public double Valence;
            public double Positive;
            public double Negative;
            public double Surprise;
        }

        public class EmotionSystem
        {
            public Emotion felt;
            public Emotion facial;
            public Emotion physiological;
            public Emotion voluntaryFacial;
            public Emotion voluntaryPhysiological;
        }

        public class AgentOutput : Entity
        {
            public EmotionSystem Emotions;
            public ActionType Action;
            public int InteractObjectId = -1;
            public int TargetId = -1;
            public double[][] Preferences; 
            public double[] Mu;
            public double[] Sigma;
            public int[] TomPredict;
            public double[] FE;
        }

        public class AgentInput : Entity
        {
            public EmotionSystem Emotions;
            public int TargetEntityId = -1;
        }
    }
}