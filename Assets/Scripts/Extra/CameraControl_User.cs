using UnityEngine;
using System.Collections;

public class CameraControl_User: MonoBehaviour
{
	float speed = 25;

	void Start ()
	{
		
	}
	
	void Update ()
	{
		if (Input.GetKey (KeyCode.RightArrow) & Input.GetKey (KeyCode.LeftShift)) {
			transform.Rotate (new Vector3 (0, 0, speed * Time.deltaTime));
		} else if (Input.GetKey (KeyCode.RightArrow)) {
			transform.Translate (new Vector3 (speed * Time.deltaTime, 0, 0));
		}
		
		if (Input.GetKey (KeyCode.LeftArrow) & Input.GetKey (KeyCode.LeftShift)) {
			transform.Rotate (new Vector3 (0, 0, -speed * Time.deltaTime));
		} else if (Input.GetKey (KeyCode.LeftArrow)) {
			transform.Translate (new Vector3 (-speed * Time.deltaTime, 0, 0));
		}
		
		if (Input.GetKey (KeyCode.DownArrow) & Input.GetKey (KeyCode.LeftShift)) {
			transform.Rotate (new Vector3 (-speed * Time.deltaTime, 0, 0));
		} else if (Input.GetKey (KeyCode.DownArrow) & Input.GetKey (KeyCode.LeftControl)) {
			transform.Translate (new Vector3 (0, 0, -speed * Time.deltaTime)); //Zoom
		} else if (Input.GetKey (KeyCode.DownArrow)) {
			transform.Translate (new Vector3 (0, -speed * Time.deltaTime, 0));
		}
		
		if (Input.GetKey (KeyCode.UpArrow) & Input.GetKey (KeyCode.LeftShift)) {
			transform.Rotate (new Vector3 (speed * Time.deltaTime, 0, 0));
		} else if (Input.GetKey (KeyCode.UpArrow) & Input.GetKey (KeyCode.LeftControl)) {
			transform.Translate (new Vector3 (0, 0, speed * Time.deltaTime)); //Zoom
		} else if (Input.GetKey (KeyCode.UpArrow)) {
			transform.Translate (new Vector3 (0, speed * Time.deltaTime, 0));
		}
	}
}
