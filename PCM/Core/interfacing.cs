using System.Numerics;
using PCM.Verbal;
using PCM.Verbal.LLM;

namespace PCM.Core
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
        public class PlayerEmotion
        {
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
            public Actions.ActionType Action;
            public int InteractObjectId = -1;
            public int TargetId = -1;
            public double[][] Preferences; // ajout de cette ligne
            public double[] Mu; // ajout de cette ligne
            public double[] Sigma;
            public int[] TomPredict;
            public double[] FE;
        }

        public class AgentInput : Entity
        {
            public EmotionSystem Emotions;
            public int TargetEntityId = -1;
        }


        /// Helper conversion functions
        private static Vector3 ConvertVertex(Geom3d.Vertex vert) => new((float)vert.X, (float)vert.Y, (float)vert.Z);
        public static Types.Emotion ConvertEmotion(Emotion emo) => new() { Pos = emo.Positive, Neg = emo.Negative, Val = emo.Valence, Surprise = emo.Surprise };

        public static Emotion ConvertEmotion(Types.Emotion emo)
        {
            return new Emotion() { Positive = emo.Pos, Negative = emo.Neg, Surprise = emo.Surprise, Valence = emo.Val };
        }

        public static Types.EmotionSystem ConvertEmotionSystem(EmotionSystem emo) => new() { Felt = ConvertEmotion(emo.felt), Facial = ConvertEmotion(emo.facial), Physiological = ConvertEmotion(emo.physiological), VoluntaryFacial = ConvertEmotion(emo.voluntaryFacial), VoluntaryPhysiological = ConvertEmotion(emo.voluntaryPhysiological) };

        public static EmotionSystem ConvertEmotionSystem(Types.EmotionSystem emo)
        {
            return new EmotionSystem() { felt = ConvertEmotion(emo.Felt), facial = ConvertEmotion(emo.Facial), physiological = ConvertEmotion(emo.Physiological), voluntaryFacial = ConvertEmotion(emo.VoluntaryFacial), voluntaryPhysiological = ConvertEmotion(emo.VoluntaryPhysiological) };
        }

        public static SceneObjects.ObjectBody ConvertBody(Body bod, int type)
        {
            var orientation = new Geom3d.Vertex(bod.Orientation.X, bod.Orientation.Y, bod.Orientation.Z).Unit();
            var defaultOrientation = new Geom3d.Vertex(0, 0, 1);
            var body = new SceneObjects.ObjectBody(
                Geom3d.Polyhedron.RectangularPrism(new Geom3d.Vertex(bod.Center), bod.Width, bod.Height, bod.Depth),
                defaultOrientation,
                new Geom3d.Vertex(bod.OrientationOrigin)
                );
            body.RotateTowardsDirection(orientation);
            body.Type = type == 0 ? SceneObjects.ObjectType.ArtificialAgent : SceneObjects.ObjectType.Ball;
            return body;
        }
        public static Body ConvertBody(SceneObjects.ObjectBody body)
        {
            return new Body()
            {
                Width = body.BodyPosition.Vertices[0].Distance(body.BodyPosition.Vertices[1]),
                Height = body.BodyPosition.Vertices[0].Distance(body.BodyPosition.Vertices[2]),
                Depth = body.BodyPosition.Vertices[0].Distance(body.BodyPosition.Vertices[4]),
                Center = ConvertVertex(body.BodyPosition.Center),
                Orientation = ConvertVertex(body.LookAt),
                OrientationOrigin = ConvertVertex(body.LookAtOrigin)

            };
        }

        public static AgentOutput[] ConvertAgentPrediction(List<FreeEnergy.State.AgentState> seq, Actions.ActionType action) => seq.Select((state) =>
        {
            AgentOutput ao = new();
            var _state = state;//fe.ComputeFOC(state);
            ao.Id = state.currentAgentId;
            ao.Body = ConvertBody(state.objectBodies[ao.Id]);
            ao.Action = action;
            ao.Emotions = ConvertEmotionSystem(state.emotions[ao.Id]);
            ao.InteractObjectId = state.interactObjectIds[ao.Id];
            ao.TargetId = state.targetIds[ao.Id];
            ao.Preferences = Utils.Copy.Copy2DDouble(state.preferences); // ajout de cette ligne
            ao.Mu = Utils.Copy.Copy1DDouble(state.mu[ao.Id]); // ajout de cette ligne
            ao.Sigma = Utils.Copy.Copy1DDouble(state.certTable[ao.Id].Select(cert => cert.certainty).ToArray());
            ao.TomPredict = Utils.Copy.Copy1DArray(state.tomPredict);
            ao.FE = Utils.Copy.Copy1DDouble(state.fe[ao.Id].Select(fe => fe.freeEnergy).ToArray());
            return ao;
        }).ToArray();


        public static Types.WorldState InputToWorldState(Input input, int playerId)
        {
            SceneObjects.ObjectBody[] positions = new SceneObjects.ObjectBody[input.Agents.Count + input.Objects.Count];
            Types.WorldState ws = new()
            {
                Emotions = input.Agents.Select((a, aindex) =>
                {
                    return aindex != playerId ? ConvertEmotionSystem(a.Value.Emotions) : new Types.EmotionSystem();
                }).ToArray()
            };
            int id = 0;

            foreach (var agent in input.Agents)
            {
                var body = ConvertBody(agent.Value.Body, 0);
                positions[id] = body;
                if (agent.Value.TargetEntityId != -1)
                {
                    ws.Interactions.Add(id, agent.Value.TargetEntityId);
                }
                else
                {
                    ws.Interactions.Remove(id);
                }
                id += 1;
            }
            foreach (var obj in input.Objects)
            {
                var body = ConvertBody(obj.Value.Body, 1);
                positions[id] = body;
                id += 1;
            }
            ws.Positions = positions;

            //Remove collisions
            SimplePhysics.Movement.RemoveCollisions(ws.Positions, ws.Interactions);
            return ws;
        }
    }
}