using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SensorConfigProperties
{
    public Vector2 angles { get; private set; }
    public Vector3 direction { get; private set; }
    public float Range { get; private set; }
    public float FOV { get; private set; }

    public SensorType sensorType;

    public SensorConfigProperties(SensorDimensions dimensions, SensorType type)
    {
        Range = Random.Range(5, 50);
        FOV = Random.Range(5, 25);

        sensorType = type;

        float sensorVert, sensorHor;

        float vert = 0f;

        if (dimensions == SensorDimensions.ThreeD)
        {
            vert = Random.Range(-30f, 30f);
        }

        float hor = Random.Range(0f, 360f);


        sensorVert = Random.Range(-30, 30);
        sensorHor = Random.Range(-90, 90);

        angles = new Vector2(vert, hor);
        direction = new Vector3(sensorVert, sensorHor);
    }

    public SensorConfigProperties(Vector2 ang, Vector3 dir, float range, float fov, SensorType type)
    {
        Range = range;
        FOV = fov;

        angles = ang;
        direction = dir;
        sensorType = type;
    }

    public override bool Equals(object other)
    {
        SensorConfigProperties otherSensor = (SensorConfigProperties)other;

        return sensorType == otherSensor.sensorType &&
            Math.Abs(this.angles.x - otherSensor.angles.x) < 0.00001f &&
                         Math.Abs(this.angles.y - otherSensor.angles.y) < 0.00001f &&
                         Math.Abs(this.direction.x - otherSensor.direction.x) < 0.00001f &&
                         Math.Abs(this.direction.y - otherSensor.direction.y) < 0.00001f &&
                         Math.Abs(this.direction.z - otherSensor.direction.z) < 0.00001f;
    }

    public override string ToString()
    {
        return "(" + angles.x + ", " + angles.y + ") " + " [" + direction.x + ", " + direction.y + "]";
    }
}
