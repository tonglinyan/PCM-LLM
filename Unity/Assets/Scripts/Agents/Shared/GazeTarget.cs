using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GazeTarget : MonoBehaviour
{
    private Vector3 target;  
    [SerializeField] float speed = 5f;

    void Start()
    {
        target = transform.position;   
    }
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, target, speed * Time.deltaTime);
    }

    public void SetTarget(Vector3 target)
    {
        this.target = target;
    }
}
