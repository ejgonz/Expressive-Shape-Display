using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// ROBOT LAYOUT
//			    90 deg			
//		  	 BL_________BR
// 180 deg   |	        | ---- 0 deg  [power switch]
//           FL_________FR
//			    270 deg

public class OmniPositionControl : MonoBehaviour {
    
    // shape display object in unity
	public GameObject renderingPlane;

    // Object to be tracked
	public GameObject targetObject;		

    // Robot platform
    public GameObject robot; 
    public Material robotMaterial = null; 
    public float pivotXoffset = 10.0f;
    public float pivotYoffset = 0.5f;
    public float pivotZoffset = 10.0f/1000.0f;

	// Controllers
	public PID PIDx;					// public to edit gains
	public PID PIDz;					// public to edit gains
    
	// Command outputs
	public System.Int16 targetSpeed = 0;	  // public just to view		
	public System.Int16 targetDirection = 0;  // public just to view
	public System.Int16 targetOmega = 0;
	public System.Int16 offsetAngle = 183;

	private bool stopRobot = false;
    
	// Use this for initialization
	void Start () {
        // Make robot a child of shape display so they have the same position
        this.transform.parent = renderingPlane.transform;
        robot.transform.localScale =  new Vector3(0.2556f, 0.1f, 0.2556f);
        robot.transform.localPosition = new Vector3( -pivotXoffset, pivotYoffset-0.05f, pivotZoffset+0.01f );
	}
	
	// Update is called once per frame
	void Update () {
        
		// Get positions
		Vector3 targetPosition = targetObject.transform.position;
		Vector3 currentPosition = this.transform.position + new Vector3( -pivotXoffset, pivotYoffset-0.05f, pivotZoffset+0.01f );
        
		// Use PID for x and z velocity outputs
		float xOutput = PIDx.Update(targetPosition.x,currentPosition.x, Time.deltaTime);
		float zOutput = PIDz.Update(targetPosition.z,currentPosition.z, Time.deltaTime);

		// Compute magnitude
		float outputMagnitude = Mathf.Sqrt(xOutput*xOutput + zOutput*zOutput);

		// Compute direction
		//Vector3 dir = targetPosition - currentPosition;
		Vector3 dir = new Vector3(xOutput, 0, zOutput);				   // direction vector based on outputs
		dir = renderingPlane.transform.InverseTransformDirection(dir); //targetObject.transform.InverseTransformDirection(dir);	// Note: may not need "targetObject"
		float angle = ((Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg) + offsetAngle + 360)%360;

		// Push to commands
        if (stopRobot) {
            targetSpeed= 0;
            targetDirection = 0;
            targetOmega = 0;
        } else {
		    targetSpeed	= (System.Int16)outputMagnitude;
		    targetDirection = (System.Int16)angle;			// TODO: may need to add an offset before sending
		    targetOmega = 0;
        }

	}
    
    /// <summary>
    /// Check collisions with any of the workspace walls.
    /// If there is a colission a flag is set to stop
    /// the robot.
    /// </summary>
    /// <param name="other"></param>
    //void OnCollisionEnter(Collision other)
    //{
    //    if (other.gameObject.tag == "wall")
    //    {
    //        Debug.Log("wall collision!");
    //        this.Stop();
    //    }
    //}

    /// <summary>
    /// Check collisions with any of the workspace walls.
    /// If there is a colission a flag is set to stop
    /// the robot.
    /// </summary>
    /// <param name="other"></param>
    //void OnCollisionExit(Collision other)
    //{
    //    if (other.gameObject.tag == "wall")
    //    {
    //        this.Move();
    //    }
    //}
    
    /// <summary>
    /// Sets a flag to stop the robot.
    /// </summary>
    public void Stop () {
		stopRobot = true;

	}

    public void Move () {
        stopRobot = false;
    }
    
	public byte[] getDataMessage() {
		byte [] speedBytes = BitConverter.GetBytes(targetSpeed);
		byte [] directionBytes = BitConverter.GetBytes(targetDirection);
		byte [] omegaBytes = BitConverter.GetBytes(targetOmega);

		byte[] data = {
			speedBytes [0],
			speedBytes [1],
			directionBytes [0],
			directionBytes [1],
			omegaBytes [0],
			omegaBytes [1]
		};
		return data;
	}

}

