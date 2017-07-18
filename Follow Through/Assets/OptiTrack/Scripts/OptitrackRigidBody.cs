//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using UnityEngine;


public class OptitrackRigidBody : MonoBehaviour
{
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;

    // Set this to true so we only allow rotation around the y-axis
    public bool ShapeDisplay = false;
    public bool Table = false;
    
    // Variables if we want to lock the display's movement to only the z-axis
    private Vector3 lockPos;
    private Quaternion lockRot;
    private bool lockPosMode = false;


    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if ( this.StreamingClient == null )
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if ( this.StreamingClient == null )
            {
                Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                this.enabled = false;
                return;
            }
        }
    }


    void Update()
    {

        // Press L to lock/unlock the position
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (lockPosMode)
            {
                Debug.Log("Lock pos mode off.");
                lockPosMode = false;
            }
            else
            {
                Debug.Log("Lock pos mode on.");
                lockPosMode = true;
                lockPos = this.transform.localPosition;
                lockRot = this.transform.localRotation;
            }
        }

        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState( RigidBodyId );
        if ( rbState != null )
        {
            this.transform.localPosition = rbState.Pose.Position;
            this.transform.localRotation = rbState.Pose.Orientation;
            if (ShapeDisplay) {
                this.transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
            } else if (Table) {
                this.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
            if ( lockPosMode ) {
                this.transform.localPosition = new Vector3( lockPos.x, lockPos.y, this.transform.localPosition.z );
                this.transform.localRotation = lockRot;
            }
        }

    }
}
