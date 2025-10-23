using UnityEngine;
using SensorDataStructure;
using Newtonsoft.Json;
using Simulation;
using static Core.Interfacing;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Sensor
{
    public class EyeTrackingSensor : MonoBehaviour
    {
        [SerializeField] private OVREyeGaze leftGaze;
        [SerializeField] private OVREyeGaze rightGaze;
        [SerializeField] private float maxDistance = 40f;
        private GazeData gazeData;

        public Core.Interfacing.PlayerEmotion playerEmotion = new Core.Interfacing.PlayerEmotion();
        public GazeData GazeData { get { return gazeData; } }


        float m_timeSinceUpdate = 0;
        [SerializeField] float m_timeBetweenUpdates = .5f;
        private bool TimeToUpdate
        {
            get { return m_timeSinceUpdate >= m_timeBetweenUpdates; }
        }


        private void Start()
        {
            if (leftGaze == null || rightGaze == null)
            {
                Debug.LogError("EyeTrackingSensor: Missing reference to leftGaze or rightGaze.");
                enabled = false;
            }
            else
            {
                gazeData = GetGazeData();
                //Debug.Log("eye tracking data: " + JsonConvert.SerializeObject(gazeData));
            }
        }
        private void Update()
        {
            m_timeSinceUpdate += Time.deltaTime;
            if (TimeToUpdate)
            {
                gazeData = GetGazeData();
                //Debug.Log("eye tracking data: " + JsonConvert.SerializeObject(gazeData));
                m_timeSinceUpdate = 0f;
            }
        }

        private TargetData GetEyeTarget(Transform eye)
        {
            Ray ray = new(eye.position, eye.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, 9))
            {
                return new TargetData(hit, true);
            }
            return new TargetData(new RaycastHit(), false);
        }

        public GazeData GetGazeData()
        {
            return new GazeData(new EyeData(leftGaze), new EyeData(rightGaze), GetEyeTarget(leftGaze.transform), GetEyeTarget(rightGaze.transform));
        }

        public int SetPlayerEmotion(double valence)
        {
            int targetId = -1;
            playerEmotion.valenceFactor = new Dictionary<int, double>();
            TargetData[] hits = new TargetData[] { gazeData.LeftGazeTarget, gazeData.RightGazeTarget };
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].IsAvalaible)
                {
                    for (int j = 0; j < Manager.Singleton.EntityNames.Length; j++)
                    {
                        if (hits[i].Hit.ObjectName.Contains(Manager.Singleton.EntityNames[j]) && !playerEmotion.valenceFactor.ContainsKey(j))
                        {
                            Debug.Log($"Hit: {j} " + hits[i].Hit.ObjectName);
                            playerEmotion.valenceFactor.Add(j, 1);
                            targetId = j;
                            break;
                        }
                    }
                }
            }
            playerEmotion.valence = valence;
            return targetId;
        }
    }
}