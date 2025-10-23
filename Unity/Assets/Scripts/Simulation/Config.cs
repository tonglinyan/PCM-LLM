using CrazyMinnow.SALSA;
using Exp;
using Meta.WitAi;
using Newtonsoft.Json;
using Schemas;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Assets.Scripts.Utils;

namespace Simulation
{
    public class Config : MonoBehaviour
    {
        [SerializeField] private string fileName;
        [SerializeField] PlayerManager player;
        [SerializeField] AgentMainController[] agents;
        [SerializeField] Box[] boxes;
        [SerializeField] private double virtualAgentTrueBelief;
        [SerializeField] VirtualAgentRole virtualAgentRole;
        [SerializeField] private int rewardBoxID;
        [SerializeField] private ArtificialAgentMode virtualAgentMode;
        string jsonPath;
        int agentCount;
        int objectCount;
        int entityCount;

        public string ConfigFileName
        {
            get { return fileName; }
            private set { fileName = value; }
        } 
        public PlayerManager Player
        {
            get { return player; }
            private set { player = value; }
        }
        public AgentMainController[] Agents
        {
            get { return agents; }
            private set { agents = value; }
        }

        public Box[] Boxs
        {
            get { return boxes; }
            private set { boxes = value; }
        }

        public ArtificialAgentMode VirtualAgentMode
        {
            get { return virtualAgentMode; }
            private set { virtualAgentMode = value; }
        }

        public void SetConfig(SimulationParametersManager SB_parameters)
        {
            virtualAgentRole = SB_parameters.virtualAgentRole;
            rewardBoxID = SB_parameters.rewardBoxID;
            virtualAgentMode = SB_parameters.AA_Mode;
            virtualAgentTrueBelief = SB_parameters.virtualAgentTrueBelief;
            bool haveFacialExpression = SB_parameters.facialExpress;

            var parameters = GetParameters();
            
            // set facial_express as optimization list
            for (int agentID = 0; agentID < Manager.NB_AGENTS; agentID ++)
            {
                if (!haveFacialExpression && parameters.Agents.ActionTags[agentID][agentID].Contains("facial_express")){
                    List<string> list = new List<string>(parameters.Agents.ActionTags[agentID][agentID]);

                    list.Remove("facial_express");
                    parameters.Agents.ActionTags[agentID][agentID] = list.ToArray();
                    parameters.Agents.VoluntaryFacialWeight[Manager.AA_ID] = 0;
                    parameters.Agents.VoluntaryFacialWeight[Manager.PARTICIPANT_ID] = 0;
                }
                if (haveFacialExpression && !parameters.Agents.ActionTags[agentID][agentID].Contains("facial_express")){
                    List<string> list = new List<string>(parameters.Agents.ActionTags[agentID][agentID]);

                    list.Add("facial_express");
                    parameters.Agents.ActionTags[agentID][agentID] = list.ToArray();
                    parameters.Agents.VoluntaryFacialWeight[Manager.AA_ID] = 0.8;
                    parameters.Agents.VoluntaryFacialWeight[Manager.PARTICIPANT_ID] = 0.8;
                }
            }

            // modify physiological sensitivity weight
            parameters.Agents.PhysiologicalSensitivity[Manager.PARTICIPANT_ID] = SB_parameters.physioSensitivity;

            // set tomPredict
            parameters.Agents.TomPredict[Manager.AA_ID][Manager.AA_ID] = SB_parameters.TomPredict;
            parameters.Agents.TomPredict[Manager.AA_ID][Manager.PARTICIPANT_ID] = SB_parameters.TomPredict > 0 ? SB_parameters.TomPredict - 1 : 0;

            WriteConfig(parameters);
            OnValidate();
        }

        private Positioning[] GetPositionings(MonoBehaviour[] monoBehaviours)
        {
            List<Positioning> positions = new();
            foreach (var monoBehaviour in monoBehaviours)
            {
                var transform = monoBehaviour.gameObject.transform;
                var vector = transform.rotation * Vector3.forward;
                positions.Add(new Positioning()
                {
                    Position = Vector3ToDoubleArray(transform.position),
                    LookAt = Vector3ToDoubleArray(new Vector3(vector.x, vector.z, vector.y))
                }); ;
            }
            return positions.ToArray();
        }

        public Schemas.Core GetParameters()
        {
            agentCount = (player == null)? agents.Length : agents.Length + 1;
            objectCount = boxes.Length;
            entityCount = agentCount + objectCount;
            jsonPath = Path.Combine(Application.streamingAssetsPath, $"{fileName}.json");
            return JsonConvert.DeserializeObject<Schemas.Core>(File.ReadAllText(jsonPath));
        }

        public MonoBehaviour[] SumUpMonoBehaviours()
        {
            if (player != null)
            {
                MonoBehaviour[] mono = new MonoBehaviour[agentCount];
                agents.CopyTo(mono, 0);
                mono[mono.Length - 1] = player;
                return mono;
            }
            else
                return agents;
        }

        public void UpdateFile()
        {
            var parameters = GetParameters();
            var agentsParameters = parameters.Agents;

            agentsParameters.Positioning = GetPositionings(SumUpMonoBehaviours());
            parameters.Objects.Positioning = GetPositionings(boxes);
            agentsParameters.Speed = ResizeArray(agentsParameters.Speed, new int[] { agentCount }, new Speed()
            {
                RealSpeed = 90,
                ProjectionSpeed = 0,
                ProjectionSpeedIncrement = 0
            });
            agentsParameters.TomPredict = ResizeMatrix(agentsParameters.TomPredict, new int[] { agentCount, agentCount }, 0);
            agentsParameters.Anxious = ResizeArray(agentsParameters.Anxious, new int[] { agentCount }, false);
            agentsParameters.Preferences = Resize3DArray(agentsParameters.Preferences, new int[] { agentCount, agentCount, entityCount }, 0.5);
            agentsParameters.TomInfluence = Resize3DArray(agentsParameters.TomInfluence, new int[] { agentCount, agentCount, agentCount }, 0);
            agentsParameters.TomUpdate = Resize3DArray(agentsParameters.TomUpdate, new int[] { agentCount, agentCount, agentCount }, 0);
            agentsParameters.MutualLoveStep = Resize3DArray(agentsParameters.MutualLoveStep, new int[] { agentCount, agentCount, agentCount }, 0);
            agentsParameters.Emogain = ResizeArray(agentsParameters.Emogain, new int[] { agentCount }, 0.5f);
            agentsParameters.PhysiologicalReactivity = ResizeArray(agentsParameters.PhysiologicalReactivity, new int[] { agentCount }, 0.5f);
            agentsParameters.FacialReactivity = ResizeArray(agentsParameters.FacialReactivity, new int[] { agentCount }, 0.5f);
            agentsParameters.VoluntaryPhysiologicalWeight = ResizeArray(agentsParameters.VoluntaryPhysiologicalWeight, new int[] { agentCount }, 0.5f);
            agentsParameters.VoluntaryFacialWeight = ResizeArray(agentsParameters.VoluntaryFacialWeight, new int[] { agentCount }, 0.5f);
            agentsParameters.PhysiologicalSensitivity = ResizeArray(agentsParameters.PhysiologicalSensitivity, new int[] { agentCount }, 0.5f);
            WriteConfig(parameters);
        }

        public void UpdateScene()
        {
            var parameters = GetParameters();
            agentCount = agents.Length;
            objectCount = boxes.Length;
            for (int i = 0; i < agentCount; i++)
            {
                var gameObject = agents[i].gameObject;
                var position = parameters.Agents.Positioning[i].Position;
                var lookAt = parameters.Agents.Positioning[i].LookAt;
                gameObject.transform.SetPositionAndRotation(new Vector3((float)position[0], (float)position[1], (float)position[2]), Quaternion.LookRotation(new Vector3((float)lookAt[2], (float)lookAt[1], (float)lookAt[0])));
            }
            for (int i = 0; i < objectCount; i++)
            {
                var gameObject = boxes[i].gameObject;
                var position = parameters.Objects.Positioning[i].Position;
                var lookAt = parameters.Objects.Positioning[i].LookAt;
                gameObject.transform.SetPositionAndRotation(new Vector3((float)position[0], (float)position[1], (float)position[2]), Quaternion.LookRotation(new Vector3((float)lookAt[2], (float)lookAt[1], (float)lookAt[0])));
            }
        }

        public void WriteConfig(Schemas.Core parameters)
        {
            string serializedParameters;
            using (StringWriter stringWriter = new())
            {
                using (JsonTextWriter jsonWriter = new(stringWriter))
                {
                    jsonWriter.Formatting = Formatting.Indented;
                    JsonSerializer serializer = new();
                    serializer.Serialize(jsonWriter, parameters);
                }
                serializedParameters = stringWriter.ToString();
            }
            File.WriteAllText(jsonPath, serializedParameters);
        }

        public int RewardBoxID
        {
            private set { }
            get { return rewardBoxID; }
        }

        public VirtualAgentRole VirtualAgentRole_
        {
            private set { }
            get { return virtualAgentRole; }
        }

        public void OnValidate()
        {
            var parameters = GetParameters();
            for (int boxID = 0; boxID < Manager.NB_BOXES; boxID++){
                parameters.Agents.Preferences[0][0][boxID + Manager.NB_AGENTS] = (rewardBoxID == boxID ? virtualAgentTrueBelief : (1 - virtualAgentTrueBelief)) * 0.3 + 0.5f;
            }
            
            parameters.Agents.Preferences[Manager.AA_ID][Manager.AA_ID][Manager.PARTICIPANT_ID] = virtualAgentRole == VirtualAgentRole.Partner ? 0.7 : 0.3;
            WriteConfig(parameters);
        }
    }
}

