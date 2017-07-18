/// <summary>
/// Calibrates to create the workspace region based on OptiTrack markers.
/// Author: A. Siu
/// July 2, 2017
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableRender : MonoBehaviour {

    public bool UsingRobotPlatform = false;

    public OptitrackStreamingClient StreamingClient;
    private bool initDone = false;
 
	public GameObject table; 
    public Material tableMaterial = null;   // material for the table 
    public float tableLength = 0.8f; //[m]
    public float tableWidth = 0.3f; //[m]

    public GameObject wallFar; 
	public GameObject wallNear; 
	public GameObject wallRight; 
	public GameObject wallLeft; 
    public float wallHeight = 1.0f; //[m]
    public float wallWidth = 0.003f; //[m]

	public GameObject volume;

    public bool HighTable = false;
    private GameObject highTable;

    private Terrain terrain;

	// Use this for initialization
	void Start () {
		
        if ( this.StreamingClient == null ) {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();
            // If we still couldn't find one, disable this component.
            if ( this.StreamingClient == null )
            {
                Debug.LogError( GetType().FullName + ": Streaming client not set, and no " + typeof( OptitrackStreamingClient ).FullName + " components found in scene; disabling this component.", this );
                return;
            }
        }

	}
	
	// Update is called once per frame
	void Update () {
        
        // If OptiTrak is streaming and we haven't initialized 
        if (StreamingClient.streamingFrames && initDone == false) {
            InitTable(); // init pins
            initDone = true;
        }
        else if (StreamingClient.streamingFrames == false ){
            return;
        }
        
	}

    /// <summary>
    /// Initialize the table and workspace objects.
    /// </summary>
    private void InitTable () {
		
        table.transform.parent = this.transform;
        table.transform.localScale = new Vector3(tableLength, 0.0001f, tableWidth);
        table.transform.localPosition = new Vector3(tableLength / 2, 0, tableWidth / 2);

        if ( HighTable) {
            highTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highTable.name = "High Table";
            highTable.transform.parent = this.transform;
            highTable.transform.localScale = new Vector3(tableLength, 0.0001f, tableWidth);
            if ( UsingRobotPlatform ) {
                highTable.transform.localPosition = new Vector3(tableLength / 2, 0.358f, tableWidth / 2);
            } else {
                highTable.transform.localPosition = new Vector3(tableLength / 2, 0.30f, tableWidth / 2);
            }
            highTable.GetComponent<Collider>().enabled = false;
            highTable.GetComponent<Renderer>().material = tableMaterial;
        }

        terrain = Terrain.activeTerrain;
        if ( terrain != null ) {
            terrain.transform.parent = this.transform;
            terrain.transform.localPosition = new Vector3(0, 0.2785f, 0);
            highTable.GetComponent<Renderer>().enabled = false;
        }
        
        float volumeHeight = 1.5f;
        volume.transform.parent = this.transform;
        volume.transform.localScale = new Vector3(tableLength, volumeHeight, tableWidth);
        volume.transform.localPosition = new Vector3(tableLength / 2, volumeHeight/2, tableWidth / 2);
        
        float scalar = 1.0f/0.0001f;
        wallFar.transform.localScale = new Vector3(tableLength, wallHeight*scalar, wallWidth);
        wallFar.transform.localPosition = new Vector3(0, (wallHeight*scalar)/2.0f, 0.5f);
        
        wallNear.transform.localScale = new Vector3(tableLength, wallHeight*scalar, wallWidth);
        wallNear.transform.localPosition = new Vector3(0, (wallHeight*scalar)/2.0f, -0.5f);
        
        wallRight.transform.localScale = new Vector3(wallWidth, wallHeight*scalar, 1.0f);
        wallRight.transform.localPosition = new Vector3(0.5f, (wallHeight*scalar)/2.0f, 0.0f);
        
        wallLeft.transform.localScale = new Vector3(wallWidth, wallHeight*scalar, 1.0f);
        wallLeft.transform.localPosition = new Vector3(-0.5f, (wallHeight*scalar)/2.0f, 0.0f);

    }
    
}
