using PCM.Core.FreeEnergy.State;
using Vertex = PCM.Core.Geom3d.Vertex;

namespace PCM.Core.FreeEnergy.InverseInferences
{
    public class BeliefInference
    {
        private List<(SimplifiedAgentState prediction, double belief)> inferences = new();
        private static int nbDist = 0;
        private static double sumDist = 0;
        private static double normDist;
        private static readonly double normOrientation = 1.0 / 3;
        private const double normEmotionVal = 0.25f;
        private readonly int inferedAgentId;
        private static readonly Dictionary<int,SimplifiedAgentState> observations = new();
        public delegate AgentState AgentBelieves(AgentState agentState, double belief);

        public BeliefInference(int inferedAgentId)
        {
            this.inferedAgentId = inferedAgentId;
        }
        public bool IsEmpty()
        {
            return inferences.Count == 0;
        }


        public void AddInference(AgentState prediction, double belief)
        {
            var inferedAgent = new SimplifiedAgentState(prediction);
            inferences.Add((inferedAgent, belief));
            UpdateSumDist(inferedAgent.Position);
        }

        public void RemoveInferences()
        {
            inferences = new();
        }

        public double GetMoreLikelyBelief()
        {
            List<(double belief, double convergence)> beliefConvergences = new();
            foreach (var (prediction, belief) in inferences)
            {
                beliefConvergences.Add((belief, ComputeConvergence(prediction)));
            }
            beliefConvergences.Sort((a, b) => b.convergence.CompareTo(a.convergence));
            return beliefConvergences.First().belief;
        }

        private double ComputeConvergence(SimplifiedAgentState prediction)
        {
            double convergence = 0;
            var position = prediction.Position;
            var observation = observations[inferedAgentId];
            convergence += position.Dot(observation.Position) / normDist;
            convergence += prediction.Orientation.Dot(observation.Orientation) / normOrientation;
            convergence += prediction.EmotionVal * observation.EmotionVal / normEmotionVal;
            return convergence;
        }

        public static void SetObservation(AgentState observation)
        {
            var currentAgentId = observation.currentAgentId;
            foreach (var agentId in observation.agentsIds)
            {
                if (agentId == currentAgentId) continue;
                observation.currentAgentId = agentId;
                observations[agentId] = new SimplifiedAgentState(observation);
                UpdateSumDist(observations[agentId].Position);
            }
            observation.currentAgentId = currentAgentId;
            UpdateNormDist();
        }

        public static void UpdateSumDist(Vertex position)
        {
            sumDist += (Math.Abs(position.X) + Math.Abs(position.Y) + Math.Abs(position.Z)) / 3;
            nbDist += 1;
        }
        public static void UpdateNormDist()
        {
            normDist = Math.Pow(sumDist / nbDist, 2);
        }
    }
}