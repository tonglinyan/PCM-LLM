using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using static Core.Interfacing;
using Core.Types;
using Newtonsoft.Json;
using System.Collections;
using UnityEditor;
using Schemas;
using TMPro;
using Exp;
using Verbal;
using UnityEngine.Networking;

namespace Simulation
{
    [RequireComponent(typeof(Streamer))]
    [RequireComponent(typeof(Config))]

    public class Manager : MonoBehaviour
    {
        [System.Serializable]
        public class IDInfo
        {
            public int ID;
            public GameObject Entity;
        }

        public static Manager Singleton { get; private set; }

        public int nbThreadMax = 2; // used for multi-threading for time prediciton optimisation

        [Header("Server config")]
        readonly HttpClient client = new();
        string message = string.Empty;
        [SerializeField] string ipAddress = "localhost";
        [SerializeField] string serverPort;

        [Header("PCM")]
        [SerializeField] float timeUntilEnd = 3f;

        int[] agentsPoints;
        //public int NB_AGENTS;
        public const int NB_BOXES = 2;
        public const int NB_AGENTS = 2;
        public const int PARTICIPANT_ID = 1;
        public const int AA_ID = 0;
        private ArtificialAgentMode AA_Mode;
        [SerializeField] int simuIteration = 0;

        [Header("PCM Core")]
        [SerializeField] int m_depthOfProcessing = 1; // number of consecutive states expected at each prediction
        [SerializeField] float m_timeBetweenUpdates = 1f;
        
        public Config config;
        private Streamer stream;
        public bool suspended = false;
        
        string m_configFilename;
        float m_timeSinceUpdate = 0;
        
        AgentMainController[] m_agents;
        Box[] m_boxes;
        private bool player;
        private PlayerManager m_player;
        private int playerID;

        MonoBehaviour[] m_entities;
        readonly Dictionary<GameObject, int> m_entityIDs = new();
        IDInfo[] m_idInfos;
        string[] m_entityNames;

        Output m_currentPrediction = null;
        bool isRunningPCM = false;
        bool isProcessing = false;
        Verbal.VerbalUpdatedManager verbalManager;

        [Space]
        [Header("Score")]
        public TMP_Text[] textScores;

        [Space]
        [Header("Attention management")]
        [Range(0, 1)][SerializeField] float distanceProbe = 0.3f;
        public int gazeDistance = 3;
        [Range(0, 0.1f)][SerializeField] float threshold = 0.05f;
        private const int MinRealCoordinate = -440;
        private const int MaxRealCoordinate = 440;
        private const int BoardSize = 11;
        private const int RealRangePerCell = (MaxRealCoordinate - MinRealCoordinate) / BoardSize;


        [Space]
        [Header("Debug / Tests")]
        //[SerializeField] private bool useDebugServer = false;
        private Task currentTask = null;
        [SerializeField] private bool logPredictionTime;
        public bool boxOpened = false;
        public string[] EntityNames {
            get => m_entityNames;
        }
        public string ConfigFileName
        {
            get => m_configFilename;
        }
        public AgentMainController[] Agents
        {
            get => m_agents;
        }
        public Box[] Objects
        {
            get => m_boxes;
        }

        public bool Player
        {
            get { return player; }
        }

        public bool LogPredictionTime
        {
            get => logPredictionTime;
            private set
            {
                logPredictionTime = value;
            }
        }
        public class CertificateHandlerOverride : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                // Always return true, bypassing certificate validation
                return true;
            }
        }


        private SimulationParametersManager simulationParameters;

        #region Awake
        void Awake()
        {
            Singleton = this;
            
            config = GetComponent<Config>();
            stream = GetComponent<Streamer>();

            agentsPoints = new int[NB_AGENTS];
            if (ScenesManager.Singleton != null)
            {
                simulationParameters = ScenesManager.Singleton.CurrentConfig;
                m_depthOfProcessing = simulationParameters.Dp;
                config.SetConfig(simulationParameters);
                agentsPoints = ScenesManager.Singleton.AgentPoint;
                textScores[AA_ID].text = "Agent: " + agentsPoints[AA_ID].ToString();
                textScores[PARTICIPANT_ID].text = "Player: " + agentsPoints[PARTICIPANT_ID].ToString();
            }
        }
        #endregion

        #region Start
        async void OnEnable()
        {
            //EnableCanvas();
            player = (config.Player != null);

            m_configFilename = config.ConfigFileName;
            m_agents = config.Agents;
            m_boxes = config.Boxs;
            //foreach (var box in m_boxes) { box.enabled = false; }
            AA_Mode = config.VirtualAgentMode;

            SelectRewardBox();

            if (player)
            {
                playerID = config.Agents.Length;
                m_player = config.Player;
                m_player.Activate = true;
                m_entities = new MonoBehaviour[m_agents.Length + m_boxes.Length + 1];
                m_agents.CopyTo(m_entities, 0);
                m_entities[m_agents.Length] = m_player;
                m_boxes.CopyTo(m_entities, m_agents.Length + 1);
            }
            else
            {
                m_entities = new MonoBehaviour[m_agents.Length + m_boxes.Length];
                m_agents.CopyTo(m_entities, 0);
                m_boxes.CopyTo(m_entities, m_agents.Length);
            }

            for (var i = 0; i < m_entities.Length; i++)
            {
                var m_entity = m_entities[i];
                //Debug.Log(m_entity.gameObject.name);
                m_entityIDs[m_entity.gameObject] = i;
            }

            client.BaseAddress = new Uri($"https://{ipAddress}:{serverPort}/");
            verbalManager = VerbalUpdatedManager.Singleton;
            if (ScenesManager.Singleton != null)
            {
                verbalManager.SetConfig(simulationParameters);
            }
            

            if (isRunningPCM == false)
            {
                await SendParameters();//Envoi des données d'initialisation (Tom, depth...)
                await ProcessRepositories(GetCurrentWorldState());
                isRunningPCM = true;
            }


            verbalManager.Init(client, m_agents, player, AA_Mode);

            /*
            verbalManager.Init(client, m_agents);
            /*if (player)
            {
                if (AA_Mode == ArtificialAgentMode.NonVerbal) return;
                else await verbalManager.PlayerCommunicate(nbIterations);
            }
            else
            {
                await verbalManager.Communicate(nbIterations, AA_Mode);
                await ParticipantChooses();
            }*/
        }

        public void SavingInitState(Schemas.Core coreParameters)
        {
            AgentOutput[] agentOutputs = new AgentOutput[m_agents.Length];
            for (int i=0; i<m_agents.Length; i++)
            {
                AgentMainController agentState = m_agents[i];
                agentOutputs[i] = new AgentOutput()
                {
                    Id = i,
                    Body = agentState.GetAgentBodyData(),
                    Emotions = agentState.emotions,
                    Action = ActionType.Idle,
                    Preferences = coreParameters.Agents.Preferences[i],
                    TomPredict = coreParameters.Agents.TomPredict[i],
                    Mu = new double[m_entities.Length],
                    Sigma = new double[m_entities.Length],
                    TargetId = -1,
                    InteractObjectId = -1,
                    FE = new double[m_entities.Length],
                };
            }
            stream.SavingInitData(agentOutputs, m_boxes, m_player, player);
        }

        public void SetSimulationFileConfig(string fileName) {
            m_configFilename = fileName;
        }

        void OnDestroy()
        {
            Debug.Log("Stop PCM process");
            isRunningPCM = false;
            StopPCM();
        }

        public async void StopPCM()
        {
            HttpResponseMessage response = await client.PostAsync($"api/stop", null); // api/UnityPCM est le endpoint de l'API qui permet d'effectuer les calculs à partir du worldstate pour générer le PCM_Output
            response.EnsureSuccessStatusCode();
        }

        public void RestartPCM()
        {
            //TODO initialisation PCM api :InitializePCMThread();
        }

        #endregion

        #region Update
        #region FixedUpdate
        async void FixedUpdate()
        {
            if (isRunningPCM)
            {
                m_timeSinceUpdate += Time.fixedDeltaTime;
                if (TimeToSendUpdateToPCM)
                {
                    if (!isProcessing && !suspended) 
                    {
                        if (simuIteration < 20)
                        {
                            isProcessing = true;
                            currentTask = HandlePCMPrediction().ContinueWith(_ => isProcessing = false);
                        }
                        else
                        {
                            if (simuIteration == 20)
                            {
                                EnableBox();
                                StopPCM();
                                currentTask = ParticipantChoosesCloserBox().ContinueWith(_ => isProcessing = false);
                                simuIteration++;
                            }
                        }
                    }
                    m_timeSinceUpdate = 0f;
                }
            }
        }


        async Task<string> SendParameters()
        {
            var verbalParameters = verbalManager.GetParameters();
            m_entityNames = verbalParameters.EntityNames;           
            var coreParameters = JsonConvert.DeserializeObject<Schemas.Core>(File.ReadAllText($"Assets/StreamingAssets/{m_configFilename}.json"));
            foreach (var position in coreParameters.Objects.Positioning)
            {
                position.Position = ExtensionTools.ToScaledArray(position.Position, false);
                position.LookAt = ExtensionTools.ToUnscaledArray(position.LookAt, false);
            }
            foreach (var position in coreParameters.Agents.Positioning)
            {
                position.Position = ExtensionTools.ToScaledArray(position.Position, false);
                position.LookAt = ExtensionTools.ToUnscaledArray(position.LookAt, false);
            }
            SavingInitState(coreParameters);
            var nonVerbalParameters = new Schemas.NonVerbal
            {
                Core = coreParameters,
                Depth = m_depthOfProcessing,
                NbThreadMax = nbThreadMax,
                NbIterations = verbalManager.NbInterations,
                PlayerID = playerID,
                SaveBestPath = stream.streaming == StreamingMode.Never? false: true,
                FilePath = verbalParameters.filePath,
            };
            var parameters = new Schemas.Parameters()
            {
                Verbal = verbalParameters,
                NonVerbal = nonVerbalParameters
            };
            var content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json"); // Conversion en HttpContent
            Debug.Log("parameters:   " + JsonConvert.SerializeObject(parameters));
            HttpResponseMessage response = await client.PostAsync($"api/initialize-parameters?AA_Mode={(int)AA_Mode}", content); // api/initialize-parameters est le endpoint de l'API qui permet d'effectuer les calculs à partir du worldstate pour générer le PCM_Output
            response.EnsureSuccessStatusCode();
            message = await response.Content.ReadAsStringAsync();
            //Debug.Log(message);
            return message;
        }

        async Task<string> ProcessRepositories(Core.Interfacing.Input currentWorldState)
        {
            var json = JsonConvert.SerializeObject(currentWorldState);
            Debug.Log("Connexion au serveur...");
            Debug.Log("currentWorldState:   " + json);
            //Conversion du currentWorldState en string pour le passer dans la requête API
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            // api/UnityPCM est le endpoint de l'API qui permet d'effectuer les calculs à partir du worldstate pour générer le PCM_Output
            HttpResponseMessage response = await client.PostAsync($"api/compute-worldstate", content); 
            message = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            //JsonConvert.DeserializeObject<Output>(response);
            Debug.Log(message);
            //Debug.Log("------------- PCM_OUTPUT reçu : " + message.ToString());
            return message;
        }


        #region Send update to PCM
        private Core.Interfacing.Input GetCurrentWorldState()
        {
            m_idInfos = new IDInfo[m_entityIDs.Count];
            int i = 0;
            foreach (GameObject entity in m_entityIDs.Keys)
            {
                m_idInfos[i] = new IDInfo
                {
                    Entity = entity,
                    ID = m_entityIDs[entity]
                };
                i++;
            }

            Core.Interfacing.Input pcmUpdate = new()
            {
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Agents = GetCurrentAgentStates(),
                Objects = GetCurrentObjectStates()
            };
            //Debug.Log(JsonConvert.SerializeObject(pcmUpdate));
            return pcmUpdate;
        }

        Dictionary<int, AgentInput> GetCurrentAgentStates()
        {
            Dictionary<int, AgentInput> agentStates = new();
            for (int robotID = 0; robotID < m_agents.Length; robotID++)
            {
                //if (robotID != playerID) { }
                AgentMainController agent = m_agents[robotID];
                int targetEntityID = agent.CurrentGrabbedObject == null ? -1 : m_entityIDs[agent.CurrentObjectOfInterest.gameObject];

                AgentInput agentState = new()
                {
                    Id = robotID,
                    Emotions = agent.emotions,
                    Body = agent.GetAgentBodyData(),
                    TargetEntityId = targetEntityID,
                };
                agentStates[agentState.Id] = agentState;
            }

            if (player)
            {
                AgentInput playerState = new()
                {
                    Id = playerID,
                    Emotions = m_player.GetEmotions(),
                    Body = m_player.GetPlayerBodyData(),
                    TargetEntityId = -1,
                };
                agentStates[playerID] = playerState;
                Debug.Log("player state got");
            }
            return agentStates;
        }

        Dictionary<int, Core.Interfacing.Entity> GetCurrentObjectStates()
        {
            Dictionary<int, Core.Interfacing.Entity> objectStates = new();

            for (int objectID = 0; objectID < m_boxes.Length; objectID++)
            {
                Core.Interfacing.Entity objectState = new()
                {
                    Id = objectID,
                    Body = m_boxes[objectID].GetBodyData()
                };
                objectStates[objectState.Id] = objectState;
            }
            return objectStates;
        }
        #endregion

        #region Handle new prediction

        async Task HandlePCMPrediction()
        {
            Core.Interfacing.Input currentWorldState = GetCurrentWorldState();
            if (player && m_player.NeedRequestPlayerEmotion())
            {
                currentWorldState.PlayerEmotion = m_player.RequestPlayerEmotion();
                Debug.Log("Send special update emotion: " + currentWorldState.PlayerEmotion.valence);
            }
            
            string response = await ProcessRepositories(currentWorldState); //Création de la requête API et attente de la réponse (PCM_Output)
            Debug.Log("output received! ");
            Output prediction = JsonConvert.DeserializeObject<Output>(response);

            AgentOutput[] agentOutputs = new AgentOutput[prediction.AgentStates.Count];
            foreach (var agent in prediction.AgentStates)
            {
                AgentOutput agentOutput = agent.Value[1];
                agentOutputs[agent.Key] = agentOutput;
            }
            
            if (!IsNewPrediction(prediction)) return;

            foreach (int agentID in prediction.AgentStates.Keys)
            {
                if (agentID == playerID)
                {
                    continue;
                }

                #region Debug (robots + vr player) - display in inspector only
                //AgentOutput lastFutureState = prediction.AgentStates[agentID].Last();

                #endregion

                #region Next state
                AgentOutput nextState = prediction.AgentStates[agentID][1];
                Body nextBody = nextState.Body;
                Core.Interfacing.EmotionSystem nextEmotions = nextState.Emotions;
                ActionType nextAction = nextState.Action;
                int nextActionTarget = nextState.TargetId;
                int nextActionInteractObject = nextState.InteractObjectId;

                ObjectOfInterest interactObject = nextActionInteractObject == -1 ? null : (ObjectOfInterest)m_entities[nextActionInteractObject];

                // TODO: handle player data
                AgentMainController agent = m_agents[agentID];

                Vector3 currentPosition = agent.transform.position;
                Vector3 nextPosition = nextBody.Center.FromPCMVector3(false);
                Vector3 nextForward = nextBody.Orientation.FromPCMVector3(false);
                nextForward.y = 0;
                nextForward = nextForward.normalized;
                
                switch (nextAction)
                {
                    case ActionType.Walk:
                        
                        Vector3 Orientation = agentID == 0? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
                        agent.SetMovingTo(nextPosition, Orientation);
                        //var body = agent.GetAgentBodyData();
                        //Debug.DrawRay(nextPosition, nextForward, Color.red, 10);
                        if (interactObject != null)
                        {
                            agent.SetFetchingObject(interactObject);
                        }
                        Stare(agent, true, nextActionTarget, nextPosition, nextForward, agentID);
                        break;
                    case ActionType.Rotate:
                        if (interactObject != null)
                        {
                            nextForward = interactObject.transform.position - currentPosition;
                            nextForward.y = 0;
                            nextForward.Normalize();
                        }
                        agent.SetRotationTarget(nextForward);
                        Stare(agent, true, nextActionTarget, nextPosition, nextForward, agentID);
                        break;
                    case ActionType.Stare:
                        Stare(agent, true, nextActionTarget, nextPosition, nextForward, agentID);
                        break;

                    case ActionType.Grab:
                        Debug.Log(transform.name + " :Try to grab: " + nextState.InteractObjectId);
                        if (interactObject != null)
                        {
                            agent.SetInteractingWithObject(interactObject);
                        }
                        else
                        {
                            Debug.Log(transform.name + " :Target is null");
                        }
                        break;
                    case ActionType.LetGo:
                        Debug.Log(transform.name + " :LetGo");
                        agent.StopInteracting();
                        break;
                }
                Stare(agent, false, nextActionTarget, nextPosition, nextForward, agentID);
                agent.SetEmotionParameters(nextEmotions);
                #endregion

                #region Last anticipated state

                //if (m_depthOfProcessing > 1)
                //{
                //    PCM.Interfacing.Body futureBody = lastFutureState.Body;
                //    int futureTargetID = lastFutureState.TargetEntityId;
                //    ObjectOfInterest futureTarget = futureTargetID < m_agents.Length ? null : m_objects[futureTargetID - m_agents.Length];
                //    if (futureTarget != null)
                //    {
                //        agent.SetLookingAtSecondaryTarget(futureTarget);
                //    }
                //    else
                //    {
                //        agent.SetLookingAtSecondaryTarget(futureBody.Center.FromPCMVector3(false));
                //    }
                //}
                #endregion

                #region Debug
                //  Debug.Log($"{robot.name} (id {agentID}) - Action : {nextAction} | Has a target ? {target != null} | Emotion valence : {nextEmotion.Valence} | Emotion positive : {nextEmotion.Positive}");
                //Debug.DrawLine(currentPosition, nextPosition, Color.green, 1f);
                //Debug.DrawLine(currentPosition + Vector3.up, currentPosition + targetForward + Vector3.up, Color.blue, 1f);
                //Debug.Log("robot : " + agentID + "  val :   " + nextState.EmotionFelt.Valence           +   "     "+ nextState.EmotionExpressed.Valence  +   "   action  :  "  + nextState.Action.ToString());

                #endregion

                var json = JsonConvert.SerializeObject(prediction);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                string message = await content.ReadAsStringAsync();
                m_currentPrediction = prediction;
            }
            isProcessing = false;
            stream.SavingData(agentOutputs, m_agents, m_boxes, m_player, player);
            
            Debug.Log(simuIteration);
            simuIteration ++;
            
            if (!suspended)
            {
                suspended = simuIteration % 6 == 0;
                if (suspended) verbalManager.State = InteractionState.Starting;
            }
        }

        private bool IsNewPrediction(Output prediction)
        {
            if (prediction == null) return false;
            if (m_currentPrediction == null) return true;

            return prediction.TimeStamp > m_currentPrediction.TimeStamp;
        }
        #endregion
        #endregion
        #region Properties
        private bool TimeToSendUpdateToPCM
        {
            get { return m_timeSinceUpdate >= m_timeBetweenUpdates; }
        }

        #endregion
        #endregion
        
        #region overt gaze
        void Stare(AgentMainController agent, bool forced,int nextActionTarget, Vector3 nextPosition, Vector3 nextForward, int agentID)
        {
            var position = CompetitionEndoExo(agent, forced, nextActionTarget, nextPosition, nextForward, agentID);
            Debug.DrawLine(agent.GetLookAtOrigin(), position, Color.red, 10);
            agent.SetLookingAtSecondaryTarget(position);
        }

        Vector3 CompetitionEndoExo(AgentMainController agent, bool forced, int nextActionTarget, Vector3 nextPosition, Vector3 nextForward, int agentID)
        {
            if (forced)
                return EndogeneousTargetPosition(agent, nextActionTarget, nextPosition, nextForward);
            else
                return ExogeneousTargetPosition(agentID, nextPosition, nextForward);
        }

        Vector3 EndogeneousTargetPosition(AgentMainController agent, int nextActionTarget, Vector3 nextPosition, Vector3 nextForward)
        {
            Vector3 position;
            if (nextActionTarget != -1) { 
                Debug.Log(nextActionTarget);
                if (nextActionTarget == playerID) {
                    position = m_player.GetPlayerLookAtOrigin();
                }
                else if (m_entities[nextActionTarget] is ObjectOfInterest objectOfInterest)
                {
                    position = objectOfInterest.GetUnscaledBodyCenter();
                }
                else {
                    position = m_agents[nextActionTarget].GetLookAtOrigin();
                }
            }
            else { 
                position = nextPosition + nextForward * gazeDistance;
                position.y = agent.GetLookAtOrigin().y;
            }
            return position;
        }

        Vector3 ExogeneousTargetPosition(int agentID, Vector3 nextPosition, Vector3 nextForward)
        {
            // Get objects in FoC;
            Vector3[] objectCenters = new Vector3[m_entities.Length];
            Vector3[] objectSizes = new Vector3[m_entities.Length];
            int entityId = 0;
            for (int robotID = 0; robotID < m_agents.Length; robotID++) {
                objectCenters[entityId] = m_agents[robotID].GetLookAtOrigin();
                objectSizes[entityId] = m_agents[robotID].GetUnscaledAgentBodySize();
                entityId++;
            }
            if (player)
            {
                objectCenters[entityId] = m_player.GetPlayerLookAtOrigin();
                objectSizes[entityId] = m_player.GetUnscaledPlayerSize();
                entityId++;
            }
            for (int objectID = 0; objectID < m_boxes.Length; objectID++)
            {
                objectCenters[entityId] = m_boxes[objectID].GetUnscaledBodyCenter();
                objectSizes[entityId] = m_boxes[objectID].GetUnscaledBodySize();
                entityId++;
            }

            List<Vector3> means = new List<Vector3>();
            List<Vector3> sigmas = new List<Vector3>();
            List<double> weights = new List<double>();

            Vector3 agentCenter = m_agents[agentID].GetLookAtOrigin();
            Vector3 virtualCenter = agentCenter + m_agents[agentID].GetLookAt() * distanceProbe;

            for (int i = 0; i < objectCenters.Length; i++)
            {
                if (i != agentID)
                {
                    if (objectInFoC(objectCenters[i]))
                    {
                        Vector3 variance = new Vector3 (Mathf.Abs(objectSizes[i].x), Mathf.Abs(objectSizes[i].y), Mathf.Abs(objectSizes[i].z));
                        float mean = (variance.x + variance.y + variance.z ) / 3;
                        double probability = ExtensionTools.MultivariateNormalPDF(virtualCenter, objectCenters[i], new Vector3(mean, mean, mean));
                        weights.Add(probability);
                        if ((m_entities[i] is AgentMainController agentMainController) || (i == playerID))
                        {
                            float min = Mathf.Min(variance.x, variance.y, variance.z);
                            sigmas.Add(new Vector3(min, min, min));
                        }
                        else { 
                            sigmas.Add(variance);
                        }
                        means.Add(objectCenters[i]);
                    }
                }
            }

            if ((weights.Count == 0) || (weights.Max() < threshold))
            {
                //return m_agents[agentID].GetLookAtOrigin() + m_agents[agentID].GetBodyOrientation() * gazeDistance;
                return nextPosition + nextForward * gazeDistance;
                //return ExtensionTools.FromPCMVector3(nextBody.OrientationOrigin, true) + ExtensionTools.FromPCMVector3(nextBody.Orientation, true) * distanceProbe;
            }

            double sum = weights.Sum();
            double[] normalizedWeights = weights.Select(distance => distance/sum).ToArray();

            Vector3[] vcMeans = means.ToArray();
            Vector3[] vcSigmas = sigmas.ToArray();

            return SampleFromGMM(normalizedWeights, vcMeans, vcSigmas); 
            
            bool objectInFoC(Vector3 body)
            {
                float ang = ExtensionTools.AngleBetweenVectorAndPoint(m_agents[agentID].GetLookAt(), m_agents[agentID].GetLookAtOrigin(), body, false);
                return ang >= MathF.Sqrt(2) / 2;
            }

            Vector3 SampleFromGMM(double[] weights, Vector3[] means, Vector3[] sigmas)
            {
                int ind = SelectGaussianByWeight(weights);
                Vector3 sampledPoint = SampleFromGaussian(means[ind], sigmas[ind]);
                return sampledPoint;
            }

            int SelectGaussianByWeight(double[] weights)
            {
                double random = UnityEngine.Random.Range(0f, 1f);
                double cumulativeWeight = 0f;
                for (int i = 0; i < weights.Length; i++)
                {
                    cumulativeWeight += weights[i];
                    if (random <= cumulativeWeight)
                    {
                        return i;
                    }
                }
                return weights.Length - 1;
            }

            Vector3 SampleFromGaussian(Vector3 mean, Vector3 sigma)
            {
                float x = RandamGaussian(mean.x, sigma.x / 4);
                float y = RandamGaussian(mean.y, sigma.y / 4);
                float z = RandamGaussian(mean.z, sigma.z / 4);
                return new Vector3(x, y, z);
            }

            float RandamGaussian(float mean, float sigma)
            {
                float u1 = UnityEngine.Random.Range(0f, 1f);
                float u2 = UnityEngine.Random.Range(0f, 1f);
                float z = MathF.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
                return mean + z * sigma;
            }
        }
        #endregion



        #region Experience-related
        /*async public Task ParticipantChooses()
        {
            HttpResponseMessage response = await client.PostAsync($"api/participant-chooses", null);
            var message = await response.Content.ReadAsStringAsync();
            Debug.Log(message);
            response.EnsureSuccessStatusCode();

            if (ScenesManager.Singleton != null)
            {
                ScenesManager.Singleton.LoadSimulationScene();
            }
            else { StartCoroutine(End()); }
            Debug.Log("participant choose");
        }*/

        async public Task QuestionAfterTrial()
        {
            HttpResponseMessage response = await client.PostAsync($"api/question-after-trial", null);
            response.EnsureSuccessStatusCode();
            var message = await response.Content.ReadAsStringAsync();
            Debug.Log(message);
        }

        public async Task ParticipantChoosesCloserBox()
        {
            await QuestionAfterTrial();
            var agent = m_agents[PARTICIPANT_ID];
            Body agentBody = agent.GetAgentBodyData();
            List<int> box_ids = new List<int>();
            double min = 1000000;
            var agentPos = ConvertRealToBoardCoordinates(agentBody.Center.X, agentBody.Center.Z);
            for (int i = 0; i < m_boxes.Length; i++)
            {
                Body objectBody = m_boxes[i].GetBodyData();
                var entityPos = ConvertRealToBoardCoordinates(objectBody.Center.X, objectBody.Center.Z);
                double distance = (agentPos.x - entityPos.x) * (agentPos.x - entityPos.x) + (agentPos.y - entityPos.y) * (agentPos.y - entityPos.y);
                if (distance < min) { box_ids = new List<int> { i }; min = distance; }
                else { if (distance == min) { box_ids.Add(i); } }
            }
            if (box_ids.Count == 1) { m_boxes[box_ids[0]].Open(); }
            else { EndSimulation(); }
        }

        public static (int x, int y) ConvertRealToBoardCoordinates(double realX, double realY)
        {
            realX = Math.Clamp(realX, MinRealCoordinate, MaxRealCoordinate - 1);
            realY = Math.Clamp(realY, MinRealCoordinate, MaxRealCoordinate - 1);

            int boardX = (int)((realX - MinRealCoordinate) / RealRangePerCell);
            int boardY = (int)((realY - MinRealCoordinate) / RealRangePerCell);

            boardX = Math.Clamp(boardX, 0, BoardSize - 1);
            boardY = Math.Clamp(boardY, 0, BoardSize - 1);

            return (boardX, boardY);
        }

        public int[] AgentsPoints
        {
            get { return agentsPoints; }
            private set { agentsPoints = value; }
        }

        #region BoxOperations
        public void SelectRewardBox()
        {
            m_boxes[1 - config.RewardBoxID].RemoveReward();
        }

        public void OpenBox(Box box)
        {
            //box.Open();
            List<Box> boxes = m_boxes.ToList();
            var id = boxes.IndexOf(box);
            int playerScore = id == config.RewardBoxID ? 1 : -1;
            int agentScore = config.VirtualAgentRole_ == VirtualAgentRole.Partner ? playerScore : -playerScore;
            agentsPoints[AA_ID] += agentScore;
            agentsPoints[PARTICIPANT_ID] += playerScore;
            isRunningPCM = false;
            
            EndSimulation(id, agentScore, playerScore);
            
            boxOpened = true;
        }

        public void EndSimulation(int id=-1, int agentScore=0, int playerSocre=0){

            StartCoroutine(DelayedAction());

            if (ScenesManager.Singleton != null)
            {
                ScenesManager.Singleton.AgentPoint = agentsPoints;
                simulationParameters.selectedBox = id;
                simulationParameters.SetScores(agentScore, playerSocre);
                //simulationParameters.SetScores(agentsPoints[AA_ID], agentsPoints[PARTICIPANT_ID]);
                stream.SaveSimulationParameters(simulationParameters);
                ScenesManager.Singleton.LoadSimulationScene();
            }
            else
            {
                StartCoroutine(End());
            }
        }

        public void EnableBox()
        {
            foreach (var box in m_boxes) { box.Interactable = true; }
        }

        #endregion
        #endregion

        IEnumerator DelayedAction()
        {
            yield return new WaitForSeconds(timeUntilEnd);
        }

        public IEnumerator End()
        {
            isRunningPCM = false;
            StopPCM();
            if (ScenesManager.Singleton == null) {
                yield return new WaitForSeconds(timeUntilEnd);
                EditorApplication.isPlaying = false;
            }
        }

        private void OnValidate()
        {
            if (!player) playerID = -1;
        }
    }
}