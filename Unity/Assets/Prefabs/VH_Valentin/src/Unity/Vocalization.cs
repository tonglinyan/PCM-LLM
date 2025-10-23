/******************************************************
 *  Copyright (c) 2023, Yvain Tisserand
 *  All rights reserved.
 *
 *  NOTICE: This header must remain intact in all copies
 *  and derivative works of this code.
 *
 *  This code is part of the Geneva Virtual Human Toolkit,
 *  developed at the University of Geneva, in the Swiss
 *  Center for Affective Science. Unauthorized use,
 *  modification, or distribution of this code is strictly
 *  prohibited.
 *
 *  For more information about the Geneva Virtual Human
 *  Toolkit and licensing details, please visit:
 *  https://doi.org/10.1145/3383652.3423904
 *
 ******************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vocalization : MonoBehaviour
{
    public AudioSource audio;
    public AudioClip[] happy;
    public AudioClip[] sad;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void PlayHappy()
    {
        audio.clip = happy[Random.Range(0, happy.Length)];
        audio.Play();
    }

    public void PlaySad()
    {
        audio.clip = sad[Random.Range(0, sad.Length)];
        audio.Play();
    }
}
