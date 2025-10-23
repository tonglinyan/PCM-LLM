using Exp;
using Schemas;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using Newtonsoft.Json;

namespace Assessment{

    public class LikertUIManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] englishPanel;
        [SerializeField] private GameObject[] frenchPanel;
        [SerializeField] private Language language;
        
        private string csvFileName = "evaluation_result.csv"; 
        private SimulationParametersManager parameters;
        private string folderPath;
        private int panelIndice;
        private Dictionary<string, string> responses = new Dictionary<string, string>();

        void Start(){
            folderPath = Path.Combine(Application.streamingAssetsPath, "DataStream");

            Debug.Log(ScenesManager.Singleton != null);
            if (ScenesManager.Singleton != null){
                parameters = ScenesManager.Singleton.CurrentConfig;
                language = parameters.language;
                folderPath = parameters.filePath;
                Debug.Log(parameters.ToString());
            }
            panelIndice = 0;
            SetLanguage(language);
        }

        private void SetLanguage(Language language)
        {

            for (int i = 0; i < englishPanel.Length; i++)
            {
                englishPanel[i].SetActive(false);
            }
            for (int i = 0; i < frenchPanel.Length; i++)
            {
                frenchPanel[i].SetActive(false);
            }

            // Activate the appropriate panel based on the selected language
            switch (language)
            {
                case Language.EN:
                    englishPanel[panelIndice].SetActive(true);
                    break;
                case Language.FR:
                    Debug.Log(frenchPanel.Length);
                    frenchPanel[panelIndice].SetActive(true);
                    break;
                default:
                    Debug.LogWarning("Unsupported language: " + language);
                    break;
            }
        }

        public void OnSaveButtonPressed()
        {
            SaveRatingSceneParameters();
        }

        public void OnNextPagePressed()
        {
            if (language == Language.EN)
            {
                englishPanel[panelIndice].SetActive(false);
                englishPanel[panelIndice + 1].SetActive(true);
            }
            else if (language == Language.FR)
            {
                frenchPanel[panelIndice].SetActive(false);
                frenchPanel[panelIndice + 1].SetActive(true);
            }
            else
            {
                Debug.LogWarning("Unsupported language: " + language);
            }
            panelIndice++;
            Debug.Log("current page: " + panelIndice);
        }

        public void OnPreviousPagePressed()
        {
            if (language == Language.EN)
            {
                englishPanel[panelIndice].SetActive(false);
                englishPanel[panelIndice - 1].SetActive(true);
                //englishPanel.SetActive(true);
            }
            else if (language == Language.FR)
            {
                frenchPanel[panelIndice].SetActive(false);
                frenchPanel[panelIndice - 1].SetActive(true);
            }
            else
            {
                Debug.LogWarning("Unsupported language: " + language);
            }
            panelIndice--;
            Debug.Log("current page: " + panelIndice);
        }

        private void SaveRatingSceneParameters()
        {
            string filePath = Path.Combine(folderPath, csvFileName);
            if (!File.Exists(filePath))
            {
                Debug.LogError("CSV file not found! Manager Scene data must be stored first.");
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 2) 
            {
                Debug.LogError("No valid data found in CSV.");
                return;
            }

            int lastIndex = lines.Length - 1;
            string[] lastRow = lines[lastIndex].Split(',');

            for (int i = 0; i < englishPanel.Length; i++)
            {
                GameObject shownPanel = language == Language.EN ? englishPanel[i] : frenchPanel[i];
                SliderManager sliderManager = shownPanel.GetComponent<SliderManager>();
                sliderManager.GetSliderValues($"panel{i+1}", responses);
            }

            for (int i = 0; i < 8; i++)
            {
                lastRow[9 + i] = responses[$"panel{i+1}"].ToString(); 
            }

            lines[lastIndex] = string.Join(",", lastRow);
            File.WriteAllLines(filePath, lines);

            Debug.Log("Rating Scene responses saved to CSV: " + filePath);
        }

        private void SaveResponses()
        {
            for (int i = 0; i < englishPanel.Length; i++)
            {
                GameObject shownPanel = language == Language.EN ? englishPanel[i] : frenchPanel[i];
                SliderManager sliderManager = shownPanel.GetComponent<SliderManager>();
                sliderManager.GetSliderValues($"panel{i+1}", responses);
            }
                
            // if save button is detected to be pressed on the last page, save all the responses.
            string header = string.Join(",", responses.Keys);
            string values = string.Join(",", responses.Values);

            if (parameters != null)
            {
                List<string> conditionParameters = new List<string>
                {
                    parameters.simulationID.ToString(),
                    parameters.virtualAgentRole.ToString(),
                    parameters.AA_Mode.ToString(),
                    parameters.virtualAgentTrueBelief.ToString(),
                    parameters.rewardBoxID.ToString(),
                    parameters.selectedBox.ToString(),
                    parameters.language.ToString(),
                    parameters.agentScore.ToString(),
                    parameters.playerScore.ToString()
                };

                string conditionHeader = "SimulationID,VirtualAgentRole,VerbalMode,VirtualAgentBelief,RewardBoxID,SelectedBox,Language,AgentScore,PlayerScore,";
                string conditionText = string.Join(",", conditionParameters);
                header = conditionHeader + header;
                values = conditionText + "," + values;
            }

            string filePath = Path.Combine(folderPath, csvFileName);
            if (!File.Exists(filePath))
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine(header);
                }
            }
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(values);
            }

            Debug.Log("Responses saved to CSV: " + filePath);

            //Reinitialise the value of sliders to 0

            if (ScenesManager.Singleton != null)
                ScenesManager.Singleton.LoadSimulationScene();
            else
                EditorApplication.isPlaying = false;
        }
    }
}
