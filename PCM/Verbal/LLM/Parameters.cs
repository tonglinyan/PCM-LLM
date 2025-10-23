//Base version generated using https://app.quicktype.io/#l=cs&r=json2csharp
//Modified by Olivier Belli

using System.Globalization;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using static PCM.Core.Interfacing;
namespace PCM.Verbal.LMM.Parameters
{

    public partial class LLMParameters
    {
        [JsonProperty("objects")]
        public Objects Objects { get; set; }

        [JsonProperty("agents")]
        public Agents Agents { get; set; }

    }

    public partial class Agents
    {

        public EmotionSystem[][] Emotions;

        [JsonProperty("actionTags")]
        public Dictionary<string, bool>[] ActionTags { get; set; }

        [JsonProperty("positioning")]
        public Positioning[] Positioning { get; set; }

        //[JsonProperty("tomInfluence")]
        //public double[][][] TomInfluence { get; set; }

        //[JsonProperty("tomUpdate")]
        //public double[][][] TomUpdate { get; set; }

        //[JsonProperty("emogain")]
        //public double[] Emogain { get; set; }

        [JsonProperty("epistemicValue")] // uncertainty
        public double[][][] EpistemicValue { get; set; }

        [JsonProperty("affectiveValue")] // mu
        public double[][][] AffectiveValue { get; set; }

        [JsonProperty("freeEnergy")] // fe
        public double[][][] FreeEnergy { get; set; }

        [JsonProperty("tomPredict")]
        public int[][] TomPredict { get; set; }

        [JsonProperty("preferences")]
        public double[][][] Preferences { get; set; }
    }

    /*public partial class EmotionSystem
    {
        [JsonProperty("felt")]
        public Emotion Felt { get; set; }

        [JsonProperty("voluntaryPhysiological")]
        public Emotion VoluntaryPhysiological { get; set; }

        [JsonProperty("physiological")]
        public Emotion Physiological { get; set; }

        [JsonProperty("voluntaryFacial")]
        public Emotion VoluntaryFacial { get; set; }
        
        [JsonProperty("facial")]
        public Emotion Facial { get; set; }
    }
    public partial class Emotion
    {
        [JsonProperty("pos")]
        public double Pos { get; set; }

        [JsonProperty("neg")]
        public double Neg { get; set; }

        [JsonProperty("val")]
        public double Val { get; set; }

        //[JsonProperty("arousal")]
        //public double Arousal { get; set; }

        [JsonProperty("surprise")]
        public double Surprise { get; set; }

        //[JsonProperty("uncertainty")]
        //public double Uncertainty { get; set; }
    }*/

    public partial class Positioning
    {
        [JsonProperty("position")]
        public double[] Position { get; set; }

        [JsonProperty("lookAt")]
        public double[] LookAt { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }
    }

    /*
    public partial class Speed
    {
        [JsonProperty("realSpeed")]
        public double RealSpeed { get; set; }

        [JsonProperty("projectionSpeed")]
        public double ProjectionSpeed { get; set; }

        [JsonProperty("projectionSpeedIncrement")]
        public double ProjectionSpeedIncrement { get; set; }
    }
    */
    
    public partial class Objects
    {
        [JsonProperty("positioning")]
        public Positioning[] Positioning { get; set; }
    }


    /*
    public partial class Loop
    {
        [JsonProperty("iter")]
        public int Iter { get; set; }

        [JsonProperty("depth")]
        public int Depth { get; set; }
    }

    public partial class SpatialStat
    {
        [JsonProperty("c")]
        public double C { get; set; }

        [JsonProperty("volume")]
        public double Volume { get; set; }

        [JsonProperty("gaussianDisp")]
        public double GaussianDisp { get; set; }

        [JsonProperty("amplificationFactor")]
        public double AmplificationFactor { get; set; }

    }

    public partial class Certainty
    {
        [JsonProperty("sigmaDistanceFactor")]
        public double SigmaDistanceFactor { get; set; }

        [JsonProperty("sigmaSharpnessFactor")]
        public double SigmaSharpnessFactor { get; set; }

        [JsonProperty("updateUncertFactor")]
        public double UpdateUncertFactor { get; set; }
    }

    public partial class SimulationParametersParser
    {
        public static SimulationParameters FromJson(string json) => JsonConvert.DeserializeObject<SimulationParameters>(json, Converter.Settings);
        public static string ReadJsonFromFile(string filename)
        {
            using (StreamReader file = new StreamReader(filename))
                return file.ReadToEnd();
        }
        public static SimulationParameters FromFile(string filename) => FromJson(ReadJsonFromFile(filename));
    }*/

    public static class Serialize
    {
        public static string ToJson(this LLMParameters self, bool prettyPrint) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
