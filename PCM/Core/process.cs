using System.Threading.Tasks;
using flexop.Api.Dtos;
using MathNet.Numerics.LinearAlgebra.Solvers;
using Newtonsoft.Json;
using PCM.Core.FreeEnergy.State;
using PCM.Core.Services;
using PCM.Schemas;
using PCM.Verbal.LLM;

enum ProcessActions
{
    Wait,
    UpdateState,
    ComputePredictions,
    Stop
}

namespace PCM.Core
{
    public class Process
    {
        private bool _initialized = false;
        private bool _continue = false;
        private readonly ManualResetEvent _mrse = new(false);
        private Thread T;
        private int nbThreadMax;

        private ProcessActions _state = ProcessActions.Wait;

        private Interfacing.Input _lastUpdate = null;
        private Interfacing.Input _currentUpdate = null;
        private Interfacing.Output _lastOutput = null;

        //private InterfaceLLM _interfacellm = null;

        private Types.WorldState _worldState = null;
        public AgentState[] _agentStates = null;
        private int _playerId = -1;
        private readonly int _predictionDelay = 0; //In milliseconds
        private long _lastPredictionTime = 0;
        private long _nextPredictionTime = 0;

        public int simulationCount = 0;
        
        private bool _saveBestPath;
        private string _filePath;
        private ArtificialAgentMode AA_Mode;
        private readonly Queue<Interfacing.PlayerEmotion> _queueForPlayerEmotion = new();

        private readonly Verbal.Manager verbalManager = SharedResources.manager.Verbal;

        /// Web Sever to output world state in JSON
        private readonly bool _launchWS = false;
        private readonly bool _sendExternalHttpServer = false;
        private SimpleHttpServer _ws;

        public int PredCounter { get; set; }

        public string[][] actionTagsArray = new string[2][];


        /// Randomization

        private List<Randomizer.PropertyRandomizer> _propertyRandomizers = new();
        public Process(bool startWebServer = false, bool sendExternalHttpServer = false)
        {
            _launchWS = startWebServer;
            _sendExternalHttpServer = sendExternalHttpServer;
        }

        private readonly List<long> predictionsTime = new();


        /// <summary>
        /// PCM Thread main function
        /// </summary>
        /// 
        private void Run()
        {
            while (_continue)
            {
                _mrse.WaitOne();
                var act = _state;
                switch (act)
                {
                    case ProcessActions.Stop:
                        _continue = false;
                        break;
                    case ProcessActions.ComputePredictions:
                        //Randomization > check if randomization needed
                        foreach (var pr in _propertyRandomizers)
                        {
                            _agentStates[pr.Agent] = pr.RandomizeIfNecessary(_agentStates[pr.Agent]);
                        }
                        var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        // Console.WriteLine("Predicting " + Thread.CurrentThread.Name);
                        
                        var agentIndices = _agentStates[0].agentsIds;
                        var nbAgent = _agentStates.Length;

                        for(int id = 0; id < actionTagsArray.Length; id++)
                        {
                            var actionTags = actionTagsArray[id];
                            if (actionTags == null)
                                continue;
                            actionTagsArray[id] = null;
                            _agentStates[id].actionDirectories[id].Update(actionTags);
                        }


                        //Do inference and prediction
                        (AgentState state, SceneObjects.ObjectBody pos, List<AgentState> seq, Actions.ActionType actionType)[] predictions = new (AgentState state, SceneObjects.ObjectBody pos, List<AgentState> seq, Actions.ActionType actionType)[nbAgent];
                        Dictionary<int, Interfacing.AgentOutput[]> AgentStates = new();

                        Action<int> predict = delegate (int id)
                        {
                            var agentIndex = _agentStates[id].currentAgentId;
                            int rest = PredCounter % 2;
                            if ((agentIndex == rest) && (agentIndex != _playerId)) 
                            {
                                var filepath = _filePath + $"_{PredCounter}";
                                (AgentState state, SceneObjects.ObjectBody pos, List<AgentState> seq, Actions.ActionType actionType) pred;
                                if (AA_Mode == ArtificialAgentMode.Verbal)
                                {
                                    //AgentState new_state = await Interface.NextStepPrediction(_agentStates[id]);
                                    // try{
                                    pred = verbalManager.LLMPrediction(_agentStates[id]).GetAwaiter().GetResult();
                                    //}
                                    //catch (Exception ex){
                                    //    Console.WriteLine("Error in LLM prediciton: " + ex.Message);
                                    //}
                                }
                                else{
                                    pred = Execution.Run.Predict(_agentStates[id], _worldState.Interactions, nbThreadMax, FreeEnergy.Constants.goalPredict);
                                }
                                pred.seq[1] = verbalManager.UpdatePostPreferences(pred.seq[1], agentIndex);
                                if (_saveBestPath) SaveBestPath.SaveBestResultToMongoDB(pred.seq, filepath);
                                if (AA_Mode != ArtificialAgentMode.NonVerbal) pred.seq[1] = verbalManager.UpdatePreferences(pred.seq[1]);

                                predictions[id] = pred;
                                AgentStates[agentIndex] = Interfacing.ConvertAgentPrediction(pred.seq, pred.actionType);    
                            }
                            else
                            {
                                var pred = (state: _agentStates[agentIndex], pos: _agentStates[agentIndex].objectBodies[agentIndex], seq: new List<AgentState>() { _agentStates[id], _agentStates[id] }, actionType: Actions.ActionType.Idle);
                                predictions[id] = pred;
                                AgentStates[agentIndex] = Interfacing.ConvertAgentPrediction(pred.seq, pred.actionType);
                            }
                        };
                        for (int agentIndex = 0; agentIndex < _agentStates.Length; agentIndex++)
                        {
                            predict(agentIndex);
                        }

                        _lastPredictionTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        LogPredictionTime(start);

                        var simplifiedPredictions = predictions.Select(prediction => prediction.seq.Skip(0).ToList()).ToArray();
                        if (AA_Mode != ArtificialAgentMode.NonVerbal) verbalManager.UpdateAgents(simplifiedPredictions);
                        _agentStates = Execution.Run.UpdateAgentsInnerStatesWithActualPositions(predictions.Select(p => p.state).ToArray(), _worldState.Positions, _worldState.Interactions);
                        _lastOutput = new Interfacing.Output()
                        {
                            TimeStamp = _lastPredictionTime,
                            AgentStates = AgentStates
                        };
                        PredCounter++;
                        /*if (!usingVerbal && SharedResources.manager.nbIterations == PredCounter)
                            SharedResources.manager.ParticipantChooses();*/
                        _nextPredictionTime = start + _predictionDelay;
                        _state = ProcessActions.Wait;

                        _mrse.Reset();
                        SharedResources.semaphore.Release();
                        break;

                    case ProcessActions.UpdateState:
                        while (_queueForPlayerEmotion.Count > 0)
                        {
                            var playerEmotionUpdate = _queueForPlayerEmotion.Dequeue();
                            var valenceFactor = new double[_worldState.Positions.Length];
                            for (int i = 0; i < _worldState.Positions.Length; i++)
                            {
                                valenceFactor[i] = playerEmotionUpdate.valenceFactor.ContainsKey(i) ? playerEmotionUpdate.valenceFactor[i] : 0;
                            }
                            for (int agentIndex = 0; agentIndex < _agentStates.Length; agentIndex++)
                            {

                                double[] priors = _agentStates[agentIndex].preferences[_playerId];
                                _agentStates[agentIndex].preferences[_playerId] = Utils.Misc.UpdatePriorsForPlayer(priors, playerEmotionUpdate.valence, _playerId, valenceFactor);

                            }
                        }

                        _currentUpdate = _lastUpdate;
                        //Do update

                        _worldState = Interfacing.InputToWorldState(_currentUpdate, _playerId);

                        var updatedAgentStates = Execution.Run.UpdateAgentsInnerStatesWithActualPositions(_agentStates, _worldState.Positions, _worldState.Interactions);
                        var updatedAgentStatesWithEmo = Execution.Run.UpdateAgentsInnerStatesWithActualEmotions(updatedAgentStates, _worldState.Positions, _worldState.Emotions);
                        _agentStates = Utils.Copy.CopyArray(updatedAgentStatesWithEmo);

                        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        if (now >= _nextPredictionTime)
                        {
                            _state = ProcessActions.ComputePredictions;
                        }
                        else
                        {
                            _state = ProcessActions.Wait;
                            _mrse.Reset();
                        }
                        break;
                }
                if (!_continue)
                    break;
            }
            Console.WriteLine("PCM process exited.");
        }

        ///Public methods

        /// <summary>
        /// Provide the initial values to the PCM
        /// </summary>
        /// <param name="worldState">Initial object positions and expressed emotions</param>
        /// <param name="agentStates">Initial inner models for agents</param>
        /// <param name="playerId">Agent id for the player</param>
        public void Init(Types.WorldState worldState, AgentState[] agentStates, int playerId, int nbThreadMax, NonVerbal nvParameters, ArtificialAgentMode aa_Mode)
        {
            _saveBestPath = nvParameters.SaveBestPath;
            _filePath = nvParameters.FilePath;
            _agentStates = agentStates;
            _worldState = worldState.Copy();
            _playerId = playerId;
            _initialized = true;
            AgentState.playerId = playerId;
            //nbThreadMax = SimulationManager.s_singleton.nbThreadMax;
            this.nbThreadMax = nbThreadMax;
            AA_Mode = aa_Mode;
        }


        public void SetPropertyRandomizers(List<Randomizer.PropertyRandomizer> pr)
        {
            _propertyRandomizers = pr;
        }

        /// <summary>
        /// Start the PCM
        /// </summary>
        public void Start()
        {
            if (!_initialized)
            {
                throw new Exception("PCM not initialized with starting values");
            }
            T = new Thread(Run);
            _continue = true;
            T.Start();
            T.Name = "PCM";
            if (_launchWS)
            {
                _ws = new SimpleHttpServer();
                _ws.SetMessage(JsonConvert.SerializeObject(_worldState));
                _ws.Start();
            }

        }
        /// <summary>
        /// Stop the pcm
        /// </summary>
        /// <param name="waitForCompletion">Wait up to 5seconds for the thread to gracefully stop. Kill it after 5s.</param>
        public void Stop(bool waitForCompletion = false)
        {
            _continue = false;
            if (_ws != null)
            {
                _ws.Stop();
            }
            if (_state == ProcessActions.Wait)
            {
                _state = ProcessActions.Stop;
                _mrse.Set();
            }
            if (waitForCompletion)
                if (T.Join(5000))
                    return;
                else
                {
                    Kill();
                    return;
                }
        }
        /// <summary>
        /// Kill the thread. Shouldn't be used. See Stop instead
        /// </summary>
        public void Kill()
        {
#pragma warning disable
            T.Abort();
#pragma warning restore
        }

        /// <summary>
        /// Get the last available prediction for agents
        /// </summary>
        /// <returns></returns>
        public Interfacing.Output GetPrediction() => _lastOutput;


        public void SendUpdate(Interfacing.Input Update)
        {
            _lastUpdate = Update;

            // Temp debug
            var wstate = Interfacing.InputToWorldState(_lastUpdate, _playerId);

            if (_ws != null)
            {
                var str = JsonConvert.SerializeObject(
                    (ws: wstate, status: _lastOutput?.AgentStates.Select(state =>
                    {
                        return (pref: state.Value[0].Preferences, mu: state.Value[0].Mu);
                    }))
                );
                _ws.SetMessage(str);
            }
            if (_sendExternalHttpServer)
            {
                var str = JsonConvert.SerializeObject(
                    (ws: wstate, status: _lastOutput?.AgentStates.Select(state =>
                    {
                        return (pref: state.Value[0].Preferences, mu: state.Value[0].Mu);
                    }))
                );
                ExternalHttpServerCom.PostUpdate(str);
            }
            ///Detect Emotion of player
            if (_playerId != -1)
            {
                if (Update.PlayerEmotion != null)
                {
                    _queueForPlayerEmotion.Enqueue(Update.PlayerEmotion);
                    Console.WriteLine("Enqueue");
                }
            }

            //end temp
            if (_state == ProcessActions.Wait)
            {

                _state = ProcessActions.UpdateState;
                _mrse.Set();
            }
        }

        public void LogPredictionTime(long startTime)
        {
            var time = _lastPredictionTime - startTime;
            predictionsTime.Add(time);
            Console.WriteLine($"prediciton took : {(int)time}  avg : {(int)predictionsTime.Average()}");
        }

        public string GetLastOutputAsString()
        {
            string jsonString = JsonConvert.SerializeObject(_lastOutput, Formatting.Indented);
            //Console.WriteLine(jsonString);
            return jsonString;
        }
    }
}