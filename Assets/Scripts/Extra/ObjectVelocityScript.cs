using UnityEngine;
using System.Collections;

public class ObjectVelocityScript : MonoBehaviour {
	private Vector3 velocity=Vector3.zero;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (GetComponent<Rigidbody>() != null) {
			velocity = GetComponent<Rigidbody>().velocity;
		}

	}

	public Vector3 getVelocity(){
		return velocity;

	}
}
