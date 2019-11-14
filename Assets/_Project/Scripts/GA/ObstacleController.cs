using UnityEngine;
using System.Collections;

public class ObstacleController : MonoBehaviour
{
    void Start()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.black, 0.5f);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.tag.Equals("Road"))
            {
                transform.position = hit.point;
                transform.Translate(Vector3.up * Random.value); //shift them up a bit by a random ammaount
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
