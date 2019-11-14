using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorConfigRadarSensor : SensorConfigSensor
{
    Material collisionMaterial;
    Material defaultMaterial;

    Vector3 obstaclePosition;
    bool detect = true;
    private int frame = 0;

    //private WaitForEndOfFrame waitForEndFrame;

    private void Start()
    {
        //waitForEndFrame = new WaitForEndOfFrame();

        collisionMaterial = new Material(Shader.Find("Skidmarks"));
        defaultMaterial = new Material(Shader.Find("Skidmarks"));
        defaultMaterial.color = new Color(NotHittingColor.r, NotHittingColor.g, NotHittingColor.b, 0.05f);

        Camera radarCamera = gameObject.AddComponent<Camera>();
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        gameObject.AddComponent<MeshRenderer>();

        radarCamera.fieldOfView = FOV;
        radarCamera.farClipPlane = Range;
        radarCamera.nearClipPlane = 0.1f;
        meshCollider.sharedMesh = CameraExtention.GenerateFrustumMesh(radarCamera);
        meshCollider.convex = true;
        meshCollider.isTrigger = true;
        meshFilter.mesh = meshCollider.sharedMesh;
        Destroy(this.GetComponent<Camera>());
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (detect)
        {
            float distanceToObstacle = 0f;
            distanceToObstacle = Vector3.Distance(transform.position, getObstacleWorldPosition(collider.gameObject));

            if (DetermineObjectCollision(collider, distanceToObstacle))
                collisionMaterial.color = new Color(SensorColor.r, SensorColor.g, SensorColor.b, 0.5f);

            frame = 0;
        }
    }

    private void OnTriggerStay(Collider collider)
    {
        if (detect)
        {
            if (ObstaclesCurrentlyInDetection.ContainsKey(collider.gameObject) && frame % 13 == 0)
            {
                Vector3 obstacleWorldPosition = getObstacleWorldPosition(collider.gameObject);
                ObstaclesCurrentlyInDetection[collider.gameObject] = Vector3.Distance(transform.position, obstacleWorldPosition);
                //Debug.DrawRay(transform.position, obstacleWorldPosition - transform.position, detectableObjectTagsAndColor[collider.gameObject.tag]);
            }

            frame++;
        }
    }

    //IEnumerator OnTriggerStay(Collider collider)
    //{
    //    if (ObstaclesCurrentlyInDetection.ContainsKey(collider.gameObject) && frame % 12 == 0)
    //    {
    //        Vector3 obstacleWorldPosition = getObstacleWorldPosition(collider.gameObject);
    //        ObstaclesCurrentlyInDetection[collider.gameObject] = Vector3.Distance(transform.position, obstacleWorldPosition);
    //        Debug.DrawRay(transform.position, obstacleWorldPosition - transform.position, detectableObjectTagsAndColor[collider.gameObject.tag]);
    //    }
    //        frame++;

    //    yield return waitForEndFrame;
    //}

    private void OnTriggerExit(Collider collider)
    {
        RemoveObstaclesCurrentlyInDetection(collider.gameObject);
    }

    private void Update()
    {
        if (detect)
        {
            if (IsHitting())
                this.GetComponent<Renderer>().material = collisionMaterial;
            else
                this.GetComponent<Renderer>().material = defaultMaterial;

            detect = gameObject.GetComponentInParent<NEATCarInputHandler>().IsRunning;
        } else
        {
            Destroy(gameObject);
        }
    }

    private Vector3 getObstacleWorldPosition(GameObject obstacle)
    {
        Vector3 worldPosition = obstacle.transform.position;

        if (obstacle.GetComponent<MeshCollider>() != null)
        {
            Mesh mesh = obstacle.GetComponent<MeshCollider>().sharedMesh;
            VertTriList vt = new VertTriList(mesh);
            Vector3 objSpacePt = obstacle.transform.InverseTransformPoint(gameObject.transform.position);
            Vector3[] verts = mesh.vertices;
            KDTreeRadar kd = KDTreeRadar.MakeFromPoints(verts);
            Vector3 meshPt = NearestPointOnMesh(objSpacePt, verts, kd, mesh.triangles, vt);
            Vector3 worldPt = obstacle.transform.TransformPoint(meshPt);
            worldPosition = worldPt;
        }

        return worldPosition;
    }


    //Methods used to determine point closest to radar on obstacle
    Vector3 NearestPointOnMesh(Vector3 pt, Vector3[] verts, KDTreeRadar vertProx, int[] tri, VertTriList vt)
    {
        //	First, find the nearest vertex (the nearest point must be on one of the triangles
        //	that uses this vertex if the mesh is convex).
        int nearest = vertProx.FindNearest(pt);

        //	Get the list of triangles in which the nearest vert "participates".
        int[] nearTris = vt[nearest];

        Vector3 nearestPt = Vector3.zero;
        float nearestSqDist = 100000000f;

        for (int i = 0; i < nearTris.Length; i++)
        {
            int triOff = nearTris[i] * 3;
            Vector3 a = verts[tri[triOff]];
            Vector3 b = verts[tri[triOff + 1]];
            Vector3 c = verts[tri[triOff + 2]];

            Vector3 possNearestPt = NearestPointOnTri(pt, a, b, c);
            float possNearestSqDist = (pt - possNearestPt).sqrMagnitude;

            if (possNearestSqDist < nearestSqDist)
            {
                nearestPt = possNearestPt;
                nearestSqDist = possNearestSqDist;
            }
        }


        return nearestPt;
    }

    Vector3 NearestPointOnTri(Vector3 pt, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 edge1 = b - a;
        Vector3 edge2 = c - a;
        Vector3 edge3 = c - b;
        float edge1Len = edge1.magnitude;
        float edge2Len = edge2.magnitude;
        float edge3Len = edge3.magnitude;

        Vector3 ptLineA = pt - a;
        Vector3 ptLineB = pt - b;
        Vector3 ptLineC = pt - c;
        Vector3 xAxis = edge1 / edge1Len;
        Vector3 zAxis = Vector3.Cross(edge1, edge2).normalized;
        Vector3 yAxis = Vector3.Cross(zAxis, xAxis);

        Vector3 edge1Cross = Vector3.Cross(edge1, ptLineA);
        Vector3 edge2Cross = Vector3.Cross(edge2, -ptLineC);
        Vector3 edge3Cross = Vector3.Cross(edge3, ptLineB);
        bool edge1On = Vector3.Dot(edge1Cross, zAxis) > 0f;
        bool edge2On = Vector3.Dot(edge2Cross, zAxis) > 0f;
        bool edge3On = Vector3.Dot(edge3Cross, zAxis) > 0f;

        //	If the point is inside the triangle then return its coordinate.
        if (edge1On && edge2On && edge3On)
        {
            float xExtent = Vector3.Dot(ptLineA, xAxis);
            float yExtent = Vector3.Dot(ptLineA, yAxis);
            return a + xAxis * xExtent + yAxis * yExtent;
        }

        //	Otherwise, the nearest point is somewhere along one of the edges.
        Vector3 edge1Norm = xAxis;
        Vector3 edge2Norm = edge2.normalized;
        Vector3 edge3Norm = edge3.normalized;

        float edge1Ext = Mathf.Clamp(Vector3.Dot(edge1Norm, ptLineA), 0f, edge1Len);
        float edge2Ext = Mathf.Clamp(Vector3.Dot(edge2Norm, ptLineA), 0f, edge2Len);
        float edge3Ext = Mathf.Clamp(Vector3.Dot(edge3Norm, ptLineB), 0f, edge3Len);

        Vector3 edge1Pt = a + edge1Ext * edge1Norm;
        Vector3 edge2Pt = a + edge2Ext * edge2Norm;
        Vector3 edge3Pt = b + edge3Ext * edge3Norm;

        float sqDist1 = (pt - edge1Pt).sqrMagnitude;
        float sqDist2 = (pt - edge2Pt).sqrMagnitude;
        float sqDist3 = (pt - edge3Pt).sqrMagnitude;

        if (sqDist1 < sqDist2)
        {
            if (sqDist1 < sqDist3)
            {
                return edge1Pt;
            }
            else {
                return edge3Pt;
            }
        }
        else if (sqDist2 < sqDist3)
        {
            return edge2Pt;
        }
        else {
            return edge3Pt;
        }
    }

}
