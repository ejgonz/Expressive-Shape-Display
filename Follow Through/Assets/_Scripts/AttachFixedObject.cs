/// <summary>
/// 
/// DEPRECATED - Instead tag the object with 'Attach' keyword.
/// 
/// Attach this to a game object to keep its position 
/// relative to the shape display.
/// Author: A. Siu
/// </summary>
/// 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this script to object you want to fix to parent object.
/// </summary>
public class AttachFixedObject : MonoBehaviour {
    
    // shapeRenderer is the parent object to which this obj will be fixed to
    ShapeCast shapeRenderer;
	public GameObject renderingPlane; 
	public float xOffset = 0.0f;
	public float yOffset = 0.0f;
	public float zOffset = 0.0f;

    // the position offset pos relative to the parent
    public Vector3 offset;

    public bool ManuallyPosition = true;
    
	void Start () {
		if (renderingPlane == null) renderingPlane = GameObject.Find("RenderingPlane");
        shapeRenderer = renderingPlane.GetComponent<ShapeCast>(); 
        this.transform.parent = shapeRenderer.transform;

        // define offset relative to shape display dimensions
        if (ManuallyPosition) {
		    offset = new Vector3 (-this.transform.position.x, -this.transform.position.y, -this.transform.position.z);
        } else {
            offset = new Vector3 (shapeRenderer.totalWidth/2+xOffset, shapeRenderer.initialY+yOffset, -shapeRenderer.totalHeight / 2 - shapeRenderer.pinSpacing + zOffset);
        }
	}
	
	void Update () {
        //offset = new Vector3 (shapeRenderer.totalWidth/2+xOffset, shapeRenderer.initialY+yOffset, -shapeRenderer.totalHeight / 2 - shapeRenderer.pinSpacing + zOffset);
        this.transform.position = shapeRenderer.transform.position - offset;
	}
}
