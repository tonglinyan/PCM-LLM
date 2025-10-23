using System.Collections;
using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

[RequireComponent(typeof(InteractionSystem))]
[RequireComponent(typeof(Animator))]
public class AgentInteractionController : MonoBehaviour
{
    public enum InteractionType
    {
        Reaching,
        Holding,
    }

    public class InteractionInfo
    {
        public Holder.HolderType holderType;
        public InteractionType interactionType;
        public float reachWeight;
        public InteractionObject originalInteractionObject;

        public InteractionInfo(Holder.HolderType holderType, InteractionType interactionType, float reachWeight, InteractionObject originalInteractionObject)
        {
            this.holderType = holderType;
            this.interactionType = interactionType;
            this.reachWeight = reachWeight;
            this.originalInteractionObject = originalInteractionObject;
        }

        public InteractionInfo(Holder.HolderType holderType) : this(holderType, InteractionType.Holding, 0f, null)
        {

        }
    }

    private InteractionSystem m_interactionSystem;
    private Animator m_animator;

    private Dictionary<Holder.HolderType, Holder> m_holders;
    private Dictionary<InteractionObject, InteractionInfo> m_interactionsInProgress;
    [SerializeField] private float m_armsLength;
    private Transform m_lookAtTarget = null;

    private InteractionObject[] m_tmpInteractionObjects;
    private InteractionInfo[] m_tmpInteractionInfos;

    #region Awake
    private void Awake()
    {
        AwakeInteractionSystem();
        AwakeHolders();
        ComputeArmsLength();
    }

    private void AwakeInteractionSystem()
    {
        m_interactionSystem = GetComponent<InteractionSystem>();
        m_interactionSystem.OnInteractionStart += OnInteractionStart;
        m_interactionSystem.OnInteractionPause += OnInteractionPause;
        m_interactionSystem.OnInteractionResume += OnInteractionResume;
        m_interactionSystem.OnInteractionStop += OnInteractionStop;
        m_interactionsInProgress = new Dictionary<InteractionObject, InteractionInfo>();
    }

    private void AwakeHolders()
    {
        m_holders = new Dictionary<Holder.HolderType, Holder>();
        Holder[] holders = GetComponents<Holder>();
        foreach (Holder holder in holders)
        {
            m_holders[holder.Type] = holder;
        }
    }
    private void ComputeArmsLength()
    {
        m_animator = GetComponent<Animator>();

        if (m_armsLength > 0f)
        {
            return;
        }

        Vector3 leftShoulderPosition = m_animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        Vector3 leftElbowPosition = m_animator.GetBoneTransform(HumanBodyBones.LeftLowerArm).position;
        Vector3 leftHandPosition = m_animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
        float leftArmLength = (leftElbowPosition - leftShoulderPosition).magnitude;
        float leftForearmLength = (leftHandPosition - leftElbowPosition).magnitude;
        m_armsLength = leftArmLength + leftForearmLength;
    }
    #endregion

    private void Update()
    {
        UpdateReaching();
        m_tmpInteractionObjects = new InteractionObject[m_interactionsInProgress.Count];
        m_tmpInteractionInfos = new InteractionInfo[m_interactionsInProgress.Count];
        m_interactionsInProgress.Keys.CopyTo(m_tmpInteractionObjects, 0);
        m_interactionsInProgress.Values.CopyTo(m_tmpInteractionInfos, 0);
    }

    #region Interaction System Events
    private void OnInteractionStart(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
    {

    }

    private void OnInteractionPause(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
    {
        if (m_interactionsInProgress.ContainsKey(interactionObject))
        {
            if (m_interactionsInProgress[interactionObject].interactionType == InteractionType.Holding)
            {
                ObjectOfInterest holdable = interactionObject.GetComponent<ObjectOfInterest>();
                Holder.HolderType holderType = m_interactionsInProgress[interactionObject].holderType;
                Holder holder = GetHolder(holderType);

                if (holdable.CanBeTransferedTo(holder) && holdable.CurrentHolder != holder)
                {
                    holder.Hold(holdable);
                }
            }
        }
    }

    private void OnInteractionResume(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
    {
        UnregisterInteraction(interactionObject);
    }

    private void OnInteractionStop(FullBodyBipedEffector effectorType, InteractionObject interactionObject)
    {
        UnregisterInteraction(interactionObject);
    }
    #endregion

    #region Interactions
    public void StartInteraction(InteractionObject interactionObject, InteractionType interactionType, Holder.HolderType holderType, float initialReachWeight = 1f)
    {
        InteractionObject originalInteractionObject = null;
        if (interactionType == InteractionType.Reaching)
        {
            originalInteractionObject = interactionObject;
            ObjectOfInterest duplicate = ObjectOfInterest.GhostDuplicate(originalInteractionObject);
            interactionObject = duplicate.InteractionObjectComponent;
            interactionObject.Initiate();
        }
        else if(interactionType == InteractionType.Holding)
        {
            Rigidbody rigidbody = interactionObject.GetComponent<Rigidbody>();
            if(rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
        }

        if ((holderType == Holder.HolderType.LeftHand) || (holderType == Holder.HolderType.TwoHand))
        {
            m_interactionSystem.StartInteraction(FullBodyBipedEffector.LeftHand, interactionObject, interrupt: true);
        }

        if ((holderType == Holder.HolderType.RightHand) || (holderType == Holder.HolderType.TwoHand))
        {
            m_interactionSystem.StartInteraction(FullBodyBipedEffector.RightHand, interactionObject, interrupt: true);
        }

        m_interactionsInProgress[interactionObject] = new InteractionInfo(holderType, interactionType, initialReachWeight, originalInteractionObject);
    }

    public void StartInteraction(ObjectOfInterest holdable, InteractionType interactionType, Holder.HolderType holderType, float initialReachWeight = 1f)
    {
        StartInteraction(holdable.InteractionObjectComponent, interactionType, holderType, initialReachWeight);
    }

    public void StartInteraction(ObjectOfInterest holdable, InteractionType interactionType, float initialReachWeight = 1f)
    {
        StartInteraction(holdable.InteractionObjectComponent, interactionType, holdable.PreferredHolderType, initialReachWeight);
    }

    public void StopInteraction(Holder.HolderType holderType, bool interruptInteraction = false)
    {
        if ((holderType == Holder.HolderType.LeftHand) || (holderType == Holder.HolderType.TwoHand))
        {
            if (interruptInteraction)
            {
                m_interactionSystem.StopInteraction(FullBodyBipedEffector.LeftHand);
            }
            else
            {
                m_interactionSystem.ResumeInteraction(FullBodyBipedEffector.LeftHand);
            }
        }

        if ((holderType == Holder.HolderType.RightHand) || (holderType == Holder.HolderType.TwoHand))
        {
            if (interruptInteraction)
            {
                m_interactionSystem.StopInteraction(FullBodyBipedEffector.RightHand);
            }
            else
            {
                m_interactionSystem.ResumeInteraction(FullBodyBipedEffector.RightHand);
            }
        }
    }

    public void StopAllInteractions(bool interruptInteraction = false)
    {
        InteractionInfo[] currentInteractions = new InteractionInfo[m_interactionsInProgress.Count];
        m_interactionsInProgress.Values.CopyTo(currentInteractions, 0);

        foreach (InteractionInfo interactionInfo in currentInteractions)
        {
            StopInteraction(interactionInfo.holderType, interruptInteraction);
        }
    }

    protected void UnregisterInteraction(InteractionObject interactionObject)
    {
        if (m_interactionsInProgress.ContainsKey(interactionObject))
        {
            Debug.Log($"{name} stopping interaction with {interactionObject.name}");
            if (m_interactionsInProgress[interactionObject].interactionType == InteractionType.Reaching)
            {
                ObjectOfInterest objectOfInterest = interactionObject.GetComponent<ObjectOfInterest>();
                if(objectOfInterest != null && !objectOfInterest.IsADuplicate)
                {
                    throw new System.Exception("Only duplicates should be registered for reaching");
                }

                DestroyReachInteractionObject(interactionObject);
            }
            else
            {
                Holder.HolderType holderType = m_interactionsInProgress[interactionObject].holderType;
                m_holders[holderType].Release();
            }
            m_interactionsInProgress.Remove(interactionObject);
        }
    }

    public bool InteractingUsing(Holder.HolderType holderType)
    {
        foreach (InteractionInfo interactionInfo in m_interactionsInProgress.Values)
        {
            if (interactionInfo.holderType == holderType)
            {
                return true;
            }
        }
        return false;
    }

    public bool InteractingWith(ObjectOfInterest holdable)
    {
        foreach(InteractionObject interactionObject in m_interactionsInProgress.Keys)
        {
            InteractionInfo interactionInfo = m_interactionsInProgress[interactionObject];
            if(interactionInfo.interactionType == InteractionType.Holding)
            {
                if(holdable.InteractionObjectComponent == interactionObject)
                {
                    return true;
                }
            }
            else
            {
                if(holdable.InteractionObjectComponent == interactionInfo.originalInteractionObject)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsInInteraction
    {
        get { return m_interactionsInProgress.Count > 0; }
    }
    #endregion

    #region Holding
    public void StartHolding(ObjectOfInterest objectOfInterest, Holder.HolderType holderType)
    {
        StartInteraction(objectOfInterest, InteractionType.Holding, holderType);
    }

    public void StartHolding(ObjectOfInterest objectOfInterest)
    {
        StartHolding(objectOfInterest, objectOfInterest.PreferredHolderType);
    }

    public ObjectOfInterest GetObjectHeld(Holder.HolderType holderType)
    {
        if (!m_holders.ContainsKey(holderType))
        {
            return null;
        }

        return m_holders[holderType].ObjectHeld;
    }

    public bool HoldingUsing(Holder.HolderType holderType)
    {
        return GetObjectHeld(holderType) != null;
    }

    public bool IsHolding(ObjectOfInterest objectOfInterest)
    {
        if (objectOfInterest == null)
        {
            throw new System.InvalidOperationException("Holdable cannot be null.");
        }

        foreach (Holder holder in m_holders.Values)
        {
            if (holder.ObjectHeld == objectOfInterest)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsObjectReachable(ObjectOfInterest objectOfInterest)
    {
        // TODO : Temporary (use a capsule collider instead)
        Vector3 leftShoulderPosition = m_animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
        Vector3 rightShoulderPosition = m_animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
        Vector3 referencePosition = (leftShoulderPosition + rightShoulderPosition) * 0.5f;
        float distanceToObject = (referencePosition - objectOfInterest.transform.position).magnitude;
        bool withinArmsReach = distanceToObject < m_armsLength;

        bool belowHead = objectOfInterest.transform.position.y < 0.9f;

        return withinArmsReach || belowHead;
    }
    #endregion

    #region LookAt
    /*public void LookAt(Transform target)
    {
        m_lookAtTarget = target;
        m_interactionSystem.lookAt.Look(target, time: Time.time + 3600f);
    }*/

    public Transform LookAtTarget
    {
        get { return m_lookAtTarget; }
    }

    #endregion

    #region Reaching for object (when the robot desires an object but cannot get it)
    #region Start reaching
    public void StartReaching(InteractionObject interactionObject, Holder.HolderType holderType, float initialReachWeight = 1f)
    {

        StartInteraction(interactionObject, InteractionType.Reaching, holderType, initialReachWeight);
    }

    public void StartReaching(ObjectOfInterest holdable, Holder.HolderType holderType, float initialReachWeight = 1f)
    {
        StartReaching(holdable.InteractionObjectComponent, holderType, initialReachWeight);
    }

    public void StartReaching(ObjectOfInterest holdable, float initialReachWeight = 1f)
    {
        StartReaching(holdable.InteractionObjectComponent, holdable.PreferredHolderType, initialReachWeight);
    }
    #endregion

    private void UpdateReaching()
    {
        foreach (InteractionObject interactionObject in m_interactionsInProgress.Keys)
        {
            InteractionInfo interactionInfo = m_interactionsInProgress[interactionObject];
            if (interactionInfo.interactionType == InteractionType.Reaching)
            {
                float reachWeight = GetDynamicReachWeight(interactionObject);
                Vector3 shouldersCenter = GetShouldersCenter();
                Vector3 originalPosition = m_interactionsInProgress[interactionObject].originalInteractionObject.transform.position;
                Vector3 worldDeltaPosition = originalPosition - shouldersCenter;
                Vector3 ghostPosition = shouldersCenter + worldDeltaPosition * reachWeight;
                interactionObject.transform.position = ghostPosition;
                interactionObject.transform.forward = worldDeltaPosition.normalized;
            }
        }
    }

    public void SetReachWeight(InteractionObject interactionObject, float reachWeight)
    {
        if (m_interactionsInProgress.ContainsKey(interactionObject))
        {
            m_interactionsInProgress[interactionObject].reachWeight = reachWeight;
        }
    }

    public Vector3 GetShouldersCenter()
    {
        Vector3 leftShoulderPosition = GetEffectorBonePosition(FullBodyBipedEffector.LeftShoulder);
        Vector3 rightShoulderPosition = GetEffectorBonePosition(FullBodyBipedEffector.RightShoulder);
        return (leftShoulderPosition + rightShoulderPosition) * 0.5f;
    }

    #region Get reaching distance to object/position
    public float GetReachingDistance(Vector3 objectPosition)
    {
        return (GetShouldersCenter() - objectPosition).magnitude;
    }

    public float GetReachingDistance(Transform objectTransform)
    {
        return GetReachingDistance(objectTransform.position);
    }

    public float GetReachingDistance(InteractionObject interactionObject)
    {
        return GetReachingDistance(interactionObject.transform);
    }

    public float GetReachingDistance(ObjectOfInterest holdable)
    {
        return GetReachingDistance(holdable.transform);
    }
    #endregion

    #region Dynamic Reaching Weight
    public float GetDynamicReachWeight(Vector3 objectPosition, float baseWeight)
    {
        return Mathf.Min(m_armsLength / GetReachingDistance(objectPosition), 1.0f) * baseWeight;
    }

    public float GetDynamicReachWeight(Transform objectTransform, float baseWeight)
    {
        return GetDynamicReachWeight(objectTransform.position, baseWeight);
    }

    public float GetDynamicReachWeight(InteractionObject ghostInteractionObject)
    {
        float baseWeight = m_interactionsInProgress[ghostInteractionObject].reachWeight;
        InteractionObject originalInteractionObject = m_interactionsInProgress[ghostInteractionObject].originalInteractionObject;
        return GetDynamicReachWeight(originalInteractionObject.transform, baseWeight);
    }

    public float GetDynamicReachWeight(ObjectOfInterest holdable)
    {
        return GetDynamicReachWeight(holdable.InteractionObjectComponent);
    }
    #endregion

    public void DestroyReachInteractionObject(InteractionObject interactionObject)
    {
        Destroy(interactionObject.gameObject);
    }
    #endregion

    #region Holders
    public Holder GetHolder(Holder.HolderType holderType)
    {
        if (!m_holders.ContainsKey(holderType))
        {
            return null;
        }

        return m_holders[holderType];
    }

    public Holder GetPreferredHolder(ObjectOfInterest holdable)
    {
        return GetHolder(holdable.PreferredHolderType);
    }

    public Holder LeftHandHolder
    {
        get { return GetHolder(Holder.HolderType.LeftHand); }
    }

    public Holder RightHandHolder
    {
        get { return GetHolder(Holder.HolderType.RightHand); }
    }

    public Holder TwoHandHolder
    {
        get { return GetHolder(Holder.HolderType.TwoHand); }
    }

    public Dictionary<Holder.HolderType, Holder> Holders
    {
        get { return m_holders; }
    }
    #endregion

    #region Helpers
    public Transform GetEffectorBoneTransform(FullBodyBipedEffector effector)
    {
        return m_interactionSystem.ik.solver.GetEffector(effector).bone;
    }

    public Vector3 GetEffectorBonePosition(FullBodyBipedEffector effector)
    {
        return GetEffectorBoneTransform(effector).position;
    }
    #endregion
}
