using UnityEngine;
using System.Collections;

public class WheelAlignment : MonoBehaviour {

	public WheelCollider CorrespondingCollider;
	public bool LeftWheels;
	public GameObject SlipPrefab;


	float RotationValue = 0.0f;

	// Update is called once per frame
	void Update () {
		RaycastHit hit = new RaycastHit();

		Vector3 ColliderCenterPoint = CorrespondingCollider.transform.TransformPoint (CorrespondingCollider.center);
	
		if ( Physics.Raycast( ColliderCenterPoint, -CorrespondingCollider.transform.up, out hit, CorrespondingCollider.suspensionDistance + CorrespondingCollider.radius, ~(1 << LayerMask.NameToLayer("Sensor")) )) {
			transform.position = hit.point + (CorrespondingCollider.transform.up * CorrespondingCollider.radius);
		} else {
			transform.position = ColliderCenterPoint - (CorrespondingCollider.transform.up * CorrespondingCollider.suspensionDistance);
		}

		if (LeftWheels) {
			transform.rotation = CorrespondingCollider.transform.rotation * Quaternion.Euler (-180, CorrespondingCollider.steerAngle + 90, -RotationValue);
		} else {
			transform.rotation = CorrespondingCollider.transform.rotation * Quaternion.Euler (0, CorrespondingCollider.steerAngle + 90, RotationValue);
		}

		RotationValue += CorrespondingCollider.rpm * ( 360/60 ) * Time.deltaTime;




		WheelHit CorrespondingGroundHit = new WheelHit();
		CorrespondingCollider.GetGroundHit (out CorrespondingGroundHit);

		// if the slip of the tire is greater than 2.0, and the slip prefab exists, create an instance of it on the ground at
		// a zero rotation.
		if ( Mathf.Abs( CorrespondingGroundHit.sidewaysSlip ) > 1.5 ) {
			if ( SlipPrefab ) {
				Instantiate( SlipPrefab, CorrespondingGroundHit.point, Quaternion.identity );
			}
		}

	}
}
