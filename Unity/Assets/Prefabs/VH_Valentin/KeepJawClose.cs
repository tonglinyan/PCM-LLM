using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepJawClose : MonoBehaviour
{
    Vector3 pos;
    Quaternion rot;
    // Start is called before the first frame update
    void Start()
    {
        pos = transform.localPosition;
        rot = transform.localRotation;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.localPosition = pos;
        transform.localRotation = rot;
    }
}
