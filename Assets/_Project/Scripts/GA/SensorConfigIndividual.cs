using System.Collections.Generic;
using UnityEngine;

public class SensorConfigIndividual : MonoBehaviour
{
    private float fitness;

    public GameObject[] Phenome { get; private set; }
    //The config of the sensors, has the SensorConfigLaserSensor script attached to it
    public SensorConfigGenome Genes { get; private set; }
    public float Fitness { get { if (fitness == -1) fitness = GetFitnessFromSensors(); return fitness; } private set { } }

    private LayerMask carLayer;

    private void Awake()
    {
        carLayer = (1 << LayerMask.NameToLayer("CarSensorApplication"));
    }

    private float GetFitnessFromSensors()
    {
        fitness = 0;
        foreach (GameObject sensor_game_obj in Phenome)
        {
            if (sensor_game_obj.GetComponent<SensorConfigLaserSensor>() != null)
                fitness += sensor_game_obj.GetComponent<SensorConfigLaserSensor>().DetectedObjects.Count;

            if (sensor_game_obj.GetComponent<SensorConfigRadarSensor>() != null)
                fitness += sensor_game_obj.GetComponent<SensorConfigRadarSensor>().DetectedObjects.Count;
        }
        return fitness;
    }

    public void SetSensorConfigPhenomeUsingGenomeSurface(SensorConfigGenome genome, GameObject car)
    {
        Genes = genome;
        fitness = -1;

        //Will hold the sensors in the phenome
        GameObject sensor_config_holder_game_obj = new GameObject("Phenome");

        //Set this transform as the parent obj for the container
        sensor_config_holder_game_obj.transform.SetParent(transform);

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 translation = new Vector3(0, 0, 0);
        transform.position += translation;

        //Make a game obj that has the SensorConfigLaserSensor script attached to it. This is the phenotypic representataion of the gene (the mapping).
        Phenome = new GameObject[genome.Genome.Length];

        for (int phenotype = 0; phenotype < Phenome.Length; phenotype++)
        {
            GameObject sensor_game_obj = new GameObject("Phenotype: Sensor " + phenotype);

            sensor_game_obj.transform.SetParent(sensor_config_holder_game_obj.transform);

            var car_gameObj = Instantiate(car, transform.position, transform.rotation) as GameObject;
            car_gameObj.SetActive(false);

            car_gameObj.layer = LayerMask.NameToLayer("CarSensorApplication");
            foreach (Transform child in car_gameObj.transform)
                child.gameObject.layer = LayerMask.NameToLayer("CarSensorApplication");

            car_gameObj.SetActive(true);

            Ray currentRay = CastRayOnVehicleBody(genome.Genome[phenotype], forward);
            RaycastHit hit;
            Physics.Raycast(currentRay, out hit, 100, carLayer);

            Destroy(car_gameObj);

            Vector3 backDir = Vector3.Normalize(-(currentRay.direction));

            sensor_game_obj.transform.localPosition = hit.point + backDir * 0.1f;
            sensor_game_obj.transform.rotation = transform.rotation;
            sensor_game_obj.transform.LookAt(currentRay.origin);

            //Fix weird issue where rays are looking down
            sensor_game_obj.transform.Rotate(new Vector3(sensor_game_obj.transform.rotation.x - 14.03624f, sensor_game_obj.transform.rotation.y, sensor_game_obj.transform.rotation.z));

            sensor_game_obj.transform.Rotate(genome.Genome[phenotype].direction);
            //sensor_game_obj.transform.Rotate(new Vector3(-10, 0, 0));

            //Debug.DrawLine(currentRay.direction, hit.point, Color.blue, 500);

            //Attach the sensor script to the game obj
            switch (genome.Genome[phenotype].sensorType)
            {
                case SensorType.Laser:
                    sensor_game_obj.AddComponent<SensorConfigLaserSensor>().Range = genome.Genome[phenotype].Range;
                    break;

                case SensorType.Radar:
                    SensorConfigRadarSensor radar = sensor_game_obj.AddComponent<SensorConfigRadarSensor>();
                    radar.Range = genome.Genome[phenotype].Range;
                    radar.FOV = genome.Genome[phenotype].FOV;
                    break;
            }

            Phenome[phenotype] = sensor_game_obj;
        }
    }

    private Ray CastRayOnVehicleBody(SensorConfigProperties gene, Vector3 forward)
    {
        Vector3 pointOnSphere = new Vector3(4 * Mathf.Sin(gene.angles.y * Mathf.Deg2Rad) * Mathf.Cos(gene.angles.x * Mathf.Deg2Rad),
                4 * Mathf.Sin(gene.angles.x * Mathf.Deg2Rad),
                4 * Mathf.Cos(gene.angles.y * Mathf.Deg2Rad) * Mathf.Cos(gene.angles.x * Mathf.Deg2Rad));

        pointOnSphere = Quaternion.FromToRotation(new Vector3(1, 0, 0), forward) * pointOnSphere;
        pointOnSphere += transform.position;

        Vector3 targetPoint = transform.position + new Vector3(0, 1, 0);
        Vector3 currentDir = (targetPoint - pointOnSphere) * 100;
        return new Ray(pointOnSphere, currentDir);
    }

    public void SetSensorConfigPhenomeUsingGenome(SensorConfigGenome genome)
    {
        Genes = genome;
        fitness = -1;

        //Will hold the sensors in the phenome
        var sensor_config_holder_game_obj = new GameObject("Phenome");

        //Set this transform as the parent obj for the container
        sensor_config_holder_game_obj.transform.SetParent(transform);
        //Recenter the game object to the parent and shift it up a bit as the sensor config must not be on the floor
        sensor_config_holder_game_obj.transform.localPosition = new Vector3(0, 0.6f, 0);

        //Make a game obj that has the SensorConfigLaserSensor script attached to it. This is the phenotypic representataion of the gene (the mapping).
        Phenome = new GameObject[genome.Genome.Length];
        for (var phenotype = 0; phenotype < Phenome.Length; phenotype++)
        {
            //Make the sensor game object
            var sensor_game_obj = new GameObject("Phenotype: Sensor " + phenotype);

            //Set the container as the parent obj for the sensor, makes things neat.
            sensor_game_obj.transform.SetParent(sensor_config_holder_game_obj.transform);

            //Recenter the game object to the parent transform
            sensor_game_obj.transform.localPosition = Vector3.zero;

            //Rotate the game obj based on the gene (this is really the mapping).
            sensor_game_obj.transform.rotation = Quaternion.Euler(genome.Genome[phenotype].angles.x, genome.Genome[phenotype].angles.y,
                0f);

            //Attach the sensor script to the game obj
            switch (genome.Genome[phenotype].sensorType)
            {
                case SensorType.Laser:
                    sensor_game_obj.AddComponent<SensorConfigLaserSensor>().Range = genome.Genome[phenotype].Range;
                    break;

                case SensorType.Radar:
                    sensor_game_obj.AddComponent<SensorConfigRadarSensor>().Range = genome.Genome[phenotype].Range;
                    var radar = sensor_game_obj.GetComponent<SensorConfigLaserSensor>();
                    radar.FOV = genome.Genome[phenotype].FOV;
                    break;
            }


            Phenome[phenotype] = sensor_game_obj;
        }
    }

}