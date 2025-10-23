using UnityEngine;
using Schemas;
using Core;
using Valve.Newtonsoft.Json;
using SensorDataStructure;
using System.Collections.Generic;
using static Core.Interfacing;

namespace Exp{

    [System.Serializable]
    public class SimulationParametersManager
    {
        public string timeStamp { get; set; }
        public int simulationID { get; set; }
        public VirtualAgentRole virtualAgentRole { get; set; }
        public ArtificialAgentMode AA_Mode { get; set; }
        public double virtualAgentTrueBelief { get; set; }
        public int TomPredict {get; set; }
        public int Dp { get; set; }
        public int rewardBoxID { get; set; }
        public double LLMUpdateWeight { get; set; }
        public Language language { get; set; }
        public int agentScore {  get; set; }
        public int playerScore { get; set; }
        public string filePath {  get; set; } 
        public int selectedBox {  get; set; }
        public bool facialExpress { get; set; }
        public double physioSensitivity { get; set; }
        public int hypothesis { get; set; }
        public SimulationParametersManager(string timeStamp, string filePath, VirtualAgentRole virtualAgentRole, ArtificialAgentMode AA_Mode, double virtualAgentTrueBelief, int TomPredict, int DP, int rewardBoxID, Language language, int simulationID, double LLMUpdateWeight, bool facialExpress, double physioSensitivity, int hypothesis)
        {
            this.timeStamp = timeStamp;
            this.filePath = filePath;
            this.virtualAgentRole = virtualAgentRole;
            this.rewardBoxID = rewardBoxID;
            this.AA_Mode = AA_Mode;
            this.LLMUpdateWeight = LLMUpdateWeight;
            this.virtualAgentTrueBelief = virtualAgentTrueBelief;
            this.TomPredict = TomPredict;
            this.Dp = DP;
            this.language = language;
            this.simulationID = simulationID;
            this.facialExpress = facialExpress;
            this.physioSensitivity = physioSensitivity;
            this.hypothesis = hypothesis;
        }

        public void SetScores(int agent, int player)
        {
            agentScore = agent;
            playerScore = player;
        }
    }

    public partial class Entity
    {
        [JsonProperty("id")]
        public int id { get; set; }

        [JsonProperty("transform")]
        public Positioning positioning { get; set; }
    }

    public class StreamingData
    {
        [JsonProperty("timeStamp")]
        public long timeStamp { get; set; }

        [JsonProperty("playerSensorData")]
        public PlayerSensorData player { get; set; }

        [JsonProperty("agents")]
        public AgentOutput[] agents { get; set; }

        /*[JsonProperty("agents")]
        public Agent[] agents { get; set; }*/

        [JsonProperty("objects")]
        public Entity[] objects { get; set; }
        
        [JsonProperty("prompts")]
        public Dictionary<string, string> prompts { get; set; }
    }
}