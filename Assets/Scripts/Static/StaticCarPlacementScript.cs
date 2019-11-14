using UnityEngine;
using System.Collections;

public class StaticCarPlacementScript : MonoBehaviour {
	public int state=1;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnCollisionEnter(Collision collision) {
		//ManagerScript.totalNumberOfCars--;
		//GameObject.Destroy (gameObject);
	}

}
