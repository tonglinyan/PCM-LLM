using PCM.Core.Types;
using PCM.Core.FreeEnergy.State;
using PCM.Core.SceneObjects;
using PCM.Core.FreeEnergy.InverseInferences;

namespace PCM.Core.Execution
{
    public static class Run
    {
        public static (AgentState state, ObjectBody pos, List<AgentState> seq, Actions.ActionType actionType) Predict(AgentState a, Dictionary<int, int> interactions, int nbThreadLeft, bool goalPredict)
        {
            a.objectBodies = SimplePhysics.Movement.RemoveCollisions(a.objectBodies, interactions);
            //a.UpdateOtherEmotionFelt();
            var agentId = a.currentAgentId;
            var canInfer = agentId == 1 && a.tomPredict[agentId] > 1;
            if (canInfer)
                a.canInfer = true;
            /*if (a.currentAgentId == 1) {
                a.emotions[0].Felt.Pos = 0.0;
                a.emotions[0].Felt.Neg = 0.8;
                a.emotions[0].Felt.Val = a.emotions[0].Felt.Pos - a.emotions[0].Felt.Neg;
            }*/
        
            var agent = FreeEnergy.Prediction.DFS.Fe.ComputeFOC(a);
            if (canInfer)
            {
                agent.canInfer = false;
                InverseInference.Start(agent, nbThreadLeft);
            }
            var (states, action) = goalPredict ? FreeEnergy.Prediction.Goal.Predict_(agent, nbThreadLeft) : FreeEnergy.Prediction.DFS.PredictV2_(agent, nbThreadLeft, agent.actionDirectories[agent.currentAgentId].depth);
            var expectedState = FreeEnergy.Prediction.Base.GetNextExpectedMove(states);
            return (state: expectedState, pos: expectedState.objectBodies[expectedState.currentAgentId], seq: states, actionType: action.GetActionType());
        }

        /// Updates for agents inner state 
        /// TODO: Check that expected emotions and positions are indeed copied | Should be ok
        public static AgentState[] UpdateAgentsInnerStatesWithActualPositions(AgentState[] agentStates, ObjectBody[] actualPositions, Dictionary<int, int> interactions)
        {
            var updatedStates = Utils.Copy.CopyArray(agentStates);
            var interactionArray = actualPositions.Select(p => -1).ToArray();

            foreach (var kv in interactions)
            {
                interactionArray[kv.Key] = kv.Value;
            }
            for (int agentIndex = 0; agentIndex < agentStates.Length; agentIndex++)
            {
                updatedStates[agentIndex].targetIds = agentStates[agentIndex].targetIds;
                for (int oindex = 0; oindex < actualPositions.Length; oindex++)
                {
                    if (oindex == agentIndex)
                    {
                        updatedStates[agentIndex].objectBodies[agentIndex] = actualPositions[agentIndex].Copy();
                    }
                    else
                    {
                        //if visible
                        if (SimplePhysics.Vision.CanSeeTarget(actualPositions[agentIndex], actualPositions[oindex]) != SimplePhysics.Vision.VisibleType.NotVisible)
                        {
                            // Console.WriteLine($"{agentIndex} > {oindex}");
                            updatedStates[agentIndex].objectBodies[oindex] = actualPositions[oindex].Copy();
                            var l = new List<ObjectBody[]>();
                            if (updatedStates[agentIndex].pbodies != null)
                                foreach (int aid in updatedStates[agentIndex].agentsIds)
                                {
                                    updatedStates[agentIndex].pbodies[aid][oindex] = updatedStates[agentIndex].objectBodies[oindex];
                                }
                        }
                    }
                }

            }
            return updatedStates;
        }
        
        public static AgentState[] UpdateAgentsInnerStatesWithActualEmotions(AgentState[] agentStates, ObjectBody[] actualPositions, EmotionSystem[] emotions)
        {
            var updatedStates = Utils.Copy.CopyArray(agentStates);
            for (int agentIndex = 0; agentIndex < agentStates.Length; agentIndex++)
            {
                for (int oindex = 0; oindex < agentStates[0].agentsIds.Length; oindex++)
                {
                    updatedStates[agentIndex].UpdateOtherEmotionFelt();
                    if (oindex == agentIndex)
                    {
                        updatedStates[agentIndex].emotions[agentIndex] = emotions[agentIndex].Copy();
                    }
                    else
                    {
                        //if visible
                        if (SimplePhysics.Vision.CanSeeTarget(actualPositions[agentIndex], actualPositions[oindex]) != SimplePhysics.Vision.VisibleType.NotVisible)
                        {
                            updatedStates[agentIndex].emotions[oindex].Facial = emotions[oindex].Facial.Copy();
                            updatedStates[agentIndex].emotions[oindex].Physiological = emotions[oindex].Physiological.Copy();
                            updatedStates[agentIndex].emotions[oindex].Felt = emotions[oindex].Felt.Copy();
                        }
                        //else keep prediction
                    }
                }
            }
            return updatedStates;
        }
    }
}