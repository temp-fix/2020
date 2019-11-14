using UnityEngine;
using System.Collections;

public class DistanceTravelledCalculator : MonoBehaviour {

	public string StartingMarker;
	public float DistanceTravelled;
	public int timesteps;
	Vector3 lastPosition;
	Vector3 tenSecondsAgoPosition;
	private bool IsRunning;

	// Use this for initialization
	void Start () {
		lastPosition = transform.position;
		tenSecondsAgoPosition = transform.position;
		IsRunning = true;
		timesteps = 0;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (IsRunning) {
			DistanceTravelled += Vector3.Distance (transform.position, lastPosition);
            //	lastPosition = transform.position;
            //	timesteps ++;

            //	//stop vehicle if it has not moved in 5 seconds)
            //	if (timesteps % 250 == 0) {
            //		if((Vector3.Distance(transform.position, tenSecondsAgoPosition) < 20)) {
            //			gameObject.GetComponent<NEATCarInputHandler> ().Stop ();
            //			IsRunning = false;

            //			RayCaster [] raycasters = transform.GetComponentsInChildren<RayCaster>();

            //			foreach (RayCaster raycast in raycasters) {
            //				raycast.RayColour = Color.white;
            //			}

            //		}
            //		tenSecondsAgoPosition = transform.position;
            //	}

        }
    }

        public void Stop () {
		IsRunning = false;
	}
}
