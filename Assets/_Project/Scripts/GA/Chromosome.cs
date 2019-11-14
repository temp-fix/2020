using UnityEngine;
using System.Collections.Generic;
using CarSensor;

public class Chromosome : MonoBehaviour
{

	private double fitness = -1;
	private double fitness_sensors = -1;
	private List<Sensor> sensorList = new List<Sensor> ();
	public List<List<DataPoint>> dynamicTestData = new List<List<DataPoint>>();


	public Chromosome (int numberOfSensors)
	{
		initialiseRandomSensors (numberOfSensors);
	}

	public Chromosome (List<Sensor> inputSensors)
	{
		this.sensorList = inputSensors;
	}

	public void initialiseRandomSensors (int numberOfSensors)
	{
		for (int i = 0; i < numberOfSensors; i++) {
			var a1 = Random.Range (0f, 2*Mathf.PI); //values are in radians
			var a2 = Random.Range (0, Mathf.Deg2Rad * 25); //values are in radians

			var a3 = Random.Range (-90, 90); //values are in degrees
			var a4 = Random.Range (-90, 90); //values are in degrees

			Sensor s = new Sensor (70, new Vector2 (a1, a2), new Vector3(a3, a4, 0), -1);

			sensorList.Add (s);  
		}
	}

	// === Mutators ===

	public void addSensors(List<Sensor> sensors) {
		sensorList = sensors;
	}

	public void setFitness (double fitness)	{		
		this.fitness = fitness;
	}

	public void calculateFitnessFromSensors() {
		foreach (Sensor sensor in sensorList) {
			if (sensor.detectsObstacle) {
				this.fitness += 1;
			}
		}
	}

	public void addTestData (List<DataPoint> testData) {
		this.dynamicTestData.Add(testData);
		
		// foreach (DataPoint currentDataPoint in testData) {
		// 	Debug.Log (currentDataPoint.sensorIndex);
		// }
	}

	public void accumulateDetection() {
		fitness_sensors = 0;
		List<int> detected = new List<int>();

		foreach (Sensor sensor in sensorList) {
			if (sensor.detectsObstacle && !detected.Contains(sensor.obstacleIndex)) {
				detected.Add (sensor.obstacleIndex);
				fitness_sensors += 1;
			}

		}
	}

	public void accumulateDetectionAndAge() {
		foreach (Sensor sensor in sensorList) {
			sensor.increaseAge ();
			if (sensor.detectsObstacle) {
				sensor.detectedForThisRun ();
			}
		}
	}

	public void resetSensorCollision() {
		foreach (Sensor sensor in sensorList) {
			sensor.detectsObstacle = false;
			sensor.obstacleIndex = -1;
		}
	}

	
	// === Accessors ===
	public double getFitness ()	{
		return fitness;
	}

	public double getFitnessSensors() {
		return fitness_sensors;
	}
	
	public List<Sensor> getSensorList ()	{
		return sensorList;
	}

	public string ToString()	{
		return "";
	}

	public double getChromosomeAge() {
		double age = 0;
		foreach (Sensor sensor in sensorList) {
			age += sensor.age;
		}
		return age / sensorList.Count;
	}

}
