using PCM.Core.FreeEnergy.State;
using PCM.Core.Geom3d;

namespace PCM.Core.FreeEnergy.InverseInferences
{
    public class SimplifiedAgentState
    {
        private Vertex position;
        private Vertex orientation;
        private double emotionVal;

        public Vertex Position
        {
            get { return position; }
            private set { position = value; }
        }
        public Vertex Orientation
        {
            get { return orientation; }
            private set { orientation = value; }
        }
        public double EmotionVal
        {
            get { return emotionVal; }
            private set { emotionVal = value; }
        }

        public SimplifiedAgentState(AgentState agent)
        {
            int agentIndex = agent.currentAgentId;
            position = agent.objectBodies[agentIndex].BodyPosition.Center;
            orientation = agent.objectBodies[agentIndex].LookAt;
            emotionVal = agent.emotions[agentIndex].Felt.Val + 1;
        }
    }
}