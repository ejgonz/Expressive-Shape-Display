using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateFollowThroughObject : MonoBehaviour {

	// Public Variables
	public bool useJellyMesh;
	public bool squashAndStretch;
	public enum Form {
		Cube,
		Sphere
	}
	public Form m_Form = Form.Cube;
	public Material	material;

	public int springStiffness	=	300;
	public float springDamper	=	0.2f;
	public float bodyMass 		= 	0.06f;
	public int bodyDrag 		= 	5;
	private float spScale 		= 	0.75f;

	public Vector3 impulse;

	public int jellyStiffness = 100;
	public float jellyDamping = 2.0f;
	public float jellyMass = 10f;
    public float jellyDrag = 6f;

	public float squashIntensity = 60f;

	// Private Variables
	private GameObject leader;		// Should be kinematic
	private GameObject follower;
	private GameObject jelly;
	private GameObject topCorners;
	private GameObject centerRefPoint;
	private List<GameObject> corners;

	void Start () {
		leader = gameObject;

        if (!useJellyMesh) {
		    // Create object
		    if (m_Form == Form.Sphere) {
			    follower = GameObject.CreatePrimitive (PrimitiveType.Sphere);
		    } else {
			    follower = GameObject.CreatePrimitive (PrimitiveType.Cube);
		    }
		    follower.name = "Follower";
		    follower.GetComponent<MeshRenderer> ().material = material;

		    // Set as rigid body
		    follower.AddComponent<Rigidbody> ();
		    follower.GetComponent<Rigidbody> ().mass = bodyMass;
		    follower.GetComponent<Rigidbody> ().drag = bodyDrag;
		    follower.GetComponent<Rigidbody> ().useGravity = false;
		    follower.GetComponent<Rigidbody> ().constraints = (RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ);

		    // Position over parent object
		    follower.transform.position = leader.transform.position;
		    follower.transform.localScale = leader.transform.localScale;

		    // Create 8 springs, attach to each corner and center
		    for (float x = -1f; x <= 1f; x = x + 2) {
			    for (float y = -1f; y <= 1f; y = y + 2) {
				    for (float z = -1f; z <= 1f; z = z + 2) {
					    SpringJoint sp = follower.AddComponent<SpringJoint> ();
					    sp.connectedBody = leader.GetComponent<Rigidbody> ();
					    sp.autoConfigureConnectedAnchor = false;
					    sp.anchor = new Vector3(spScale*x,spScale*y,spScale*z);
					    sp.connectedAnchor = new Vector3(spScale*x,spScale*y,spScale*z);
					    sp.spring = springStiffness;
					    sp.damper = springDamper;
					    sp.tolerance = 0.006f;
				    }
			    }
		    }
		    SpringJoint sp_center = follower.AddComponent<SpringJoint> ();
		    sp_center.connectedBody = leader.GetComponent<Rigidbody> ();
		    sp_center.autoConfigureConnectedAnchor = false;
		    sp_center.anchor = new Vector3(0,0,0);
		    sp_center.connectedAnchor = new Vector3(0,0,0);
		    sp_center.spring = springStiffness;
		    sp_center.spring = springDamper;

            // Add Squash & Stretch
			if (squashAndStretch) {
				follower.AddComponent<SquashAndStretch> ();
			}

        } else {
			// Remove renderer of follower
			//follower.GetComponent<MeshRenderer>().enabled = false;
			//follower.GetComponent<Collider> ().enabled = false;

			// Create jelly object
			if (m_Form == Form.Sphere) {
				jelly = GameObject.CreatePrimitive (PrimitiveType.Sphere);
			} else {
				jelly = GameObject.CreatePrimitive (PrimitiveType.Cube);
			}
			jelly.name = "JellyObject";
			jelly.GetComponent<Collider> ().enabled = false;

			// Position over follower
			jelly.transform.position = leader.transform.position;

			// Add jelly mesh
			jelly.AddComponent<JellyMesh> ();
			JellyMesh jm = jelly.GetComponent<JellyMesh> ();
			jm.m_Style = JellyMesh.PhysicsStyle.Cube;
			jm.m_UseGravity = false;
			jm.m_MeshScale = leader.transform.localScale;

			jm.m_Drag = jellyDrag;
			jm.m_AngularDrag = 1;
			jm.m_LockRotationX = true;
			jm.m_LockRotationZ = true;
			jm.m_LockPositionY = true;		// TODO: make this a variable
			jm.m_Interpolation = RigidbodyInterpolation.Interpolate;

			jm.m_Stiffness = jellyStiffness;	// 500
			jm.m_DampingRatio = jellyDamping;	// 10
			jm.m_Mass = jellyMass;				// 1

			// Attach reference points
			topCorners = new GameObject("Top Corners");
			corners = new List<GameObject>();
			foreach (JellyMesh.ReferencePoint rp in jm.m_ReferencePoints) {
				GameObject rpGameObj = rp.GameObject;
				if (transform.InverseTransformPoint (rpGameObj.transform.position).y <= 0) {
					FixedJoint fj = rpGameObj.AddComponent<FixedJoint> ();
					fj.connectedBody = leader.GetComponent<Rigidbody> ();

					// Store center point
					if (transform.InverseTransformPoint (rpGameObj.transform.position).y == 0) {
						centerRefPoint = rpGameObj;
						topCorners.transform.position = centerRefPoint.transform.position;
					}

				} else {
					// Group top corners together
					rpGameObj.transform.SetParent(topCorners.transform);
					corners.Add (rpGameObj);
				}
			}

		} 

		// Initialize impulse
		impulse = new Vector3(0,50,0);
	}

	void FixedUpdate () {

		
		if (useJellyMesh) {
            // Rotate top Jelly Corners, if rotated
			RotateTopCorners ();
            
            // Update jelly characteristics
            jelly.GetComponent<JellyMesh> ().m_Stiffness = jellyStiffness;	// 500
			jelly.GetComponent<JellyMesh> ().m_DampingRatio = jellyDamping;	// 10
			jelly.GetComponent<JellyMesh> ().m_Mass = jellyMass;			// 1
            jelly.GetComponent<JellyMesh> ().m_Drag = jellyDrag;

            // Impulse top jelly corners
            if ( Input.GetKeyDown(KeyCode.Space) )
		    {
		        foreach (GameObject corner in corners) {
			        corner.GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);
		        }
		    }

		} else {
            // Update spring stiffness, dampening and mass
		    follower.GetComponent<Rigidbody> ().mass = bodyMass;
		    follower.GetComponent<Rigidbody> ().drag = bodyDrag;

            // Squash and Stretch
		    if (squashAndStretch) {
			    follower.GetComponent<SquashAndStretch> ().intensity = squashIntensity;
		    }

		    SpringJoint[] springs = follower.GetComponents<SpringJoint>();
		    foreach (SpringJoint spring in springs) {
			    spring.spring = springStiffness;
			    spring.damper = springDamper;
		    }

		    if ( Input.GetKeyDown(KeyCode.Space) )
		    {
			    follower.GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);
		    }
        }


	}

	void RotateTopCorners() {
		Quaternion prevRotation;

		// Detatch and move
		prevRotation = topCorners.transform.rotation;
		topCorners.transform.DetachChildren ();
		topCorners.transform.position = centerRefPoint.transform.position;

		// Reattach
		foreach (GameObject corner in corners) {
			corner.transform.SetParent(topCorners.transform);
		}

		// Rotate
		topCorners.transform.rotation = prevRotation;

		
	}
}
