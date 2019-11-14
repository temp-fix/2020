using UnityEngine;
using System.Collections;

public class DrivingAIController : MonoBehaviour
{
    public float MaxTimeAlive; //min of 25

    private float _timeAlive;

    void Start()
    {
        if (MaxTimeAlive < 10f)
        {
            MaxTimeAlive = 10f;
        }
    }

    void FixedUpdate()
    {
        _timeAlive += Time.fixedDeltaTime;

        if (_timeAlive >= MaxTimeAlive)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("DrivingAI_end"))
        {
            Destroy(gameObject);
        }
    }
}
