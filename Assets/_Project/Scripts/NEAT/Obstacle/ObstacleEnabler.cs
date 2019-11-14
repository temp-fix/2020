using UnityEngine;
using System.Collections;
using System;

public class ObstacleEnabler : MonoBehaviour
{
    public GameObject associatedDisabler;
    public GameObject obstacle;

    void Start()
    {
        if (associatedDisabler == null)
            throw new Exception("Please specify object disabler in inspector");

        if (associatedDisabler.GetComponent<ObstacleDisabler>().obstacle != obstacle)
            throw new Exception("Obstacles in disabler does not match with enabler obstacle");

        obstacle.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Car") || other.tag.Equals("Car_GA_AIDriving"))
        {
            if (!obstacle.activeSelf)
                obstacle.SetActive(true);
        }
    }
}
