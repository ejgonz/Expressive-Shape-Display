using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotTargetObject : MonoBehaviour {
	
	//public GameObject robot;
	public OmniPositionControl robotobj; 
    public bool debug = false;
    
    /// <summary>
    /// Check if the object is out of the workspace bounds.
    /// </summary>
    /// <param name="other"></param>
    void OnCollisionExit(Collision other)
    {
        if (other.gameObject.tag == "workspaceVolume")
        {
            robotobj.Stop ();
            if (debug) {
                Debug.Log("target out of workspace bounds");
            }
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "workspaceVolume")
        {
            robotobj.Move ();
            if (debug) {
                Debug.Log("target inside of workspace bounds");
            }
        }
    }

    
}
