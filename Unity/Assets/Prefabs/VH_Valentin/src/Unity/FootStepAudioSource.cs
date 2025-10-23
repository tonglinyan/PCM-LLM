using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootStepAudioSource : MonoBehaviour
{
    [SerializeField] private AudioSource audioS;
    [SerializeField] private AudioVariants m_footstepAudioVariants;
    [SerializeField] private float verticalTreshold = 0.1f;
    private bool newStep = false;
    private float initFootPosition_y; 
    public void Awake()
    {
        initFootPosition_y = transform.position.y;
    }
    
    public void Update()
    {
        if (Mathf.Abs(transform.position.y - initFootPosition_y) > verticalTreshold)
        {
            newStep = true;
            return;

        }
        else if (newStep)
            newStep = false;
        else
            return;
        AudioClip randomAudioClip = m_footstepAudioVariants.GetRandomClip();
        audioS.pitch = Random.Range(0.95f, 1.05f);
        audioS.PlayOneShot(randomAudioClip);
    }
}
