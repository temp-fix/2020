using UnityEngine;
using System.Collections;



//[ExecuteInEditMode()]


public class meshCreator : MonoBehaviour {


	public Material m;
	public PhysicMaterial pyMat;
	public bool spawnCircle;
	public float circRadius;
	public float radStart,radEnd;
	// Use this for initialization
	public GameObject[] vertexList;




	void createMeshFromArray(){


		GameObject g = new GameObject ();
		
		Instantiate (g, new Vector3 (0, 0, 0),Quaternion.identity);

		int checks = vertexList.Length;

		Vector3[] newVertices=new Vector3[checks*2];
		Vector2[] newUV = new Vector2[checks * 2];
		int[] Triangles = new int[checks*2*3*2*2];
		
		for (int i=0; i<checks; i++) {
			Vector3 current=Vector3.zero;
			foreach (Transform child in vertexList[i].transform) {
				current=child.position;
			}
			newVertices[i*2]=current;
			newVertices[i*2+1]=current+new Vector3(0,9f,0);

			newUV[i*2]=current;
			newUV[i*2+1]=current+new Vector3(0,9f,0);
		}

		for (int i=0; i<vertexList.Length; i++) {
			foreach (Transform child in vertexList[i].transform) {
				GameObject.Destroy (child.gameObject);
			}
		}
		
		for (int i=0; i<checks*2; i++) {
			Triangles[i*12]=i%(checks*2);
			Triangles[i*12+1]=(i+1)%(checks*2);
			Triangles[i*12+2]=(i+2)%(checks*2);
			
			Triangles[i*12+3]=(i+1)%(checks*2);
			Triangles[i*12+4]=(i+2)%(checks*2);
			Triangles[i*12+5]=(i+3)%(checks*2);
			
			Triangles[i*12+8]=i%(checks*2);
			Triangles[i*12+7]=(i+1)%(checks*2);
			Triangles[i*12+6]=(i+2)%(checks*2);
			
			Triangles[i*12+11]=(i+1)%(checks*2);
			Triangles[i*12+10]=(i+2)%(checks*2);
			Triangles[i*12+9]=(i+3)%(checks*2);
		}
		
		Mesh mesh = new Mesh ();
		g.transform.GetComponent<MeshFilter> ();
		
		if(!g.transform.GetComponent<MeshFilter> () ||  !g.transform.GetComponent<MeshRenderer> () ) //If you will havent got any meshrenderer or filter
		{
			g.transform.gameObject.AddComponent<MeshFilter>();
			g.transform.gameObject.AddComponent<MeshRenderer>();
		}
		
		g.transform.GetComponent<MeshFilter> ().mesh = mesh;
		
		mesh.name = "MyOwnObject";
		
		mesh.vertices = newVertices;
		mesh.triangles = Triangles;
		mesh.uv = newUV;
		g.transform.gameObject.GetComponent<Renderer>().material = m;
		
		
		mesh.RecalculateNormals ();
		;
		//transform.gameObject.renderer.material = material; //If you want a material.. you have it :)
		MeshCollider meshc = g.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		meshc.GetComponent<Collider>().material = pyMat;
		meshc.sharedMesh = mesh; 


	}
	void createCircle(float radius, float height, float radS, float radE){
		radS *= Mathf.PI;
		radE *= Mathf.PI;
		GameObject g = new GameObject ();

		Instantiate (g, new Vector3 (0, 0, 0),Quaternion.identity);

		float angleStepSize = 0.02f*Mathf.PI;
		
		int checks = Mathf.RoundToInt (2 * (radE-radS) / angleStepSize);
		Vector3[] newVertices=new Vector3[checks*2];
		Vector2[] newUV = new Vector2[checks * 2];
		int[] Triangles = new int[checks*2*3*2];
		Vector3 currentpos = transform.position;
		for (int i=0; i<checks; i++) {
			newVertices[i*2]=currentpos+new Vector3(radius*Mathf.Sin (i*angleStepSize+radS),0f,radius*Mathf.Cos (i*angleStepSize+radS));
			newVertices[i*2+1]=currentpos+new Vector3(radius*Mathf.Sin (i*angleStepSize+radS),height,radius*Mathf.Cos (i*angleStepSize+radS));

			newUV[i*2]=new Vector2(radius*Mathf.Sin (i*angleStepSize+radS),radius*Mathf.Cos (i*angleStepSize+radS));
			newUV[i*2+1]=new Vector2(radius*Mathf.Sin (i*angleStepSize+radS),radius*Mathf.Cos (i*angleStepSize+radS));
		}
		
		for (int i=0; i<checks; i++) {
			Triangles[i*12]=i%(checks*2);
			Triangles[i*12+1]=(i+1)%(checks*2);
			Triangles[i*12+2]=(i+2)%(checks*2);
			
			Triangles[i*12+3]=(i+1)%(checks*2);
			Triangles[i*12+4]=(i+2)%(checks*2);
			Triangles[i*12+5]=(i+3)%(checks*2);
			
			Triangles[i*12+8]=i%(checks*2);
			Triangles[i*12+7]=(i+1)%(checks*2);
			Triangles[i*12+6]=(i+2)%(checks*2);
			
			Triangles[i*12+11]=(i+1)%(checks*2);
			Triangles[i*12+10]=(i+2)%(checks*2);
			Triangles[i*12+9]=(i+3)%(checks*2);
		}
		
		Mesh mesh = new Mesh ();
		g.transform.GetComponent<MeshFilter> ();
		
		if(!g.transform.GetComponent<MeshFilter> () ||  !g.transform.GetComponent<MeshRenderer> () ) //If you will havent got any meshrenderer or filter
		{
			g.transform.gameObject.AddComponent<MeshFilter>();
			g.transform.gameObject.AddComponent<MeshRenderer>();
		}
		
		g.transform.GetComponent<MeshFilter> ().mesh = mesh;
		
		mesh.name = "MyOwnObject";
		
		mesh.vertices = newVertices;
		mesh.triangles = Triangles;
		mesh.uv = newUV;
		g.transform.gameObject.GetComponent<Renderer>().material = m;
		

		mesh.RecalculateNormals ();
		;
		//transform.gameObject.renderer.material = material; //If you want a material.. you have it :)
		MeshCollider meshc = g.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		meshc.GetComponent<Collider>().material = pyMat;
		meshc.sharedMesh = mesh; 
	
	}


	void Start () {
		//createCircle (80, 10);
		//createCircle (120, 10);
		if (spawnCircle) {
			createCircle (circRadius, 10,radStart,radEnd);
		}else{

			createMeshFromArray ();}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
