using UnityEngine;
using UnityEngine.Assertions;
using UnityStandardAssets.Vehicles.Car;

public class SpawnCars : MonoBehaviour
{
    private float _timeSinceLastSpawn;

    public GameObject CarToSpawn;

    public int MaxNumberOfCarsToSpawn;
        //The max number of cars that will spawn (anywhere between 1 and this number), min of 1

    public float SecondsUntilNextSpawn; //Period of spawning, min of 5
    public Vector2 SpawnPlane; // specify a range in which the car can spawn, makes it more random

    public GameObject[] TargetsForAI;

    private void Start()
    {
        //Make sure the car has a driving AI
        Assert.IsTrue(CarToSpawn.GetComponent<CarAIControl>() != null);

        if (SecondsUntilNextSpawn < 1)
        {
            SecondsUntilNextSpawn = 1;
        }

        if (MaxNumberOfCarsToSpawn < 1)
        {
            MaxNumberOfCarsToSpawn = 1;
        }

        _timeSinceLastSpawn = 0;
    }

    private void FixedUpdate()
    {
        if (_timeSinceLastSpawn >= SecondsUntilNextSpawn)
        {
            int no_cars_to_spawn = Random.Range(1, MaxNumberOfCarsToSpawn);

            for (int i = 0; i < no_cars_to_spawn; i++)
            {

                //Make gameObj
                var car_gameObj = Instantiate(CarToSpawn, transform.position, transform.rotation) as GameObject;

                //Move it based on a random value in the spawn plane
                car_gameObj.transform.Translate(new Vector3(Random.Range(-SpawnPlane.x/2, SpawnPlane.x/2), 0f, 0f),
                    Space.Self);

                //Set this transform as the parent obj for the car, makes things neat.
                car_gameObj.transform.SetParent(transform);

                if (!car_gameObj.activeSelf)
                {
                    car_gameObj.SetActive(true);
                }

                //Set the way points for the AI
                car_gameObj.GetComponent<CarAIControl>().SetCheckPoints(TargetsForAI);
            }

            //Reset timer
            _timeSinceLastSpawn = 0f;
        }
        else
        {
            _timeSinceLastSpawn += Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, SpawnPlane);
    }
}