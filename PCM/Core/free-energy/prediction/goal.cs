using PCM.Core.FreeEnergy.State;
using PCM.Core.SceneObjects;
using System.Collections.Concurrent;
using PCM.Core.Actions;

namespace PCM.Core.FreeEnergy.Prediction
{
    public static class Goal
    {
        public static (List<AgentState> states, PCM.Core.Actions.Action action) Predict_(AgentState agent, int nbThreadLeft, int[] tomPredict = null)
        {
            var pred = _predict(agent, nbThreadLeft, tomPredict);
            List < AgentState > states = new()
            {
                agent,
                pred.state
            };
            AgentState state = GetNextExpectedMove(states).Copy();
            for(int i = 0; i < pred.stepCountToReachGoal; i++)
            {
                state = Base.Fe.ComputeFOC(state);
                state = DoActionIfPossibleImproved(state.currentAgentId, state, pred.goal.stepAction);
                states.Add(state.Copy());
            }
            return (states, pred.goal.stepAction);
        }
        
        public static (AgentState state, Actions.Goal goal, int stepCountToReachGoal) _predict(AgentState agent, int nbThreadLeft, int[] tomPredict = null)
        {
            var tomPredictLevel = tomPredict ?? agent.tomPredict;
            var agentId = agent.currentAgentId;
            var predictOthers = tomPredictLevel[agentId] >= 1;
            //adjust prediction level for others prediction
            var adjustedTomPredictLevel = Base.AdjustPredictionLevel(tomPredictLevel, agentId);
            agent.pbodies = new List<ObjectBody[]>();
            foreach (var aId in agent.agentsIds)
            {
                agent.pbodies.Add(agent.objectBodies);
            }
            var goalDirectory = agent.goalDirectories[agentId];
            ConcurrentDictionary<int, (Actions.Goal goal, AgentState state, double score, int stepCountToReachGoal)> results = new();
            void predict(int i)
            {
                var goal = goalDirectory.goals[i];
                var currentState = agent.Copy();
                var simulatedState = DoActionIfPossibleImproved(currentState.currentAgentId, currentState.Copy(), goal.simulatedAction);
                if (simulatedState == null)
                    return;
                var othersPred = new List<ObjectBody[]>();
                foreach (var aId in simulatedState.agentsIds)
                {
                    othersPred.Add(simulatedState.objectBodies);
                }
                var nextState = DoActionIfPossibleImproved(currentState.currentAgentId, currentState, goal.stepAction);
                if (predictOthers && nextState != null)
                {
                    foreach (var targetAgentId in nextState.agentsIds)
                    {
                        if (targetAgentId != nextState.currentAgentId)
                        {
                            // Console.WriteLine($"{agentId} predicting {targetAgentId}");
                            var targetAgent = nextState.Copy();
                            // if (agentId == 0 && adjustedTomPredictLevel[0] == 1)
                            //     targetAgent.preferences[0][1] = 0.45;
                            targetAgent.currentAgentId = targetAgentId;
                            foreach (var agentIndex in targetAgent.agentsIds)
                            {
                                if (agentIndex != targetAgent.currentAgentId)
                                {
                                    targetAgent.postPreferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                                    targetAgent.preferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                                    targetAgent.physiologicalReactivity = 0;
                                    targetAgent.facialReactivity = 1;
                                    targetAgent.voluntaryPhysiologicalWeight = 0;
                                    targetAgent.voluntaryFacialWeight = 1;
                                    targetAgent.physiologicalSensitivity = 0;
                                    targetAgent.tomUpdate[agentIndex] = Enumerable.Repeat<double>(0, targetAgent.agentsIds.Length).ToArray();
                                }
                            }
                            if (targetAgent.pbodies != null)
                            { //temporary, not good
                                targetAgent.objectBodies = Core.Utils.Copy.CopyArray(agent.pbodies[targetAgentId]);
                            }
                            var agentPrediciton = _predict(targetAgent, nbThreadLeft, adjustedTomPredictLevel);
                            var agentMove = agentPrediciton.state;
                            for (int j = 0; j < agentMove.stepCountToReachGoal; j++)
                            {
                                var nextAgentMove = DoActionIfPossibleImproved(agentMove.currentAgentId, agentMove, agentPrediciton.goal.stepAction);
                                if (nextAgentMove == null)
                                    break;
                                agentMove = nextAgentMove;
                            }
                            simulatedState.objectBodies[targetAgentId] = agentMove.objectBodies[targetAgentId];
                            simulatedState.emotions[targetAgentId] = agentMove.emotions[targetAgentId];
                            othersPred[targetAgentId] = agentMove.objectBodies;
                        }
                    }
                }
                simulatedState.pbodies = othersPred;
                simulatedState = Base.Fe.ComputeFOC(simulatedState);
                var score = simulatedState.GetCurrentFreeEnergy();
                results.TryAdd(i, (goal, state: nextState, score, simulatedState.stepCountToReachGoal));
            }
            var goalCount = goalDirectory.goals.Count;
            if (nbThreadLeft > 1)
            {
                var tmp = nbThreadLeft;
                nbThreadLeft /= Math.Min(goalCount, nbThreadLeft);
                Parallel.For(0, goalCount, new ParallelOptions { MaxDegreeOfParallelism = tmp }, i => predict(i));
            }
            else
                for (int i = 0; i < goalCount; i++)
                    predict(i);
            var results1 = results.OrderBy(r => r.Value.score);
            var bestResult = results1.First().Value;
            var bestResults = new List<(Actions.Goal goal, AgentState state, double score, int stepCountToReachGoal)>
            {
                bestResult
            };
            var min = bestResult.score;
            var results2 = results1.Skip(1);
            foreach (var node in results2)
            {
                if (node.Value.score != min)
                    break;
                bestResults.Add(node.Value);
            }
            if (bestResults.Count != 1)
            {
                var selectedResult = bestResults.FirstOrDefault(result => result.goal.stepAction.GetActionType() == ActionType.Idle);
                if (selectedResult == (null, null, 0, 0))
                    bestResult = bestResults[new Random().Next(bestResults.Count)];
            }

            var state = Base.Fe.ComputeFOC(bestResult.state);
            //first element of the lineage is the leaf > reverse
            //UnityEngine.Debug.Log("agent : " + agent.currentAgentId + "    action : " + bestResult.goal.stepAction.ToString());
            return (state, bestResult.goal, bestResult.stepCountToReachGoal - 1);
        }

        public static AgentState GetNextExpectedMove(List<AgentState> prediction)
        {
            return prediction[1];
        }

        public static AgentState DoActionIfPossibleImproved(int agentIndex, AgentState s, Actions.Action action)
        {
            if (action.GetActionType() == ActionType.LetGo)
            {
                if (s.targetIds[s.currentAgentId] == -1 || s.targetIds[s.currentAgentId] != action.GetTargetId())
                    return null;
                // else{
                //     Console.WriteLine("can let go " + action.GetTargetId());
                // }
            }
            else if (action.GetActionType() == ActionType.Grab)
            {
                var targetId = action.GetTargetId();
                var dist = s.objectBodies[agentIndex].BodyPosition.Center.Distance(s.objectBodies[targetId].BodyPosition.Center);
                var grabbedBySomeone = s.targetIds.Contains(targetId);
                // Console.WriteLine($"{targetId} {grabbedBySomeone}");
                if (dist > Constants.minDist || grabbedBySomeone || s.targetIds[s.currentAgentId] != -1)
                {
                    return null;
                }
            }
            var outstate = action.Execute(agentIndex, s);
            if (outstate == null)
                return null;
            var outpos = outstate.objectBodies[agentIndex];
            var inpos = s.objectBodies[agentIndex];

            //Check if the position is the same
            if (action.GetActionType() == ActionType.Rotate && inpos.LookAt.Equals(outpos.LookAt))
            {
                return outstate;
            }

            // Create the collider (convexHull of the 2 positions), bottom face
            var colliderPoints = new List<Geom3d.Vertex>();
            colliderPoints.AddRange(inpos.BodyPosition.GetBottomFaceVertices());
            colliderPoints.AddRange(outpos.BodyPosition.GetBottomFaceVertices());
            //var agentCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(SimplePhysics.Collider.ConvexHull2D(colliderPoints));
            //Check collider against other colliders
            //TODO: Temp (removed collisions)
            // for (var objectIndex = 0; objectIndex < s.objectBodies.Length; objectIndex++)
            //     if (objectIndex != agentIndex && objectIndex != outstate.grabbedEntityId)
            //     {
            //         var otherCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(new Geom3d.Polygon(s.objectBodies[objectIndex].BodyPosition.GetBottomFaceVertices().ToArray()));
            //         if (agentCollider.collides(otherCollider))
            //         {
            //             return null;
            //         }
            //     }
            return outstate;
        }
    }
}