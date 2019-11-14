using Assets.Car;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

public enum BehaviourCharacterisationType
{
    Speed,
    Location,
    CohesionAndSpeed,
};

public enum RunType
{
    NoveltySearch,
    Hybrid,
    Fitness,
    Evaluation
}

[RequireComponent(typeof(CarDriving))]
public class NEATCarInputHandler : UnitController
{
    private Rigidbody _rigidbody;
    private IBlackBox box;
    private float CumulativeDistance;
    private float CumulativeWallDistance;
    private int CumulativeWallDistanceTicks;

    private float[] BehaviourCharacterisation;
    private BehaviourCharacterisationType bcType = BehaviourCharacterisationType.Speed;
    private RunType runType = RunType.Fitness;

    private GameObject CurrentCheckPointGameObj;

    private float novelty = -1;
    public float fitness;
    public bool fullThrottle; //Keep throttle full and network brakes instead of accelerates.
    public bool IsRunning;
    public Dictionary<string, List<float>> sensorInputs;
    private CarDriving m_Car; // the car controller we want to use

    private bool MovingForward;
    private int NumberOfCPsSuccessfullyCrossed;
    private int NumberOfWPsSuccessfullyCrossed;
    private int NumberOfCollisions;

    public bool UseSensors;
    public bool UseMetaSensors;
    public string SensorConfigXmlFile;
    public List<SensorConfigSensor> Sensors;

    public GameObject[] Waypoints;
    private Transform m_Target;
    public int waypointNum = 0;
    private float topSpeed;

    private int frame;

    private void Awake()
    {
        // get the car controller
        m_Car = GetComponent<CarDriving>();

    }

    // Use this for initialization
    private void Start()
    {
        topSpeed = gameObject.GetComponent<CarDriving>().MaxSpeed;
        NumberOfCollisions = 0;
        MovingForward = true;
        NumberOfCPsSuccessfullyCrossed = 0;
        NumberOfWPsSuccessfullyCrossed = 0;
        CumulativeDistance = 0f;
        CumulativeWallDistance = 0f;
        CumulativeWallDistanceTicks = 0;

        BehaviourCharacterisation = Enumerable.Repeat(0.0f, 100).ToArray(); //Initialise BC with 0.0 values

        _rigidbody = GetComponent<Rigidbody>();
        sensorInputs = new Dictionary<string, List<float>>();
        if (UseSensors)
            LoadSensors(loadGenomeFromFile(Application.persistentDataPath + "/" + SensorConfigXmlFile));

        frame = 0;
    }

    private void FixedUpdate()
    {
        frame++;
        if (IsRunning)
        {
            if (bcType != BehaviourCharacterisationType.CohesionAndSpeed && frame % 50 == 0)
                RecordBehaviourAtTimestep(frame / 50 - 1);

            //Input array for NEAT
            ISignalArray inputArr = box.InputSignalArray;

            //Input the distance from the sensors into NEAT
            //The closer the car is to a wall, the closer the input is to 1
            var i = 0;

            if (UseSensors)
            {
                foreach (SensorConfigSensor sensor in Sensors)
                {
                    var sensorDistanceInput = sensor.Range;
                    //var sensorObjectNumberInput = 0;
                    if (sensor && sensor.IsHitting())
                    {
                        sensorDistanceInput = sensor.NearestObstacleDistance() / sensor.Range;
                        //sensorObjectNumberInput = sensor.CurrentlyDetectedObjectNumber;
                    }
                    else
                        sensorDistanceInput = 1; //sensor.Range / sensor.Range;
                    //print(inputArr[i]);
                    inputArr[i] = sensorDistanceInput;
                    //print(sensorObjectNumberInput);
                    //inputArr[i + 1] = sensorObjectNumberInput;
                    i++;
                }
            }

            if (UseMetaSensors)
            {
                //float max_distance_between_waypoints = 0f;
                //float distance_to_next_waypoint = 0f;

                m_Target = Waypoints[waypointNum].transform;

                //if (waypointNum == 0)
                //    max_distance_between_waypoints = (Waypoints[waypointNum].transform.position - (new Vector3(0, 0, 0))).magnitude;
                //else
                //{
                //    max_distance_between_waypoints = (Waypoints[waypointNum].transform.position - Waypoints[waypointNum - 1].transform.position).magnitude;
                //}

                //distance_to_next_waypoint = (m_Target.position - transform.position).magnitude / max_distance_between_waypoints;

                //if (distance_to_next_waypoint < 0)
                //    distance_to_next_waypoint = 0;

                //if (distance_to_next_waypoint > 1)
                //    distance_to_next_waypoint = 1;

                Vector3 vectorToTarget = m_Target.transform.position - transform.position;
                int sign = Vector3.Cross(transform.forward, vectorToTarget).z < 0 ? -1 : 1; //To determine if target is to left or to right of direction of travel
                float angle_from_waypoint = sign * Vector3.Angle(transform.forward, vectorToTarget);
                angle_from_waypoint /= 180;

                Debug.DrawRay(transform.position, vectorToTarget, Color.yellow);


                inputArr[i] = angle_from_waypoint;
                //inputArr[i + 1] = distance_to_next_waypoint;
                inputArr[i + 1] = _rigidbody.velocity.magnitude * 3.6f / topSpeed;
            }

            //Use NEAT to gen outputs
            box.Activate();

            //Get the output array from NEAT
            ISignalArray outputArr = box.OutputSignalArray;

            var steer = (float)(outputArr[0] * 2 - 1);
            var gas = (float)(outputArr[1] * 2 - 1);
            //Debug.Log("Gas: " + gas + " steer: " + steer);

            if (gas < 0)
                m_Car.Move(steer, 0, gas, 0);
            else
                m_Car.Move(steer, gas, 0, 0);

        }
        else
        {
            // pass the input to the car!
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            float hndbrk = Input.GetAxis("Jump");

            m_Car.Move(h, v, v, hndbrk);
            GetFitness();
        }
    }

    public override void SetWaypoints(GameObject[] waypoints)
    {
        Waypoints = waypoints;
        if (gameObject.GetComponent<CarDrivingAIControl>() != null)
        {
            gameObject.GetComponent<CarDrivingAIControl>().SetCheckPoints(waypoints);
            gameObject.GetComponent<CarDrivingAIControl>().SetTarget(waypoints[0].transform);
        }
    }

    public override bool Stopped
    {
        get
        {
            return !IsRunning;
        }
    }

    public override Dictionary<string, List<float>> SensorInputs
    {
        get
        {
            return sensorInputs;
        }
    }

    public override void Stop()
    {
        IsRunning = false;

        DistanceTravelledCalculator distCalc = transform.GetComponent<DistanceTravelledCalculator>();
        if (distCalc != null)
            distCalc.Stop();


        CarDrivingAIControl aiControl = transform.GetComponent<CarDrivingAIControl>();
        if (aiControl != null)
            aiControl.StopDriving();

        //print(sensorInputs.ToString());
    }

    public override void Activate(IBlackBox box)
    {
        this.box = box;
        IsRunning = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.tag.Equals("Wall") || collision.collider.tag.Equals("Obstacle"))
        {
            NumberOfCollisions++;

            RayCaster[] raycasters = transform.GetComponentsInChildren<RayCaster>();

            foreach (RayCaster raycast in raycasters)
            {
                raycast.RayColour = Color.red;
            }

            if (runType == RunType.Evaluation)
                Stop(); //Stop when colliding with the wall or obstacle
        }

        if (collision.collider.tag.Equals("Car_GA_AIDriving"))
        {
            NumberOfCollisions++;
            if (runType == RunType.Evaluation)
            {
                Stop(); //Stop when colliding with another car

                if (collision.gameObject.GetComponent<NEATCarInputHandler>() != null)
                    collision.gameObject.GetComponent<NEATCarInputHandler>().Stop(); //Stop the other car as well
            }
        }
    }

    public void WayPointEntered(GameObject waypoint)
    {
        if (Waypoints[waypointNum] == waypoint)
        {
            NumberOfWPsSuccessfullyCrossed++;
            ++waypointNum;

            if (waypointNum >= Waypoints.Length)
            {
                Stop(); //Stop when all checkpoints reached
                waypointNum = 0;
            }

            m_Target = Waypoints[waypointNum].transform;
        }
    }

    public void CheckPointEntered(GameObject CP)
    {
        //		print ("Current CP: " + CurrentCheckPointGameObj);
        if (CurrentCheckPointGameObj == null)
        {
            //First time going through a CP
            CurrentCheckPointGameObj = CP; //update current CP
            MovingForward = true;
            NumberOfCPsSuccessfullyCrossed++;

            if (CurrentCheckPointGameObj.name != "check_point")
            {
                //If it moves backwards the first time
                MovingForward = false;
            }
        }
        else
        {
            //Check if the CP the car went through is the correct one
            //(by checking if the CPs prevCP is equal to the cars current)
            if (CP.GetComponent<CheckPoint>().PreviousCheckPoint ==
                CurrentCheckPointGameObj.GetComponent<CheckPoint>())
            {
                CumulativeDistance += Vector3.Distance(CurrentCheckPointGameObj.transform.position,
                CP.transform.position);

                CurrentCheckPointGameObj = CP; //update current CP

                MovingForward = true;
                NumberOfCPsSuccessfullyCrossed++;
            }
            else
            {
                MovingForward = false;
            }
        }
    }

    private float AddToCumlativeWallDistace()
    {
        //This is the formula used (based on harmonic mean):
        //          ( ( 1/(SIGMA [1/(1+distance to the wall)] -> for each sensor) ) - 1 )

        //        Explanation:
        //        As the car gets closer to a wall, the sigma part becomes bigger (tending to to 1), making the final result smaller. However, if genrally the car is far away from the surrounding walls,
        //        the sigma part will be small, inverting that makes it big. Making it effetive in giving fitness for staying away from walls =D.
        //        The subtraction of 1 is there because if the car was completly close to the surrounding walls, the final result would be 1, but we need it to be 0...
        if (_rigidbody.velocity.magnitude > 0.5f && MovingForward) //if moving forward at a significant speed
        {
            var dist = 0f;
            foreach (SensorConfigSensor sensor in Sensors)
            {
                //Get the distance to the wall. If no wall (or no object) set the distance to the sensors range.
                var distance = sensor.NearestObstacleDistance();

                if (distance == 0)
                    distance = sensor.Range;

                //The summation
                dist += (1 / (1 + distance));
            }

            //The inversion and subtraction
            dist = (1 / dist) - 1;
            //It maxes out at around 3
            //            Debug.Log(dist);

            CumulativeWallDistance += dist;
            CumulativeWallDistanceTicks++;
        }

        return CumulativeWallDistance;
    }

    public override float GetFitness()
    {
        switch (runType)
        {
            case RunType.Evaluation:
                fitness = getEvaluationFitness();
                break;
            case RunType.Fitness:
                fitness = getObjectiveFitness();
                break;
            case RunType.NoveltySearch:
            case RunType.Hybrid:
                fitness = getObjectiveFitness(); //Return Objective Fitness for NS - as we need to log this
                break;
        }

        return fitness;
    }

    public override float GetNovelty()
    {
        return getNoveltyScore();
    }

    private float getEvaluationFitness()
    {
        return (float)NumberOfWPsSuccessfullyCrossed / Waypoints.Length;
    }

    private float getObjectiveFitness()
    {
        float contribution_NumberOfWPsSuccessfullyCrossed = (float)NumberOfWPsSuccessfullyCrossed / Waypoints.Length;
        int contribution_NumberOfCollisions = NumberOfCollisions;
        float fitness = (contribution_NumberOfWPsSuccessfullyCrossed) * (float)Math.Pow(0.9, contribution_NumberOfCollisions);

        return (fitness < 0) ? 0f : fitness;
    }

    private float getNoveltyScore()
    {
        if (novelty == -1)
            throw new NotImplementedException();

        return novelty;

    }

    public override int GetNumberOfInputsIntoNeat()
    {
        int numberOfInputs = 0;

        if (UseSensors)
            numberOfInputs += loadGenomeFromFile(Application.persistentDataPath + "/" + SensorConfigXmlFile).Genome.Length;

        if (UseMetaSensors)
            numberOfInputs += 2;

        return numberOfInputs;
    }

    public override int GetNumberOfOutputsNeededFromNeat()
    {
        return 2;
    }

    private void LoadSensors(SensorConfigGenome genome)
    {
        GameObject sensors = new GameObject("Sensors");

        //Attach the individual script to the sensor conifg game object and set its the phenome using the given genome
        sensors.AddComponent<SensorConfigIndividual>().SetSensorConfigPhenomeUsingGenomeSurface(genome, gameObject);
        GameObject[] SensorGameObjects = sensors.GetComponent<SensorConfigIndividual>().Phenome;

        foreach (GameObject sensor in SensorGameObjects)
        {
            if (sensor.GetComponent<SensorConfigLaserSensor>() != null)
                Sensors.Add(sensor.GetComponent<SensorConfigLaserSensor>());
            if (sensor.GetComponent<SensorConfigRadarSensor>() != null)
                Sensors.Add(sensor.GetComponent<SensorConfigRadarSensor>());
        }

        sensors.transform.SetParent(gameObject.transform);
        sensors.transform.localPosition = new Vector3(0, 0.5f, 0);
        sensors.transform.localRotation = new Quaternion(0, 0, 0, 0);

    }

    private SensorConfigGenome loadGenomeFromFile(string filepath)
    {
        using (XmlReader xr = XmlReader.Create(filepath))
        {
            return SensorConfigXmlIO.ReadGenome(xr);
        }
    }

    public void RecordBehaviourAtTimestep(int timestep)
    {
        if (timestep < BehaviourCharacterisation.Length)
        {
            switch (bcType)
            {
                case BehaviourCharacterisationType.CohesionAndSpeed:
                    //This should not be called from here.
                    throw new NotImplementedException();

                case BehaviourCharacterisationType.Location:
                    BehaviourCharacterisation[timestep] = gameObject.GetComponent<CarDriving>().transform.position.x + gameObject.GetComponent<CarDriving>().transform.position.z;
                    break;

                case BehaviourCharacterisationType.Speed:
                    BehaviourCharacterisation[timestep] = gameObject.GetComponent<CarDriving>().CurrentSpeed;
                    break;
            }
        }
    }

    public override void SetBehaviourCharacterisation(BehaviourCharacterisationType type)
    {
        bcType = type;
    }

    public override void SetRunType(RunType type)
    {
        runType = type;
    }

    public override void SetNovelty(float novelty)
    {
        this.novelty = novelty;
    }

    public override float[] GetBehaviourCharacterisation()
    {
        return BehaviourCharacterisation;
    }
}
