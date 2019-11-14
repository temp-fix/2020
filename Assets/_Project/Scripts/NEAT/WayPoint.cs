using UnityEngine;
using System.Collections;

public class WayPoint : MonoBehaviour {
    public void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Car") || other.tag.Equals("Car_GA_AIDriving"))
        {
            if (other.GetComponent<NEATCarInputHandler>() != null)
                other.GetComponent<NEATCarInputHandler>().WayPointEntered(this.gameObject);
        }
    }
}
