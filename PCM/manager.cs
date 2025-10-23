using static PCM.Core.Interfacing;
using static PCM.Core.Execution.Run;
using PCM.Core.Actions;
using PCM.Misc;
using PCM.Core.FreeEnergy.InverseInferences;
using PCM.Core.FreeEnergy;
using PCM.Schemas;
using PCM.Core.SceneObjects;

namespace PCM
{
    public class Manager
    {
        //private static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        public bool isRunningPCM = false;

        public Core.Process m_mainPCMProcess;
        public string m_configFilename;

        //default value, can be modified 
        public int m_depthOfProcessing = 3; // TODO GET VALUE FROM GE number of consecutive states expected at each prediction
        public int nbThreadMax = 12;
        public int nbIterations;
        public int _playerId = -1;
        public Input _lastUpdate;
        public Schemas.Parameters Parameters { private get; set; }
        public Verbal.Manager Verbal { get; private set; }
        ArtificialAgentMode AA_Mode;

        public string ComputeWorldstate(Input inputWorldState)
        { 
            //string m_configFilename = "DemoPCMVR-S03-Make-Peace-v02.json";
            //_parameters = JsonConvert.DeserializeObject<PCMSimParameters>(File.ReadAllText(m_configFilename));

            //prgm._lastUpdate = JsonConvert.DeserializeObject<Input>(worldState);
            _lastUpdate = inputWorldState;

            if(!isRunningPCM)
                StartSimulation();
                
            Input currentWorldState = _lastUpdate;

            m_mainPCMProcess.SendUpdate(currentWorldState);

            SharedResources.semaphore.Wait();
            return m_mainPCMProcess.GetLastOutputAsString();
        }
        
        #region Start

        public void StopPCM()
        {
            m_mainPCMProcess.Stop();
            isRunningPCM = false;
        }

        public async Task Init(Schemas.Parameters parameters, ArtificialAgentMode AA_Mode){
            var coreParameters = parameters.NonVerbal;
            var depthOfProcessing = coreParameters.Depth;
            if (depthOfProcessing > 0) m_depthOfProcessing = depthOfProcessing;
            var nbThreadMax = coreParameters.NbThreadMax;
            if (nbThreadMax > 0) this.nbThreadMax = nbThreadMax;
            nbIterations = coreParameters.NbIterations;
            Parameters = parameters;
            var verbalParameters = parameters.Verbal;
            verbalParameters.Depth = Math.Min(verbalParameters.Depth, coreParameters.Depth);
            Verbal = new Verbal.Manager(verbalParameters, AA_Mode);
            await Verbal.InitLLM();
            this.AA_Mode = AA_Mode;
        }

        public void RestartPCM()
        {
            StartSimulation();
        }

        public void StartSimulation()
        {
            // Setting up the decimal parsing symbol as '.' in case it is not already
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            Thread.CurrentThread.CurrentCulture = customCulture;

            LoadParameters(Parameters.NonVerbal.Core, out Core.FreeEnergy.State.AgentState[] agentStates, out Core.Types.WorldState worldState, out List<Core.FreeEnergy.State.Randomizer.PropertyRandomizer> propertyRandomizers);

            worldState = InputToWorldState(_lastUpdate, _playerId);

            for (int agentIndex = 0; agentIndex < agentStates.Length; agentIndex++)
            {
                for (int oindex = 0; oindex < worldState.Positions.Length; oindex++)
                {
                agentStates[agentIndex].objectBodies[oindex] = worldState.Positions[oindex].Copy();
                var l = new List<ObjectBody[]>();
                if (agentStates[agentIndex].pbodies != null)
                    foreach (int aid in agentStates[agentIndex].agentsIds)
                    {
                        agentStates[agentIndex].pbodies[aid][oindex] = agentStates[agentIndex].objectBodies[oindex];
                    }
                }
            }

            agentStates = UpdateAgentsInnerStatesWithActualPositions(agentStates, worldState.Positions, worldState.Interactions);

            FreeEnergy freeEnergyCompute = new();
            agentStates = agentStates.Select(agent => freeEnergyCompute.ComputeFOC(agent)).ToArray();

            if (Verbal != null) Verbal.InitAgents(agentStates);

            m_mainPCMProcess = new Core.Process(startWebServer: false, true);
            if (AA_Mode == ArtificialAgentMode.Verbal)
                agentStates[0].actionDirectories[0].Update(Array.Empty<string>());
            m_mainPCMProcess.Init(worldState, agentStates, _playerId, nbThreadMax, Parameters.NonVerbal, AA_Mode);
            m_mainPCMProcess.Start();
            isRunningPCM = true;
        }

        public void ParticipantChooses()
        {
            m_mainPCMProcess.actionTagsArray = new string[][] { null, new string[] { "walk" } };
        }

        void LoadParameters(Schemas.Core parameters, out Core.FreeEnergy.State.AgentState[] agentStates, out Core.Types.WorldState worldState, out List<Core.FreeEnergy.State.Randomizer.PropertyRandomizer> propertyRandomizers)
        {
            //if (Parameters == null)
            //    Parameters = SimulationParametersParser.FromJson(_parameters);
            //Console.WriteLine("ici");
            
            SetPCMConstants(parameters);
            InitPossibleActionsList(parameters);

            //Get the randomizers
            propertyRandomizers = parameters.Settings.Simulation.Randomization.Select(rdm =>
            {
                return new Core.FreeEnergy.State.Randomizer.PropertyRandomizer()
                {
                    Agent = rdm.Agent,
                    Property = rdm.Property,
                    Magnitude = rdm.Magnitude,
                    Indices = rdm.Indices,
                    Delta = rdm.RandomizationCondition.Delta,
                    DeltaType = rdm.RandomizationCondition.Type == "time" ? Core.FreeEnergy.State.Randomizer.DeltaType.FixedTime : (rdm.RandomizationCondition.Type == "difference" ? Core.FreeEnergy.State.Randomizer.DeltaType.DifferenceCounter : throw new Exception($"Unsupported randomization difference type {rdm.RandomizationCondition.Type}"))
                };
            }).ToList();

            GetInitialAgentsAndWorldState(parameters, out agentStates, out worldState);
        }

        void SetPCMConstants(Schemas.Core parameters)
        {
            Constants.nSigma = parameters.Settings.Simulation.NSigma;
            Constants.nPsyS = parameters.Settings.Simulation.NPsyS;
            Constants.sevuncertaingain = parameters.Settings.Simulation.Sevuncertaingain;
            Constants.sensoryEvidenceUpdateWeight = parameters.Settings.Simulation.SensoryEvidenceUpdateWeight;
            // SimulationParamsConstants.SpatialStat_ClippingDistance = par.Settings.SimulationParams.SpatialStat.ClippingDistance;
            // SimulationParamsConstants.SpatialStat_GainFactor = par.Settings.SimulationParams.SpatialStat.GainFactor;
            Constants.depth = m_depthOfProcessing;
            Constants.minDist = parameters.Settings.Simulation.MinDist;
            Constants.goalPredict = parameters.Settings.Simulation.GoalPredict;

            Utils.SigmaDistanceFactor = parameters.Settings.Simulation.Certainty.SigmaDistanceFactor;
            Utils.SigmaSharpnessFactor = parameters.Settings.Simulation.Certainty.SigmaSharpnessFactor;
            Utils.UpdateUncertFactor = parameters.Settings.Simulation.Certainty.UpdateUncertFactor;
            if (parameters.Settings.Simulation.AlgSelection != null)
            {
                Constants.algSelection = parameters.Settings.Simulation.AlgSelection;
            }
        }

        static void InitPossibleActionsList(Schemas.Core parameters)
        {
            Core.FreeEnergy.SpatialStat.Init(parameters.Settings.Simulation.SpatialStat.C, parameters.Settings.Simulation.SpatialStat.Volume, parameters.Settings.Simulation.SpatialStat.GaussianDisp, parameters.Settings.Simulation.SpatialStat.AmplificationFactor);
        }

        void GetInitialAgentsAndWorldState(Schemas.Core parameters, out Core.FreeEnergy.State.AgentState[] agentStates, out Core.Types.WorldState worldState)
        {
            Core.SceneObjects.ObjectBody[] allEntityBodies = GetAllInitialEntityBodies(parameters);
            int entityCount = allEntityBodies.Length;

            double[] emotionGain = parameters.Agents.Emogain;
            double[] physiologicalReactivity = parameters.Agents.PhysiologicalReactivity;
            double[] facialReactivity = parameters.Agents.FacialReactivity;
            double[] physiologicalSensitivity = parameters.Agents.PhysiologicalSensitivity;
            double[] voluntaryPhysiologicalWeight = parameters.Agents.VoluntaryPhysiologicalWeight;
            double[] voluntaryFacialWeight = parameters.Agents.VoluntaryFacialWeight;
            string[][][] actionTags = parameters.Agents.ActionTags;
            int agentCount = parameters.Agents.Positioning.Length; // robots count + 1 (vr player)
            int[] agentIndices = Enumerable.Range(0, agentCount).ToArray();

            double[][][] tomUpdate = Core.Utils.Arrays.Normalize3DArray(parameters.Agents.TomUpdate);
            double[][][] tomInfluence = Core.Utils.Arrays.Normalize3DArray(parameters.Agents.TomInfluence);
            double[][][] preferences = parameters.Agents.Preferences;
            double[][][] mutualLove = parameters.Agents.MutualLoveStep;

            Core.Types.Emotion[] defaultEmotions = GetDefaultEmotions(agentCount);
            ActionDirectory[][] actionDirectories = new ActionDirectory[agentCount][];
            GoalDirectory[][] goalDirectories = new GoalDirectory[agentCount][];
            for (int i = 0; i < agentCount; i++)
            {
                actionDirectories[i] = new ActionDirectory[agentCount];
                goalDirectories[i] = new GoalDirectory[agentCount];
                for (int j = 0; j < agentCount; j++)
                {
                    List<int> entityList = new List<int>();
                    for (int entity = agentCount; entity < entityCount; entity++){
                        if (entity != i)
                        entityList.Add(entity);
                    }
                    actionDirectories[i][j] = new ActionDirectory(actionTags[i][j], entityList, entityCount, m_depthOfProcessing);
                    goalDirectories[i][j] = new GoalDirectory(actionTags[i][j], entityList, entityCount, j);
                    //ctionDirectories[i][j] = new ActionDirectory(actionTags[i][j], new List<int> { 2, 3 }, entityCount, m_depthOfProcessing);
                    //goalDirectories[i][j] = new GoalDirectory(actionTags[i][j], new List<int> { 2, 3 }, entityCount, j);
                }
            }
            int robotIndex = 0;
            agentStates = new Core.FreeEnergy.State.AgentState[agentCount];

            InverseInference.Init(m_depthOfProcessing);
            for (int id = 0; id < agentCount; id++)
            {
                Core.FreeEnergy.State.AgentState agentState = new(id, agentIndices)
                {
                    objectBodies = Core.Utils.Copy.CopyArray(allEntityBodies),
                    emotions = agentIndices.Select(e => new Core.Types.EmotionSystem()).ToArray(),
                    emogain = Core.Utils.Copy.Copy1DDouble(emotionGain),
                    actionDirectories = actionDirectories[id],
                    goalDirectories = goalDirectories[id],
                    inferences = InverseInference.InitInferences(id, agentStates.Length),
                    // if (id != PlayerID)
                    // {
                    preferences = ExtensionTools.ExtendMatrix(preferences[robotIndex], agentCount, entityCount, 0.5),
                    speed = parameters.Agents.Speed[robotIndex].RealSpeed,
                    tomInfluence = ExtensionTools.ExtendMatrix(tomInfluence[robotIndex], agentCount, entityCount, 0),
                    tomUpdate = ExtensionTools.ExtendMatrix(tomUpdate[robotIndex], agentCount, entityCount, 0),
                    tomPredict = ExtensionTools.ExtendArray(parameters.Agents.TomPredict[robotIndex], agentCount, 0),
                    projectionSpeed = parameters.Agents.Speed[robotIndex].ProjectionSpeed,
                    projectionSpeedIncrement = parameters.Agents.Speed[robotIndex].ProjectionSpeedIncrement,
                    isAnxious = parameters.Agents.Anxious != null && parameters.Agents.Anxious[robotIndex],
                    mutualLoveStep = mutualLove[id],
                    targetIds = agentIndices.Select(v => -1).ToArray(), // just added
                    interactObjectIds = agentIndices.Select(v => -1).ToArray(),
                    physiologicalReactivity = physiologicalReactivity[id],
                    facialReactivity = facialReactivity[id],
                    voluntaryPhysiologicalWeight = voluntaryPhysiologicalWeight[id],
                    voluntaryFacialWeight = voluntaryFacialWeight[id],
                    physiologicalSensitivity = physiologicalSensitivity[id]
                };
                robotIndex++;
                // }
                // else
                // {
                //     agentState.preferences = ExtensionTools.GetFilledMatrix<double>(agentCount, entityCount, 0.5);
                //     agentState.speed = 0;
                //     agentState.tomInfluence = ExtensionTools.GetFilledMatrix<double>(agentCount, entityCount, 0);
                //     agentState.tomUpdate = ExtensionTools.GetFilledMatrix<double>(agentCount, entityCount, 0);
                //     agentState.tomPredict = ExtensionTools.GetFilledArray(agentCount, 0);
                //     agentState.projectionSpeed = 0;
                //     agentState.projectionSpeedIncrement = 0;
                //     agentState.mutualLoveStep = mutualLove[id];
                //     agentState.isAnxious = false;
                // }

                agentStates[id] = agentState;
            }

            worldState = new Core.Types.WorldState
            {
                Positions = allEntityBodies,
                Emotions = agentIndices.Select(e => new Core.Types.EmotionSystem()).ToArray(),
                NewTextInput = false
            };
            //Console.WriteLine("agentState");
            //Console.WriteLine(JsonConvert.DeserializeObject(agentStates));
        }

        private Core.SceneObjects.ObjectBody[] GetAllInitialEntityBodies(Schemas.Core parameters)
        {
            Core.SceneObjects.ObjectBody[] agentBodies = GetInitialAgentBodies(parameters.Agents.Positioning);
            Core.SceneObjects.ObjectBody[] objectBodies = GetInitialObjectBodies(parameters.Objects.Positioning);

            // all entities, including objects, robots and the vr player
            Core.SceneObjects.ObjectBody[] allEntityBodies = new Core.SceneObjects.ObjectBody[agentBodies.Length + objectBodies.Length];
            agentBodies.CopyTo(allEntityBodies, 0);
            objectBodies.CopyTo(allEntityBodies, agentBodies.Length);

            return allEntityBodies;
        }

        private Core.SceneObjects.ObjectBody[] GetInitialAgentBodies(Positioning[] references)
        {
            Core.SceneObjects.ObjectBody[] agentBodies = new Core.SceneObjects.ObjectBody[references.Length];
            int referenceIndex = 0;
            for (int i = 0; i < agentBodies.Length; i++)
            {
                if (i == _playerId)
                {
                    continue;
                }

                Core.SceneObjects.ObjectType agentType = Core.SceneObjects.ObjectType.ArtificialAgent;
                Core.SceneObjects.ObjectBody agentPosition = SetupPCMObjectBody(references[referenceIndex], agentType);
                agentBodies[i] = agentPosition;
                referenceIndex++;
            }

            //TODO Get player bool value from game engine info
            //if (player)
            //{
            //    Interfacing.Body playerBodyInfo = GetPlayerState().Body;
            //    Geom3d.Vertex playerCenter = new PCM.Geom3d.Vertex(playerBodyInfo.Center);
            //    Geom3d.Polyhedron playerBodyPosition = PCM.Geom3d.Polyhedron.RectangularPrism(playerCenter, playerBodyInfo.Width, playerBodyInfo.Height, playerBodyInfo.Depth);
            //    Geom3d.Vertex playerOrientation = new PCM.Geom3d.Vertex(playerBodyInfo.Orientation);
            //    SceneObjects.ObjectBody playerBody = new PCM.SceneObjects.ObjectBody(playerBodyPosition, playerOrientation);
            //    agentBodies[PlayerID] = playerBody;
            //}
            return agentBodies;
        }


        static private Core.SceneObjects.ObjectBody[] GetInitialObjectBodies(Positioning[] references)
        {
            Core.SceneObjects.ObjectBody[] objectBodies = new Core.SceneObjects.ObjectBody[references.Length];
            for (int i = 0; i < objectBodies.Length; i++)
            {
                Core.SceneObjects.ObjectType objectType = (Core.SceneObjects.ObjectType)references[i].Type;
                Core.SceneObjects.ObjectBody objectPosition = SetupPCMObjectBody(references[i], objectType);
                objectBodies[i] = objectPosition;
            }

            return objectBodies;
        }

        static Core.SceneObjects.ObjectBody SetupPCMObjectBody(Positioning reference, Core.SceneObjects.ObjectType objectType)
        {

            Core.SceneObjects.ObjectBody objectBody = Core.SceneObjects.ObjectBody.Create(objectType);

            double objectHeight = Core.SceneObjects.DemoVR.GetDimensions(objectType).height * 0.5f;
            Core.Geom3d.Vertex offset = new(reference.Position[0], objectHeight, reference.Position[1]);
            objectBody.BodyPosition.Translate_inplace(offset);

            Core.Geom3d.Vertex direction = new(reference.LookAt[0], 0, reference.LookAt[1]);
            objectBody.RotateTowardsDirection(direction);

            return objectBody;
        }

        static private Core.Types.Emotion[] GetDefaultEmotions(int count)
        {
            Core.Types.Emotion[] emotions = new Core.Types.Emotion[count];
            for (int i = 0; i < count; i++)
            {
                emotions[i] = new Core.Types.Emotion
                {
                    Pos = 0,
                    Neg = 0,
                    Arousal = 0,
                    Val = 0,
                    Surprise = 0,
                    Uncertainty = 0
                };
            }
            return emotions;
        }

        #endregion
    }
}