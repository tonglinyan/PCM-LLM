using UnityEngine.AI;
using SensorDataStructure;
using Newtonsoft.Json;
using UnityEngine;

namespace Sensor
{
    public class BodyTrackingSensor : MonoBehaviour
    {
        [SerializeField] private Transform Head;
        [SerializeField] private Transform LeftController;
        [SerializeField] private Transform RightController;

        [SerializeField] private OVRInput.Controller leftXRController;
        [SerializeField] private OVRInput.Controller rightXRController;


        private const float boundaryOffset = 0.1f;
        private NavMeshAgent m_agent;

        private BodyData bodyData;
        public BodyData BodyData { get { return bodyData; } }

        float m_timeSinceUpdate = 0f;
        [SerializeField] static float m_timeBetweenUpdates = 0.5f;
        private bool TimeToUpdate
        {
            get { return m_timeSinceUpdate >= m_timeBetweenUpdates; }
        }


        private void Start()
        {
            //body = GetComponent<Rigidbody>();
            bodyData = GetBodyData();
            //OriginTransform.position = new Vector3((float)Head.position[0], (float)Head.position[1], (float)Head.position[2]);
            Debug.Log("body data: " + JsonConvert.SerializeObject(bodyData));
        }

        public void SetAgent(NavMeshAgent agent)
        {
            m_agent = agent;
        }

        private void Update()
        {
            m_timeSinceUpdate += Time.deltaTime;
            if (TimeToUpdate)
            {
                bodyData = GetBodyData();
                //Debug.Log("body data: " + JsonConvert.SerializeObject(bodyData));

                if (bodyData.Head.IsAvalaible)
                {
                    Vector3 targetPosition = new Vector3((float)Head.position[0], transform.position.y, (float)Head.position[2]);
                    //Vector3 targetPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                    NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, boundaryOffset, NavMesh.AllAreas);
                    if (hit.hit)
                    {
                        m_agent.SetDestination(new Vector3((float)Head.position[0], transform.position.y, (float)Head.position[2]));
                    }
                    else
                    {
                        TriggerHapticFeedback(1.0f, 0.8f, 0.3f);
                        m_agent.SetDestination(hit.position);
                        Debug.Log("out of walkable region");
                    }
                }
                m_timeSinceUpdate = 0f;
            }
        }

        public BodyPartData GetHeadData()
        {
            var active = OVRPlugin.hasVrFocus && OVRPlugin.hasInputFocus;
            //Debug.Log("head active: " + active.ToString());
            return new BodyPartData(Head, active);
        }

        public BodyPartData GetLeftControllerData()
        {
            var controllerType = OVRInput.GetActiveController();
            var active = controllerType.HasFlag(OVRInput.Controller.LTouch) || controllerType.HasFlag(OVRInput.Controller.Touch);
            //Debug.Log("left controller active: " + active.ToString());
            return new BodyPartData(LeftController, active);
        }

        public BodyPartData GetRightControllerData()
        {
            var controllerType = OVRInput.GetActiveController();
            var active = controllerType.HasFlag(OVRInput.Controller.RTouch) || controllerType.HasFlag(OVRInput.Controller.Touch);
            //Debug.Log("right controller active: " + active.ToString());
            return new BodyPartData(RightController, active);
        }

        public BodyData GetBodyData()
        {
            return new BodyData(GetHeadData(), GetLeftControllerData(), GetRightControllerData());
        }

        #region PCM data        
        public Core.Interfacing.Body GetPlayerBodyData(NavMeshAgent m_body, EyeData leftGaze, EyeData rightGaze)
        {
            Vector3 center = Head.transform.position;
            center.y /= 2;
            Vector3 size = new Vector3(m_body.radius * 2f, m_body.height, m_body.radius * 2f);
            Debug.DrawLine(center, center+GetLookAt(leftGaze, rightGaze), Color.blue, 20);
            return ExtensionTools.PCMBodyFromUnityData(center, size, GetLookAt(leftGaze, rightGaze), GetLookAtOrigin(leftGaze, rightGaze));
        }

        public Vector3 GetUnscaledPlayerCenter()
        {
            Vector3 center = Head.transform.position;
            center.y /= 2;
            return Head.transform.position;
        }

        public Vector3 GetLookAt(EyeData leftGaze, EyeData rightGaze)
        {

            return new Vector3((float)Head.forward[0], (float)Head.forward[1], (float)Head.forward[2]);

        }

        public Vector3 GetLookAtOrigin(EyeData leftGaze, EyeData rightGaze)
        {

            return new Vector3((float)bodyData.Head.Transform.Position[0], (float)bodyData.Head.Transform.Position[1], (float)bodyData.Head.Transform.Position[2]);

        }

        /*private Vector3 GetAverageVector3(double[] left, double[] right)
        {
            if (left.Length == 3 && right.Length == 3)
            {
                return new Vector3(
                    (float)((left[0] + right[0]) / 2),
                    (float)((left[1] + right[1]) / 2),
                    (float)((left[2] + right[2]) / 2)
                );
            }
            Debug.LogError("Array length mismatch for LookAt calculation");
            return Vector3.zero;
        }*/
        #endregion

        void TriggerHapticFeedback(float frequency, float amplitude, float duration)
        {
            OVRInput.SetControllerVibration(frequency, amplitude, leftXRController);
            OVRInput.SetControllerVibration(frequency, amplitude, rightXRController);
            Invoke(nameof(StopHapticFeedback), duration);
        }

        private void StopHapticFeedback()
        {
            OVRInput.SetControllerVibration(0, 0, leftXRController);
            OVRInput.SetControllerVibration(0, 0, rightXRController);
        }

    }
}