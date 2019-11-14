using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SpawnObstacles : MonoBehaviour
{
    public int NumberOfObstacles; // How many obstacles to spawn within the plane

    public GameObject Obstacle;
    public Vector2 SpawnPlane;

    private List<GameObject> _obstaceRefs = new List<GameObject>(); 

    public void SpawnNewSetOfObstacles()
    {
        if (_obstaceRefs.Count > 0)
        {
            foreach (GameObject obst in _obstaceRefs)
            {
                Destroy(obst);
            }
        }

        //Spawn obstablces relative to center
        for (var i = 0; i < NumberOfObstacles; i++)
        {
            GameObject obst = Instantiate(Obstacle);
            if (!obst.activeSelf)
            {
                obst.SetActive(true);
            }

            obst.transform.SetParent(transform);

            obst.transform.localPosition = new Vector3(
                Random.Range(-SpawnPlane.x / 2, SpawnPlane.x / 2), //x
                Random.Range(-SpawnPlane.y / 2, SpawnPlane.y / 2), //y
                0f);

            _obstaceRefs.Add(obst);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, SpawnPlane);
    }
}