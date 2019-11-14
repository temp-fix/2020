using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Assets.Car;



public class TrackTester : MonoBehaviour {
    private TrackChanger trackChanger;
    private List<GameObject> cameras;
    public GameObject mainCamera;
    public GameObject[] cars;
    private int currentCarIndex;

    void Awake () {
        trackChanger = gameObject.GetComponent<TrackChanger>();
    }

    void Start()
    {
        cameras = new List<GameObject>();
        cameras.Add(mainCamera);

        GameObject[] startPositions = trackChanger.getAllStartingPositions();
        var obj = Instantiate(cars[currentCarIndex], startPositions[0].transform.position, startPositions[0].transform.rotation) as GameObject;
        var controller = obj.GetComponent<UnitController>();
        controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(Convert.ToInt32(startPositions[0].name.Substring(startPositions[0].name.Length - 1))));

        for (int pos = 1; pos < startPositions.Length; pos++)
        {
            int startIndex = Convert.ToInt32(startPositions[pos].name.Substring(startPositions[pos].name.Length - 1));
            GameObject startPosition = startPositions[pos];
            var cars_obj = Instantiate(cars[currentCarIndex], startPosition.transform.position, startPosition.transform.rotation) as GameObject;
            var car_controller = cars_obj.GetComponent<UnitController>();
            car_controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(startIndex));

            Vector3 rotation = cars_obj.transform.eulerAngles;
            Transform[] children = new Transform[cars_obj.transform.childCount];

            int i = 0;
            foreach (Transform child in cars_obj.transform)
            {
                children[i] = child;
                i++;
            }

            for (i = 0; i < children.Length; i++)
            {
                children[i].SetParent(obj.transform);
                children[i].rotation = Quaternion.AngleAxis(rotation.y, Vector3.up);
                children[i].GetComponent<CarDriving>()._currentRotation = children[i].eulerAngles;
            }

            Destroy(cars_obj);
            Destroy(car_controller);
        }
    }

        // Update is called once per frame
        void Update () {
		
	    }
}
