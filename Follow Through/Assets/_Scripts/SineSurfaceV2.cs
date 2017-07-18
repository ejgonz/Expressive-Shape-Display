/*	Generate Sine Surface
 * 	
 * 	Author: Eric Gonzalez
 * 	Date: June 2nd 2017
 * 	Description: Creates and animates mesh, allowing user
 * 	to play with parameters.
 * 
 * 	TODO: implement blockiness and asynchronous movement
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineSurfaceV2 : MonoBehaviour {

	// Display size 
	public float size = 0.19f;
	public int gridSize = 50; // Number of rows/columns in grid

	// Independent params (sliders)
	[Range(0.0f, 0.060f)] 
	public float amp = 0.040f;			// Vertical blob displacement
	[Range(0.010f, 0.200f)]
	public float radius = 0.06f;		// Blob radius
	[Range(0.0f, 0.25f)]
	public float phiStep = 0.1f;	// Speed of sine wave
	[Range(0.1f, 1.5f)]
	public float followThroughSize = 0.3f;
	[Range(0f, 0.030f)]
	public float blockSize = 0f;
	[Range(0, 10)]
	public int AsyncRate = 0;

	// Private variables
	private float k; 					// Wave vector
	private float phiInit = Mathf.PI/2;
	private float phi;
	private float lambda;

	private MeshFilter filter;
	private MeshCollider meshc;

	// Use this for initialization
	void Start () {
		this.gameObject.AddComponent<MeshFilter>();
		MeshFilter filter = GetComponent<MeshFilter> ();
		filter.mesh = GenerateMesh();

		// Initialize parameters
		lambda = radius/followThroughSize;
		k = 2*Mathf.PI/lambda;
		phi = phiInit;
	}

	// Update is called once per frame
	void Update () {
		updateMesh();
	}

	// Extrernal function for generating mesh.
	// Obtained from World of Zero: https://youtu.be/iwsZAg7dReM
	Mesh GenerateMesh()
	{
		Mesh mesh = new Mesh();

		var vertices = new List<Vector3>();
		var normals = new List<Vector3>();
		var uvs = new List<Vector2>();

		// Create vertices
		for (int x = 0; x < gridSize + 1; ++x)
		{
			for (int y = 0; y < gridSize + 1; ++y)
			{
				vertices.Add(new Vector3(-size * 0.5f + size * (x / ((float)gridSize)), 0, -size * 0.5f + size * (y / ((float)gridSize))));
				normals.Add(Vector3.up);
				uvs.Add(new Vector2(x / (float)gridSize, y / (float)gridSize));
			}
		}

		// Create triangles
		var triangles = new List<int>();
		var vertCount = gridSize + 1;
		for (int i = 0; i < vertCount * vertCount - vertCount; ++i)
		{
			if ((i + 1)%vertCount == 0)
			{
				continue;
			}
			triangles.AddRange(new List<int>()
				{
					i + 1 + vertCount, i + vertCount, i,
					i, i + 1, i + vertCount + 1
				});
		}

		// Set mesh
		mesh.SetVertices(vertices);
		mesh.SetNormals(normals);
		mesh.SetUVs(0, uvs);
		mesh.SetTriangles(triangles, 0);

		// Make mesh collider
		meshc = gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		meshc.sharedMesh = mesh; // Give it your mesh here

		return mesh;
	}

	// Updates mesh to create sinusoidal surface 
	void updateMesh() {
		// Reinitialize k in case lambda has changed
		lambda = radius/followThroughSize;
		k = 2*Mathf.PI/lambda;

		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;

		// Get vector of radii
		float[] radii = new float[vertices.Length];
		int i = 0;
		while (i < vertices.Length) {
			radii [i] = Mathf.Sqrt ((vertices[i].x * vertices[i].x)  + (vertices[i].z * vertices[i].z));
			i++;
		}

		// Update y-value of each vertex
		int vertIndex = 0;	
		for (i = 0; i < gridSize + 1; i++) {
			for (int j = 0; j < gridSize + 1; j++) {

				// Discretize radius
				float discretizedR = discretize(radii[vertIndex], blockSize); 

				vertices [vertIndex].y = getSineVal (discretizedR) * getDecay(0f,1f,followThroughSize*lambda,0,discretizedR);
				vertIndex++;
			}
		}


		int randInt = Random.Range(1,10);
		if (randInt >= AsyncRate) {
			// Update phi for next iteration
			phi -= phiStep;
		} else {
			// Jitter at 30% rate
			randInt = Random.Range(1,10);
			if (randInt <= 3) phi += phiStep;
		}
			
		mesh.vertices = vertices;
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();	
		meshc.sharedMesh = mesh;

	}

	//Helper Functions

	/* 	getSineVal
	 * 		Returns y-value for sinusoidal surface given a radius r
	 * 		(where r is previously formed from x,z coordinates)
	 */
	float getSineVal(float r) {
		// Ensure max of 1.5 wavelengths are produced
		//	(produces central blob with at most a single ring of follow-through)
		if (r > followThroughSize * lambda) {
			return 0;
		} else {
			return (((amp / 2) * Mathf.Sin (k * r + phi) + (amp / 2)));
		}
	}

	/* 	getDecay
	 * 		Computes the value of a sine function at x formed from two
	 * 		desired points (x1,y1) and (x2,y2). Returns a multiplier 0->1 
	 * 		(based on this sine function) to serve as the decay factor for 
	 * 		a given r-value. Returns 1 when R is 0, 0 when r = lambda*1.5;
	 */
	float getDecay (float x1,float y1, float x2, float y2, float x) {
		return ((y2-y1)/2)*Mathf.Cos(Mathf.PI*(x-x2)/Mathf.Abs(x2-x1))+((y1+y2)/2);
	}

	/* 	discretize
	 * 		Bins value of r into stepSize increments.
	 * 		Ex. if j*stepSize < r < (j+1)*stepSize, returns j*stepSize
	 */
	float discretize (float r, float stepSize) {
		int step = 0;
		float discreteVal = 0;
		if (stepSize == 0) {
			return r;
		} else {
			while (r > (step + 1) * stepSize) {
				// Take next step
				discreteVal = step * stepSize;
				step++;
			}
		}
		return discreteVal;
	}
}
