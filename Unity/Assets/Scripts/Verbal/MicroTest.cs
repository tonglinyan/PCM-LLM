using UnityEngine;

public class MicroTest : MonoBehaviour
{
    private AudioSource audioSource;
    private string microphoneName;
    void Start()
    {

        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length > 0)
        {

            microphoneName = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneName);

            audioSource.clip = Microphone.Start(microphoneName, true, 10, 44100);
            audioSource.loop = true;

            while (!(Microphone.GetPosition(microphoneName) > 0)) { }


            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No microphone detected!");
        }
    }
}