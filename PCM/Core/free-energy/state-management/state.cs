using PCM.Core.Types;
using PCM.Core.SceneObjects;
using PCM.Core.Actions;
using PCM.Core.FreeEnergy.InverseInferences;
using static PCM.Core.FreeEnergy.InverseInferences.BeliefInference;
using static PCM.Core.Utils.Tree;
namespace PCM.Core.FreeEnergy.State
{
    public class AgentState : Core.Utils.Copy.ICopyable<AgentState>
    {
        //TODO: [PCM VR DEMO 2021 AD HOCK MODIF]
        public static int playerId = -1;
        public List<ObjectBody[]> pbodies;

        public ActionDirectory[] actionDirectories;
        public GoalDirectory[] goalDirectories;
        public int stepCountToReachGoal;

        public Dictionary<(AgentBelieves agentBelieves, int inferedAgentId), BeliefInference[]> inferences;
        public bool canInfer = false;
        /* 
        * Sensory Data
        * Data available through senses, i.e. objects positions,
        * shown emotions
        */
        public ObjectBody[] objectBodies;
        public EmotionSystem[] emotions;
        public double speed;
        public double projectionSpeed;
        public double projectionSpeedIncrement;

        public int[] targetIds;
        public int[] interactObjectIds;

        /* 
        * Internal data 
        * Internal data for the agents, i.e. preferences, theory of mind
        */
        public double[][] preferences;
        public double[][] postPreferences;
        // [Obsolete("TOM is obsolete. Use tomInfluence and tomUpdate")]
        // public double[][] tom;
        public int[] tomPredict;
        public double[][] tomInfluence;
        public double[][] tomUpdate;
        public double[][] mutualLoveStep;

        /* 
         * Simulation parameters like emogain, agentsIds. Those should not 
         * change through the simulation
         */
        public double[] emogain;
        public double physiologicalReactivity;
        public double facialReactivity;
        public double voluntaryPhysiologicalWeight;
        public double voluntaryFacialWeight;
        public double physiologicalSensitivity;
        public bool expressionSpontaneous = false;
        //Array of ids of agents in this.positions
        public int[] agentsIds;
        public int currentAgentId;

        //TODO: tidy that
        public bool isAnxious;

        /*
         * Constants
         */
        public double sevuncertaingain = Constants.sevuncertaingain;
        public double sensoryEvidenceUpdateWeight = Constants.sensoryEvidenceUpdateWeight;
        public double neutralref = Constants.neutralref;


        /*
         * Computed / resulting values
         */
        public (double freeEnergy, double energy, double entropy)[][] fe;
        public (double certainty, double uncertainty)[][] certTable;

        public double? stateFE;

        //public double stateFE_pos;
        //public double stateFE_neu;
        //public double stateFE_neg;

        public double[][] mu;
        public double[][] spatialStats;
        public Node<AgentStateNode> node;
        public static double _millis = 0;

        //Constructor to generate an empty set
        public AgentState(int currentAgentId, int[] agentsIds)
        {
            this.currentAgentId = currentAgentId;
            this.agentsIds = agentsIds;
        }

        public AgentState(int currentAgentId, int[] agentsIds,
                double[][] preferences, ObjectBody[] objectBodies, ActionDirectory[] actionDirectories, GoalDirectory[] goalDirectories,
                Dictionary<(AgentBelieves agentBelieves, int inferedAgentId), BeliefInference[]> inferences,
                EmotionSystem[] emotions, double[] emogain, double physiologicalReactivity, double facialReactivity, double voluntaryFacialWeight, 
                double voluntaryPhysiologicalWeight, double physiologicalSensitivity, double speed, double[][] normalizedTomInfluence, 
                double[][] normalizedTomUpdate, int[] tomPredict, double[][] mutualLoveStep, double projSpeed, double projSpeedIncr, bool isAnxious, 
                int[] targetIds, int[] grabbedIds, bool expressionSpontaneous)
        {
            this.currentAgentId = currentAgentId;
            this.agentsIds = agentsIds;
            this.preferences = Core.Utils.Copy.Copy2DDouble(preferences);
            this.objectBodies = Core.Utils.Copy.CopyArray(objectBodies);
            this.actionDirectories = actionDirectories;
            this.goalDirectories = goalDirectories;
            this.inferences = inferences;
            this.emotions = Core.Utils.Copy.CopyArray(emotions);
            this.emogain = Core.Utils.Copy.Copy1DDouble(emogain);
            this.physiologicalReactivity = physiologicalReactivity;
            this.facialReactivity = facialReactivity;
            this.voluntaryPhysiologicalWeight = voluntaryPhysiologicalWeight;
            this.voluntaryFacialWeight = voluntaryFacialWeight;
            this.physiologicalSensitivity = physiologicalSensitivity;
            this.speed = speed;
            tomInfluence = Core.Utils.Copy.Copy2DDouble(normalizedTomInfluence);
            tomUpdate = Core.Utils.Copy.Copy2DDouble(normalizedTomUpdate);
            this.tomPredict = Core.Utils.Copy.Copy1DArray(tomPredict);
            projectionSpeed = projSpeed;
            projectionSpeedIncrement = projSpeedIncr;
            this.isAnxious = isAnxious;
            this.targetIds = Core.Utils.Copy.Copy1DArray(targetIds);
            interactObjectIds = Core.Utils.Copy.Copy1DArray(grabbedIds);
            this.mutualLoveStep = Core.Utils.Copy.Copy2DDouble(mutualLoveStep);
            this.expressionSpontaneous = expressionSpontaneous;
        }

        /// <summary>
        /// Performs a deep copy of the State
        /// </summary>
        /// <returns>State</returns>
        public AgentState Copy()
        {
#if COMPUTE_TIME
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
#endif
            double[][] newPostPref = null;
            if (postPreferences != null)
            {
                newPostPref = new double[postPreferences.Length][];
                for (var i = 0; i < newPostPref.Length; i++)
                    newPostPref[i] = postPreferences[i].ToArray();
            }
            (double certainty, double uncertainty)[][] newCertTable = null;
            if (certTable != null)
            {
                newCertTable = new (double certainty, double uncertainty)[certTable.Length][];
                for (var i = 0; i < newCertTable.Length; i++)
                    newCertTable[i] = certTable[i].ToArray();
            }
            double[][] newMu = null;
            if (mu != null)
            {
                newMu = Core.Utils.Copy.Copy2DDouble(mu);
            }
            double[][] newSS = null;
            if (spatialStats != null)
            {
                newSS = Core.Utils.Copy.Copy2DDouble(spatialStats);
            }
            var pbod = new List<ObjectBody[]>();
            if (pbodies != null)
            {
                foreach (var body in pbodies)
                    pbod.Add(Core.Utils.Copy.CopyArray(body));
            }

            //var newActionDirectories = actionDirectories.Select(x => x.Copy()).ToArray();
            var sp = new AgentState(currentAgentId, agentsIds, preferences, objectBodies, 
                actionDirectories, goalDirectories, inferences, emotions, emogain, physiologicalReactivity, 
                facialReactivity, voluntaryFacialWeight, voluntaryPhysiologicalWeight, physiologicalSensitivity, 
                speed, tomInfluence, tomUpdate, tomPredict,mutualLoveStep, 
                projectionSpeed, projectionSpeedIncrement, isAnxious, targetIds, interactObjectIds, expressionSpontaneous)
            {
                fe = fe != null ? fe.Select(v => v.ToArray()).ToArray() : fe,
                certTable = newCertTable,
                postPreferences = newPostPref,
                neutralref = neutralref,
                sensoryEvidenceUpdateWeight = sensoryEvidenceUpdateWeight,
                sevuncertaingain = sevuncertaingain,
                mu = newMu,
                spatialStats = newSS,
                stateFE = stateFE,
                pbodies = pbod.Count > 0 ? pbod : null
            };
#if COMPUTE_TIME
                _millis += sw.Elapsed.TotalMilliseconds;
#endif
            return sp;
        }

        public void UpdateOtherEmotionFelt()
        {
            foreach(var agentId in agentsIds)
            {
                if (agentId != currentAgentId)
                {
                    var physiological = emotions[agentId].Physiological * physiologicalSensitivity;
                    var facial = emotions[agentId].Facial * (1 - physiologicalSensitivity);
                    emotions[agentId].Felt = physiological + facial;
                }
            }
        }

        public void UpdateFacialEmotion()
        {
            if (expressionSpontaneous){
                emotions[currentAgentId].Facial = emotions[currentAgentId].Felt;
            }
            else{
                var facialVoluntary = emotions[currentAgentId].VoluntaryFacial * voluntaryFacialWeight;
                var facialUnvoluntary = emotions[currentAgentId].Felt  * (1 - voluntaryFacialWeight);
                emotions[currentAgentId].Facial = (facialVoluntary + facialUnvoluntary) * facialReactivity;
            }
        }

        public void UpdatePhysiologicalEmotion()
        {
            emotions[currentAgentId].Physiological = (emotions[currentAgentId].VoluntaryPhysiological * voluntaryPhysiologicalWeight + emotions[currentAgentId].Felt * (1 - voluntaryPhysiologicalWeight)) * physiologicalReactivity;
        }

        /// <summary>
        /// Performs a shallow copy of the State
        /// </summary>
        /// <returns>State</returns>
        public AgentState ShallowCopy()
        {
            return new AgentState(currentAgentId, agentsIds)
            {
                objectBodies = objectBodies,
                actionDirectories = actionDirectories,
                goalDirectories = goalDirectories,
                inferences = inferences,
                canInfer = canInfer,
                emotions = emotions,
                preferences = preferences,
                postPreferences = postPreferences,
                emogain = emogain,
                physiologicalReactivity = physiologicalReactivity,
                facialReactivity = facialReactivity,
                voluntaryPhysiologicalWeight = voluntaryPhysiologicalWeight,
                voluntaryFacialWeight = voluntaryFacialWeight,
                physiologicalSensitivity = physiologicalSensitivity,
                sevuncertaingain = sevuncertaingain,
                sensoryEvidenceUpdateWeight = sensoryEvidenceUpdateWeight,
                neutralref = neutralref,
                speed = speed,
                fe = fe,
                certTable = certTable,
                tomInfluence = tomInfluence,
                tomUpdate = tomUpdate,
                projectionSpeed = projectionSpeed,
                projectionSpeedIncrement = projectionSpeedIncrement,
                tomPredict = tomPredict,
                isAnxious = isAnxious,
                mu = mu,
                spatialStats = spatialStats,
                stateFE = null,
                pbodies = pbodies,
                targetIds = targetIds,
                interactObjectIds = interactObjectIds,
                mutualLoveStep = mutualLoveStep,
                expressionSpontaneous = expressionSpontaneous
            };
        }

        public double GetCurrentFreeEnergy()
        {
            // if (stateFE.HasValue)
            //     return stateFE.Value;
            if (fe == null)
                throw PCM.Core.Utils.Misc.Error("AgentState", "Can't get Fe, it has to be computed first");

            double freeEnergy = 0;
            foreach (int agentIndex in agentsIds)
            {
                freeEnergy += fe[agentIndex].Sum(v => v.freeEnergy) * tomInfluence[currentAgentId][agentIndex];
            }
            stateFE = freeEnergy;
            return freeEnergy;
        }
    }
}