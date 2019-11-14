using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SensorConfig : MonoBehaviour {

	public LayerMask sensorPlacementLayer;

	// Use this for initialization
	void Start () {
	
	}

	public void initCreateSensorsForDynamicTests(int numberOfConfigurations){
		List<List<GameObject>> laserSensorListForDynamicTesting = new List<List<GameObject>>();
		for(int i = 0; i < numberOfConfigurations; i++){
			laserSensorListForDynamicTesting.Add(new List<GameObject>());
		}			
	}
		
//	public bool createSensor(float range, Vector2 angle, Vector3 direction, float beamAngle, int genotypeIndex) {
//		int beams;
//		float degrees = 1.0f;
//		SensorRay currentRay;
//		RaycastHit hit;
//
//		Vector3 targetPoint;
//		Vector3 currentDirection;
//
//		Vector3 forward = Vector3.forward;
//		Vector3 currentPosition = transform.position;
//		Vector3 pointOnSphere = new Vector3 (4 * Mathf.Sin (angle.x) * Mathf.Cos (angle.y), 4 * Mathf.Sin (angle.y), 4 * Mathf.Cos (angle.x) * Mathf.Cos (angle.y));
//
//		pointOnSphere = Quaternion.FromToRotation (new Vector3 (1, 0, 0), forward) * pointOnSphere;
//		pointOnSphere += transform.position;
//
//		targetPoint = transform.position + new Vector3 (0, 1, 0);
//		currentDirection = (targetPoint - pointOnSphere) * 100;
//
//		currentRay = new SensorRay (pointOnSphere, currentDirection);
//
//		if (Physics.Raycast (currentRay, hit, 100, sensorPlacementLayer)) {
//			if (hit.transform.gameObject.layer == 8) { //TODO:FIND OUT WHY 8
//				Vector3 backDirection = Vector3.Normalize (-currentDirection);
//
//				GameObject newSensor = Instantiate (basicLaserSensor, hit.point + backDir * 0.1, transform.rotation);
//				newSensor.transform.LookAt (pointOnSphere);
//				newSensor.transform.Rotate (direction);
//
//				Component sensorScript = newSensor.GetComponent ("LaserSensor");
//				sensorScript.setParams (beams, degrees, range);
//
//				newSensor.transform.parent = transform; //set parent of sensor to sensor group
//
//				Debug.DrawLine (pointOnSphere - translation, hit.point - translation, Color.blue, 10000f);
//
//				sensorScript.applySensor ();
//
//				int sensorNearestIndex = sensorScript.getNearestIndex ();
//
//				if (sensorScript == -1) {
//					GameObject.Destroy (newSensor);
//					return false;
//				}
//
//				return true;
//			} else {
//				return false;
//			}
//		}
//		transform.position = currentPosition;
//	}
}
