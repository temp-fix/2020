using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CarSensor;
using System.Linq;

public class ManagerScript : MonoBehaviour
{
	//PARAMETERS
	int populationSize = 5;
	int numberOfSensors = 10;
	int numberOfCars = 10;
	int generations = 50;
	int runs =10;
	float eliteRetainment = 0.5f; //Top % to keep for next run.
	//TODO: Generalisastion tests and elite retainment across runs.

	StaticCarControlScript currentCarScript;

	public Camera mainCamera;
	public GameObject StaticCarControl;
	public GameObject StaticCarPlacement;
	public GameObject currentCar = null;

	int currentInvalidSensorCount = 0;
	int totalNumberOfCars = 0;
	int currentGeneration;
	int currentRun;

	bool[] hitCars;
	List<GameObject> carTargets;
	List<Chromosome> chromosomes;
	List<Chromosome> eliteChromosomes;
	Operations geneticOperators = new Operations ();


	void Start ()
	{
		currentRun = 0;
		initRun ();
	}
		
	// Update is called once per frame
	void Update ()
	{
		if (currentRun < runs) {
			if (currentGeneration < generations) {
				evolve ();
				logPopulationFitness ();
				evaluatePopulation ();
			} else {
				currentRun++;
				endTest ();
				logRunStatistics ();
				initRun ();



			}
			currentGeneration++;
		} else {
			endTest ();
		}
	}

	void initTest (int numberOfCars)
	{
		randomCarPlacement (numberOfCars, 25f);
	}

	private void initRun() {
		currentGeneration = 0;

		initTest (numberOfCars);
		initCarFirstCall ();

		if (chromosomes != null) {
			foreach (Chromosome c in chromosomes) {
				c.accumulateDetectionAndAge ();
				c.resetSensorCollision ();
				c.setFitness (-1);
			}
		}

		chromosomes = initPopulation (populationSize);
		evaluatePopulation ();


		/*
		Call InitTest(number of cars) to create a test environment. Call getFitness(sensorlist) to evaluate fitness of a particular sensor. Call endTest() to destroy the game environment/before calling InitTest()
		you'll be able to see the lasers that were placed during all of these calls -- I've left it on so you can see how changing the angle parameters changes the distribution of the lasers
		To remove it go to the laserScript file and comment out the Debug.drawLine lines
		*/
	}

	private List<Chromosome> initPopulation (int populationSize)
	{
		List<Chromosome> chromosomes = new List<Chromosome> ();

		if (eliteChromosomes == null) {
			eliteChromosomes = new List<Chromosome> ();
		} else {
			chromosomes.AddRange (eliteChromosomes);
		}

		for (int i = eliteChromosomes.Count; i < populationSize; i++) {
			chromosomes.Add (new Chromosome (numberOfSensors)); 
			//TODO: create dynamic mating for variable length.
		}

		return chromosomes;
	}

	private void evaluatePopulation ()
	{
		foreach (Chromosome c in chromosomes) {
			if (c.getFitness () == -1) {
				c.setFitness (evaluateChromosome (c).x); 
				c.accumulateDetection ();

//				//new way
//				evaluateChromosome (c);
//				c.calculateFitnessFromSensors();
			}
		}
	}

	private void logRunStatistics () {
		double averageFitness = 0.0f;
		double averageAge = 0.0f;
		double totalFitness = 0;

		foreach (Chromosome c in chromosomes) {
			totalFitness += c.getFitness ();
		} 

		foreach (Chromosome c in chromosomes) {
			averageAge += c.getChromosomeAge ();
		}
			
		averageFitness = totalFitness / chromosomes.Count;
		averageAge /= chromosomes.Count;

		Debug.Log ("Run: " + currentRun + " Max Fitness: " + eliteChromosomes [0].getFitness() + " Avg Fitness: " + averageFitness + " Avg Age: " + averageAge);
	}

	private void logPopulationFitness ()
	{
		string debugString = "";

		foreach (Chromosome c in chromosomes) {
			debugString += (c.getFitness () + " ");
		}

		debugString += "| ";

		foreach (Chromosome c in chromosomes) {
			debugString += (c.getFitnessSensors () + " ");
		}

		Debug.Log (debugString);
	}

	void evolve ()
	{
		//sort chromosomes by fitness
		chromosomes = chromosomes.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();

		List<Chromosome> children = new List<Chromosome> ();	
		foreach (Chromosome parent in chromosomes) {
			int randomMate = Random.Range (0, chromosomes.Count);

			while (randomMate == chromosomes.IndexOf (parent)) {
				randomMate = Random.Range (0, chromosomes.Count);	//Keep randomising if random number ends up being itself
			}

			Chromosome parentMate = chromosomes [randomMate]; 
			//			Chromosome child = geneticOperators.mate (parent, parentMate);
			Chromosome child = geneticOperators.eliteMate (parent, parentMate);

			float probability = Random.Range (0, 100);
			if (probability < 20) {
				//reinitialise useless sensors in child with 20% probability
				child = geneticOperators.sensorInitialisation (child);
			} else if (probability < 35) { 
				//mutate child with 35% probability
				child = geneticOperators.sensorMutation (child);
			}



			child.setFitness (evaluateChromosome (child).x);
			child.accumulateDetection ();
//			evaluateChromosome (child);
//			child.calculateFitnessFromSensors ();

			//Selection Pressure (Out of parent1, parent2 and child, only fittest gets to next generation)
			List<Chromosome> parentsAndChild = new List<Chromosome> ();
			parentsAndChild.Add (parent);
			parentsAndChild.Add (parentMate);
			parentsAndChild.Add (child);
			parentsAndChild = parentsAndChild.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();

			children.Add (parentsAndChild [0]);
			//			Debug.Log ("Fitness P1, P2, C: " + parent.getFitness () + ", " + parentMate.getFitness () + ", " + child.getFitness ()); 

		}
		chromosomes.Clear ();
		chromosomes.AddRange (children);

		//sort chromosomes by fitness
		chromosomes = chromosomes.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();
	}

	Vector3 evaluateChromosome (Chromosome c)
	{
		hitCars = new bool[numberOfCars];

		List<Sensor> sensorList = c.getSensorList ();
		initCar (sensorList);
		currentInvalidSensorCount = 0;
		List<int> seenList = currentCarScript.applySensors ();
		int carsSeen = 0;

		foreach (int seen in seenList) {
			if (seen > -1) {
				if (!hitCars [seen]) {
					hitCars [seen] = true;
					++carsSeen;
				}
			}		
		}

		Dictionary<int,int> detectingSensors = currentCarScript.getDetectingSensorsList ();
		if (detectingSensors.Count > 0) {
			foreach (Sensor sensor in sensorList) {
				if (detectingSensors.ContainsKey (sensor.index)) {
					sensor.detects (detectingSensors [sensor.index]);
				}
			}
		}

		Vector3 ret = new Vector3 (carsSeen, totalNumberOfCars, currentInvalidSensorCount);
		//		Debug.Log (ret);

		return ret;
	}

	void randomCarPlacement (int numberOfCars, float range)
	{
		totalNumberOfCars = 0;
		carTargets = new List<GameObject> ();
		hitCars = new bool[numberOfCars];
		for (int i = 0; i < numberOfCars; i++) {
			bool placed = false;
			while (!placed) {
				float x = Random.Range (-range, range);
				float z = Random.Range (-range, range);
				while (x * x + z * z < 25f) {
					x = Random.Range (-range, range);
					z = Random.Range (-range, range);
				}

				hitCars [i] = false;
				float yAngle = Random.Range (0, 360);
				GameObject current = (GameObject)Instantiate (StaticCarPlacement, new Vector3 (x, 0, z), Quaternion.Euler (0, yAngle, 0));
				int checkCount = 0;
				foreach (GameObject g in carTargets) {

					if (!g.GetComponent<Collider>().bounds.Intersects (current.GetComponent<Collider>().bounds)) {
						++checkCount;
					} else {
						break;
					}
				}
				if (checkCount == carTargets.Count) {
					//check in field of view

					bool allCarsCanBeSeen = true;
					for (int currentCheckCar = 0; currentCheckCar < totalNumberOfCars; currentCheckCar++) {
						mainCamera.transform.position = new Vector3 (0, 0.5f, 0);
						mainCamera.transform.LookAt (carTargets.ElementAt (currentCheckCar).transform);

						Plane[] planes = GeometryUtility.CalculateFrustumPlanes (mainCamera);
						if (!GeometryUtility.TestPlanesAABB (planes, carTargets.ElementAt (currentCheckCar).GetComponent<Collider>().bounds)) {
							allCarsCanBeSeen = false;
							break;

						}
					}
					mainCamera.transform.position = new Vector3 (0, 0.5f, 0);
					mainCamera.transform.LookAt (current.transform);

					Plane[] currentPlanes = GeometryUtility.CalculateFrustumPlanes (mainCamera);
					if (!GeometryUtility.TestPlanesAABB (currentPlanes, current.GetComponent<Collider>().bounds)) {
						allCarsCanBeSeen = false;

					}

					if (allCarsCanBeSeen) {
						placed = true;
						objectIndex currentScript = (objectIndex)current.GetComponent ("objectIndex");
						currentScript.index = i;
						carTargets.Add (current);
						totalNumberOfCars++;
					} else {

						Debug.Log ("!!!!!!!!!!");
						GameObject.Destroy (current);
					}









					//


				} else {
					GameObject.Destroy (current);
				}
			}

		}
		foreach (GameObject g in carTargets) {
			g.GetComponent<Collider>().enabled = false;		
		}
		mainCamera.transform.position = new Vector3 (0, 100, 0);
		mainCamera.transform.LookAt (new Vector3 (0, 0, 0));

	}



	void initCarFirstCall ()
	{
		currentCar = (GameObject)Instantiate (StaticCarControl, new Vector3 (0, 0, 0), Quaternion.identity);

	}

	void initCar (List<Sensor> sensors)
	{
		int i = 0; //index to keep track of which sensorstruct is which sensor.

		currentInvalidSensorCount = 0;
		currentCarScript = (StaticCarControlScript)currentCar.GetComponent ("StaticCarControlScript");
		currentCarScript.resetLaserSensors ();

		foreach (Sensor sensor in sensors) {
			bool res;

			if (sensor.theta < 0) {
				//Debug.Log (sensor.distance);
				res = currentCarScript.createLaserSensorByAngle (sensor.distance, sensor.angles, sensor.direction, i);
			} else {

				res = currentCarScript.createConeSensorByAngle (sensor.distance, sensor.theta, sensor.angles, sensor.direction, i);
			}

			if (res == false) {
				currentInvalidSensorCount++;
			}

			sensor.setIndex (i);
			//			Debug.Log(i + " " + sensor.index);
			i++;
		}
	}

	Vector3 runTest (List<Sensor> sensors)
	{//returns number of cars seen, total cars, number of invalid sensors
		currentInvalidSensorCount = 0;
		initCar (sensors);
		randomCarPlacement (50, 45);
		List<int> seenList = currentCarScript.applySensors ();
		int carsSeen = 0;

		foreach (int seen in seenList) {

			if (seen > -1) {
				//Debug.Log (seen);
				if (!hitCars [seen]) {
					hitCars [seen] = true;
					carsSeen++;
				}
			}		
		}

		Vector3 ret = new Vector3 (carsSeen, totalNumberOfCars, currentInvalidSensorCount);
		Debug.Log (ret);
		return ret;

	}

	void endTest ()
	{

		//sort chromosomes by fitness
		chromosomes = chromosomes.OrderByDescending (chromosome => chromosome.getFitness ()).ToList ();
		eliteChromosomes = new List<Chromosome> ();
		for (int i = 0; i < populationSize * eliteRetainment; i++) {
			eliteChromosomes.Add (chromosomes [i]);
		}

		//Debug.Log (carTargets.Count);
		foreach (GameObject g in carTargets) {
			GameObject.Destroy (g);
		}
		carTargets = new List<GameObject> ();

		currentCarScript = null;
		hitCars = null;
		currentInvalidSensorCount = 0;
		totalNumberOfCars = 0;

		GameObject.Destroy (currentCar);
		currentCar = null;

	}

}
