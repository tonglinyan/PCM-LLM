//Base version generated using https://app.quicktype.io/#l=cs&r=json2csharp
//Modified by Olivier Belli

using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace Schemas
{
    public partial class Parameters
    {
        [JsonProperty("nonVerbal")]
        public NonVerbal NonVerbal { get; set; }

        [JsonProperty("verbal")]
        public Verbal Verbal { get; set; }
    }
    public partial class NonVerbal
    {
        [JsonProperty("core")]
        public Core Core { get; set; }

        [JsonProperty("depth")]
        public int Depth { get; set; }

        [JsonProperty("nbThreadMax")]
        public int NbThreadMax { get; set; }

        [JsonProperty("nbIterations")]
        public int NbIterations { get; set; }

        [JsonProperty("playerID")]
        public int PlayerID { get; set; }

        [JsonProperty("saveBestPath")]
        public bool SaveBestPath { get; set; }

        [JsonProperty("filePath")]
        public string FilePath { get; set; }
    }


    public partial class Core
    {
        [JsonProperty("objects")]
        public Objects Objects { get; set; }

        [JsonProperty("agents")]
        public Agents Agents { get; set; }

        [JsonProperty("settings")]
        public Settings Settings { get; set; }
    }

    public partial class Agents
    {
        [JsonProperty("actionTags")]
        public string[][][] ActionTags { get; set; }

        [JsonProperty("positioning")]
        public Positioning[] Positioning { get; set; }

        [JsonProperty("preferences")]
        public double[][][] Preferences { get; set; }

        [JsonProperty("tomInfluence")]
        public double[][][] TomInfluence { get; set; }

        [JsonProperty("tomUpdate")]
        public double[][][] TomUpdate { get; set; }

        [JsonProperty("mutualLoveStep")]
        public double[][][] MutualLoveStep { get; set; }

        [JsonProperty("emogain")]
        public double[] Emogain { get; set; }

        [JsonProperty("tomPredict")]
        public int[][] TomPredict { get; set; }

        [JsonProperty("speed")]
        public Speed[] Speed { get; set; }

        [JsonProperty("anxious")]
        public bool[] Anxious { get; set; }

        [JsonProperty("physiologicalReactivity")]
        public double[] PhysiologicalReactivity { get; set; }

        [JsonProperty("facialReactivity")]
        public double[] FacialReactivity { get; set; }

        [JsonProperty("physiologicalSensitivity")]
        public double[] PhysiologicalSensitivity { get; set; }

        [JsonProperty("voluntaryPhysiologicalWeight")]
        public double[] VoluntaryPhysiologicalWeight { get; set; }

        [JsonProperty("voluntaryFacialWeight")]
        public double[] VoluntaryFacialWeight { get; set; }
    }

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
    public partial class Speed
    {
        [JsonProperty("realSpeed")]
        public double RealSpeed { get; set; }

        [JsonProperty("projectionSpeed")]
        public double ProjectionSpeed { get; set; }

        [JsonProperty("projectionSpeedIncrement")]
        public double ProjectionSpeedIncrement { get; set; }

    }
    public partial class Objects
    {
        [JsonProperty("positioning")]
        public Positioning[] Positioning { get; set; }
    }

    public partial class Settings
    {
        [JsonProperty("simulation")]
        public Simulation Simulation { get; set; }
    }

    public partial class Simulation
    {
        //obsolete
        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("nSigma")]
        public int NSigma { get; set; }

        [JsonProperty("nPsyS")]
        public int NPsyS { get; set; }

        [JsonProperty("sevuncertaingain")]
        public double Sevuncertaingain { get; set; }

        [JsonProperty("sensoryEvidenceUpdateWeight")]
        public double SensoryEvidenceUpdateWeight { get; set; }

        [JsonProperty("neutralref")]
        public double Neutralref { get; set; }

        [JsonProperty("spatialStat")]
        public SpatialStat SpatialStat { get; set; }

        [JsonProperty("algSelection")]
        public string AlgSelection { get; set; }

        [JsonProperty("certainty")]
        public Certainty Certainty { get; set; }

        [JsonProperty("randomization")]
        public Randomization[] Randomization { get; set; }

        [JsonProperty("minDist")]
        public double MinDist { get; set; }

        [JsonProperty("goalPredict")]
        public bool GoalPredict { get; set; }

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

    public class Randomization
    {
        [JsonProperty("agent")]

        public int Agent { get; set; }

        [JsonProperty("magnitude")]

        public double Magnitude { get; set; }

        [JsonProperty("indices")]
        public List<(int, int)> Indices { get; set; }

        [JsonProperty("property")]
        public string Property { get; set; }

        [JsonProperty("condition")]
        public RandomizationCondition RandomizationCondition { get; set; }

    }

    public class RandomizationCondition
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("delta")]
        public int Delta { get; set; }
    }

    public partial class Verbal
    {
        [JsonProperty("preferenceUpdateWeight")]
        public double PreferenceUpdateWeight { get; set; }

        [JsonProperty("PCM_CoreUpdatesMemoryLength")]
        public int PCM_CoreUpdatesMemoryLength { get; set; }

        [JsonProperty("depth")]
        public int Depth { get; set; }

        [JsonProperty("contextJSON")]
        public string ContextJSON { get; set; }

        [JsonProperty("entityNames")]
        public string[] EntityNames { get; set; }

        [JsonProperty("filePath")]
        public string filePath { get; set; }

        /*[JsonProperty("PCMtoLLM")]
        public bool PCMtoLLM { get; set; }

        [JsonProperty("LLMtoPCM")]
        public bool LLMtoPCM { get; set; }*/

        [JsonProperty("Hypothesis")]
        public int Hypothesis { get; set; }
    }

    public enum ArtificialAgentMode
    {
        NonVerbal,
        Verbal,
        Verbal_NonVerbal
    }

    public enum VirtualAgentRole
    {
        Partner,
        Adversary
    }

    public enum Language
    {
        FR,
        EN
    }
}