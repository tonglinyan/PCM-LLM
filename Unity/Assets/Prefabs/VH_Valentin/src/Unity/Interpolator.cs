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
public enum InterpolationMethod{Linear, Spherical};
public class Interpolator : MonoBehaviour {

		
	[SerializeField] Transform target;
	[SerializeField] Vector3 startPoint;
	[SerializeField] Vector3 endPoint;

	[SerializeField] Vector3 startAngle;
	[SerializeField] Vector3 endAngle;

	public bool activation = false;
	public bool interpolationDone = false;
	public float journeyTime = 1.0f;
	private float startTime;
	private InterpolationMethod method;

	public bool haveOrientation = false;


	// Use this for initialization
	void Awake() {

	}

	// Update is called once per frame
	void LateUpdate () {

		if (activation){
			Vector3 center = (startPoint + endPoint) * 0.5f;
			center -= new Vector3 (0, 1, 0);
			Vector3 pointARelCenter = startPoint - center;
			Vector3 pointBRelCenter = endPoint - center;
			float fracComplete = (Time.time - startTime) / journeyTime;
			if (method == InterpolationMethod.Linear) {
				target.position = Vector3.Lerp (pointARelCenter, pointBRelCenter, fracComplete);
				//target.rotation = Quaternion.Lerp (target.rotation, endPoint.rotation, fracComplete);
			} else if (method == InterpolationMethod.Spherical) {
				target.position = Vector3.Slerp(pointARelCenter, pointBRelCenter, fracComplete);
				//target.rotation = Quaternion.Slerp (target.rotation, endPoint.rotation, fracComplete);
			}
			target.position += center;

            if (haveOrientation)
            {

				Vector3 centerAngle = (startAngle + endAngle) * 0.5f;
				centerAngle -= new Vector3(0, 1, 0);
				Vector3 pointARelCenterAngle = startAngle - center;
				Vector3 pointBRelCenterAngle = endAngle - center;
				
				if (method == InterpolationMethod.Linear)
				{
					target.eulerAngles = Vector3.Slerp(pointARelCenterAngle, pointBRelCenterAngle, fracComplete);
					//target.rotation = Quaternion.Lerp (target.rotation, endPoint.rotation, fracComplete);
				}
				
				target.eulerAngles += centerAngle;

			}
					

			if (fracComplete >= 1.0f) {
				activation = false;
				interpolationDone = true;
				haveOrientation = false;
			}

		}

	}
	public void SetTargetControllerIK(Transform t){
		target = t;
	}


	public void InterpolateToDestination(Vector3 pos, InterpolationMethod met = InterpolationMethod.Linear, float travelTime = 1.0f)
	{

		endPoint = pos;
		//endPoint.rotation = Quaternion.identity;

		startPoint = target.position;
		method = met;
		journeyTime = travelTime;

		startTime = Time.time;
		activation = true;

	}
	public void ResetInterpolator(Vector3 pos)
    {
		target.position = pos;
		startPoint = pos;
		endPoint = pos;
		activation = false;
	}
	public void InterpolateToDestinationWithOrientation(Vector3 pos, Vector3 angle, InterpolationMethod met = InterpolationMethod.Linear, float travelTime = 1.0f)
	{

		endPoint = pos;
		endAngle = angle;
		//endPoint.rotation = Quaternion.identity;

		startPoint = target.position;
		startAngle = target.eulerAngles;
		method = met;
		journeyTime = travelTime;
		
		startTime = Time.time;
		haveOrientation = true;
		activation = true;

	}

	public void StartInterpolator(){
		activation = true;
	}
}
