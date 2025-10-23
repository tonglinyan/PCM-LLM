using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour {
    
    private bool m_isRotating = false;
    private float m_speed = 1f;

    private bool m_isLightTurningOn = false;
    private bool m_isLightTurningOff = false;

    private Renderer m_renderer;
    private Material m_material;
    private Color m_emissiveColor = Color.black;

    private float t = 0.0f;


    void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_material = m_renderer.material;
    }
    
	
	void Update () {

        if (m_isRotating)
        {
            transform.Rotate(Vector3.back*Time.deltaTime*m_speed);
        }

        if (m_isLightTurningOn)
        {
            m_emissiveColor = Color.Lerp(Color.black, Color.white, t);

            t += Time.deltaTime * (m_speed/2);

            if (t >= 1)
            {
                m_isLightTurningOn = false;
                t = 1;
            }

            m_material.SetColor("_EmissionColor", m_emissiveColor);
        }

        if (m_isLightTurningOff)
        {
            m_emissiveColor = Color.Lerp(Color.black, Color.white, t);

            t -= Time.deltaTime * (m_speed / 2);

            if (t <= 0)
            {
                m_isLightTurningOff = false;
                t = 0;
            }

            m_material.SetColor("_EmissionColor", m_emissiveColor);
        }

    }


    public void ToogleRotation(bool state)
    {
        m_isRotating = state;

        if (m_isRotating)
        {
            m_isLightTurningOn = true;
            m_isLightTurningOff = true;
        }
        else{
            m_isLightTurningOn = false;
            m_isLightTurningOff = true;
        }
    }


    public void SetRotationSpeed(float speed)
    {
        m_speed = speed;
    }
}
