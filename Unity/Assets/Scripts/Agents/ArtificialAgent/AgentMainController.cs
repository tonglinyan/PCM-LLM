using System.Collections.Generic;
using UnityEngine;
using Core;
using System.Linq;
using System;
using LeastSquares.Overtone;
using System.Threading.Tasks;

[RequireComponent(typeof(AgentNavController))]
[RequireComponent(typeof(AgentEmotionController))]
[RequireComponent(typeof(AgentInteractionController))]
[RequireComponent(typeof(Animator))]
public class AgentMainController : MonoBehaviour
{
    public enum AgentControllerState
    {
        Idle,
        LookingAtObject,
        FetchingObject,
        ReachingForObject,
        GrabingObject, // holding, about to hold or releasing
    }

    // Constants
    public const float DesireThreshold = 0.5f;
    public const int MainMaterialIndex = 0;

    // Static
    static readonly List<AgentMainController> s_agents = new List<AgentMainController>();

    [Header("Unity Components")]
    [SerializeField] private SkinnedMeshRenderer m_mesh;
    [SerializeField] private Camera m_camera;
    private AgentEmotionController m_emotionController;
    private AgentNavController m_navController;
    private AgentInteractionController m_interactionController;
    public Animator m_animator;


    [Header("Verbal")]
    [SerializeField] private TTSPlayer _player;
    private string base64string;

    [Header("State and Interest")]
    [SerializeField] private bool m_allowLossOfInterestMidcourse = false;
    private AgentControllerState m_state = AgentControllerState.Idle;
    private float m_levelOfInterest = 0;
    
    private ObjectOfInterest m_objectOfInterest;
    private ObjectOfInterest m_grabbedObject;


    [SerializeField] private GazeTarget m_secondaryLookAtTargetProxy;
    [SerializeField] private SkinnedMeshRenderer m_referenceMesh;
    [SerializeField] private Transform[] eyes;

    public TTSPlayer Speaker => _player;
    // PCM
    public Interfacing.EmotionSystem emotions = new Interfacing.EmotionSystem()
    {
        felt = new Interfacing.Emotion(),
        physiological = new Interfacing.Emotion(),
        facial = new Interfacing.Emotion(),
        voluntaryFacial = new Interfacing.Emotion(),
        voluntaryPhysiological = new Interfacing.Emotion()
    };

    private PCMEmotionFacialParameters m_pcmEmotionFacialParameters;

    public string Base64String
    {
        get => base64string;
        set => base64string = value;
    }

    public Transform[] Eyes
    {
        get => Eyes;
        set { eyes = value; }
    }

    public Vector3 GetLookAtOrigin()
    {
        return eyes.Aggregate(Vector3.zero, (acc, eye) => acc + eye.position) / eyes.Length;
    }

    public Vector3 GetLookAt()
    {
        return eyes.Aggregate(Vector3.zero, (acc, eye) => acc + eye.forward) / eyes.Length;
    }

    public Vector3 GetBodyOrientation()
    {
        return transform.rotation * Vector3.forward;
    }

    [ContextMenu("Repair Skinned Mesh Renderer")] // Use when the rig has changed to remap the bones to the mesh
    private void RepairSkinnedMeshRenderer()
    {
        ExtensionTools.RepairSkinnedMeshRenderer(m_mesh, m_referenceMesh, transform);
    }

    #region Awake
    private void Awake()
    {
        AwakeStatic();
        AwakeComponents();
        AwakePCM();
    }

    private void AwakeComponents()
    {
        m_emotionController = GetComponent<AgentEmotionController>();
        m_navController = GetComponent<AgentNavController>();
        m_interactionController = GetComponent<AgentInteractionController>();
        m_animator = GetComponent<Animator>();
    }

    private void AwakeStatic()
    {
        if (!s_agents.Contains(this))
        {
            s_agents.Add(this);
        }
      //  InitColor();
    }

    private void OnDestroy()
    {
        if (s_agents.Contains(this))
        {
            s_agents.Remove(this);
        }
    }

    private void AwakePCM()
    {
        m_pcmEmotionFacialParameters = new PCMEmotionFacialParameters(gameObject);
    }
    #endregion

    #region Update

    private void Update()
    {
        CamCapture();
        SetEmotionParameters(emotions);
    }

    public void CamCapture()
    {
        RenderTexture currentRT = new RenderTexture(720, 480, 24);
        m_camera.targetTexture = currentRT;
        Texture2D Image = new Texture2D(m_camera.targetTexture.width, m_camera.targetTexture.height, TextureFormat.RGB24, false);


        m_camera.Render();
        RenderTexture.active = m_camera.targetTexture;
        Image.ReadPixels(new Rect(0, 0, m_camera.targetTexture.width, m_camera.targetTexture.height), 0, 0);
        Image.Apply();

        byte[] Bytes = Image.EncodeToPNG();

        m_camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(Image);
        Destroy(currentRT);

        base64string = Convert.ToBase64String(Bytes);
        //Debug.Log("Length of base64string: " + base64string.Length);
    }

    public void RebootAgent(Vector3 pos)
    {
        m_navController.RebootController(pos);
        StopInteracting();
        SetIdle();
    }
    #endregion

    #region Speaking

    public async Task SpeakOvertone(string text_input)
    {
        await _player.Speak(text_input ?? string.Empty);
    }
    #endregion

    #region Controller state
    #region Get controller state and interactions state
    private AgentControllerState GetNextInteractionState()
    {
        if (!IsInterestedInAnObject)
        {
            return AgentControllerState.Idle;
        }

        if (DoesNotHaveStrongInterest)
        {
            return AgentControllerState.LookingAtObject;
        }

        if (!InRadiusOfDestination)
        {
            return AgentControllerState.FetchingObject;
        }

        if (!CanHoldObjectOfInterest)
        {
            return AgentControllerState.ReachingForObject;
        }

        return AgentControllerState.GrabingObject;
    }

    private bool CurrentInteractionChanged()
    {
        // assumes the state did not change but the interaction did
        if (CurrentState == AgentControllerState.Idle)
        {
            return false;
        }

        if (CurrentState == AgentControllerState.LookingAtObject)
        {
            return m_interactionController.LookAtTarget != m_objectOfInterest;
        }

        if (CurrentState == AgentControllerState.FetchingObject)
        {
            return m_navController.TargetOfInterest != m_objectOfInterest;
        }

        if (CurrentState == AgentControllerState.ReachingForObject)
        {
            return !InteractingWithObjectOfInterest;
        }

        if (CurrentState == AgentControllerState.GrabingObject)
        {
            return !InteractingWithObjectOfInterest;
        }

        return false;
    }

    #endregion

    #region Set Controller State
    public void SetControllerState(AgentControllerState newState)
    {
        switch (newState)
        {
            case AgentControllerState.Idle:
                SetIdle();
                break;
            case AgentControllerState.LookingAtObject:
                SetLookingAtMainObjectOfInterest();
                break;
            case AgentControllerState.FetchingObject:
                SetFetchingObject();
                break;
            case AgentControllerState.ReachingForObject:
                SetReachingForObject();
                break;
            case AgentControllerState.GrabingObject:
                SetGrabingObject();
                break;
        }
    }

    public void SetIdle()
    {
        //m_interactionController.LookAt(null);
        m_camera.transform.LookAt(m_secondaryLookAtTargetProxy.transform);
        m_navController.ClearTarget();
        // StopInteracting();

        m_state = AgentControllerState.Idle;
    }

    private void SetLookingAtMainObjectOfInterest()
    {
        //m_interactionController.LookAt(m_objectOfInterest.transform);
        m_camera.transform.LookAt(m_objectOfInterest.transform);
        m_navController.ClearTarget();
        // StopInteracting();

        m_state = AgentControllerState.LookingAtObject;
    }

    public void SetLookingAtMainObjectOfInterest(ObjectOfInterest objectOfInterest)
    {
        m_objectOfInterest = objectOfInterest;
        SetLookingAtMainObjectOfInterest();
    }

    public void SetLookingAtSecondaryTarget(Vector3 position)
    {
        float dot = ExtensionTools.AngleBetweenVectorAndPoint(GetBodyOrientation(), GetLookAtOrigin(), position, true);
        Vector3 possibleGazeDirection = ExtensionTools.VectorBetweenTwoPoints(GetLookAtOrigin(), position, true);
        if (dot <= 0)
        {
            SetRotationTarget(possibleGazeDirection);
        }
        m_secondaryLookAtTargetProxy.SetTarget(position);
        //m_interactionController.LookAt(m_secondaryLookAtTargetProxy.transform);
        m_camera.transform.LookAt(m_secondaryLookAtTargetProxy.transform);
    }

    /*public void SetLookingAtSecondaryTarget(Transform transform)
    {
        m_interactionController.LookAt(transform);
        m_camera.transform.LookAt(transform);
    }*/

    /*public void SetLookingAtSecondaryTarget(ObjectOfInterest objectOfInterest)
    {
        SetLookingAtSecondaryTarget(objectOfInterest.transform);
    }*/

    public void SetRotationTarget(Vector3 forward)
    {
        m_navController.SetRotationTarget(forward);
    }

    public void SetMovingTo(Vector3 destination, Vector3 orientation)
    {
        m_navController.SetDestination(destination, orientation);
    }

    private void SetFetchingObject()
    {
        //m_interactionController.LookAt(m_objectOfInterest.transform);
        m_camera.transform.LookAt(m_objectOfInterest.transform);
        m_navController.GoToObjectOfInterest(m_objectOfInterest);
        // StopInteracting(true);

        m_state = AgentControllerState.FetchingObject;
    }

    public void SetFetchingObject(ObjectOfInterest objectOfInterest)
    {
        m_objectOfInterest = objectOfInterest;
        SetFetchingObject();
    }

    private void SetReachingForObject()
    {
        //m_interactionController.LookAt(m_objectOfInterest.transform);
        m_camera.transform.LookAt(m_objectOfInterest.transform);
        m_navController.ClearTarget();
        StopInteracting();
        m_interactionController.StartReaching(m_objectOfInterest);

        m_state = AgentControllerState.ReachingForObject;
    }

    private void SetGrabingObject()
    {
        Debug.Log("Set grabing : " + m_objectOfInterest.name);
        //m_interactionController.LookAt(m_objectOfInterest.transform);
        m_camera.transform.LookAt(m_objectOfInterest.transform);
        m_navController.ClearTarget();
        StopInteracting();
        m_interactionController.StartHolding(m_objectOfInterest);
        m_grabbedObject = m_objectOfInterest;

        m_state = AgentControllerState.GrabingObject;
    }

    public void SetInteractingWithObject(ObjectOfInterest objectOfInterest)
    {
        m_objectOfInterest = objectOfInterest;
        if (InRadiusOfDestination)
        {
            if(CanHoldObjectOfInterest)
            {
                Debug.Log(transform.name + " :Can Hold Object Of Interest");
                SetGrabingObject();
            }
            else
            {
                Debug.Log(transform.name + " :Can not Hold Object Of Interest");
                SetReachingForObject();
            }
        }
        else
        {
            Debug.Log(transform.name + " : Not in the radius ");
            SetFetchingObject();
        }
    }

    public void StopInteracting(bool interruptInteraction = false)
    {
        m_interactionController.StopAllInteractions(interruptInteraction);
        m_grabbedObject = null;
    }
    #endregion
    #endregion


    #region Object of Interest
    public void SetObjectOfInterest(Transform objectOfInterest, float levelOfInterest)
    {
        m_levelOfInterest = levelOfInterest;

        if (objectOfInterest != null)
        {
            m_objectOfInterest = objectOfInterest.GetComponent<ObjectOfInterest>();
        }
        else
        {
            m_objectOfInterest = null;
        }
    }

    public void SetObjectOfInterest(ObjectOfInterest objectOfInterest)
    {
        m_objectOfInterest = objectOfInterest;
    }

    public void SetObjectOfInterest(ObjectOfInterest objectOfInterest, float levelOfInterest)
    {
        SetObjectOfInterest(objectOfInterest);
        m_levelOfInterest = levelOfInterest;
    }

    public void ClearInterest()
    {
        m_objectOfInterest = null;
        m_objectOfInterest = null;
        m_levelOfInterest = 0f;
    }

    public Holder GetCurrentPreferredHolder()
    {
        return m_interactionController.GetPreferredHolder(m_objectOfInterest);
    }
    #endregion

    #region PCM Parameters
    public bool Moving 
    {
        get { return m_navController.Moving; }
    }

    public PCMEmotionFacialParameters GetPCMEmotionParameters()
    {
        return m_pcmEmotionFacialParameters;
    }

    public void SetPCMEmotionFacialParameters(float positiveLevel, float negativeLevel, float surpriseLevel)
    {
            emotions.facial.Positive = positiveLevel;
            emotions.facial.Negative = negativeLevel;
            emotions.facial.Surprise = surpriseLevel;
            emotions.facial.Valence = positiveLevel - negativeLevel;
            m_emotionController.SetEmotionFacialValenceAndSurprise((float)emotions.facial.Positive, (float)emotions.facial.Negative, (float)emotions.facial.Surprise) ;
    }
    public void SetPCMEmotionPhysiologicalParameters(float positiveLevel, float negativeLevel, float surpriseLevel)
    {
            emotions.physiological.Positive = positiveLevel;
            emotions.physiological.Negative = negativeLevel;
            emotions.physiological.Surprise = surpriseLevel;
            emotions.physiological.Valence = positiveLevel - negativeLevel;
            m_emotionController.SetEmotionPhysiologicalValenceAndSurprise((float)emotions.physiological.Positive, (float)emotions.physiological.Negative, (float)emotions.physiological.Surprise);
    }

    public void SetEmotionParameters(Core.Interfacing.EmotionSystem emotions)
    {
        SetPCMEmotionFacialParameters((float)emotions.facial.Positive, (float)emotions.facial.Negative, (float)emotions.facial.Surprise);
        SetPCMEmotionPhysiologicalParameters((float)emotions.physiological.Positive, (float)emotions.physiological.Negative, (float)emotions.physiological.Surprise);
        this.emotions.voluntaryFacial = emotions.voluntaryFacial;
        this.emotions.voluntaryPhysiological = emotions.voluntaryPhysiological;
    }

    public Core.Interfacing.Body GetAgentBodyData()
    {
        Vector3 center = new (m_mesh.bounds.center.x, m_mesh.sharedMesh.GetSubMesh(1).bounds.center.z, m_mesh.bounds.center.z);
        Vector3 size = Vector3.Scale(m_mesh.localBounds.size, m_mesh.rootBone.lossyScale);
        
        //TODO
        //Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        //Vector3 forward;
        //  forward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        //if (m_navController.Moving)
        //{

        //    forward = (transform.position -  m_navController.GetDestination()).normalized;
        //}
        //else
        //{
        //    forward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        //}
        
        //var temp = ExtensionTools.PCMBodyFromUnityData(center, size, GetLookAt(), GetLookAtOrigin());
        //Debug.DrawLine(ExtensionTools.FromPCMVector3(temp.OrientationOrigin, false), ExtensionTools.FromPCMVector3(temp.OrientationOrigin, false) + ExtensionTools.FromPCMVector3Unscale(temp.Orientation, false), Color.red, 10);

        return ExtensionTools.PCMBodyFromUnityData(center, size, GetLookAt(), GetLookAtOrigin());
    }
    public Vector3 GetUnscaledAgentBodyCenter()
    {
        return new (m_mesh.bounds.center.x, m_mesh.sharedMesh.GetSubMesh(1).bounds.center.z, m_mesh.bounds.center.z);
    }

    public Vector3 GetUnscaledAgentBodySize()
    {
        return Vector3.Scale(m_mesh.localBounds.size, m_mesh.rootBone.lossyScale);
    }
    #endregion

    #region Robot color
    public void SetColor(Color color)
    {
        Material material = m_mesh.materials[MainMaterialIndex];
        material.SetColor("_BaseColor", color);
    }

    private void InitColor()
    {
        float deltaAngle = 128f;
        float hue = (deltaAngle * (s_agents.Count - 1) / 360f) % 1f;
        float saturation = s_agents.Count > 1 ? 0.2f : 0f;
        float value = 1.0f;
        SetColor(Color.HSVToRGB(hue, saturation, value));
    }
    #endregion

    #region Properties
    public AgentControllerState CurrentState
    {
        get { return m_state; }
    }

    public float LevelOfInterest
    {
        get { return m_levelOfInterest; }
        set { m_levelOfInterest = value; }
    }

    public bool IsInterestedInAnObject
    {
        get { return m_objectOfInterest != null; }
    }

    public bool HasStrongInterest
    {
        get { return m_levelOfInterest >= DesireThreshold; }
    }

    public bool DoesNotHaveStrongInterest
    {
        get { return !HasStrongInterest; }
    }

    public bool LostInterestForObjectMidCourse
    {
        get { return DoesNotHaveStrongInterest && m_allowLossOfInterestMidcourse; }
    }

    public bool InRadiusOfDestination
    {
        get { return m_navController.WithinRadiusOf(m_objectOfInterest); }
    }

    public bool IsObjectOfInterestReachable
    {
        get { return m_interactionController.IsObjectReachable(m_objectOfInterest) && InRadiusOfDestination; }
    }

    public bool CanHoldObjectOfInterest
    {
        get { return m_objectOfInterest.CanBeTransferedTo(GetCurrentPreferredHolder()) && IsObjectOfInterestReachable; }
    }

    public bool IsInInteraction
    {
        get { return m_interactionController.IsInInteraction; }
    }

    public bool InteractingWithObjectOfInterest
    {
        get { return m_interactionController.InteractingWith(m_objectOfInterest); }
    }

    public ObjectOfInterest CurrentObjectOfInterest
    {
        get { return m_objectOfInterest; }
    }

    public ObjectOfInterest CurrentGrabbedObject
    {
        get { return m_grabbedObject; }
    }
    #endregion

    #region Static properties
    static public AgentMainController[] AllRobots
    {
        get { return s_agents.ToArray(); }
    }

    #endregion
}