using Schemas;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;
using UnityEngine.InputSystem.Controls;

namespace Exp{
    public class ScenesManager : MonoBehaviour
    {
        public static ScenesManager Singleton { get; private set;}

        [SerializeField] private string sceneName;
        [SerializeField] private string assessmentSceneName;
        [SerializeField] private string csvFileName;
        [SerializeField] private Language language;
        [SerializeField] private StreamingMode streaming;

        private string timeStamp;
        private int[] agentPoint = new int[] { 0, 0 };
        private SimulationParametersManager currentConfig;
        private List<SimulationParametersManager> parametersList = new List<SimulationParametersManager>();
        private string streamingFolderPath;
        private int loadCount = 0;
        private int maxLoadCount;

        public StreamingMode Streaming
        {
            get { return streaming; }
        }

        public int[] AgentPoint
        {
            get { return agentPoint; }
            set { agentPoint = value; }
        }

        public SimulationParametersManager CurrentConfig
        {
            get { return currentConfig; }
            private set { currentConfig = value; }
        }

        void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
                DontDestroyOnLoad(gameObject);

                // Create logs folder path
                timeStamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
                streamingFolderPath = Path.Combine(Application.streamingAssetsPath, $"DataStream/Logs_{timeStamp}"); //Simulation/Log_yyMMddHHmm
                Directory.CreateDirectory(streamingFolderPath);

                InitializeParameters();

                maxLoadCount = parametersList.Count;
                // TODO shuffle the list or not ???
                LoadSimulationScene();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {

        }

        // Method to initialize parameters from the Control Scene
        public void InitializeParameters()
        {
            int simulationID = 0;
            VirtualAgentRole virtualAgentRole = VirtualAgentRole.Partner;
            ArtificialAgentMode aaMode = ArtificialAgentMode.Verbal;
            double LLMUpdateWeight = 0.5;
            int DP = 2;
            //int hypothesis = 1;
            //int tomPredict = 1;
            int FacialExpress = 0;
            double physioSensitivity = 0.0;
            for (int i = 0; i < 2; i++){
            
                //foreach (VirtualAgentRole virtualAgentRole in Enum.GetValues(typeof(VirtualAgentRole))){
                    //for (int FacialExpress = 1; FacialExpress < 2; FacialExpress++){
                    //for (int tomPredict = 1; tomPredict < 2; tomPredict++){
                    //for (int hypothesis = 2; hypothesis < 3; hypothesis ++){
                        //double physioSensitivity = FacialExpress==0? 0.5 : 1;
                        //int n = - FacialExpress * 25 + 58;

                        //foreach (ArtificialAgentMode aaMode in Enum.GetValues(typeof(ArtificialAgentMode))){
                        for (int hypothesis = 1; hypothesis < 2; hypothesis+=2)
                        //{
                        //double max = 0.9;
                        //if (FacialExpress == 0)
                        //{
                        //    max = 0.1;
                        //}


                        //for (double physioSensitivity = 1.0; physioSensitivity < 1.1; physioSensitivity += 0.5)
                        //{
                        //simulationID++;
                        //InsertCombinason(i, simulationID, FacialExpress, LLMUpdateWeight, tomPredict, DP, virtualAgentRole, aaMode, physioSensitivity, hypothesis);
                        if ((hypothesis == 2) || (hypothesis == -1))
                        {
                            for (int tomPredict = 1; tomPredict < 2; tomPredict++)
                            {
                            simulationID++;
                            InsertCombinason(1, simulationID, FacialExpress, LLMUpdateWeight, tomPredict, DP, virtualAgentRole, aaMode, physioSensitivity, hypothesis);
                            }
                        }
                        else
                        {
                            int tomPredict = 0;
                            simulationID++;
                            InsertCombinason(1, simulationID, FacialExpress, LLMUpdateWeight, tomPredict, DP, virtualAgentRole, aaMode, physioSensitivity, hypothesis);
                        }
                    //}

                    //}
                //}
                
            }
            Debug.Log(JsonConvert.SerializeObject(parametersList));
        }

        public void InsertCombinason(int i, int simulationID, double FacialExpress, double LLMUpdateWeight, int tomPredict, int DP, VirtualAgentRole virtualAgentRole, ArtificialAgentMode aaMode, double physioSensitivity, int hypothesis){
            System.Random random = new System.Random();

            int virtualAgentTrueBelief = i==0? 0:1;
            int rewardBoxID = random.Next(0, 2);
            SimulationParametersManager par = new SimulationParametersManager(timeStamp, streamingFolderPath, virtualAgentRole, aaMode, virtualAgentTrueBelief, tomPredict, DP, rewardBoxID, language, simulationID, LLMUpdateWeight, Convert.ToBoolean(FacialExpress), physioSensitivity, hypothesis);
            parametersList.Add(par);
        }

        public void LoadSimulationScene()
        {
            if (loadCount < maxLoadCount)
            {
                currentConfig = parametersList[loadCount];
                Debug.Log("Scene loaded for the " + (loadCount+1) + " times.");
                SceneManager.LoadSceneAsync(sceneName);
                loadCount++;
            }
            else
            {
                Application.Quit();
                EditorApplication.isPlaying = false;
            }
        }

        public void LoadAssessmentScene()
        {
            Debug.Log("participant score: " + agentPoint[1]);
            Debug.Log("agent score; " + agentPoint[0]);
            currentConfig.SetScores(agentPoint[0], agentPoint[1]);
            SceneManager.LoadSceneAsync(assessmentSceneName);
        }
    }
}


