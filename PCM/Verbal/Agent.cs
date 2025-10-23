using MongoDB.Driver.Linq;
using PCM.Core.FreeEnergy.State;
using SharpCompress.Common;
using static PCM.Verbal.LLM.Interface;

namespace PCM.Verbal
{
    public class Agent
    {
        readonly Dialog _dialog;
        public List<List<string>> triplesList;
        public readonly int id;
        public static int[] AgentsIds { get; private set; }
        public static int NbEntities { get; private set; }
        public double[][] preferences;
        public bool preferencesUpdated = false;
        public static int PCM_CoreUpdatesMemoryLength { private get; set; }
        public static int Depth { private get; set; }
        public string perception;

        public Agent(AgentState agentPCM)
        {
            _dialog = new Dialog();
            id = agentPCM.currentAgentId;
            triplesList = new();
            triplesList.Add(new List<string> { ParametersToTriples(agentPCM) });
            if (AgentsIds == null)
            {
                AgentsIds = agentPCM.agentsIds;
                NbEntities = agentPCM.objectBodies.Length;
            }
            preferences = Core.Utils.Copy.Copy2DDouble(agentPCM.preferences);
        }

        /// <summary>
        /// Add new message to the dialog turns
        /// </summary>
        /// <param name="role"></param> person who gave this message
        /// <param name="content"></param> reply
        public void Listen(string role, string content)
        {
            _dialog.AddNewMessage(role, content);
        }

        /// <summary>
        /// Add new visual perception
        /// </summary>
        /// <param name="image"></param> visual perception
        public void See(string image)
        {
            perception = image;
        }

        /// <summary>
        /// Get the input query from the dialog turns
        /// </summary>
        /// <param name="text"></param> query
        /// <param name="speaker"></param> person (role) who gave this query
        public void GetLastMessageListened(out string text, out string speaker)
        {
            _dialog.GetLastMessage(out text, out speaker);
        }

        /// <summary>
        /// Insert prediction from PCM, including current belief, prediction tau 1 until prediction tau DP
        /// </summary>
        /// <param name="PCM_Agent"></param>
        public void Update(List<AgentState> prediction)
        {
            var triples = new List<string>();
            for (int depth_i = 0; depth_i <= Depth; ++depth_i)
            {
                triples.Add(ParametersToTriples(prediction[depth_i]));
            }

            triplesList.Insert(0, triples);
            if (triplesList.Count > PCM_CoreUpdatesMemoryLength)
                triplesList.RemoveAt(PCM_CoreUpdatesMemoryLength);
        }

        /// <summary>
        /// Insert prediction from LLM, modify the prediction tau 1
        /// </summary>
        /// <param name="prediction"></param>
        public void Update(AgentState PCM_Agent)
        {
            var triples = triplesList[0];
            if (triples.Count > 2)
                triples[1] = ParametersToTriples(PCM_Agent);
        }

        public string ParametersToLLM(int hypothesis = -1)
        {
            string text = "";

            for (int iter = 0; iter < triplesList.Count; iter++)
            {
                var triples = triplesList[iter];
                var parts = new List<string>();

                for (int depth = 0; depth < triples.Count; depth++)
                {
                    string timestep;

                    if (iter == 0)
                        timestep = "belief at t step";
                    else if (depth == 0)
                        timestep = $"belief at t-{iter} step";
                    else if (depth == 1)
                        timestep = $"predicted belief at t-{iter} step for the next step";
                    else
                        timestep = $"predicted belief at t-{iter} step for next {depth} steps";

                    var extractedTriples = Extraction(triples[depth], hypothesis);
                    parts.Add("'" + timestep + "': {" + extractedTriples + "}, ");
                }

                string TextForInteraction = string.Join(", ", parts);
                text = TextForInteraction + (string.IsNullOrEmpty(text) ? "" : ", ") + text;
            }

            return text;
        }

        public string Extraction(string TriplesString, int hypothesis)
        {
            string[] triples = TriplesString.Split(", ");
            List<string> selectedTriples = new List<string>();
            switch (hypothesis) {
                case -1:
                    return TriplesString;
                case 0:
                    foreach (var tri in triples)
                    {
                        if (id == 0)
                        {
                            if (tri.Contains("preference"))
                            {
                                selectedTriples.Add(tri);
                            }
                        }
                        if (tri.Contains("orientation") || tri.Contains("position") || tri.Contains("valence"))
                            selectedTriples.Add(tri);
                    }
                    break;
                case 1:
                    foreach (var tri in triples)
                    {
                        if (tri.Contains("orientation") || tri.Contains("position") || tri.Contains("valence") || tri.Contains("preference"))
                            selectedTriples.Add(tri);
                    }
                    break;
                case 2: 
                    foreach (var tri in triples)
                    {
                        if (tri.Contains("orientation") || tri.Contains("position") || tri.Contains("valence") || tri.Contains("preference") || tri.Contains("theory of mind"))
                            selectedTriples.Add(tri);
                    }
                    break;
            }
            return String.Join(", ", selectedTriples);
        }
    }
}