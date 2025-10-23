using Schemas;
using SensorDataStructure;
using System;
using UnityEngine;
using Core;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using static Core.Interfacing;
using Simulation;
using UnityEngine.Playables;

namespace Exp
{
    public class Streamer : MonoBehaviour
    {
        [SerializeField] private string streamingFolderPath;
        public StreamingMode streaming;
        private string folderPath;
        private string jsonFolderPath;
        private int perceptionIte = 0;
        private int iteration = 0;
        private int simulationId;
        private bool textAvailable = false;
        private Dictionary<string, string> prompt;
        private string filePathForLLM;
        public string FilePath() => filePathForLLM;
        private string csvFileName = "evaluation_result.csv"; 
        void Awake()
        {
            if (ScenesManager.Singleton != null)
            {
                streaming = ScenesManager.Singleton.Streaming;
                SimulationParametersManager parameters = ScenesManager.Singleton.CurrentConfig;
                folderPath = parameters.filePath;
                simulationId = parameters.simulationID;
            }
            else
            {
                folderPath = Application.streamingAssetsPath + $"/DataStream/Logs_{DateTime.UtcNow.ToString("yyyyMMddHHmm")}";
                simulationId = 0;
            }

            jsonFolderPath = Path.Combine(folderPath, $"simulation{simulationId}");
            var list = jsonFolderPath.Split("Logs_");
            filePathForLLM = list[1];

            if (streaming != StreamingMode.Never)
            {
                Directory.CreateDirectory(jsonFolderPath);
            }
        }

        public void SavingText(string speaker, string text)
        {
            textAvailable = true;
            prompt = new Dictionary<string, string>();
            prompt[speaker] = text;
        }

        public void SavingInitData(AgentOutput[] output, ObjectOfInterest[] m_boxes, PlayerManager m_player, bool player)
        {
            if (streaming != StreamingMode.Never)
            {
                string fileName = Path.Combine(jsonFolderPath, $"log_{iteration}.json");
                string streamingData = GetStreamingData(output, m_boxes, m_player, player);
                File.WriteAllText(fileName, streamingData);
                iteration++;

                Schemas.Core parameters = Manager.Singleton.config.GetParameters();
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
                File.WriteAllText(Path.Combine(jsonFolderPath, "config.json"), serializedParameters);

            }
        }

        public void SavingData(AgentOutput[] output, AgentMainController[] m_agents, ObjectOfInterest[] m_boxes, PlayerManager m_player, bool player)
        {
            if (streaming == StreamingMode.Always || (streaming == StreamingMode.WhenTextInput && textAvailable))
            {
                string fileName = Path.Combine(jsonFolderPath, $"log_{iteration}.json");
                string streamingData = GetStreamingData(output, m_boxes, m_player, player);
                File.WriteAllText(fileName, streamingData);
                iteration++;
                SavingPerception(m_agents);
            }
        }

        public void SavingPerception(AgentMainController[] m_agents)
        {
            for (int agentId = 0; agentId < m_agents.Length; agentId++)
            {
                byte[] Bytes = Convert.FromBase64String(m_agents[agentId].Base64String);
                string path = jsonFolderPath;
                string fileName = $"agent{agentId}_{perceptionIte}";
                File.WriteAllBytes($"{path}/{fileName}.png", Bytes);
            }
            perceptionIte++;
        }

        public string GetStreamingData(AgentOutput[] agents, ObjectOfInterest[] m_boxes, PlayerManager m_player, bool player)
        {
            //Debug.Log(JsonConvert.SerializeObject(agents[0]));
            // player data
            PlayerSensorData playerData = new();
            if (player)
            {
                var bodyData = m_player.GetBodyData();
                var faceData = m_player.GetFaceData();
                var gazeData = m_player.GetGazeData();
                playerData = new PlayerSensorData(bodyData, faceData, gazeData);
                agents[agents.Length - 1].TargetId = m_player.TargetId();
                agents[agents.Length - 1].Emotions = m_player.GetEmotions();
            }

            // objects data
            Exp.Entity[] objectStates = new Exp.Entity[m_boxes.Length];
            for (int objectID = 0; objectID < m_boxes.Length; objectID++)
            {
                Interfacing.Body body = m_boxes[objectID].GetBodyData();

                Positioning position = new()
                {
                    Position = new double[] { body.Center.X, body.Center.Y, body.Center.Z },
                    LookAt = new double[] { body.Orientation.X, body.Orientation.Y, body.Orientation.Z },
                    Type = 1,
                };
                Exp.Entity objectState = new()
                {
                    id = objectID,
                    positioning = position
                };
                objectStates[objectID] = objectState;
            }

            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            StreamingData streamingData = new()
            {
                timeStamp = timeStamp,
                player = playerData,
                agents = agents,
                objects = objectStates
            };

            if (textAvailable)
            {
                streamingData.prompts = prompt;
            }

            textAvailable = false;
            string text = JsonConvert.SerializeObject(streamingData, Formatting.Indented);
            return text;
        }

        public void SaveSimulationParameters(SimulationParametersManager parameters)
        {
            string csvfolderPath = Path.Combine(Application.streamingAssetsPath, "DataStream");
            string filePath = Path.Combine(csvfolderPath, csvFileName);
            bool isNewFile = !File.Exists(filePath);
            
            List<string> conditionParameters = new List<string>
            {
                parameters.timeStamp.ToString(),
                parameters.simulationID.ToString(),
                parameters.virtualAgentRole.ToString(),
                parameters.AA_Mode.ToString(),
                parameters.LLMUpdateWeight.ToString(),
                parameters.TomPredict.ToString(),
                parameters.Dp.ToString(),
                parameters.facialExpress.ToString(),
                parameters.physioSensitivity.ToString(),
                parameters.hypothesis.ToString(),
                parameters.agentScore.ToString(),
                parameters.playerScore.ToString(),
                parameters.virtualAgentTrueBelief.ToString(),
                parameters.rewardBoxID.ToString(),
                parameters.selectedBox.ToString(),
                parameters.language.ToString()
            };

            string conditionHeader = "TimeStamp,SimulationID,VirtualAgentRole,VerbalMode,LLMPrefUpdateWeight,ToM,DepthProcessing,FacialExpression,ParticipantPhysiologicalSensitivity,LLMHypothesis,AgentScore,PlayerScore,VirtualAgentBelief,RewardBoxID,SelectedBox,Language";
            string conditionText = string.Join(",", conditionParameters);

            if (isNewFile)
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(conditionHeader + ",Q1,Q2,Q3,Q4,Q5,Q6,Q7,Q8");
                    writer.WriteLine(conditionText + ",,,,,,,,"); 
                }
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(conditionText + ",,,,,,,,"); 
                }
            }

            Debug.Log("Manager Scene parameters saved to CSV: " + filePath);
        }
    }

    public enum StreamingMode
    {
        Always,
        WhenTextInput,
        Never
    }

}