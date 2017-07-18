using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class TerrainManipulate : MonoBehaviour {
    // Vars for Terrain Data and Height
	public Terrain terrain;
	TerrainData tData;

	int heightmapWidth; // width of terrain map
	int heightmapHeight; // height of terrain map

    public float speed = 0.5f;
    private float maxDist = 0.1f;
    private float leastDist = 0.06f;
	float currentHeight = 0.0f; // initial height before runtime
	float[,] originalHeights; // initial array of heights for each coordinate in terrain map
    float activeThreshold = 0.25f;

    private GameObject wandTop;
    private GameObject wandBottom;
    private GameObject wandMesh;
    //private GameObject lowerWandTop;
    //private GameObject lowerWandBottom;

    public Material raiseWandMaterial;
    public Material lowerWandMaterial;
    public Material neutralWandMaterial;
    private Ray ray;

    public bool debug = false;

	// Use this for initialization
	void Start () {
        // Set the Terrain, resetting height and flattening at runtime
        terrain = Terrain.activeTerrain;
		tData = terrain.terrainData;
    
        wandTop = GameObject.Find("WandTop");
        wandBottom = GameObject.Find("WandBottom");
        wandMesh = GameObject.Find("WandMesh");
        //lowerWandTop = GameObject.Find("LowerWandTop");
        //lowerWandBottom = GameObject.Find("LowerWandBottom");
        
        heightmapHeight = tData.heightmapHeight;
		heightmapWidth = tData.heightmapWidth;

		originalHeights = tData.GetHeights(0,0, heightmapWidth, heightmapHeight);

        terrain.heightmapMaximumLOD = 0;

		for (int y = 0; y < heightmapHeight; y++) {
			for (int x = 0; x < heightmapWidth; x++) {
				originalHeights[x,y] = 0.05f; // set initial terrain height to 0.05f 
			}
		}
		tData.SetHeights(0, 0, originalHeights);
	}
	
	// Update is called once per frame
	void Update () {
        
        float raiseWandHeight = wandTop.transform.position.y;
        //float lowerWandHeight = lowerWandTop.transform.position.y;

        //if (raiseWandHeight < activeThreshold && lowerWandHeight < activeThreshold) { 
        //    raiseWandTop.SetActive(true);
        //    lowerWandTop.SetActive(true);
        //    ray = new Ray();
        //}
        //else if (raiseWandHeight >= activeThreshold && lowerWandHeight < activeThreshold) {
        //    raiseWandTop.SetActive(true);
        //    lowerWandTop.SetActive(false);
        //    ray = new Ray(raiseWandTop.transform.position, -raiseWandTop.transform.up);
        //}
        //else if (lowerWandHeight >= activeThreshold && raiseWandHeight < activeThreshold) {
        //    raiseWandTop.SetActive(false);
        //    lowerWandTop.SetActive(true);
        //    ray = new Ray(lowerWandTop.transform.position, -lowerWandTop.transform.up);
        //}
        ray = new Ray(wandTop.transform.position, -wandTop.transform.up);

		RaycastHit hit;
		if (Physics.Raycast (ray, out hit)) {
            
			int terrainX = (int)((hit.point.x-terrain.transform.position.x)*heightmapWidth/tData.size.x);
			int terrainY = (int)((hit.point.z-terrain.transform.position.z)*heightmapHeight/tData.size.z);
			int size = 10;
            
            /*
            // Calculate distance between wand markers
            if ( lowerWandTop.activeSelf ) {
              if (debug) {
                    Debug.DrawRay(lowerWandTop.transform.position, -lowerWandTop.transform.up, Color.red);
                }
                speed = 400.0f*((maxDist+leastDist)/2.0f-Vector3.Distance(raiseWandTop.transform.position, raiseWandBottom.transform.position));
                
            } else if ( raiseWandTop.activeSelf ) {
                if (debug) {
                    Debug.DrawRay(raiseWandTop.transform.position, -raiseWandTop.transform.up, Color.red);
               }*/
            speed = 400.0f*((maxDist+leastDist)/2.0f-Vector3.Distance(wandTop.transform.position, wandBottom.transform.position));
            if (speed >= 0.5f) {
                wandMesh.GetComponent<Renderer>().material = raiseWandMaterial; 
            }
            else if (speed > -4.0f && speed < 0.5f) {
                speed = 0.0f;
                wandMesh.GetComponent<Renderer>().material = neutralWandMaterial; 
            }
            else {
                wandMesh.GetComponent<Renderer>().material = lowerWandMaterial; 
            }
          //  }
			float[,] heights = tData.GetHeights (0, 0, heightmapWidth, heightmapHeight);
			for (int x = 0; x < heightmapWidth; x++) {
				for (int y = 0; y < heightmapHeight; y++) {
					float currentRadiusSqr = (new Vector2 (terrainX, terrainY) - new Vector2 (x, y)).sqrMagnitude;
					if (currentRadiusSqr < size * size) {
						float newHeight = speed*((size*size-currentRadiusSqr)/62500.0f);
						heights [y, x] += newHeight;
					}
				}
			}
			tData.SetHeights (0, 0, heights); // update heights for terrain map
		}
	}

}
			
