using Core;
using UnityEngine;
using SensorDataStructure;
using UnityEngine.AI;
using Sensor;

[RequireComponent(typeof(FaceTrackingSensor))]
[RequireComponent(typeof(EyeTrackingSensor))]
[RequireComponent(typeof(BodyTrackingSensor))]
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerManager : MonoBehaviour
{
    [Header("Tracking Sensor")]
    private FaceTrackingSensor m_faceTrackingSensor;
    private EyeTrackingSensor m_eyeTrackingSensor;
    private BodyTrackingSensor m_bodyTrackingSensor;

    [Header("Body")]
    private NavMeshAgent m_agent;
    //[SerializeField] private CharacterController m_characterController;

    [Header("Emotions")]
    private double valence = 0;

    [Header("EyeTracking")]
    private int targetId = -1;

    [Header("Emotions raycast")]
    private bool requestSendEmotion = false;
    private bool isActivated = false;

    private void Awake()
    {
        m_faceTrackingSensor = GetComponent<FaceTrackingSensor>();
        m_eyeTrackingSensor = GetComponent<EyeTrackingSensor>();
        m_bodyTrackingSensor = GetComponent<BodyTrackingSensor>();
        m_agent = GetComponent<NavMeshAgent>();
        m_bodyTrackingSensor.SetAgent(m_agent);
    }
    private void Update()
    {
        valence = m_faceTrackingSensor.valence;
        targetId = m_eyeTrackingSensor.SetPlayerEmotion(valence);
        requestSendEmotion = (targetId != -1);
        //m_bodyTrackingSensor.UpdateCenter(m_agent);
        Debug.DrawLine(ExtensionTools.FromPCMVector3Unscale(GetPlayerBodyData().Center, false), GetPlayerLookAtOrigin(), Color.yellow, 15);
    }

    #region PCM data
    public Core.Interfacing.PlayerEmotion RequestPlayerEmotion()
    {
        requestSendEmotion = false;
        return m_eyeTrackingSensor.playerEmotion;
        //return new Core.Interfacing.PlayerEmotion();
    }
    public bool NeedRequestPlayerEmotion()
    {
        return requestSendEmotion;
    }
    public Core.Interfacing.Body GetPlayerBodyData() => m_bodyTrackingSensor.GetPlayerBodyData(m_agent, m_eyeTrackingSensor.GazeData.LeftEye, m_eyeTrackingSensor.GazeData.RightEye); //new Core.Interfacing.Body() { Height = 180, Width = 70, Depth = 70, Center = new (0, 165, 256), OrientationOrigin = new (0, 170, 258), Orientation = new (0,0,-1)};

    public Vector3 GetPlayerLookAtOrigin() => m_bodyTrackingSensor.GetLookAtOrigin(m_eyeTrackingSensor.GazeData.LeftEye, m_eyeTrackingSensor.GazeData.RightEye);
    public Vector3 GetUnscaledPlayerCenter() => m_bodyTrackingSensor.GetUnscaledPlayerCenter();
    public Vector3 GetUnscaledPlayerSize()
    {
        return new Vector3(m_agent.radius * 2f, m_agent.height, m_agent.radius * 2f);
    }
    public Interfacing.EmotionSystem GetEmotions() => m_faceTrackingSensor.emotions;//new Interfacing.EmotionSystem(){felt = new Interfacing.Emotion(), physiological = new Interfacing.Emotion(), facial = new Interfacing.Emotion(),voluntaryFacial = new Interfacing.Emotion(), voluntaryPhysiological = new Interfacing.Emotion()}

    public float Height() => m_agent.height;
    #endregion

    #region Streaming
    public BodyData GetBodyData() => m_bodyTrackingSensor.BodyData;
    public FaceData GetFaceData() => m_faceTrackingSensor.FaceData;
    public GazeData GetGazeData() => m_eyeTrackingSensor.GazeData;
    public Interfacing.Emotion GetEmotion() => m_faceTrackingSensor.facialEmotion();
    public int TargetId() => targetId;

    #endregion

    public bool Activate
    {
        get { return isActivated; }
        set { isActivated = value; }
    }
}