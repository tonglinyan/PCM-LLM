using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudioSource : MonoBehaviour
{
    private AudioSource m_audioSource;

    [SerializeField] private Transform m_animatorRoot;
    [SerializeField] private float m_heightTriggerThreshold = 0f;
    [SerializeField] private float m_amplitudeThreshold = 0.05f;
    [SerializeField] private AudioVariants m_footstepAudioVariants;
    private bool m_belowHeightThreshold = false;

	private void Awake()
	{
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        Vector3 deltaToRoot = transform.position - m_animatorRoot.position;
        float footHeight = Vector3.Dot(m_animatorRoot.up, deltaToRoot);

        if (m_belowHeightThreshold)
        {
            if(footHeight > (m_heightTriggerThreshold + m_amplitudeThreshold))
            {
                m_belowHeightThreshold = false;
            }
        }
        else
        {
            if(footHeight < m_heightTriggerThreshold)
            {
                AudioClip randomAudioClip = m_footstepAudioVariants.GetRandomClip();
                m_audioSource.pitch = Random.Range(0.95f, 1.05f);
                m_audioSource.PlayOneShot(randomAudioClip);
                m_belowHeightThreshold = true;
            }
        }
    }
}
