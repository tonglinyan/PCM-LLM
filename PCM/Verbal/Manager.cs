using PCM.Core.FreeEnergy.State;
using PCM.Verbal.LLM;
using MathNet.Numerics.LinearAlgebra;
using PCM.Schemas;
using PCM.Core.SceneObjects;
using static PCM.Core.Utils.Tree;
using PCM.Core.Actions;

namespace PCM.Verbal
{
    public class Manager
    {
        const int ARTIFICIAL_ID = 0;
        const int PARTICIPANT_ID = 1;
        double preferenceUpdateWeight = 0.5f;
        string participantContext;
        string[] entityNames;
        List<Agent> agents;
        Agent participant, artificialAgent;
        Interface interfaceLLM;
        bool withContext = false;
        string filePath;

        public Manager(Schemas.Verbal parameters, ArtificialAgentMode aa_mode)
        {
            preferenceUpdateWeight = parameters.PreferenceUpdateWeight;
            entityNames = parameters.EntityNames;
            var contextPrompt = ContextPromptParser.FromJson(parameters.ContextJSON);
            interfaceLLM = new Interface(contextPrompt, entityNames, parameters, aa_mode);
            participantContext = contextPrompt.UserContext;
            Agent.PCM_CoreUpdatesMemoryLength = parameters.PCM_CoreUpdatesMemoryLength;
            Agent.Depth = parameters.Depth;
            filePath = parameters.FilePath;
        }

        public void InitAgents(AgentState[] agentStates)
        {
            agents = agentStates.Select(agentPCM => new Agent(agentPCM)).ToList();

            participant = agents[PARTICIPANT_ID];
            artificialAgent = agents[ARTIFICIAL_ID];
            participant.Listen("system", participantContext.Replace("{host}", entityNames[1]).Replace("{speaker}", entityNames[0]));
            interfaceLLM.nbAgents = agents.Count; 
        }


        public async Task<(AgentState outstate, ObjectBody pos, List<AgentState> seq, ActionType actionType)> LLMPrediction(AgentState state)
        {
            AgentState copy_state = state.Copy();
            (AgentState state, Core.Actions.Action[] actions) prediction = await interfaceLLM.NextStepPrediction(state, agents[state.currentAgentId]);

            predictionTree(copy_state, prediction);
            List<AgentState> seq = new List<AgentState> {copy_state, prediction.state};

            return (prediction.state, prediction.state.objectBodies[prediction.state.currentAgentId], seq, prediction.actions[0].GetActionType());
        }

        public void predictionTree(AgentState rootState, (AgentState state, Core.Actions.Action[] actions) prediction)
        {
            
            var root = new Node<AgentStateNode>(new (rootState, rootState.actionDirectories[rootState.currentAgentId]._dfsList[0].value));
            var childNode = new Node<AgentStateNode>(new(prediction.state, prediction.actions));

            root.AddChild(childNode);

            root.value.bestNode = true;
            root.value.score = 0;
            childNode.value.bestNode = true;
            root.value.score = 0;

            rootState.node = root;
            prediction.state.node = childNode;

        }

        /// <summary>
        /// LLM acting as participant, and generating the first question.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ParticipantAsks(TextImageData data)
        {
            if (data.text != "null")
            {
                participant.Listen(entityNames[ARTIFICIAL_ID], data.text);
            }
            participant.See(data.image);

            await interfaceLLM.Interaction(participant, withContext);
            /*if (!withContext)
            {
                withContext = true;
            }*/
            participant.GetLastMessageListened(out string question, out string _);

            return question;
        }

        /// <summary>
        /// LLM acting as participant, answering the question.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ArtificialAgentAnswers(TextImageData data)
        {
            withContext = true;
            artificialAgent.See(data.image);
            artificialAgent.Listen(entityNames[PARTICIPANT_ID], data.text);
            //string question = data.Question;
            /*if (question != "null"){
                participant.Listen(entityNames[PARTICIPANT_ID], question);
            }*/
   
            await interfaceLLM.Interaction(artificialAgent, withContext);
            artificialAgent.GetLastMessageListened(out string answer, out string _);

            return answer;
        }

        public async Task LastQuestion()
        {
            for (int agentId = 0; agentId < agents.Count(); agentId++)
            {
                await interfaceLLM.LastQuestion(agents[agentId]);
            }
            Console.WriteLine("executed");
        }


        public AgentState UpdatePostPreferences(AgentState updatedState, int agentId)
        {
            var agent = agents[agentId];
            updatedState.postPreferences = PCM.Core.Utils.Copy.Copy2DDouble(updatedState.preferences);
            if (!agent.preferencesUpdated) return updatedState;

            /*updatedState.postPreferences = (
                Matrix<double>.Build.DenseOfRowArrays(agent.preferences) * preferenceUpdateWeight + 
                Matrix<double>.Build.DenseOfRowArrays(updatedState.postPreferences) * (1 - preferenceUpdateWeight)
            ).PointwiseMaximum(0).PointwiseMinimum(1).ToRowArrays();*/

            updatedState.postPreferences = (
                Matrix<double>.Build.DenseOfRowArrays(agent.preferences).Map(v => v == 0.5 ? 0 : v) * preferenceUpdateWeight +
                Matrix<double>.Build.DenseOfRowArrays(updatedState.preferences).MapIndexed((i, j, v) => agent.preferences[i][j] == 0.5 ? v : v * (1 - preferenceUpdateWeight))
            ).PointwiseMaximum(0).PointwiseMinimum(1).ToRowArrays();


            /*int rows = updatedState.postPreferences.Length;
            int cols = updatedState.postPreferences[0].Length;
            double[][] newPostPreferences = new double[rows][];

            for (int i = 0; i < rows; i++)
            {
                newPostPreferences[i] = new double[cols];
                for (int j = 0; j < cols; j++)
                {
                        double logitValue = Math.Log( updatedState.postPreferences[i][j] / (1 - updatedState.postPreferences[i][j]));
                        logitValue += preferenceUpdateWeight * agent.preferences[i][j];
                        newPostPreferences[i][j] = 1 / (1 + Math.Exp(-logitValue ));;
                }
            }
            updatedState.postPreferences = newPostPreferences; 


            StateModifier.NormalisePref(updatedState.postPreferences[1]);               
            if (agentId == 1)
                StateModifier.NormalisePref(updatedState.postPreferences[0]);*/
            if (agentId == 0)
                updatedState.postPreferences[0] = updatedState.preferences[0];

            agent.preferencesUpdated = false;
            agent.Update(updatedState);

            return updatedState;
        }

        public AgentState UpdatePreferences(AgentState agentState){
            agentState.preferences = agentState.postPreferences;
            return agentState;
        }


        public void UpdateAgents(List<AgentState>[] predictions)
        {
            for (int agentId = 0; agentId < predictions.Length; agentId++)
            {
                agents[agentId].Update(predictions[agentId]);
            }
        }

        public async Task InitLLM(){
            await interfaceLLM.InitModel(filePath);
        }
    }
}