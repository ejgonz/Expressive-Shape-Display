using UnityEngine;
using System.Collections;

public class LaserScript : MonoBehaviour
{
	public LineRenderer laserLineRenderer;
   
	void Start() {
		Vector3[] initLaserPositions = new Vector3[ 2 ] { Vector3.zero, Vector3.zero };
	}

	void Update() 
	{
		if (Input.GetMouseButton (0)) { 
			ShootLaserFromTargetPosition (this.gameObject.transform.position, this.gameObject.transform.up, 5.0f);
			laserLineRenderer.enabled = true;
		} else {
			laserLineRenderer.enabled = false;
		}
	}

	void ShootLaserFromTargetPosition( Vector3 targetPosition, Vector3 direction, float length )
	{
		Ray ray = new Ray( targetPosition, direction );
		RaycastHit raycastHit;
		Vector3 endPosition = targetPosition + ( length * direction );

		if( Physics.Raycast( ray, out raycastHit, length ) ) {
			endPosition = raycastHit.point;
		}

		laserLineRenderer.SetPosition( 0, targetPosition );
		laserLineRenderer.SetPosition( 1, endPosition );
	}
}