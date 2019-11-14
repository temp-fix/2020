using System.Collections.Generic;
using UnityEngine;

public class SensorConfigLaserSensor : SensorConfigSensor
{
    LineRenderer line;
    LayerMask layersExceptSensor;
    float hitDistance;

    private void Start()
    {
        LineRenderer line = gameObject.AddComponent<LineRenderer>();

        line.SetPosition(0, new Vector3(0, 0, 0));
        line.SetPosition(1, new Vector3(0, 0, Range));
        line.SetWidth(0, 0.2f);
        line.useWorldSpace = false;

        //Ignore Sensor Layer
        layersExceptSensor = ~(1 << LayerMask.NameToLayer("Sensor"));
    }

    private void OnDrawGizmos()
    {
        //This is the same code that the casting will do, so it does acuratly show the ray
        //Vector3 forward = transform.TransformDirection(Vector3.forward) * Range;
        //Debug.DrawRay(transform.position, forward, IsHitting ? SensorColor : Color.gray);
    }

    private void Update()
    {
        RemoveAllObstaclesCurrentlyInDetection();

        RaycastHit hit;
        Ray ray = new Ray(transform.position, transform.TransformDirection(Vector3.forward));

        if (Physics.Raycast(ray, out hit, Range, layersExceptSensor))
            DetermineObjectCollision(hit.collider, hit.distance);

        if (IsHitting())
            this.GetComponent<Renderer>().material.color = SensorColor;
        else
            this.GetComponent<Renderer>().material.color = NotHittingColor;
    }
}