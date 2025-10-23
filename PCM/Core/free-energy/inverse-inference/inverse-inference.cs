using PCM.Core.FreeEnergy.State;
using static PCM.Core.FreeEnergy.InverseInferences.BeliefInference;

namespace PCM.Core.FreeEnergy.InverseInferences
{
    public static class InverseInference
    {
        private static int depth;
        private static int discretization;

        public static void Init(int depth, int discretization = 2)
        {
            InverseInference.depth = depth;
            InverseInference.discretization = discretization;
        }
        public static Dictionary<(AgentBelieves agentBelieves, int inferedAgentId), BeliefInference[]> InitInferences(int agentId, int nbAgents)
        {
            var inferences = new Dictionary<(AgentBelieves agentBelieves, int inferedAgentId), BeliefInference[]>();
            for (int agentId_i = 0; agentId_i < nbAgents; agentId_i++)
            {
                if (agentId == agentId_i) continue;
                var beliefInferences = new BeliefInference[depth];
                for (int depth_i = 1; depth_i < depth + 1; depth_i++)
                {
                    beliefInferences[depth_i - 1] = new BeliefInference(agentId_i);
                }
                var inferedAgentId = agentId_i;
                inferences[(new AgentBelieves((AgentState agent, double belief) =>
                {
                    agent.postPreferences[inferedAgentId][2] = belief;
                    agent.preferences[inferedAgentId][2] = belief;
                    agent.postPreferences[inferedAgentId][3] = 1.5f - belief;
                    agent.preferences[inferedAgentId][3] = 1.5f - belief;
                    return agent;
                }), agentId_i)] = beliefInferences;
            }
            return inferences;
        }

        public static void Start(AgentState agent, int nbThreadLeft) 
        {
            var inferences = agent.inferences;
            foreach (var (agentBelieves,inferedAgentId) in inferences.Keys)
            {
                var inferedAgend = agent.Copy();
                for (double belief = 0.5f; belief <= 1; belief += 1f / discretization)
                {
                    agentBelieves(inferedAgend, belief);
                    var predictions = Prediction.DFS.PredictOtherPredictions(inferedAgend, inferedAgentId, nbThreadLeft, Prediction.Base.AdjustPredictionLevel(inferedAgend.tomPredict, inferedAgend.currentAgentId));
                    for (int depth_i = 1; depth_i < depth + 1; depth_i++)
                    {
                        inferences[(agentBelieves, inferedAgentId)][depth_i - 1].AddInference(predictions.states[depth_i], belief);
                    }
                }
            }
        }

        public static AgentState UpdateAgentBeliefs(AgentState agent)
        {
            var inferences = agent.inferences;
            SetObservation(agent);
            foreach (var (key,val) in inferences)
            {
                var beliefInference = val[0];
                if (beliefInference.IsEmpty()) return agent;
                agent = key.agentBelieves(agent, beliefInference.GetMoreLikelyBelief());
                beliefInference.RemoveInferences();
            }
            return agent;
        }
    }


}