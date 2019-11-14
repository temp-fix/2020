using UnityEngine;
using System.Collections;
using System;

public class ObstacleDisabler : MonoBehaviour {
    public GameObject associatedEnabler;
    public GameObject obstacle;

    void Start () {
        if (associatedEnabler == null)
            throw new Exception("Please specify object enabler in inspector");

        if (associatedEnabler.GetComponent<ObstacleEnabler>().obstacle != obstacle)
            throw new Exception("Obstacles in enabler does not match with disabler obstacle");

        obstacle.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Car") || other.tag.Equals("Car_GA_AIDriving"))
        {
            if (obstacle.activeSelf)
                obstacle.SetActive(false);
        }
    }

}
