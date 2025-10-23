using System.IO;
using System.Net.Http;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using Oculus.Voice;
using TMPro;
using Schemas;
using Simulation;
using Exp;
using System.Collections;
using System.Threading.Tasks;

namespace Verbal
{
    public enum InteractionState { Starting, Waiting, Speaking, Listening, SendingRequest, Done }

    [RequireComponent(typeof(Streamer))]
    public class VerbalUpdatedManager : MonoBehaviour
    {
        [Range(0, 1)][SerializeField] double preferenceUpdateWeight = 1f;
        [SerializeField] int PCM_CoreUpdatesMemoryLength = 1;
        [SerializeField] int depth = 1;
        [SerializeField] string contextFileName;
        [SerializeField] string[] entityNames;
        [SerializeField] private int nbIterations;
        private Streamer streamingData;

        [Header("Speech to Text")]
        [SerializeField] private AppVoiceExperience appVoiceExperience;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip promptStartClip;
        [SerializeField] AudioClip promptEndClip;
        [SerializeField] TextMeshProUGUI transcriptionText;
        [SerializeField] int timeOfTranscriptionExtension;

        [Header("Text to Speech")]
        [SerializeField] TextMeshProUGUI TTSText;
        private bool isSpeaking = false;
        //[SerializeField] AudioSource[] TTSAudioSources;
        HttpClient client;

        [Header("Communication between models")]
        /*[SerializeField] bool PCMToLLM;
        [SerializeField] bool LLMToPCM;*/
        [Range(-1, 2)][SerializeField] public int hypothesis;
        string question;
        bool isListening = false;
        
        private InteractionState state = InteractionState.Waiting;
        private int interactionId = 0;
        private int agentId = 0;
        private bool hasPlayer;
        private ArtificialAgentMode AA_mode;

        AgentMainController[] agents;

        public static VerbalUpdatedManager Singleton { get; private set; }
        public int NbInterations { get { return nbIterations; } }
        public double LLMUpdateWeight { get { return preferenceUpdateWeight; } set { preferenceUpdateWeight = value; } }

        public InteractionState State { get {return state; } set { state = value; }}

        void Awake()
        {
            Singleton = this;
            streamingData = GetComponent<Streamer>();
            appVoiceExperience.VoiceEvents.OnRequestCompleted.AddListener(ReactivateVoice);
            appVoiceExperience.VoiceEvents.OnPartialTranscription.AddListener(OnPartialTranscription);
            appVoiceExperience.VoiceEvents.OnFullTranscription.AddListener(OnFullTranscription);
        }

        private void OnDisable()
        {
            appVoiceExperience.VoiceEvents.OnRequestCompleted.RemoveAllListeners();
            appVoiceExperience.VoiceEvents.OnPartialTranscription.RemoveAllListeners();
            appVoiceExperience.VoiceEvents.OnFullTranscription.RemoveAllListeners();
            foreach (var agent in agents)
            {
                agent.Speaker.OnPlaybackComplete -= HandlePlaybackComplete;
            }
        }

        public void SetConfig(SimulationParametersManager parameters)
        {
            LLMUpdateWeight = parameters.LLMUpdateWeight;
            hypothesis = parameters.hypothesis;
        }

        public void Init(HttpClient client, AgentMainController[] agents, bool player, ArtificialAgentMode aa_mode)
        {
            this.client = client;
            this.agents = agents;
            this.hasPlayer = player;
            AA_mode = aa_mode;
            /*if (AA_mode == ArtificialAgentMode.NonVerbal){
                state = InteractionState.Done;
            }
            else{*/
            question = "null";
            foreach (var agent in agents)
            {
                agent.Speaker.OnPlaybackComplete += HandlePlaybackComplete;
            }
            state = InteractionState.Waiting;
            //StartCoroutine(WaitForSecond(5));
            //}
            //Debug.Log("nb iteration: " + nbIterations);
        }

        public Schemas.Verbal GetParameters()
        {
            var verbalParameters = new Schemas.Verbal();
            string jsonPath = Path.Combine(Application.streamingAssetsPath, $"{contextFileName}.json");
            verbalParameters.ContextJSON = File.ReadAllText(jsonPath);
            verbalParameters.Depth = depth;
            verbalParameters.PCM_CoreUpdatesMemoryLength = PCM_CoreUpdatesMemoryLength;
            verbalParameters.PreferenceUpdateWeight = preferenceUpdateWeight;
            verbalParameters.EntityNames = entityNames;
            verbalParameters.filePath = streamingData.FilePath();
            /*verbalParameters.PCMtoLLM = PCMToLLM;
            verbalParameters.LLMtoPCM = LLMToPCM; */
            verbalParameters.Hypothesis = hypothesis;
            return verbalParameters;
        }

        async Task Update()
        {
            switch (state)
            {
                case InteractionState.Starting:
                    Debug.Log("Verbal, starting: " + interactionId + " " + nbIterations);
                    if (interactionId < nbIterations)
                    {
                        agentId = hasPlayer ? 0 : 1;
                        Debug.Log("has player: " + hasPlayer);
                        if (hasPlayer)
                        {
                            question = "";
                            state = InteractionState.Listening;
                        }
                        else state = InteractionState.SendingRequest;
                    }
                    else {
                        //Manager.Singleton.EnableBox();
                        //if (!hasPlayer) Manager.Singleton.ParticipantChoosesCloserBox();
                        state = InteractionState.Done;
                    }
                    break;

                case InteractionState.SendingRequest:
                    Debug.Log("Verbal, SendingRequest");
                    SendAgentRequest();
                    state = InteractionState.Waiting;
                    break;

                case InteractionState.Listening:
                    if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
                    {
                        Debug.Log("index pressed");
                        StartVoiceListening();
                    }
                    if (OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger))
                    {
                        appVoiceExperience.Deactivate();
                        Debug.Log("index released");
                        isListening = false;
                        audioSource.clip = promptEndClip;
                        audioSource.Play();
                        //state = InteractionState.SendingRequest;
                        Debug.Log("text: " + question);
                    }
                    Debug.Log("Verbal, Listening");
                    break;

                case InteractionState.Speaking:

                    if (!isSpeaking)
                    {
                        if (agentId == 1 )//&& AA_mode != ArtificialAgentMode.NonVerbal)
                        {
                            agentId = 1 - agentId;
                            state = InteractionState.SendingRequest;
                        }
                        else
                        {
                            interactionId++;
                            state = InteractionState.Waiting;
                            Manager.Singleton.suspended = false;
                            /*if (AA_mode == ArtificialAgentMode.NonVerbal)
                                StartCoroutine(WaitForSecond(10));
                            else state = InteractionState.Waiting;*/
                        }
                    }
                    break;

                case InteractionState.Waiting:
                    // Interaction sequence finished
                    Debug.Log("Waiting");
                    break;

                case InteractionState.Done:
                    Debug.Log("Done");
                    this.enabled = false;
                    break;
            }
        }

        private IEnumerator WaitForSecond(int second)
        {
            yield return new WaitForSeconds(second);
            state = InteractionState.Starting;
        }


        private async void SendAgentRequest()
        {
            string perception = agents[agentId].Base64String;
            var data = new { text = question, image = perception };
            Debug.Log("input: " + question);
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"api/speak?agentId={agentId}", content);
            //agents[agentId].m_animator.SetBool("Thinking", true);
            response.EnsureSuccessStatusCode();
            var message = await response.Content.ReadAsStringAsync();
            message = message ?? string.Empty;
            Debug.Log(message);
            if (message == string.Empty){
                Manager.Singleton.StopPCM();
                Manager.Singleton.EndSimulation(-2);
            }
            else {
                await agents[agentId].SpeakOvertone(message);
            }

            isSpeaking = true;
            Debug.Log("isspeaking: " + isSpeaking);
            string entityname = Manager.Singleton.EntityNames[agentId];
            streamingData.SavingText($"agent{agentId}", message);
            TTSText.text = entityname + " : " + message;
            Debug.Log("text: " + message);
            question = message;
            state = InteractionState.Speaking;
        }


        void StartVoiceInput()
        {
            if (appVoiceExperience != null)
            {
                isListening = true;
                appVoiceExperience.Activate();
                Debug.Log("Voice input listening started.");
            }
            else Debug.LogError("VoiceExperience is not initialized!");

        }

        public void StartVoiceListening()
        {
            if (!isListening)
            {
                audioSource.clip = promptStartClip;
                audioSource.Play();
                StartVoiceInput();
            }
        }

        void ReactivateVoice()
        {
            if (isListening)
            {
                appVoiceExperience.Activate();
                Debug.Log("reactivated !! ");
            }

        }

        void OnPartialTranscription(string transcription)
        {
            transcriptionText.text = transcription;
        }

        void OnFullTranscription(string transcription)
        {
            //ExtendTranscription(transcription);
            transcriptionText.text = transcription;
            question += transcription;
            Debug.Log("full transcription! ");
            if (!isListening)
            {
                state = InteractionState.SendingRequest;
            }
        }

        private void HandlePlaybackComplete()
        {
            isSpeaking = false;
        }

        void OnDestroy()
        {
            /*Destroy(appVoiceExperience);
            GameObject buffer = GameObject.Find("AudioBuffer");
            if (buffer != null)
            {
                Destroy(buffer);
                Debug.Log("AudioBuffer cleaned up in OnDestroy.");
            }
            void Cleanup()
            {
                GameObject buffer = GameObject.Find("AudioBuffer");
                if (buffer != null)
                {
                    buffer.transform.SetParent(null); // Detach from any parent
                    Destroy(buffer);
                    Debug.Log("AudioBuffer removed.");
                }
            }*/
        }
    }
}