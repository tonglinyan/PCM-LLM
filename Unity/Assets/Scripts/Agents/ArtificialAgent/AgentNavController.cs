using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class AgentNavController : MonoBehaviour
{
    // [SerializeField] private Transform m_leftHand;
    // [SerializeField] private Transform m_rightHand;

    private Animator m_animator;
    private NavMeshAgent m_navMeshAgent;

    private Vector3 m_currentDestination;
    private Vector2 m_velocity = Vector2.zero;
    private Vector2 m_smoothDeltaPosition = Vector2.zero;

    private ObjectOfInterest m_targetOfInterest;
    private Vector3? m_currentRotationTarget;

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_navMeshAgent = GetComponent<NavMeshAgent>();
        m_navMeshAgent.updatePosition = false;

    }

    #region Updates
    private void Update()
    {
        UpdateRotation();
        UpdateDestination();
        UpdateVelocity();
        UpdateAnimatorBlendTree();
    }

    private void UpdateRotation()
    {
        if (m_currentRotationTarget.HasValue)
        {
            float angleToTarget = Vector3.Angle(transform.forward, m_currentRotationTarget.Value);
            float anglePerUpdate = m_navMeshAgent.angularSpeed * Time.deltaTime;
            Quaternion targetRotation = Quaternion.LookRotation(m_currentRotationTarget.Value, Vector3.up);


            if (angleToTarget < anglePerUpdate)
            {
                transform.rotation = targetRotation;
                m_currentRotationTarget = null;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, anglePerUpdate);
            }
        }
    }

    private void UpdateDestination()
    {
        //TODO tyan
        if ((m_targetOfInterest != null) && DestinationHasChanged)
        {
            SetDestination(m_targetOfInterest.transform.position);
        }
    }

    public void UpdateVelocity()
    {
        Vector3 worldDeltaPosition = m_navMeshAgent.nextPosition - transform.position;

        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        float smoothFactor = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        m_smoothDeltaPosition = Vector2.Lerp(m_smoothDeltaPosition, deltaPosition, 0.9f);

        if (Time.deltaTime > 1e-5f)
        {
            m_velocity = m_smoothDeltaPosition / Time.deltaTime;
        }
    }

    public void UpdateAnimatorBlendTree()
    {
        m_animator.SetBool("Moving", Moving);
        m_animator.SetFloat("VelocityX", m_velocity.x);
        m_animator.SetFloat("VelocityY", m_velocity.y);
    }

    private void OnAnimatorMove()
    {
        transform.position = m_navMeshAgent.nextPosition;
    }
    #endregion

    public void SetRotationTarget(Vector3 forward)
    {
        forward.y = 0;
        forward.Normalize();

        float angleToTarget = Vector3.Angle(transform.forward, forward);
        if (angleToTarget >= 3f) // condition to avoid jittering rotation
        {
            m_currentRotationTarget = forward;
        }
    }

    public void SetDestination(Vector3 target, Vector3 orientation=new())
    {
        m_currentDestination = target;
        m_currentRotationTarget = orientation;
        m_navMeshAgent.isStopped = false;
        m_navMeshAgent.SetDestination(target);
    }

    public Vector3 GetDestination()
    {
        return m_currentDestination;
    }
    public Vector3 GetNextStep()
    {
        return m_navMeshAgent.nextPosition;
    }

    public void RebootController(Vector3 pos)
    {
        m_currentRotationTarget = null;
        m_navMeshAgent.isStopped = true;
        m_navMeshAgent.nextPosition = pos;
    }
    public void GoToObjectOfInterest(ObjectOfInterest target)
    {
        m_targetOfInterest = target;
        SetDestination(target.transform.position);
    }

    public void ClearTarget()
    {
        if (m_targetOfInterest != null)
        {
            m_targetOfInterest = null;
            m_navMeshAgent.isStopped = true;
        }
    }

    public bool WithinRadiusOf(ObjectOfInterest holdable)
    {
        Vector3 deltaWorldPosition = holdable.transform.position - transform.position;
        deltaWorldPosition = Vector3.ProjectOnPlane(deltaWorldPosition, transform.up);
        float distance = deltaWorldPosition.magnitude;
        float targetRadius = holdable == null ? 0f : holdable.Radius;
        return distance < (m_navMeshAgent.radius + targetRadius);
    }

    public bool WithinRadiusOfTarget
    {
        get { return WithinRadiusOf(m_targetOfInterest); }
    }

    public bool WithinRadiusOfDestination
    {
        get { return m_navMeshAgent.remainingDistance < m_navMeshAgent.radius; }
    }

    public bool DestinationHasChanged
    {
        get
        {
            if (Vector3.Distance(m_currentDestination, m_targetOfInterest.transform.position) < 0.01f)
            {
                return false;
            }
            NavMeshHit navMeshHit;
            NavMesh.SamplePosition(m_targetOfInterest.transform.position, out navMeshHit, 2f, NavMesh.AllAreas);
            if (!navMeshHit.hit) return false;
            return Vector3.Distance(navMeshHit.position, m_navMeshAgent.destination) > 0.01f;
        }
    }

    public bool Moving
    {
        get { return (m_velocity.magnitude > 0.001f) && !WithinRadiusOfDestination; }
    }

    public ObjectOfInterest TargetOfInterest
    {
        get { return m_targetOfInterest; }
    }
}
