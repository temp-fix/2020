using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class SensorConfigSensor : MonoBehaviour
{
    public float Range { get; set; }
    public float FOV { get; set; }
    public Color SensorColor { get; set; }
    protected Color NotHittingColor = Color.white;
    protected Dictionary<string, Color> detectableObjectTagsAndColor;
    public HashSet<int> DetectedObjects { get; protected set; }
    public Dictionary<GameObject, float> ObstaclesCurrentlyInDetection { get; private set; }

    protected void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Sensor");

        ObstaclesCurrentlyInDetection = new Dictionary<GameObject, float>();

        detectableObjectTagsAndColor = new Dictionary<string, Color>();
        DetectedObjects = new HashSet<int>();
        detectableObjectTagsAndColor.Add("Obstacle", Color.red);
        detectableObjectTagsAndColor.Add("Car_GA_AIDriving", Color.blue);
        detectableObjectTagsAndColor.Add("Wall", Color.magenta);
    }

    protected bool DetermineObjectCollision(Collider collider, float distance)
    {
        if (detectableObjectTagsAndColor.ContainsKey(collider.tag))
        {
            AddObstaclesCurrentlyInDetection(collider.gameObject, distance);

            DetectedObjects.Add(collider.transform.GetInstanceID());
            SensorColor = detectableObjectTagsAndColor[collider.tag];

            return true;
        }

        return false;
    }

    protected void AddObstaclesCurrentlyInDetection(GameObject gameObject, float distance)
    {
        if (ObstaclesCurrentlyInDetection.ContainsKey(gameObject))
            ObstaclesCurrentlyInDetection[gameObject] = distance;
        else
            ObstaclesCurrentlyInDetection.Add(gameObject, distance);
    }

    protected void RemoveObstaclesCurrentlyInDetection(GameObject gameObject)
    {
        ObstaclesCurrentlyInDetection.Remove(gameObject);
    }

    protected void RemoveAllObstaclesCurrentlyInDetection()
    {
        ObstaclesCurrentlyInDetection = new Dictionary<GameObject, float>();
    }

    public bool IsHitting()
    {
        return ObstaclesCurrentlyInDetection.Count > 0;
    }

    public float NearestObstacleDistance()
    {
        if (ObstaclesCurrentlyInDetection.Count > 0)
            return ObstaclesCurrentlyInDetection.OrderBy(kvp => kvp.Value).First().Value;
        else
            return 0;
    }
}
