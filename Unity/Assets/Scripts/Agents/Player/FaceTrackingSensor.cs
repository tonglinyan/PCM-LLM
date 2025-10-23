using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SensorDataStructure;
using AUMapping;
using Newtonsoft.Json;
using Core;

namespace Sensor
{
    [RequireComponent(typeof(OVRFaceExpressions))]
    [RequireComponent(typeof(AUToEmotion))]
    public class FaceTrackingSensor : MonoBehaviour
    {
        private OVRFaceExpressions source;
        private AUToEmotion auToEmotion;
        public Interfacing.EmotionSystem emotions = new Interfacing.EmotionSystem()
        {
            felt = new Interfacing.Emotion(),
            physiological = new Interfacing.Emotion(),
            facial = new Interfacing.Emotion(),
            voluntaryFacial = new Interfacing.Emotion(),
            voluntaryPhysiological = new Interfacing.Emotion()
        };
        private FaceData faceData;
        //private FaceData faceDataTest;
        public FaceData FaceData { get { return faceData; } }
        public bool IsAvailable => source.ValidExpressions;
        public double valence = 0;

        float m_timeSinceUpdate = 0;
        [SerializeField] static float m_timeBetweenUpdates = 0.5f;
        private bool TimeToUpdate
        {
            get { return m_timeSinceUpdate >= m_timeBetweenUpdates; }
        }

        private void Start()
        {
            #region test with streaming data
            //var jsonPath = Path.Combine(Application.streamingAssetsPath, $"DataStream/Data_before_1128/Logs_202411261732633588508/simulation0/log_3.json");
            //var data = JsonConvert.DeserializeObject<StreamingData>(File.ReadAllText(jsonPath));
            //faceDataTest = data.player.PlayerFaceData;
            //Debug.Log(JsonConvert.SerializeObject(data));
            #endregion
            auToEmotion = GetComponent<AUToEmotion>();
            source = GetComponent<OVRFaceExpressions>();
            faceData = GetFaceData();
            valence = auToEmotion.GetValenceValue(faceData, emotions);
            Debug.Log("facial expression data: " + JsonConvert.SerializeObject(faceData));
        }

        private void Update()
        {
            m_timeSinceUpdate += Time.deltaTime;
            if (TimeToUpdate)
            {
                faceData = GetFaceData();
                valence = auToEmotion.GetValenceValue(faceData, emotions);
                m_timeSinceUpdate = 0f;
                //Debug.Log("facial expression data: " + JsonConvert.SerializeObject(faceData));
            }
        }

        public ActivationData[] GetActionUnits()
        {
            if (!IsAvailable) return null;
            float[] weights = source.ToArray();

            List<ActivationData> actionUnits = new();
            var actionUnitIds = Enumerable.Range(0, (int)OVRFaceExpressions.FaceExpression.Max - 1);

            foreach (var unitId in actionUnitIds)
            {
                var unitLabel = (OVRFaceExpressions.FaceExpression)unitId;
                actionUnits.Add(new ActivationData(unitLabel.ToString(), weights[unitId]));
            }
            return actionUnits.ToArray();
        }

        public FaceConfidenceZoneData[] GetFaceConfidence()
        {
            if (!IsAvailable) return null;

            List<FaceConfidenceZoneData> zones = new();
            var actionUnitIds = Enumerable.Range(0, (int)OVRFaceExpressions.FaceRegionConfidence.Max - 1);
            var weightConfidenceLabels = actionUnitIds.Select(i => (OVRFaceExpressions.FaceRegionConfidence)i);
            foreach (var w in weightConfidenceLabels)
            {
                bool success = source.TryGetWeightConfidence(w, out var value);
                var zone = new FaceConfidenceZoneData(w.ToString(), value, success);
                zones.Add(zone);
            }
            return zones.ToArray();
        }

        public FaceData GetFaceData()
        {
            return new FaceData(GetActionUnits(), GetFaceConfidence(), IsAvailable);
        }

        public Interfacing.Emotion facialEmotion() => emotions.facial;
    }
}
