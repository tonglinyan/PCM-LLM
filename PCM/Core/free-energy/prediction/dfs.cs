using PCM.Core.FreeEnergy.State;
using static PCM.Core.Utils.Tree;
using PCM.Core.SceneObjects;
using System.Collections.Concurrent;
using static PCM.Core.Utils.Tree.Node<PCM.Core.Actions.Action>;
using PCM.Core.Actions;

namespace PCM.Core.FreeEnergy.Prediction
{
    public static class DFS
    {
        public static FreeEnergy Fe = new();

        public static (List<AgentState> states, Actions.Action action) Predict_(AgentState agent, int nbThreadLeft, int[] tomPredict = null)
        {
            var tomPredictLevel = tomPredict ?? agent.tomPredict;
            var agentId = agent.currentAgentId;
            var predictOthers = tomPredictLevel[agentId] >= 1;
            var adjustedTomPredictLevel = Base.AdjustPredictionLevel(tomPredictLevel, agentId);
            agent.pbodies = new List<ObjectBody[]>();
            foreach (var aId in agent.agentsIds)
            {
                agent.pbodies.Add(agent.objectBodies);
            }
            var states = new List<AgentState>() { agent };
            ConcurrentBag<(Node<AgentStateNode> node, List<AgentState> states, double score)> results = new();
            Action<Node<AgentStateNode>, List<AgentState>, int, double> predict = null;
            predict = delegate (Node<AgentStateNode> node, List<AgentState> states, int nbThreadLeft, double parent_score)
            {
                //Console.WriteLine($"Total threads in use: {ThreadPool.ThreadCount}");
                var parentExplNode = node;
                var resultState = states.Last().Copy();
                var lineage = node.GetLineage();
                foreach (var action in node.value.actions)
                {
                    resultState = DoActionIfPossibleImproved(resultState.currentAgentId, resultState, action);
                    if (resultState == null)
                        break;
                }
                if (resultState != null)
                {
                    if (predictOthers)
                    {
                        var othersPred = new List<ObjectBody[]>();

                        foreach (var aId in resultState.agentsIds)
                        {
                            othersPred.Add(resultState.objectBodies);
                        }
                        foreach (var targetAgentId in resultState.agentsIds)
                        {
                            if (targetAgentId != resultState.currentAgentId)
                            {
                                var agentPrediction = PredictOtherPredictions(resultState, targetAgentId, nbThreadLeft, adjustedTomPredictLevel);
                                var agentMove = Base.GetNextExpectedMove(agentPrediction.states);
                                node.value.AddOtherAgents(agentPrediction.states[0].node);
                                resultState.objectBodies[targetAgentId] = agentMove.objectBodies[targetAgentId];
                                resultState.emotions[targetAgentId] = agentMove.emotions[targetAgentId];
                                othersPred[targetAgentId] = agentMove.objectBodies;
                            }
                        }
                        resultState.pbodies = othersPred;
                    }
                    resultState = Fe.ComputeFOC(resultState);
                    double score;
                    score = resultState.GetCurrentFreeEnergy();
                    
                    node.value.agentState = resultState;
                    node.value.score = score;
                    resultState.node = node;
                    
                    if (agentId == 1)
                        score += parent_score;
                    var newStates = states.ToList();
                    newStates.Add(resultState);
                    if (node.DirectChildren == 0)
                        results.Add((node, newStates, score));
                    else
                    {
                        foreach (var child in node.children){
                            predict(child, newStates, 1, score);
                        }
                        //nbThreadLeft = Environment.ProcessorCount - ThreadPool.ThreadCount > 0? Environment.ProcessorCount - ThreadPool.ThreadCount:1;
                        //Console.WriteLine($"nb thread left: {nbThreadLeft}");
                        //Parallel.ForEach(node.children, new ParallelOptions { MaxDegreeOfParallelism = nbThreadLeft }, child => predict(child, newStates, nbThreadLeft / Math.Min(node.DirectChildren, nbThreadLeft), score)); 
                    }
                }
            };
            var root = agent.actionDirectories[agentId]._dfsList[0];
            var childCount = root.DirectChildren;
            var agentRoot = InitAgentDfs(agent, root);
            agent.node = agentRoot;
            //nbThreadLeft = Environment.ProcessorCount - ThreadPool.ThreadCount > 0? Environment.ProcessorCount - ThreadPool.ThreadCount:1;
            //Console.WriteLine($"nb thread left: {nbThreadLeft}");
            Parallel.ForEach(agentRoot.children, new ParallelOptions { MaxDegreeOfParallelism = nbThreadLeft }, child => predict(child, states, nbThreadLeft / Math.Min(childCount, nbThreadLeft), 0)); 
            var results1 = results.OrderBy(r => r.score);
            var bestResult = results1.First();
            var bestResults = new List<(Node<AgentStateNode> node, List<AgentState> states, double score)>
            {
                bestResult
            };
            var min = bestResult.score;
            foreach (var node in results1)
            {
                if (node.score != min)
                    break;
                bestResults.Add(node);
            }
            bestResult = bestResults.OrderBy(result => GetIdleCount(result.node)).Last();
            var path = GetLineageOfBestPath(bestResult.node);
            path.Reverse();
            return (states: bestResult.states.Select(state =>
            {
                return state;
            }).ToList(), action: path[1].value.actions[0]);
        }
        
        public static (List<AgentState> states, Actions.Action action) PredictOtherPredictions(AgentState agent, int targetAgentIndex, int nbThreadLeft, int[] adjustedTomPredictLevel)
        {
            var targetAgent = agent.Copy();
            targetAgent.currentAgentId = targetAgentIndex;
            foreach (var agentIndex in targetAgent.agentsIds)
            {
                if (agentIndex != targetAgentIndex)
                {
                    targetAgent.postPreferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                    targetAgent.preferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                    /*targetAgent.physiologicalReactivity = 1;
                    targetAgent.facialReactivity = 1;
                    targetAgent.voluntaryPhysiologicalWeight = 0;
                    targetAgent.voluntaryFacialWeight = 1;
                    targetAgent.physiologicalSensitivity = 0;*/
                    targetAgent.UpdateOtherEmotionFelt();
                }
            }
            return Predict_(targetAgent, nbThreadLeft, adjustedTomPredictLevel);
        }

        public static (List<AgentState> states, Actions.Action action) PredictV2_(AgentState agent, int nbThreadLeft, int depth=1, int[] tomPredict = null)
        {
            var tomPredictLevel = tomPredict ?? agent.tomPredict;
            var agentId = agent.currentAgentId;
            var predictOthers = tomPredictLevel[agentId] >= 1;
            var adjustedTomPredictLevel = Base.AdjustPredictionLevel(tomPredictLevel, agentId);
            agent.pbodies = new List<ObjectBody[]>();
            foreach (var aId in agent.agentsIds)
            {
                agent.pbodies.Add(agent.objectBodies);
            }
            var states = new List<AgentState>() { agent };
            ConcurrentBag<(Node<AgentStateNode> node, List<AgentState> states, double score)> results = new();
            Action<Node<AgentStateNode>, List<AgentState>, int, double> predict = null;
            predict = delegate (Node<AgentStateNode> node, List<AgentState> states, int nbThreadLeft, double parent_score)
            {
                //Console.WriteLine($"Total threads in use: {ThreadPool.ThreadCount}");
                var parentExplNode = node;
                var resultState = states.Last().Copy();
                var lineage = node.GetLineage();
                foreach (var action in node.value.actions)
                {
                    resultState = DoActionIfPossibleImproved(resultState.currentAgentId, resultState, action);
                    if (resultState == null)
                        break;
                }
                if (resultState != null)
                {
                    if (predictOthers)
                    {
                        var othersPred = new List<ObjectBody[]>();

                        foreach (var aId in resultState.agentsIds)
                        {
                            othersPred.Add(resultState.objectBodies);
                        }
                        foreach (var targetAgentId in resultState.agentsIds)
                        {
                            if (targetAgentId != resultState.currentAgentId)
                            {
                                var agentPrediction = PredictOtherPredictionsV2(resultState, targetAgentId, nbThreadLeft, adjustedTomPredictLevel);
                                var agentMove = Base.GetNextExpectedMove(agentPrediction.states);
                                node.value.AddOtherAgents(agentPrediction.states[0].node);
                                resultState.objectBodies[targetAgentId] = agentMove.objectBodies[targetAgentId];
                                resultState.emotions[targetAgentId] = agentMove.emotions[targetAgentId];
                                othersPred[targetAgentId] = agentMove.objectBodies;
                            }
                        }
                        resultState.pbodies = othersPred;
                    }
                    resultState = Fe.ComputeFOC(resultState);
                    double score;
                    score = resultState.GetCurrentFreeEnergy();
                    
                    node.value.agentState = resultState;
                    node.value.score = score;
                    resultState.node = node;
                    
                    /*if (agentId == 1)
                        score += parent_score;*/
                    var newStates = states.ToList();
                    newStates.Add(resultState);
                    if (node.DirectChildren == 0)
                        results.Add((node, newStates, score));
                    else
                    {
                        foreach (var child in node.children){
                            predict(child, newStates, 1, score);
                        }
                    }
                }
            };
            var root = agent.actionDirectories[agentId]._dfsList[0];
            var childCount = root.DirectChildren;
            var agentRoot = InitAgentDfsV2(agent, root, depth);
            agent.node = agentRoot;
            //nbThreadLeft = Environment.ProcessorCount - ThreadPool.ThreadCount > 0? Environment.ProcessorCount - ThreadPool.ThreadCount:1;
            //Console.WriteLine($"nb thread left: {nbThreadLeft}");
            Parallel.ForEach(agentRoot.children, new ParallelOptions { MaxDegreeOfParallelism = nbThreadLeft }, child => predict(child, states, nbThreadLeft / Math.Min(childCount, nbThreadLeft), 0)); 
            var results1 = results.OrderBy(r => r.score);
            var bestResult = results1.First();
            var bestResults = new List<(Node<AgentStateNode> node, List<AgentState> states, double score)>
            {
                bestResult
            };
            var min = bestResult.score;
            foreach (var node in results1)
            {
                if (node.score != min)
                    break;
                bestResults.Add(node);
            }
            bestResult = bestResults.OrderBy(result => GetIdleCount(result.node)).Last();
            var path = GetLineageOfBestPath(bestResult.node);
            path.Reverse();
            return (states: bestResult.states.Select(state =>
            {
                return state;
            }).ToList(), action: path[1].value.actions[0]);
        }
        
        public static (List<AgentState> states, Actions.Action action) PredictOtherPredictionsV2(AgentState agent, int targetAgentIndex, int nbThreadLeft, int[] adjustedTomPredictLevel)
        {
            var targetAgent = agent.Copy();
            targetAgent.currentAgentId = targetAgentIndex;
            foreach (var agentIndex in targetAgent.agentsIds)
            {
                if (agentIndex != targetAgentIndex)
                {
                    targetAgent.postPreferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                    targetAgent.preferences[agentIndex] = Enumerable.Repeat(0.5, targetAgent.objectBodies.Length).ToArray();
                    /*targetAgent.physiologicalReactivity = 1;
                    targetAgent.facialReactivity = 1;
                    targetAgent.voluntaryPhysiologicalWeight = 0;
                    targetAgent.voluntaryFacialWeight = 1;
                    targetAgent.physiologicalSensitivity = 0.5;*/
                    targetAgent.UpdateOtherEmotionFelt();
                }
            }
            return PredictV2_(targetAgent, nbThreadLeft, 1, adjustedTomPredictLevel);
        }

        public static AgentState DoActionIfPossibleImproved(int agentIndex, AgentState s, Actions.Action action)
        {
            if (action.GetActionType() == ActionType.LetGo)
            {
                if (s.targetIds[s.currentAgentId] == -1 || s.interactObjectIds[s.currentAgentId] != action.GetTargetId())
                    return null;
            }
            if (action.GetActionType() == ActionType.Grab)
            {
                var targetId = action.GetTargetId();
                var dist = s.objectBodies[agentIndex].BodyPosition.Center.Distance(s.objectBodies[targetId].BodyPosition.Center);
                var grabbedBySomeone = s.targetIds.Contains(targetId);

                if (dist > Constants.minDist || grabbedBySomeone || s.interactObjectIds[s.currentAgentId] != -1)
                {
                    return null;
                }
            }
            var outstate = action.Execute(agentIndex, s);
            if (outstate == null)
                return null;
            var outpos = outstate.objectBodies[agentIndex];
            var inpos = s.objectBodies[agentIndex];
            if (action.GetActionType() == ActionType.Rotate && inpos.LookAt.Equals(outpos.LookAt))
            {
                return outstate;
            }
            //var colliderPoints = new List<Geom3d.Vertex>();
            //colliderPoints.AddRange(inpos.BodyPosition.GetBottomFaceVertices());
            //colliderPoints.AddRange(outpos.BodyPosition.GetBottomFaceVertices());
            //var agentCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(SimplePhysics.Collider.ConvexHull2D(colliderPoints));
            //for (var objectIndex = 0; objectIndex < s.objectBodies.Length; objectIndex++)
            //    if (objectIndex != agentIndex && objectIndex != outstate.targetId[agentIndex])
            //    {
            //         var otherCollider = new SimplePhysics.Collider.Convex2DPolygonCollider(new Geom3d.Polygon(s.objectBodies[objectIndex].BodyPosition.GetBottomFaceVertices().ToArray()));
            //    if (agentCollider.collides(otherCollider))
            //    {
            //        return null;
            //    }
            //}
            return outstate;
        }

        public static Node<AgentStateNode> InitAgentDfs(AgentState agent, Node<Actions.Action[]> actionRoot){
            var nodesPerDepthLevel = new List<List<Node<AgentStateNode>>>();
            var root = new Node<AgentStateNode> (new (agent, actionRoot.value));
            nodesPerDepthLevel.Add(new List<Node<AgentStateNode>> { root });
            //Console.WriteLine("actions");
            //Console.WriteLine(JsonConvert.SerializeObject(nodesPerDepthLevel));
            //Generate the action tree
            
            for (var i = 1; i <= agent.actionDirectories[agent.currentAgentId].depth; i++)
            {
                var currentLevelNodes = new List<Node<AgentStateNode>>();
                foreach (var parent in nodesPerDepthLevel[i-1])
                {
                    foreach (Actions.Action[] combinedAction in agent.actionDirectories[agent.currentAgentId].CombinedActionList)
                    {
                        var a = new Node<AgentStateNode> (new (combinedAction));
                        parent.AddChild(a);
                        currentLevelNodes.Add(a);
                    }
                }
                nodesPerDepthLevel.Add(currentLevelNodes);
            }
            return root;
        }
        /// <summary>
        /// Init agent tree structure, the difference with InitAgentDfs is the depth of tree.
        /// In InitAgentDfsV2, while having ToM>0 and inferring others' actions, depth of processing = 1, instead of the real depth of processing.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="actionRoot"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static Node<AgentStateNode> InitAgentDfsV2(AgentState agent, Node<Actions.Action[]> actionRoot, int depth){
            var nodesPerDepthLevel = new List<List<Node<AgentStateNode>>>();
            var root = new Node<AgentStateNode> (new (agent, actionRoot.value));
            nodesPerDepthLevel.Add(new List<Node<AgentStateNode>> { root });
            //Console.WriteLine("actions");
            //Console.WriteLine(JsonConvert.SerializeObject(nodesPerDepthLevel));
            //Generate the action tree
            
            for (var i = 1; i <= depth; i++)
            {
                var currentLevelNodes = new List<Node<AgentStateNode>>();
                foreach (var parent in nodesPerDepthLevel[i-1])
                {
                    foreach (Actions.Action[] combinedAction in agent.actionDirectories[agent.currentAgentId].CombinedActionList)
                    {
                        var a = new Node<AgentStateNode> (new (combinedAction));
                        parent.AddChild(a);
                        currentLevelNodes.Add(a);
                    }
                }
                nodesPerDepthLevel.Add(currentLevelNodes);
            }
            return root;
        }
    }
}