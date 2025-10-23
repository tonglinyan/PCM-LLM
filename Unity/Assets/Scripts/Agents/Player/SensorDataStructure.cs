using System.Globalization;
using UnityEngine;

namespace SensorDataStructure
{

    public struct TransformData
    {
        public TransformData(Transform transform)
        {
            Position = new double[] { transform.position.x, transform.position.y, transform.position.z };
            LookAt = new double[] { transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z };
            Scale = new double[] { transform.localScale.x, transform.localScale.y, transform.localScale.z };
            Forward = new double[] { transform.forward.x, transform.forward.y, transform.forward.z };
        }
        public double[] Position { get; set; }
        public double[] LookAt { get; set; }
        public double[] Scale { get; set; }
        public double[] Forward { get; set; }
    }


    #region face data
    public struct ActionUnit
    {
        public int Number { get; set; }
        public string Label { get; set; }
        public ActionUnit(int number, string label)
        {
            Number = number;
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string[] words = label.Split(" ");

            for (int w = 0; w < words.Length; w++)
            {
                words[w] = textInfo.ToTitleCase(words[w]);
            }
            Label = string.Join("", words);
        }
    }

    public struct ActivationData
    {
        public ActivationData(string label, float weight)
        {
            Label = label;
            Weight = weight;
        }
        public string Label { get; set; }
        public float Weight { get; set; }
    }

    public struct FaceConfidenceZoneData
    {
        public FaceConfidenceZoneData(string label, float confidence, bool isvalid)
        {
            Label = label;
            Confidence = confidence;
            IsValid = isvalid;
        }

        public string Label { get; set; }
        public float Confidence { get; set; }
        public bool IsValid { get; set; }
    }

    public struct FaceData
    {
        public FaceData(ActivationData[] actiData, FaceConfidenceZoneData[] confidenceData, bool isAvailable)
        {
            ActionUnitData = actiData;
            FaceConfidenceData = confidenceData;
            IsAvailable = isAvailable;
        }

        public ActivationData[] ActionUnitData { get; set; }
        public FaceConfidenceZoneData[] FaceConfidenceData { get; set; }
        public bool IsAvailable { get; set; }

    }
    #endregion 


    #region body data
    public struct BodyPartData
    {
        public BodyPartData(Transform transform, bool isAvalaible)
        {
            Transform = new TransformData(transform);
            IsAvalaible = isAvalaible;
        }
        public TransformData Transform { get; set; }
        public bool IsAvalaible { get; set; }
    }

    public struct BodyData
    {
        public BodyData(BodyPartData headPartData, BodyPartData leftControllerData, BodyPartData rightControllerData)
        {
            Head = headPartData;
            LeftController = leftControllerData;
            RightController = rightControllerData;
        }
        public BodyPartData Head { get; set; }
        public BodyPartData LeftController { get; set; }
        public BodyPartData RightController { get; set; }
    }
    #endregion


    #region eye data
    public struct EyeData
    {
        public EyeData(OVREyeGaze gaze)
        {
            IsAvailable = gaze.EyeTrackingEnabled;
            Transform = new TransformData(gaze.transform);
            Confidence = IsAvailable ? gaze.Confidence : 0;
        }
        public TransformData Transform { get; set; }
        public float Confidence { get; set; }
        public bool IsAvailable { get; set; }
    }

    public struct RaycastHitData
    {
        public RaycastHitData(RaycastHit hit)
        {
            Point = new double[] { hit.point.x, hit.point.y, hit.point.z };
            Normal = new double[] { hit.normal.x, hit.normal.y, hit.normal.z };
            Distance = hit.distance;
            if (hit.transform != null) { ObjectName = hit.transform.name; }
            else { ObjectName = ""; }
        }
        public double[] Point { get; set; }
        public double[] Normal { get; set; }

        public double Distance { get; set; }

        public string ObjectName { get; set; }
    }

    public struct TargetData
    {
        public TargetData(RaycastHit hit, bool isAvalaible)
        {
            Hit = new RaycastHitData(hit);
            IsAvalaible = isAvalaible;
        }
        public RaycastHitData Hit { get; set; }
        public bool IsAvalaible { get; set; }
    }

    public struct GazeData
    {
        public GazeData(EyeData leftEye, EyeData rightEye, TargetData leftGazeTarget, TargetData rightGazeTarget)
        {
            LeftEye = leftEye;
            RightEye = rightEye;
            LeftGazeTarget = leftGazeTarget;
            RightGazeTarget = rightGazeTarget;
        }
        public EyeData LeftEye { get; set; }
        public EyeData RightEye { get; set; }
        public TargetData LeftGazeTarget { get; set; }
        public TargetData RightGazeTarget { get; set; }
    }
    #endregion

    #region player
    public struct PlayerSensorData
    {
        public PlayerSensorData(BodyData bodyData, FaceData faceData, GazeData gazeData)
        {
            PlayerBodyData = bodyData;
            PlayerFaceData = faceData;
            PlayerGazeData = gazeData;

        }

        public BodyData PlayerBodyData { get; set; }
        public FaceData PlayerFaceData { get; set; }
        public GazeData PlayerGazeData { get; set; }
    }
    #endregion
}