using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class ConeSensor : MonoBehaviour {
	float radius;
	float distance;

	private List<Vector3> pointVel;
	private List<Vector3> pointsOnCone;
	private int nearestCollisionIndex=-3;
	private float nearestDist;
	
	public int getNearestIndex(){
		return nearestCollisionIndex;
	}


	public List<Vector3> getPointVel(){
		return pointVel;
	}

	public void setPrams(float theta, float dist){
		dist = 10;
		radius = Mathf.Tan(theta)*dist;
		//Debug.Log (radius);
		distance = dist;
		//get points on cone
		pointsOnCone = new List<Vector3> ();
		int maxlines = 0;
		for (float x=-radius; x<=radius&&maxlines<10; x+=0.5f) {
			for(float y=-radius;y<=radius&&maxlines<10;y+=0.5f){
				if(x*x+y*y<radius*radius){
					//Debug.Log (x+" "+y);
					pointsOnCone.Add(new Vector3(x,y,dist));
					++maxlines;
				}
			}		
		}

	}





	public List<Vector3> applySensor(){//each collision: pos
		//Debug.Log ("!");
		nearestDist = 100000000000f;
		nearestCollisionIndex = -3; 
		List<Vector3> collisions = new List<Vector3>();
		if (pointsOnCone == null) {
			//Debug.Log ("---------------------");
			return collisions;		
		}
		pointVel = new List<Vector3> ();
		Vector3 forward = transform.TransformDirection(Vector3.forward) * distance;

		Quaternion rotation = Quaternion.FromToRotation (new Vector3(0,0,distance), forward);
		Debug.Log (pointsOnCone.Count);
		int maxTally = 0;
		foreach(Vector3 current in pointsOnCone){
			if(maxTally>10){
				return collisions;
			}
			++maxTally;
			Vector3 currentDir=rotation*current;
			Ray currentRay=new Ray(transform.position,currentDir);
			RaycastHit hit;
			if(Physics.Raycast(currentRay,out hit,Vector3.Magnitude(currentDir))){
				collisions.Add (hit.point);
				ObjectVelocityScript ovs=(ObjectVelocityScript)hit.transform.GetComponent("ObjectVelocityScript");
				Vector3 vel=Vector3.zero;
				if(ovs!=null){
					//Debug.Log ("!!!!!!!!!!!!!!!!!!!!!!!!!!!");
					vel=ovs.getVelocity();
				}
				pointVel.Add(vel);
				objectIndex oi=(objectIndex)hit.transform.GetComponent("objectIndex");
				if(oi!=null){
					if(Vector3.Distance(transform.position,hit.transform.position)<nearestDist){
						nearestCollisionIndex=oi.index;
					}
				}

				Debug.DrawLine(transform.position,hit.point,Color.red);
			}else{
				Debug.DrawRay(transform.position, currentDir, Color.green);
			}
			
		}
		return collisions;
}
}
