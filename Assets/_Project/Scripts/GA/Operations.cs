using UnityEngine;
using CarSensor;
using System.Collections.Generic;
using System.Linq;

public class Operations : MonoBehaviour
{

	public Chromosome eliteMate (Chromosome a, Chromosome b)
	{
		int numberToAdd;
		int eliteSensors = 0;
		
		List<Sensor> aSensors = a.getSensorList ().ConvertAll (sensor => new Sensor (sensor.distance, sensor.angles, sensor.direction, sensor.theta, 
		                                                                             sensor.detectsObstacle, sensor.index, sensor.obstacleIndex)); 

		List<Sensor> bSensors = b.getSensorList ().ConvertAll (sensor => new Sensor (sensor.distance, sensor.angles, sensor.direction, sensor.theta, 
		                                                                             sensor.detectsObstacle, sensor.index, sensor.obstacleIndex));
		List<Sensor> detectingSensors = new List<Sensor> ();
		List<Sensor> dedupDetectingSensors = new List<Sensor> ();

		Chromosome child;

		if (a.getFitness () > 0) {
			foreach (Sensor sensor in aSensors) {
				if (sensor.detectsObstacle) {
					detectingSensors.Add (sensor);
				}
			}
		}

		if (b.getFitness () > 0) {
			foreach (Sensor sensor in bSensors) {
				if (sensor.detectsObstacle) {
					detectingSensors.Add (sensor);
				}
			}
		}

		List<int> seenObstacles = new List<int> ();

		foreach (Sensor sensor in detectingSensors) {
			if (!seenObstacles.Contains (sensor.obstacleIndex)) {
				seenObstacles.Add (sensor.obstacleIndex);
				dedupDetectingSensors.Add (sensor);
			}
		}


		eliteSensors = dedupDetectingSensors.Count;
		numberToAdd = aSensors.Count - eliteSensors;

		while (numberToAdd > 0) {
			int randomSensor;
			List<Sensor> parentSensors = new List<Sensor> ();
			parentSensors.AddRange (aSensors);
			parentSensors.AddRange (bSensors);

			randomSensor = Random.Range (0, parentSensors.Count);

			while (dedupDetectingSensors.Contains(parentSensors[randomSensor])) {
				randomSensor = Random.Range (0, parentSensors.Count);
			}

			dedupDetectingSensors.Add (parentSensors [randomSensor]);
			numberToAdd--;
		}

		child = new Chromosome (dedupDetectingSensors); //Create New Child from detecting sensors and random sensors from both parents

		return child;
	}

	public Chromosome mate (Chromosome a, Chromosome b)
	{
		int randomPosition;
		int numberToRemove; 

		//create deepcopy
		List<Sensor> tempSensorList = a.getSensorList ().ConvertAll (sensor => new Sensor (sensor.distance, sensor.angles, sensor.direction, sensor.theta, sensor.index)); 

		randomPosition = Random.Range (0, tempSensorList.Count);
		numberToRemove = tempSensorList.Count - randomPosition;
		tempSensorList.RemoveRange (randomPosition, numberToRemove); //Remove all sensors following the random pos.
		tempSensorList.AddRange (b.getSensorList ().GetRange (randomPosition, numberToRemove)); // Add sensors from parent B.

		Chromosome child = new Chromosome (tempSensorList); //Create New Child with A's sensors

		return child;

	}

	public Chromosome sensorInitialisation (Chromosome chromosome) {
		List<Sensor> sensors = chromosome.getSensorList ();
		List<Sensor> newSensors = new List<Sensor> ();
		Chromosome result;

		foreach (Sensor sensor in sensors) {
			if (!sensor.detectsObstacle) { 
				//only reinitialise sensors that don't detect anything
				var a1 = Random.Range (0f, 2 * Mathf.PI); //values are in radians
				var a2 = Random.Range (0.1f, 0.3f); //values are in radians

				var a3 = Random.Range (-1, 1); //values are in degrees
				var a4 = Random.Range (-1, 1); //values are in degrees

				newSensors.Add (new Sensor (70, new Vector2 (a1, a2), new Vector3 (a3, a4, 0), -1));
			} else {
				newSensors.Add (sensor);
			}
		}

		result = new Chromosome (newSensors);
		return result;
	}

	public Chromosome sensorMutation (Chromosome chromosome)
	{

		List<Sensor> sensors = chromosome.getSensorList ();
		List<Sensor> newSensors = new List<Sensor> ();
		Chromosome result;

		float distance;
		Vector2 angles;
		Vector3 direction;

		foreach (Sensor sensor in sensors) {
			if (!sensor.detectsObstacle) { 
				//only mutate sensors that don't detect anything

				distance = sensor.distance; // * Random.Range (0.5f, 1.5f);

				angles = sensor.angles;
				direction = sensor.direction;

				angles.x = angles.x * Random.Range (0.5f, 1.5f);
				angles.y = angles.y * Random.Range (0.5f, 1.5f);

				direction.x = (direction.x) * Random.Range (0.5f, 1.5f);
				direction.y = (direction.y) * Random.Range (0.5f, 1.5f);

				newSensors.Add (new Sensor (distance, angles, direction, -1, sensor.index));
			} else {
				newSensors.Add (sensor);
			}
		}

		result = new Chromosome (newSensors);
		return result;
	}
}
