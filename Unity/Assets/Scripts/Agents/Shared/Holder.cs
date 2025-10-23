using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Holder : MonoBehaviour
{
    public enum HolderType
    {
        LeftHand,
        RightHand,
        TwoHand,
        None
    }

    public enum AgentType
    {
        PCMAgent,
        VRAgent,
    }

    public enum HoldTransferFlags
    {
        AllowAny,
        OnlyPCMAgent,
        OnlyVRAgent,
        AllowNone,
    }

    [SerializeField] private Transform m_holdReference;
    [SerializeField] private HolderType m_type;
    [SerializeField] private AgentType m_agentType = AgentType.PCMAgent;
    [SerializeField] private HoldTransferFlags m_allowedHoldTransfers = HoldTransferFlags.OnlyVRAgent;
    private ObjectOfInterest m_objectHeld;
    private Vector3 m_holdPositionOffset;
    private Quaternion m_holdRotationOffset;

    private void Awake()
    {
        if (m_holdReference == null)
        {
            m_holdReference = transform;
        }
    }

    public void Hold(ObjectOfInterest objectOfInterest)
    {
        
        if (!objectOfInterest.Holdable)
        {
            return;
        }
        

        if (objectOfInterest.Held)
        {
            Debug.Log("Object is held");
            Holder otherHolder = objectOfInterest.CurrentHolder;
            Debug.Log("holder: " + otherHolder.name);
            
            if (otherHolder.AllowsHoldTransfersTo(this))
            {
                Debug.Log("Allow transfert");
                if (otherHolder.m_agentType == AgentType.PCMAgent)
                {
                    otherHolder.gameObject.GetComponent<AgentMainController>().StopInteracting();
                }
                otherHolder.TransferHoldTo(this);
               
            }
        }
        else
        {
            Debug.Log("Object is NOT held");
            m_objectHeld = objectOfInterest;
            if (m_agentType == AgentType.VRAgent)
            {
                m_holdPositionOffset = objectOfInterest.transform.position - m_holdReference.position;
                m_holdRotationOffset = objectOfInterest.transform.rotation * Quaternion.Inverse(m_holdReference.rotation);
            }
            objectOfInterest.SetHolder(this);
        }
    }

    public void TransferHoldTo(Holder otherHolder)
    {
        if ((m_objectHeld != null) && (otherHolder != null))
        {
            ObjectOfInterest currentObject = m_objectHeld;
            Release();
            otherHolder.Hold(currentObject);
        }
    }

    public void Release()
    {
        if (m_objectHeld != null)
        {
            Debug.Log("Release");
            m_objectHeld.Release();
            m_objectHeld = null;
        }
    }

    private bool _AllowsHoldTransfersTo(AgentType agentType)
    {
        return m_allowedHoldTransfers switch
        {
            HoldTransferFlags.AllowAny => true,
            HoldTransferFlags.AllowNone => false,
            HoldTransferFlags.OnlyVRAgent => agentType == AgentType.VRAgent,
            HoldTransferFlags.OnlyPCMAgent => agentType == AgentType.PCMAgent,
            _ => false,
        };
    }

    public bool AllowsHoldTransfersTo(Holder holder)
    {
        if (holder.gameObject == gameObject)
        {
            return true;
        }

        return _AllowsHoldTransfersTo(holder.m_agentType);
    }

    #region Properties
    #region Transform
    public Vector3 Position
    {
        get { return m_holdReference.position; }
    }

    public Quaternion Rotation
    {
        get { return m_holdReference.rotation; }
    }

    public Vector3 HoldPosition
    {
        get
        {
            if (m_agentType == AgentType.VRAgent)
            {
                return m_holdReference.position + m_holdPositionOffset;
            }
            else
            {
                return Position;
            }
        }
    }

    public Quaternion HoldRotation
    {
        get
        {
            if (m_agentType == AgentType.VRAgent)
            {
                return m_holdRotationOffset * m_holdReference.rotation;
            }
            else
            {
                return Rotation;
            }
        }
    }
    #endregion

    #region Current object held
    public ObjectOfInterest ObjectHeld
    {
        get { return m_objectHeld; }
    }

    public bool Holding
    {
        get { return m_objectHeld != null; }
    }
    #endregion

    #region Transfer hold
    public HolderType Type
    {
        get { return m_type; }
    }

    public HoldTransferFlags AllowedHoldTransfers
    {
        get { return m_allowedHoldTransfers; }
    }

    public AgentType HolderAgentType
    {
        get { return m_agentType; }
    }
    #endregion
    #endregion
}
