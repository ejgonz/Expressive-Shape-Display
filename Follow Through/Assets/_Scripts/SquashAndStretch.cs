using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquashAndStretch : MonoBehaviour {

	public float intensity = 100.0f;
	public float velocityThreshold = 0.5f;
	public bool useAcceleration = false;

	private Rigidbody rb;
	private float velX;
	private float velY;
	private float velZ;
	private float squash;
	private float xScale;
	private float yScale;
	private float zScale;
	private float originalScaleX;
	private float originalScaleY;
	private float originalScaleZ;
	private Quaternion directionTraveling;

	private GameObject parentSquasher;
	private GameObject squashee;
	private Quaternion childRotation;

	// For acceleration calculations
	private float accel;
	private float avgAccel;
	private float thisVel;
	private float lastVel = 0;
	private int windowSize = 3;

	private Queue accelWindow;

	void Start ()
	{
		accelWindow = new Queue ();

		// Create an empty parent for the target object
		squashee = gameObject;
		parentSquasher = new GameObject ("SquashParent");
		squashee.transform.SetParent (parentSquasher.transform);

		rb = GetComponent<Rigidbody>();
		originalScaleX = transform.localScale.x;
		originalScaleY = transform.localScale.y;
		originalScaleZ = transform.localScale.z;
	}

	void Update () 
	{
		// Calculate simple moving average for acceleration
		thisVel = rb.velocity.magnitude;
		accel = (thisVel - lastVel) / Time.deltaTime;

		accelWindow.Enqueue (accel);
		if (accelWindow.Count > windowSize) {
			accelWindow.Dequeue ();
		}

		float sum = 0;
		foreach (float a in accelWindow) {
			sum += a;
		}
		avgAccel = sum / accelWindow.Count;

	}
		
	void FixedUpdate ()
	{
		if (useAcceleration) {
			squash = (avgAccel / intensity);
		} else {
			squash = (rb.velocity.magnitude / intensity);
		}
		xScale = ((squash / -2) + originalScaleX);
		yScale = ((squash / -2) + originalScaleY);
		zScale = (squash + originalScaleZ);

		if (rb.velocity.magnitude > velocityThreshold) {

			directionTraveling = Quaternion.LookRotation(rb.velocity);

			// Remove relationship
			parentSquasher.transform.DetachChildren ();
			// Position and rotate
			parentSquasher.transform.position = squashee.transform.position;
			parentSquasher.transform.rotation = directionTraveling;
			// Reinstate relationship
			squashee.transform.SetParent (parentSquasher.transform);

		}

		// Scale parent
		parentSquasher.transform.localScale = new Vector3 (xScale, yScale, zScale);

		// Make sure child scale is 1
		squashee.transform.localScale = new Vector3(1 ,1, 1);
	}
}
