using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioVariant", menuName = "Audio/AudioVariant", order = 1)]
public class AudioVariants : ScriptableObject
{
	[SerializeField] private AudioClip[] m_clips;

	public AudioClip[] Clips
    {
        get { return m_clips; }
    }

    public AudioClip GetRandomClip()
    {
        return m_clips.GetRandomElement();
    }
}
