using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CarSensor;
using System.Linq;
using UnityStandardAssets.Vehicles.Car;

public class DynamicTestController : MonoBehaviour
{
	//EA vars
	List<Chromosome> chromosomes;
	Operations geneticOperators = new Operations ();
	int generations;
	int generation = 0;

	public GameObject spawnObject;
	public GameObject mainCamera;
	public GameObject[] checkPoints;
	public GameObject[] checkPointsForSensorCar;
	private GameObject[] carsInTest;
	private GameObject currentCar;
	int checkSensorPeriod, totalTimeForTest;
	float currentTime, lastCheckedSensors;
	Car currentCarScript;
	private bool[] seenCheckpoints;
	bool testRunning = false;
	private bool[,] hitCars;
	private int[] seenCarsTally;
	int curretnNumberOfChromosomesInTest;

	void printFitness()
	{
		string output = "";
		Debug.Log ("Generation: " + generation);

		foreach(Chromosome c in chromosomes) {
			output += c.getFitness() + " ";
		}

		Debug.Log(output);
	}


	// Use this for initialization
	void Start ()
	{
		if(generation == 0) {
		Time.timeScale = 1; //speedup
			generations = 10;
			chromosomes = initRandomPopulation(10);
		}

		initTestAndRun (20, 1, 60);
	}

	List<Chromosome> initRandomPopulation(int populationSize) {
		List<Chromosome> population = new List<Chromosome>();
		for (int i=0; i<populationSize; i++) {
			population.Add (new Chromosome (20));
		}
		return population;
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (!testRunning) {
			return;		
		}
		currentTime += Time.deltaTime;
		//Debug.Log (Time.time - lastCheckedSensors);
		//Debug.Log (checkSensorPeriod);
		if (Time.time - lastCheckedSensors > checkSensorPeriod) {
//			addSeenCars ();
			//Debug.Log (seenCarsTally);
			lastCheckedSensors = Time.time;
		}
		if (currentTime > totalTimeForTest) {
			endTest ();		
			evolve();
		}
	}

	void endTest ()
	{
		testRunning = false;
		CarCamera cc = (CarCamera)mainCamera.GetComponent ("CarCamera");
		cc.topView = true;
		foreach (GameObject g in carsInTest) {
			GameObject.Destroy (g);		
		}
		GameObject.Destroy (currentCar);
	}

	void addSeenCars ()
	{
		for(int i = 0; i < chromosomes.Count; i++) {
			chromosomes[i].addTestData(currentCarScript.getAllDataForDynamicTests(i));
		}

		 int timestep = chromosomes[0].dynamicTestData.Count - 1;
		  // Debug.Log(chromosomes[0].dynamicTestData.Count); //Timestep (x)
		 // Debug.Log(" " + chromosomes[0].dynamicTestData[timestep].Count); // Sensor (y) (sensor data AT timestep x)


		//  //Count how many collisions there are
		// int sensorWithCollisions = 0;
		// for(int i = 0; i < chromosomes[0].dynamicTestData[0].Count; i++) {
		// 	if (chromosomes[0].dynamicTestData[timestep][i].Count > 0) {
		// 		sensorWithCollisions = i;
		// 		break; 
		// 	}
		// }

		//  if(chromosomes[0].dynamicTestData[timestep][0].Count > 0) {
		//  Debug.Log("  " + chromosomes[0].dynamicTestData[timestep][sensorWithCollisions].Count); //Collisions (collision of sensor data y at timestep x)
		//  string sensorCollisions = "";
		//  foreach(DataPoint collision in chromosomes[0].dynamicTestData[timestep][sensorWithCollisions]) {
		//  	sensorCollisions += collision + " ";
		//  }
		//  Debug.Log(sensorCollisions);
		// }


	}

	void initTestAndRun (int testCars, int checkPeriod, int totalSeconds)
	{

		int numberOfGenotypes = chromosomes.Count;
		curretnNumberOfChromosomesInTest = numberOfGenotypes;
		seenCarsTally = new int[numberOfGenotypes];
		currentTime = 0;
		lastCheckedSensors = 0;
		totalTimeForTest = totalSeconds;
		checkSensorPeriod = checkPeriod;
		seenCheckpoints = new bool[checkPoints.Length];
		carsInTest = new GameObject[testCars];
		hitCars = new bool[numberOfGenotypes, testCars];

		for (int i=0; i<checkPoints.Length; i++) {
			seenCheckpoints [i] = false;		
		}

		for (int e=0; e<numberOfGenotypes; e++) {
			for (int i=0; i<testCars; i++) {
				hitCars [e, i] = false;		
			}		
		}

		seenCheckpoints [0] = true;
		int placed = 0;
		while (placed<testCars) {
			seenCheckpoints [placed + 1] = true;
			carsInTest [placed] = createUnity5NPAI (placed + 1, 15, placed);
			++placed;
		}

		currentCar = createUnity5NPAI (0, 25, -1);
//		currentCarScript = (Car)currentCar.GetComponent ("Car");
//		currentCarScript.checkPoints = checkPointsForSensorCar;//use unique checkpoints, must have same length as other car checkpoints
//		currentCarScript.initCreateSensorsForDynamicTests (numberOfGenotypes);
//		//currentCarScript.resetLaserSensors ();
//
//		for(int i = 0; i < chromosomes.Count; i++) {
//
//
//			foreach (Sensor sensor in chromosomes[i].getSensorList()) {
//				if (sensor.theta < 0) {
//					//Debug.Log (sensor.distance);
//
//					currentCarScript.createLaserSensorByAngleForDynamicTests (sensor.distance, sensor.angles, sensor.direction, i);
//				} else {
//					//res = currentCarScript.createConeSensorByAngle (sensor.distance, sensor.theta, sensor.angles, sensor.direction);
//				}
//			}
//		}

		/*foreach (SensorStruct sensor in sensors) {
			bool res;
			if (sensor.theta < 0) {
				//Debug.Log (sensor.distance);
				res = currentCarScript.createLaserSensorByAngle (sensor.distance, sensor.angles, sensor.direction);
			} else {
				res = currentCarScript.createConeSensorByAngle (sensor.distance, sensor.theta, sensor.angles, sensor.direction);
			}
			
			if (res == false) {
				currentInvalidSensorCount++;
			}
		}*/
		testRunning = true;
		CarCamera cc = (CarCamera)mainCamera.GetComponent ("CarCamera");
		cc.target = currentCar.transform;
		cc.topView = false;
	}


	GameObject createUnity5NPAI(int start, float targetVelocity, int index) {
		GameObject g = (GameObject)Instantiate (spawnObject, checkPoints [start].transform.position + new Vector3 (0, 0.2f, 0), Quaternion.identity);
		var forward = g.transform.TransformDirection (Vector3.forward);
		g.transform.LookAt (checkPoints [(start + 1) % checkPoints.Length].transform.position);
		CarAIControl carS = g.GetComponent<CarAIControl>();
//
		carS.checkPoints = checkPoints;
		carS.checkPointNum = (start + 1) % checkPoints.Length;


		return g;
	}

	GameObject createNPAI (int start, float targetVelocity, int index)
	{
		GameObject g = (GameObject)Instantiate (spawnObject, checkPoints [start].transform.position + new Vector3 (0, 0.2f, 0), Quaternion.identity);
		var forward = g.transform.TransformDirection (Vector3.forward);
		g.transform.LookAt (checkPoints [(start + 1) % checkPoints.Length].transform.position);
		//g.rigidbody.isKinematic = true;
		//g.transform.Rotate(Quaternion.ToEulerAngles(Quaternion.FromToRotation(forward,to)));
		Car carS = (Car)g.GetComponent ("Car");
		carS.checkPoints = checkPoints;
		carS.checkPointNum = (start + 1) % checkPoints.Length;
		carS.AIDriving = true;
		carS.cyclePath = false;
		carS.targetVelocity = targetVelocity;
		carS.userSteeringOverride = false;
		carS.NPAIDriving = true;
		carS.AStar = false;
		objectIndex oi = (objectIndex)g.GetComponent ("objectIndex");
		oi.index = index;
		return g;
	}



	////////EA

	void evolve () {
		//sort chromosomes by fitness
		chromosomes = chromosomes.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();
		printFitness ();

		List<Chromosome> children = new List<Chromosome> ();
	
		foreach (Chromosome parent in chromosomes) {
		 	int randomMate = Random.Range (0, chromosomes.Count);

		 	while (randomMate == chromosomes.IndexOf(parent)) {
		 		randomMate = Random.Range (0, chromosomes.Count);	//Keep randomising if random testCars ends up being itself
		 	}

		 	Chromosome parentMate = chromosomes [randomMate]; 
		 	Chromosome child = geneticOperators.mate (parent, parentMate);

		 	if (Random.Range (0, 100) < 35) {
		 		//mutate child with 35% probability
		 		child = geneticOperators.sensorMutation (child);
		 	}

		 	//CAN'T do below scheme because needs full generational replacement.
			// child.setFitness (evaluateChromosome (child).x);

			// //Selection Pressure (Out of parent1, parent2 and child, only fittest gets to next generation)
			// List<Chromosome> parentsAndChild = new List<Chromosome> ();
			// parentsAndChild.Add (parent);
			// parentsAndChild.Add (parentMate);
			// parentsAndChild.Add (child);
			// parentsAndChild = parentsAndChild.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();

		 	// children.Add (parentsAndChild [0]);

		 	children.Add(child);
		}

		chromosomes.Clear ();
		chromosomes.AddRange (children);

		generation++;
		if(generation < generations) {
			Start();
		} else {
			endTest();
		}
	}


	///////////
}
