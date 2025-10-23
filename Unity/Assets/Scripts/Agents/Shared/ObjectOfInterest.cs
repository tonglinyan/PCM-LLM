using System.Collections.Generic;
using RootMotion.FinalIK;
using UnityEngine;

[RequireComponent(typeof(InteractionObject))]
[RequireComponent(typeof(Mesh))]
public class ObjectOfInterest : MonoBehaviour
{
    public const int ObjectOfInterestLayer = 3;
    public const int ObjectOfInterestLayerMask = 1 << ObjectOfInterestLayer;

    static private List<ObjectOfInterest> s_allObjectsOfInterest = new List<ObjectOfInterest>();

    [SerializeField] private float m_radius = 0f;
    [SerializeField] private bool m_holdable = true;
    [SerializeField] private Holder.HolderType m_preferredHolderType = Holder.HolderType.TwoHand;

    protected Collider m_collider;
    protected Rigidbody m_rigidbody;
    protected InteractionObject m_interactionObject;
    protected MeshRenderer m_mesh;
    protected Holder m_holder;
    
	protected void Awake()
	{
        m_collider = GetComponent<Collider>();
        m_rigidbody = GetComponent<Rigidbody>();
        m_interactionObject = GetComponent<InteractionObject>();
        m_mesh = GetComponent<MeshRenderer>();
        if (!s_allObjectsOfInterest.Contains(this))
        {
            if (!IsADuplicate)
            {
                s_allObjectsOfInterest.Add(this);
            }
        }
    }
    protected void OnDestroy()
    {
        if (s_allObjectsOfInterest.Contains(this))
        {
            s_allObjectsOfInterest.Remove(this);
        }
    }

    protected void Update()
    {
        if (Held)
        {
            transform.position = m_holder.HoldPosition;
            transform.rotation = m_holder.HoldRotation;
        }
       // Debug.Log("REF " + ObjectOfInterestLayer+"  "+gameObject.name);
    }

    #region Hold / Release / Transfer
    public bool SetHolder(Holder holder)
    {
        if(Holdable && (holder != null))
        {
            m_holder = holder;
            m_rigidbody.useGravity = false;
            m_rigidbody.isKinematic = true;
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_collider.isTrigger = true;

            return true;
        }
        else
        {
            return false;
        }
    }

    public void Release()
    {
        m_holder = null;
        m_rigidbody.useGravity = true;
        m_rigidbody.isKinematic = false;
        m_rigidbody.velocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;
        m_collider.isTrigger = false;

    }

    public bool CanBeTransferedTo(Holder holder)
    {
        return Holdable && (!Held || m_holder.AllowsHoldTransfersTo(holder));
    }

    #endregion

    public Core.Interfacing.Body GetBodyData()
    {
        Vector3 center = m_mesh.bounds.center;
        //Vector3 size = Vector3.Scale(m_mesh.localBounds.extents * 2, transform.lossyScale);
        Vector3 size = CalculateWorldSize(Vector3.Scale(m_mesh.localBounds.extents * 2, transform.lossyScale), transform.rotation);
        //TODO tyan: debug forward of objects
        //Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z);
        Vector3 forward = new Vector3(transform.forward.x, transform.forward.y, transform.forward.z);

        //Debug.DrawLine(center, center + forward, Color.red, 10);
        //Debug.DrawLine(ExtensionTools.temp.OrientationOrigin, temp.OrientationOrigin + temp.Orientation, Color.red, 10);
        return ExtensionTools.PCMBodyFromUnityData(center, size, forward, center);
    }

    public Vector3 GetUnscaledBodyCenter()
    {
        return m_mesh.bounds.center;
    }

    public Vector3 GetUnscaledBodySize()
    {
        Vector3 localSize = Vector3.Scale(m_mesh.localBounds.extents * 2, transform.lossyScale);
        float tmp = localSize.x;
        localSize.x = localSize.z;
        localSize.z = tmp;
        return CalculateWorldSize(localSize, transform.rotation);
    }

    Vector3 CalculateWorldSize(Vector3 localSize, Quaternion orientation)
    {
        // Define the local axes of the object
        Vector3 localRight = new Vector3(1, 0, 0);  // Local X-axis
        Vector3 localUp = new Vector3(0, 1, 0);     // Local Y-axis
        Vector3 localForward = new Vector3(0, 0, 1); // Local Z-axis

        // Rotate the local axes to align with the world frame
        Vector3 rightWorld = orientation * localRight;
        Vector3 upWorld = orientation * localUp;
        Vector3 forwardWorld = orientation * localForward;

        // Combine contributions from each local axis scaled by the local size
        Vector3 worldSize = new Vector3(
            Mathf.Abs(localSize.x * rightWorld.x) + Mathf.Abs(localSize.y * upWorld.x) + Mathf.Abs(localSize.z * forwardWorld.x),
            Mathf.Abs(localSize.x * rightWorld.y) + Mathf.Abs(localSize.y * upWorld.y) + Mathf.Abs(localSize.z * forwardWorld.y),
            Mathf.Abs(localSize.x * rightWorld.z) + Mathf.Abs(localSize.y * upWorld.z) + Mathf.Abs(localSize.z * forwardWorld.z)
        );
        return worldSize;
    }

    #region Properties
    public bool Holdable
    {
        get { return m_holdable; }
    }

    public bool Held
    {
        get { return m_holder != null; }
    }

    public Holder.HolderType PreferredHolderType
    {
        get { return m_preferredHolderType; }
    }

    public float Radius
    {
        get { return m_radius; }
    }

    public InteractionObject InteractionObjectComponent
    {
        get { return m_interactionObject; }
    }

    public Holder CurrentHolder
    {
        get { return m_holder; }
    }

    public bool IsADuplicate
    {
        get { return m_mesh == null; }
    }
    #endregion

    #region Static helpers
    static public ObjectOfInterest GhostDuplicate(InteractionObject interactionObject)
    {
        GameObject newGameObject = ExtensionTools.DuplicateGameObject(interactionObject.gameObject, typeof(ObjectOfInterest), typeof(InteractionObject));
        foreach (InteractionTarget interactionTarget in interactionObject.GetTargets())
        {
            InteractionTarget newInteractionTarget = Instantiate(interactionTarget);
            newInteractionTarget.transform.SetParent(newGameObject.transform);
            newInteractionTarget.transform.localPosition = interactionTarget.transform.localPosition;
            newInteractionTarget.transform.localRotation = interactionTarget.transform.localRotation;
            newInteractionTarget.transform.localScale = interactionTarget.transform.localScale;
        }

        ObjectOfInterest newObject = newGameObject.GetComponent<ObjectOfInterest>();
        newObject.InteractionObjectComponent.otherLookAtTarget = interactionObject.lookAtTarget;

        return newObject;
    }
    static public ObjectOfInterest GhostDuplicate(ObjectOfInterest originalObject)
    {
        return GhostDuplicate(originalObject.InteractionObjectComponent);
    }
    #endregion

    static public ObjectOfInterest[] AllObjectsOfInterest
    {
        get { return s_allObjectsOfInterest.ToArray(); }
    }
}
