using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class CubeSection : MonoBehaviour {
	public float width;
	public Vector3 leftCorner;
	public Vector3[] points;
	public List<List<Vector3> > neighbours=new List<List<Vector3> >();
	public void createPoints(){
		Random.seed = (int)leftCorner.magnitude;
		int numberOfPoints = Random.Range (1, 4);

		points = new Vector3[numberOfPoints];
		for(int i=0;i<numberOfPoints;i++){
			points[i]=new Vector3(Random.value,0,Random.value)*width+leftCorner;

			neighbours.Add(new List<Vector3>());
		}
		if (leftCorner.magnitude < 0.000001f) {//if at origin

			points[0]=new Vector3(0,0,0);
		}
	}




}
