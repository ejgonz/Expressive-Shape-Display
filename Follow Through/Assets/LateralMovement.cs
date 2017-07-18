using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LateralMovement : MonoBehaviour {

    public float distancePerSecond = 5.0f/1000; //[m/s]
    public float totalTravel = 155.0f/1000; //[m]
    private float startingPos = 0.0f;
    private float direction = 1.0f;

    // shape display object in unity
	private ShapeCast shapeRenderer;
	public GameObject renderingPlane;

	// Use this for initialization
	void Start () {

		// Get the shape display object
		shapeRenderer = renderingPlane.GetComponent<ShapeCast> ();
        
	}

    private void Update()
    {
       // if (shapeRenderer.initDone) {
            startingPos = this.transform.position.z;
        //}
    }

    private void FixedUpdate()
    {
        //if ( shapeRenderer.initDone ) {
            if ((this.transform.position.z > startingPos + totalTravel )
                || (this.transform.position.z < startingPos - totalTravel ))
            {
                direction = -direction;
            }
            this.transform.Translate(0, 0, direction * distancePerSecond * Time.deltaTime);
        //}
    }
}
