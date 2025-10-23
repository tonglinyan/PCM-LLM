using UnityEngine;
using Meta.WitAi.Configuration;
using System.Collections.Generic;
using Oculus.Voice;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.Data.Configuration;
using Schemas;

public class LanguageConfig : MonoBehaviour
{
    [SerializeField] private WitRuntimeConfiguration witRuntimeConfigurationEn;
    [SerializeField] private WitRuntimeConfiguration witRuntimeConfigurationFr;
    [SerializeField] private AppVoiceExperience appVoiceExperience;
    [SerializeField] private TTSWit TTSWit;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLanguageConfig(Language language)
    {
        if (language == Language.FR)
        {
            appVoiceExperience.RuntimeConfiguration = witRuntimeConfigurationFr;
            TTSWit.Configuration = witRuntimeConfigurationFr.witConfiguration;
        }
        else
        {
            appVoiceExperience.RuntimeConfiguration = witRuntimeConfigurationEn;
            TTSWit.Configuration = witRuntimeConfigurationEn.witConfiguration;
        }
        
    }
}
