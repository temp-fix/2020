using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class LaserSensor : MonoBehaviour
{
	public bool detectsObstacle = false;
	public int numberOfBeams = 1;
	public float degrees = 0;
	public float distance = 100000f;
	public int index = -1;

	public void setPrams (int nob, float deg, float dist, int ind)
	{
		numberOfBeams = nob;
		degrees = deg;
		distance = dist;
		index = ind;
	}
	
	public void showSensorLines ()
	{
		Vector3 forward = transform.TransformDirection (Vector3.forward) * distance;
		Vector3 currentDir = Quaternion.AngleAxis (-degrees / 2, transform.TransformDirection (Vector3.up)) * forward;
		for (int i=0; i<numberOfBeams; i++) {
			Debug.DrawRay (transform.position, currentDir, Color.green);	
			if (numberOfBeams != 1) {
				currentDir = Quaternion.AngleAxis (degrees / (numberOfBeams - 1), transform.TransformDirection (Vector3.up)) * currentDir;
			}
		}
	}
	
	private List<Vector3> pointVel;
	private int nearestCollisionIndex = -3;
	private float nearestDist;
	
	public int getNearestIndex ()
	{
		return nearestCollisionIndex;
	}
	
	public List<Vector3> getPointVel ()
	{
		return pointVel;
	}
	
	public List<Vector3> applySensor ()
	{
		//Debug.Log (transform.position);
		nearestDist = 100000000000f;
		nearestCollisionIndex = -3; 
		List<Vector3> collisions = new List<Vector3> ();
		pointVel = new List<Vector3> ();
		Vector3 forward = transform.TransformDirection (Vector3.forward) * distance;
		Vector3 currentDir = Quaternion.AngleAxis (-degrees / 2, transform.TransformDirection (Vector3.up)) * forward;
		
		for (int i=0; i<numberOfBeams; i++) {
			Ray currentRay = new Ray (transform.position, currentDir);
			RaycastHit hit;
			if (Physics.Raycast (currentRay, out hit, distance)) {
				
				collisions.Add (hit.point);
				ObjectVelocityScript ovs = (ObjectVelocityScript)hit.transform.GetComponent ("ObjectVelocityScript");
				Vector3 vel = Vector3.zero;
				if (ovs != null) {
					vel = ovs.getVelocity ();
				}
				pointVel.Add (vel);
				objectIndex oi = (objectIndex)hit.transform.GetComponent ("objectIndex");
				if (oi != null) {
					if (Vector3.Distance (transform.position, hit.transform.position) < nearestDist) {
						nearestCollisionIndex = oi.index;
					}
				}
				Debug.DrawLine (transform.position, hit.point, Color.red);

			} else {
				 Debug.DrawRay(transform.position, currentDir, Color.green);
			}
			if (numberOfBeams != 1) {
				currentDir = Quaternion.AngleAxis (degrees / (numberOfBeams - 1), transform.TransformDirection (Vector3.up)) * currentDir;
			}
		}
		
		return collisions;
		//show ray
		//Debug.DrawLine(Camera.main.ScreenPointToRay(Input.mousePosition),hit.point,Color.green);
		//
		
	}
	
	public DataPoint applySensorForAllDynamicTestData(){
		nearestDist = 100000000000f;
		nearestCollisionIndex = -3; 
		
		Vector3 forward = transform.TransformDirection (Vector3.forward) * distance;
		Vector3 currentDir = Quaternion.AngleAxis (-degrees / 2, transform.TransformDirection (Vector3.up)) * forward;
		//List<Vector3> ret=new List<Vector3>();
		DataPoint ret = null;
		for (int i=0; i<numberOfBeams; i++) {
			Ray currentRay = new Ray (transform.position, currentDir);
			RaycastHit hit;
			if (Physics.Raycast (currentRay, out hit, distance)) {
				ObjectVelocityScript ovs = (ObjectVelocityScript)hit.transform.GetComponent ("ObjectVelocityScript");
				Vector3 vel = Vector3.zero;
				if (ovs != null) {
					vel = ovs.getVelocity ();
				}
				
				objectIndex oi = (objectIndex)hit.transform.GetComponent ("objectIndex");
				if (oi != null) {
					if (Vector3.Distance (transform.position, hit.transform.position) < nearestDist) {
						nearestCollisionIndex = oi.index;

						Vector3 relPos=currentDir;
						relPos.Normalize();
						relPos*=Vector3.Distance (transform.position, hit.transform.position);
						//ret.Add (relPos);
						//ret.Add(transform.parent.rigidbody.velocity-vel);
						//ret.Add (new Vector3(oi.index,0,0));
						ret=new DataPoint(transform.parent.GetComponent<Rigidbody>().velocity-vel,relPos,oi.index);
					}
				}
				Debug.DrawLine (transform.position, hit.point, Color.red, 1, true);
			} else {
				Debug.DrawRay(transform.position, currentDir, Color.green,1, true);
			}
			if (numberOfBeams != 1) {
				currentDir = Quaternion.AngleAxis (degrees / (numberOfBeams - 1), transform.TransformDirection (Vector3.up)) * currentDir;
			}
		}
		/*if (ret.Count > 0) {
			Debug.Log ("----");
			Debug.Log (ret[0]);
			Debug.Log (ret[1]);
			Debug.Log (ret[2]);
		}*/

		return ret;
	}
	
	
	
	
	
	
	
	
	
	
	//SortedList<int,int> w=new SortedList<int, int>();
	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		//showSensorLines ();
		
		/*
		Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
		Debug.Log (transform.position);
		Debug.Log (forward);
		Debug.DrawRay (transform.position, forward, Color.green);
		*/
	}
}