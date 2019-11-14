using UnityEngine;
using System.Collections;

public class BrakePadAlignment : MonoBehaviour {

	public WheelCollider CorrespondingCollider;
	public bool LeftWheels;
	
	// Update is called once per frame
	void Update () {
		if (LeftWheels) {
			transform.rotation = CorrespondingCollider.transform.rotation * Quaternion.Euler (180, CorrespondingCollider.steerAngle + 90, 0);
		} else {
			transform.rotation = CorrespondingCollider.transform.rotation * Quaternion.Euler (0, CorrespondingCollider.steerAngle + 90, 0);
		}
	}
}
