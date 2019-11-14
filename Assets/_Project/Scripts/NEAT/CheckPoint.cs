using UnityEngine;
using System.Collections;

public class CheckPoint : MonoBehaviour
{
    public CheckPoint PreviousCheckPoint; //The check point that preceeds this one

    public void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Car"))
        {
            if (other.GetComponent<NEATCarInputHandler>() != null)
                other.GetComponent<NEATCarInputHandler>().CheckPointEntered(this.gameObject);
        }
    }
}
