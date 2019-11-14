using UnityEngine;

public class RayCaster : MonoBehaviour
{
    public float SensorRange;
    public RaycastHit HitObj { get; private set; }
	public Color RayColour = Color.green;
    // Use this for initialization
    private void Start()
    {
        HitObj = new RaycastHit();
    }

    // Update is called once per frame
    private void Update()
    {
        //Raycsting
        var ray = new Ray(transform.position, transform.TransformDirection(Vector3.forward));
        RaycastHit hit;
        Physics.Raycast(ray, out hit, SensorRange);
        HitObj = hit;
    }

    private void OnDrawGizmos()
    {
        //Draw the ray in the scene view
        Vector3 forward = transform.TransformDirection(Vector3.forward)*SensorRange;
		Debug.DrawRay(transform.position, forward, RayColour);
    }
}