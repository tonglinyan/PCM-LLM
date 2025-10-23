using System.Numerics;
using PCM.Core.FreeEnergy.State;
using PCM.Core.Geom3d;
using Newtonsoft.Json;
using PCM.Core.Actions;
using PCM.Schemas;
using PCM.Core.SceneObjects;

namespace PCM.Verbal.LLM
{
    public class Interface
    {
        readonly HTTPClient model;
        ContextPrompt contextPrompt;
        static string[] entityNames;
        private const int BoardSize = 11; // 11x11 damier
        private const int MinRealCoordinate = -440;
        private const int MaxRealCoordinate = 440;
        private const int RealRangePerCell = (MaxRealCoordinate - MinRealCoordinate) / BoardSize;
        static bool LLMtoPCM;
        static bool PCMtoLLM;
        static int hypothesis;
        public int nbAgents;

        static Dictionary<string, int> moveDirectionMap = new Dictionary<string, int>
        {
            { "forward", 0 },
            { "left forward", -1 },
            { "left", -2 },
            { "left backward", -3 },
            { "backward", -4 },
            { "right forward", 1 },
            { "right", 2 },
            { "right backward", 3 },
        };

        public Interface(ContextPrompt contextPrompt, string[] entityNames, Schemas.Verbal parameters, ArtificialAgentMode aa_mode)
        {
            model = new HTTPClient();
            this.contextPrompt = contextPrompt;
            Interface.entityNames = entityNames;
            LLMtoPCM = !(aa_mode == ArtificialAgentMode.NonVerbal);
            PCMtoLLM = !(aa_mode == ArtificialAgentMode.NonVerbal);            
            hypothesis = parameters.Hypothesis;
        }

        public string InstructionPrompt(int agentId, string speaker, string host, double pref)
        {
            string instructionPrompt = contextPrompt.QAInstruction[agentId];
            instructionPrompt = GameMode(instructionPrompt, pref);
            instructionPrompt = instructionPrompt.Replace("{host}", host).Replace("{speaker}", speaker);
            return instructionPrompt;
        }

        public string GameMode(string text, double pref)
        {
            if (text.Contains("{game_mode}")){
                if (pref > 0)
                {
                    return text.Replace("{game_mode}", "You are playing the role of a partner, so your goal is to help {speaker} approach the box containing the treasure.");
                }
                else
                {
                    return text.Replace("{game_mode}", "You are playing the role of an adversary, so your goal is to prevent {speaker} from approaching the box containing the treasure.");
                }
            }
            return text;
        }

        public string[] InstructionNextAction(int agentId, string host, string speaker, double pref)
        {
            string instructionPrompt = contextPrompt.APInstruction[agentId];
            instructionPrompt = GameMode(instructionPrompt, pref);
            instructionPrompt = instructionPrompt.Replace("{host}", host).Replace("{speaker}", speaker);
            //Console.WriteLine("indice:  " + nbAgents + hypothesis);
            string query = contextPrompt.APInstruction[nbAgents + hypothesis];
            string entityString = string.Join(
                ",",
                entityNames.Where((item, index) => index != agentId).Select(item => $"'{item}'")
            );
            string preferenceUpdate = string.Join(
                "\n",
                entityNames.Where((item, index) => index != agentId).Select(item => $"'{item}': <float between -1 and 1>, ")
            );
            query = query.Replace("{entities}", entityString).Replace("{preferenceUpdate}", preferenceUpdate);
            query = query.Replace("{host}", host).Replace("{speaker}", speaker);
            
            return new string[] { instructionPrompt, query };
        }


        #region Numeric to Triples
        public static string ParametersToTriples(AgentState agentState)
        {

            string text = "";
            if (PCMtoLLM)
            {
                foreach (var agentId in agentState.agentsIds)
                {
                    string agentName = entityNames[agentId];
                    for (int entityId = 0; entityId < agentState.objectBodies.Length; entityId++)
                    {
                        string subject = entityNames[entityId];

                        if (entityId != agentId)
                        {

                            double value = (agentState.preferences[agentId][entityId] * 2 - 1) * 100;
                            string str = value.ToString("0.##");

                            text += $"'{agentName} | preference towards {subject} | {str}%', ";

                            value = (agentState.mu[agentId][entityId] * 2 - 1) * 100;
                            str = value.ToString("0.##");
                            text += $"'{agentName} | satisfaction with situation involving {subject} | {str}%', ";

                            value = agentState.certTable[agentId][entityId].certainty * 100;
                            str = value.ToString("0.##");
                            text += $"'{agentName} | visibility of {subject} | {str}%', ";

                            value = agentState.fe[agentId][entityId].freeEnergy;
                            str = value.ToString("0.##");
                            text += $"'{agentName} | expectation violation about {subject} | {str}', ";

                        }
                    }
                    if (agentId == agentState.currentAgentId)
                    {
                        /// <summary>
                        /// physiological, facial, felt 
                        /// </summary>
                        /// <value></value>
                        double val = agentState.emotions[agentId].Felt.Val;
                        string str = val.ToString("0.##");
                        text += $"'{agentName} | felt emotion valence | {str}', ";
                        val = agentState.emotions[agentId].Facial.Val;
                        str = val.ToString("0.##");
                        text += $"'{agentName} | facial emotion valence | {str}', ";
                        val = agentState.emotions[agentId].Physiological.Val;
                        str = val.ToString("0.##");
                        text += $"'{agentName} | physiological emotion valence | {str}', ";
                    }
                    else
                    {
                        double val = agentState.emotions[agentId].Felt.Val;
                        string str = val.ToString("0.##");
                        text += $"'{agentName} | felt emotion valence | {str}', ";
                    }
                    int tom = agentState.tomPredict[agentId];
                    text += $"'{agentName} | theory of mind order | {tom}', ";
                }
                /*for (int entityId = 0; entityId < agentState.objectBodies.Length; entityId++)
                {
                    string subject = "";
                    if (entityId < agentState.agentsIds.Length)
                    {
                        subject = $"agent {entityId}";
                    }
                    else
                    {
                        subject = $"box {entityId - agentState.agentsIds.Length}";
                    }
                    double X = agentState.objectBodies[entityId].BodyPosition.Center.X;
                    double Y = agentState.objectBodies[entityId].BodyPosition.Center.Y;
                    double Z = agentState.objectBodies[entityId].BodyPosition.Center.Z;

                    text += $"'{subject} | position | {X.ToString("0.##")}, {Y.ToString("0.##")}, {Z.ToString("0.##")}', ";

                    X = agentState.objectBodies[entityId].LookAt.X;
                    Y = agentState.objectBodies[entityId].LookAt.Y;
                    Z = agentState.objectBodies[entityId].LookAt.Z;
                    text += $"'{subject} | orientation | {X.ToString("0.##")}, {Y.ToString("0.##")}, {Z.ToString("0.##")}', ";
                }
                */

                text += SpatialInfoDescriptionV2(agentState);
            }
            else
            {
                int agentId = agentState.currentAgentId;
                string agentName = entityNames[agentId];
                for (int entityId = 0; entityId < agentState.objectBodies.Length; entityId++)
                {
                    string subject = entityNames[entityId];

                    if (entityId != agentId)
                    {

                        double value = (agentState.preferences[agentId][entityId] * 2 - 1) * 100;
                        string str = value.ToString("0.##");
                        text += $"'{agentName} | preference towards {subject} | {str}%', ";

                    }
                }
            }

            return text;
        }


        public static string SpatialInfoDescription(AgentState agentState) {
            string text = "";
            // write the spatial information in natural language
            Vertex currentAgentPosition = agentState.objectBodies[agentState.currentAgentId].BodyPosition.Center;
            Vertex currentAgentLookAt = agentState.objectBodies[agentState.currentAgentId].LookAt;

            for (int agentId = 0; agentId < agentState.agentsIds.Length; agentId++)
            {
                string agentName = entityNames[agentId];
                Vertex agentPosition = agentState.objectBodies[agentId].BodyPosition.Center;
                Vertex agentLookAt = agentState.objectBodies[agentId].LookAt;

                for (int entityId = agentState.agentsIds.Length; entityId < agentState.objectBodies.Length; entityId++)
                {
                    string subj = entityNames[entityId];
                    Vertex objectPosition = agentState.objectBodies[entityId].BodyPosition.Center;

                    Vector3 localDirection = LocalDirection(agentPosition, agentLookAt, objectPosition);
                    Vector3 localOrientation = Vector3.Normalize(objectPosition.ToVector3() - agentPosition.ToVector3());
                    localOrientation.Y = 0;

                    Vector3 lookAt = agentLookAt.ToVector3();
                    lookAt.Y = 0;

                    float angle = MathF.Acos(Vector3.Dot(lookAt, localOrientation) / (MathF.Sqrt(Vector3.Dot(lookAt, lookAt)) * MathF.Sqrt(Vector3.Dot(localOrientation, localOrientation))));
                    string x_relative = angle < MathF.PI / 4 ? "front" : "";
                    x_relative = angle > MathF.PI * 3 / 4 ? "back" : x_relative;
                    string y_relative = localDirection.Y > 0 ? "right" : "left";
                    y_relative = localDirection.Y == 0 ? "" : y_relative;
                    //Console.WriteLine($"{subj} | {x_relative} {y_relative} of | {agentName}");

                    text += $"'{subj} | {x_relative} {y_relative} of | {agentName}', ";
                }

                if (agentId != agentState.currentAgentId)
                {
                    string subj = entityNames[agentState.currentAgentId];

                    Vector3 localDirection = LocalDirection(currentAgentPosition, currentAgentLookAt, agentPosition);

                    string x_relative = localDirection.X > 0 ? "front" : "back";
                    text += $"'{subj} | {x_relative} | {agentName}', ";
                    //Console.WriteLine($"{subj} | {x_relative} | {agentName}");
                }
                var targetId = agentState.targetIds[agentId];
                if (targetId != -1)
                {
                    text += $"'{agentName} | is looking at | {entityNames[targetId]}', ";
                }
            }

            return text;
        }

        public static string SpatialInfoDescriptionV2(AgentState agentState){
            List<string> text = new List<string> ();

            for (int agentId = 0;  agentId < agentState.agentsIds.Length; agentId++){
                string agentName = entityNames[agentId];
                Vertex agentPosition = agentState.objectBodies[agentId].BodyPosition.Center;
                Vertex agentLookAt = agentState.objectBodies[agentId].LookAt;

                var position = ConvertRealToBoardCoordinates(-agentPosition.X, agentPosition.Z);
                text.Add($"'{agentName} | position | ({position.x} {position.y})'");
                text.Add($"'{agentName} | orientation | ({position.x + (int)Math.Round(agentLookAt.X)} {position.y + (int)Math.Round(agentLookAt.Z)})'");
            }

            for (int entityId = agentState.agentsIds.Length; entityId < agentState.objectBodies.Length; entityId++)
            {
                string subj = entityNames[entityId];
                Vertex objectPosition = agentState.objectBodies[entityId].BodyPosition.Center;

                var position = ConvertRealToBoardCoordinates(-objectPosition.X, objectPosition.Z);
                text.Add($"'{subj} | position | ({position.x} {position.y})'");
            }
            return string.Join(", ", text);
        }


        static string ExplicationOfMoves(AgentState agentState)
        {
            int stepSize = 80;
            ObjectBody[] objectBodies = agentState.objectBodies;
            var agentLookAt = objectBodies[agentState.currentAgentId].LookAt.ToVector3();
            var agentPosition = objectBodies[agentState.currentAgentId].BodyPosition.Center.ToVector3();
            Dictionary<string, Vector3> directions = GetMovementDirections(agentLookAt);

            List<string> MoveInterpretation = new List<string>();
            foreach (var kv in directions)
            {
                string dirName = kv.Key;
                Vector3 offset = kv.Value * stepSize;
                Vector3 newPosition = agentPosition + offset;

                List<string> explications = new List<string>();
                for (int i = 0; i < objectBodies.Length; i++)
                {
                    if (i != agentState.currentAgentId)
                    {
                        float oldDist = Vector3.Distance(agentPosition, objectBodies[i].BodyPosition.Center.ToVector3());
                        float newDist = Vector3.Distance(newPosition, objectBodies[i].BodyPosition.Center.ToVector3());

                        string relation = newDist < oldDist ? "closer to" : "further from";
                        explications.Add($"{relation} {entityNames[i]}");
                    }
                }
                MoveInterpretation.Add($"- '{dirName}': {string.Join(", ", explications)}");
            }
            //return "";
            return "Orientation guide:\n" + string.Join("\n", MoveInterpretation);
        }


        static Dictionary<string, Vector3> GetMovementDirections(Vector3 lookAt)
        {
            Vector3 forward = Vector3.Normalize(lookAt);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            Vector3 left = -right;
            Vector3 back = -forward;

            return new Dictionary<string, Vector3>
            {
                { "forward", forward },
                { "backward", back },
                { "right", right },
                { "left", left },
                { "left forward", Vector3.Normalize(forward + left) },
                { "right forward", Vector3.Normalize(forward + right) },
                { "left backward", Vector3.Normalize(back + left) },
                { "right backward", Vector3.Normalize(back + right) }
            };
        }
        
        public static (int x, int y) ConvertRealToBoardCoordinates(double realX, double realY)
        {
            realX = Math.Clamp(realX, MinRealCoordinate, MaxRealCoordinate - 1);
            realY = Math.Clamp(realY, MinRealCoordinate, MaxRealCoordinate - 1);

            int boardX = (int)Math.Floor((realX + RealRangePerCell / 2) / RealRangePerCell);
            int boardY = (int)Math.Floor((realY + RealRangePerCell / 2) / RealRangePerCell);

            return (boardX, boardY);
        }


        public static Vector3 LocalDirection(Vertex refPosition, Vertex refLookAt, Vertex objectPosition){
            Vector3 directionToObjectB = objectPosition.Sub(refPosition).ToVector3();
            Vector3 up = Vector3.UnitY;
            Vector3 right = Vector3.Normalize(Vector3.Cross(refLookAt.ToVector3(), up));
            up = Vector3.Cross(right, refLookAt.ToVector3()); 
            Vector3 localDirection = new Vector3(Vector3.Dot(directionToObjectB, refLookAt.ToVector3()), Vector3.Dot(directionToObjectB, right), Vector3.Dot(directionToObjectB, up));
            return localDirection;
        }
        #endregion

        public string PCMUpdatingInstruction(string speaker, string host, int agentId)
        {
            string PCMprompt = contextPrompt.PUInstruction[agentId];
            PCMprompt = PCMprompt.Replace("{host}", host).Replace("{speaker}", speaker);
            return PCMprompt;
        }

        /// <summary>
        /// LLM for question-answering
        /// </summary>
        /// <param name="agent"></param> the agent who needs to reply to query
        /// <param name="withContext"></param> 
        /// <returns></returns>
        public async Task Interaction(Agent agent, bool withContext = true)
        {
            string host = entityNames[agent.id];
            agent.GetLastMessageListened(out string query, out string speaker);
            string input_prompt = string.Join("#", "QA", speaker, host, withContext ? InstructionPrompt(agent.id, speaker, host, agent.preferences[agent.id][1 - agent.id]) : query, (withContext && PCMtoLLM) ? agent.ParametersToLLM(hypothesis) : "None", withContext ? query : "None");  
            input_prompt += withContext ? "image: " + agent.perception : "" ;
            string output_text = await model.SendInputAsync(input_prompt);

            agent.Listen(host, output_text);
            
            if (withContext && LLMtoPCM)
            {
                await PreferenceUpdating(agent, speaker, host, query);
                //_ = Task.Run(() => PreferenceUpdating(agent, speaker, host, query));
            }
        }

        /// <summary>
        /// LLM for preference udpating
        /// </summary>
        /// <param name="agent"></param> the agent who needs to reply to query
        /// <param name="speaker"></param> entity of speaker
        /// <param name="host"></param> entity of host
        /// <param name="query"></param> query
        /// <returns></returns> update agent
        public async Task PreferenceUpdating(Agent agent, string speaker, string host, string query){
            if (hypothesis != 0) {
                string input_prompt = string.Join("#", "PU", speaker, host, PCMUpdatingInstruction(speaker, host, agent.id), agent.ParametersToLLM(hypothesis), query);
                string output_text = await model.SendInputAsync(input_prompt);
            
                PUPostProcessing(output_text, agent);
            }
        }

        /// <summary>
        /// LLM for action-prediction
        /// </summary>
        /// <param name="agent"></param> the agent who needs to reply to query
        /// <param name="withContext"></param> 
        /// <returns></returns>
        public async Task<(AgentState, Core.Actions.Action[])> NextStepPrediction(AgentState state, Agent agent, int retryCount = 5)
        {
            AgentState copy_state = state.Copy();
            int agentId = state.currentAgentId;
            string host = entityNames[agentId];
            string speaker = entityNames[1 - agentId];
            string[] strs = InstructionNextAction(agentId, host, speaker, agent.preferences[agent.id][1-agent.id]);
            string query = strs[1].Replace("{orientation_interpretation}", ExplicationOfMoves(state)) ;
            string input_prompt = string.Join("#", "AP", " ", host, strs[0], agent.ParametersToLLM(hypothesis), query);
            //input_prompt += "image: " + agent.perception;      
            string output_text = await model.SendInputAsync(input_prompt);
            Console.WriteLine("Output from LLM: \n" + output_text);
            try
            {
                LLMOutput llmOutput = JsonConvert.DeserializeObject<LLMOutput>(output_text);
                return APPostProcessing(llmOutput, copy_state);
            }
            catch (JsonException ex)
            {
                Console.WriteLine("JSON DeserializeObject failed: " + ex.Message);
                if (retryCount > 0)
                    return await NextStepPrediction(state, agent, retryCount - 1);
                else
                    throw new Exception("Max retries reached. Could not parse LLM output.", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("APPostProcessing failed: " + ex.Message);
                if (retryCount > 0)
                    return await NextStepPrediction(state, agent, retryCount - 1);
                else
                    throw new Exception("Max retries reached. Post-processing failed.", ex);
            }
        }

        public async Task<string> LastQuestion(Agent agent)
        {
            string host = entityNames[agent.id];
            string speaker = entityNames[1 - agent.id];
            string input_prompt = string.Join("#", "LQ", speaker, host, "None", agent.ParametersToLLM(hypothesis), contextPrompt.LastQuestion[agent.id].Replace("{host}", host).Replace("{speaker}", speaker));
            string output_text = await model.SendInputAsync(input_prompt);
            Console.WriteLine("Final question asked! ");
            return output_text;
        }

        /// <summary>
        /// Post-processing of action prediction
        /// </summary>
        /// <param name="output_text"></param> output from LLM
        /// <param name="agent"></param> 
        /// <returns></returns>
        (AgentState agent, Core.Actions.Action[] action) APPostProcessing(LLMOutput llmOutput, AgentState agent){
            int agentId = agent.currentAgentId;

            Core.Actions.Action emoAction = new Core.Actions.Action((int agentId, AgentState agent) =>
            {
                return agent;
            }, ActionType.Idle);
                
            agent.emotions[agentId].Physiological.Pos = llmOutput.emotion.physiologicalexpression.positive;
            agent.emotions[agentId].Physiological.Neg = llmOutput.emotion.physiologicalexpression.negative;
            agent.emotions[agentId].Physiological.Val = agent.emotions[agentId].Physiological.Pos - agent.emotions[agentId].Physiological.Neg;

            agent.emotions[agentId].Facial.Pos = llmOutput.emotion.facialexpression.positive;
            agent.emotions[agentId].Facial.Neg = llmOutput.emotion.facialexpression.negative;
            agent.emotions[agentId].Facial.Val = agent.emotions[agentId].Facial.Pos - agent.emotions[agentId].Facial.Neg;

            agent.emotions[agentId].Felt.Pos = llmOutput.emotion.feltexpression.positive;
            agent.emotions[agentId].Felt.Neg = llmOutput.emotion.feltexpression.negative;
            agent.emotions[agentId].Felt.Val = agent.emotions[agentId].Felt.Pos - agent.emotions[agentId].Felt.Neg;

            if (hypothesis >= 1 && agentId > 0)
            {
                foreach (var preference in llmOutput.preference)
                {
                    int localEntityId = Array.FindIndex(entityNames, e => e.ToLower().Equals(preference.Key, StringComparison.OrdinalIgnoreCase));

                    if (llmOutput.CleanPreference(preference.Value, out double new_preference))
                    {
                        agent.preferences[agentId][localEntityId] = new_preference;
                    }
                }
                agent.preferences[agentId] = StateModifier.NormalizePref(agent.preferences[agentId]);
            }

            Core.Actions.Action action;
            if (llmOutput.move.action.Contains("move"))
            {
                Vertex lookAt = agentId == 0 ? new Vertex(0, 0, 1) : new Vertex(0, 0, -1);
                double angle = Math.Atan2(lookAt.Z, lookAt.X);
                moveDirectionMap.TryGetValue(llmOutput.move.direction, out int d);
                Vertex orientation = new Vertex(
                    Math.Cos(angle + d * Math.PI / 4),
                    0,
                    Math.Sin(angle + d * Math.PI / 4)
                );
                action = new Core.Actions.Action((int agentId, AgentState agent) =>
                {
                    agent.targetIds[agentId] = -1;
                    return agent.actionDirectories[agentId].AgentMovesTowardsDirection2(agentId, agent, orientation);
                }, ActionType.Walk);
            }
            else if (llmOutput.move.action.Contains("rotate"))
            {
                int localEntityId = Array.FindIndex(entityNames, e => e.ToLower().Equals(llmOutput.move.direction, StringComparison.OrdinalIgnoreCase));

                action = new Core.Actions.Action((int agentId, AgentState agent) =>
                {
                    if (agentId == localEntityId)
                        return agent;
                    agent.targetIds[agentId] = localEntityId;
                    agent = agent.actionDirectories[agentId].AgentRotatesTowardsPoint(agentId, agent, agent.objectBodies[localEntityId].BodyPosition.Center);
                    return agent;
                }, ActionType.Rotate, localEntityId);
            }
            else if (llmOutput.move.action.Contains("stay idle"))
            {
                action = new Core.Actions.Action((int agentId, AgentState agent) =>
                {
                    return agent;
                }, ActionType.Idle);
            }
            else
            {
                throw new Exception($"Unsupported action: {llmOutput.move.action}");
            }

            AgentState outstate = action.Execute(agentId, agent);
            return (outstate, new Core.Actions.Action[]{action, emoAction});
        }

        /// <summary>
        /// Post-processing of preference updating
        /// </summary>
        /// <param name="message"></param> updation from LLM
        /// <param name="agent"></param> agent state
        /// <returns></returns>
        void PUPostProcessing(string message, Agent agent)
        {
            if (message.Length>0){
                double[][] pref_variation = new double[Agent.AgentsIds.Length][];

                foreach (int agentId in Agent.AgentsIds)
                {
                    pref_variation[agentId] = new double[entityNames.Length];
                    for (int entityId = 0; entityId < entityNames.Length; entityId++)
                    {
                        pref_variation[agentId][entityId] = 0.5;
                    }
                }

                string[] preference_list = message.Split(", ");
                foreach (string triple in preference_list)
                {

                    int firstIndex = triple.IndexOf('|');
                    int secondIndex = triple.IndexOf('|', firstIndex + 1);

                    string part1 = triple.Substring(0, firstIndex);
                    string part2 = triple.Substring(firstIndex + 1, secondIndex - firstIndex - 1);
                    string part3 = triple.Substring(secondIndex + 1);

                    int line = -1;
                    for (int agentId = 0; agentId < Agent.AgentsIds.Length; agentId++) {
                        if (part1.ToLower().Contains(entityNames[agentId].ToLower()))
                        {
                            line = agentId; break;
                        }
                    }

                    int col = -1;
                    for (int entityId = 0; entityId < entityNames.Length; entityId++)
                    {
                        if (part2.ToLower().Contains(entityNames[entityId].ToLower())) {  
                            col = entityId; break;
                        }
                    }
                    
                    if ((line >= 0) && (col >= 0)) {
                        if (part3.Contains("more negative") || part3.Contains("less positive"))
                        {
                            pref_variation[line][col] = 0;
                        }
                        if (part3.Contains("more positive") || part3.Contains("less negative"))
                        {
                            pref_variation[line][col] = 1;
                        }
                    }
                }
                agent.preferencesUpdated = true;
                agent.preferences = PCM.Core.Utils.Copy.Copy2DDouble(pref_variation);
                Console.Write("preference update by LLM: ");
                foreach (var line in agent.preferences){
                    foreach (var element in line){
                        Console.Write(element + " ");
                    }
                }
            }
        }

        public async Task InitModel(string message){
            await model.InitAsync(message);
        }
    }


}
