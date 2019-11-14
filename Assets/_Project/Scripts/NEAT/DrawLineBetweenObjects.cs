using UnityEngine;
using System.Collections;

public class DrawLineBetweenObjects : MonoBehaviour {
	public Color RayColour = Color.red;
	public Transform target;
	Vector3 heading;

	private void OnDrawGizmos()
	{
		//Draw the ray in the scene view
		heading = target.position - transform.position;

//		Debug.DrawRay(transform.position, heading * Vector3.Distance(transform.position, target.position), RayColour, 2, true);
		Debug.DrawLine (transform.position, target.position, RayColour, 1f, false);
	}
}
