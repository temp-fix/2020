// Track BuildR
// Available on the Unity3D Asset Store
// Copyright (c) 2013 Jasper Stocker http://support.jasperstocker.com
// For support contact email@jasperstocker.com
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using System;
using System.Text;
using System.Xml;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

/// <summary>
/// Track BuildR Track holds all the point and curve data
/// It is responsible for holding other track based data
/// It also generates all the curve data in Recalculate Data which is used in Generator class
/// It has some functions to allow other scripts to access basic track information
/// </summary>

public class TrackBuildRTrack : MonoBehaviour
{
    public enum BumperModes
    {
        Automatic,
        Always,
        AlwaysLeft,
        AlwaysRight
    }

    [SerializeField]
    private List<TrackBuildRPoint> _points = new List<TrackBuildRPoint>();
    
    [SerializeField]
    private bool _looped = true;

    public Transform baseTransform;
    private const float CLIP_THREASHOLD = 0.5f;

    [SerializeField]
    private bool _disconnectBoundary = false;

    [SerializeField]
    private bool _renderBoundaryWallReverse = false;

    public bool includeCollider = true;
    public bool includeColliderRoof = true;
    public float trackColliderWallHeight = 5.0f;

    public bool trackBumpers = true;
    public float bumperAngleThresold = 0.5f;
    public float bumperWidth = 0.5f;
    public float bumperHeight = 0.03f;
    public float additionalBumperHeight = 0.0f;
//    public bool alwaysBumpers = false;
    public BumperModes bumperMode = BumperModes.Automatic;

    //Stunt piece values
    public int loopPoints = 6;
    public float loopRadius = 15.0f;
    public float jumpHeight = 10.0f;
    public float maxJumpLength = 50.0f;
    public float twistAngle = 0.0f;
    public int twistPoints = 6;
    public float twistRadius = 20.0f;

    public bool drawMode = false;

    //Diagram variables
    public GameObject diagramGO;
    public Mesh diagramMesh;
    public string diagramFilepath = "";
    public Material diagramMaterial = null;
    public float scale = 1.0f;
    public Vector3 scalePointA = Vector3.zero;
    public Vector3 scalePointB = Vector3.right;
    public bool showDiagram = false;
    public int assignedPoints = 0;
    
    [SerializeField]
    private bool _showWireframe = true;

    [SerializeField]
    private float _trackLength;

    [SerializeField]
    private float _meshResolution = 1.5f;//the world unit distance for mesh face sizes for nextNormIndex completed mesh
    public float editMeshResolution = 3.0f;//the world unit distance for mesh face sizes - used when editing the track to reduce redraw time

    [SerializeField]
    private List<TrackBuildRTexture> _textures = new List<TrackBuildRTexture>();

    //Terrain
    public float terrainMergeWidth = 10;
    public AnimationCurve mergeCurve = new AnimationCurve(new Keyframe(0,0,0,0), new Keyframe(1,1,0,0));
    public Terrain mergeTerrain = null;
    public float terrainAccuracy = 0.25f;
    public float terrainMergeMargin = 1.75f;
    public float conformAccuracy = 10.0f;

    [SerializeField]
    private bool _tangentsGenerated = false;
    [SerializeField]
    private bool _lightmapGenerated = false;
    [SerializeField]
    private bool _optimised = false;
    [SerializeField]
    private bool _render = true;

    public int lastPolycount = 0;

    [SerializeField]
    private bool _debugKML = false;

    public void InitTextures()
    {
        _textures.Clear();
        TrackBuildRTexture newTrackTexture = gameObject.AddComponent<TrackBuildRTexture>();
        newTrackTexture.customName = "Track Texture";
        _textures.Add(newTrackTexture);
        TrackBuildRTexture newOffroadTexture = gameObject.AddComponent<TrackBuildRTexture>();
        newOffroadTexture.customName = "Offroad Texture";
        _textures.Add(newOffroadTexture);
        TrackBuildRTexture newWallTexture = gameObject.AddComponent<TrackBuildRTexture>();
        newWallTexture.customName = "Wall Texture";
        _textures.Add(newWallTexture);
        TrackBuildRTexture newBumperTexture = gameObject.AddComponent<TrackBuildRTexture>();
        newBumperTexture.customName = "Bumper Texture";
        _textures.Add(newBumperTexture);
        TrackBuildRTexture newExtrudeTexture = gameObject.AddComponent<TrackBuildRTexture>();
        newExtrudeTexture.customName = "Extrude Texture";
        _textures.Add(newExtrudeTexture);
    }

    public void CheckDiagram()
    {
        if (diagramMesh == null)
        {
            diagramMesh = new Mesh();
            diagramMesh.vertices = new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) };
            diagramMesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            diagramMesh.triangles = new[] { 1, 0, 2, 1, 2, 3 };
        }
        if (diagramGO == null)
        {
            diagramGO = new GameObject("Diagram");
            diagramGO.transform.parent = transform;
            diagramGO.transform.localPosition = Vector3.zero;
            diagramGO.AddComponent<MeshFilter>().mesh = diagramMesh;
            diagramMaterial = new Material(Shader.Find("Unlit/Texture"));
            diagramGO.AddComponent<MeshRenderer>().material = diagramMaterial;
            diagramGO.AddComponent<MeshCollider>().sharedMesh = diagramMesh;
        }
    }

    void OnEnable()
    {
        hideFlags = HideFlags.HideInInspector;
    }

    public TrackBuildRPoint this[int index]
    {
        get
        {
            if (_looped && index > _points.Count - 1)//loop value around
                index = index % _points.Count;
            index = Mathf.Clamp(index, 0, _points.Count - 1);
            if (index < 0)
                Debug.LogError("Index can't be minus");
            if (index >= _points.Count)
                Debug.LogError("Index out of range");
            return _points[index];
        }
    }

    public TrackBuildRPoint[] points
    {
        get
        {
            return _points.ToArray();
        }
    }

    public TrackBuildRTexture Texture(int index)
    {
        if (_textures.Count == 0)
            Debug.LogError("There are no textures to access: "+index);
        if(index < 0)
            Debug.LogError("Index can't be minus: " + index);
        if(index >= _textures.Count)
            Debug.LogError("Index out of range: " + index);
        return _textures[Mathf.Clamp(index,0,_textures.Count-1)];
    }

    public void AddTexture()
    {

#if UNITY_EDITOR
        TrackBuildRTexture newTexture = Undo.AddComponent<TrackBuildRTexture>(gameObject);
#else
        TrackBuildRTexture newTexture = gameObject.AddComponent<TrackBuildRTexture>();
#endif
        newTexture.customName = "new texture " + (_textures.Count + 1);
        _textures.Add(newTexture);
    }

    public void RemoveTexture(TrackBuildRTexture texture)
    {
        int textureIndex = _textures.IndexOf(texture);


#if UNITY_EDITOR
        Undo.IncrementCurrentGroup();
        Undo.RecordObject(this, "Remove Texture");
        _textures.Remove(texture);
        Undo.DestroyObjectImmediate(texture);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#else
        _textures.Remove(texture);
#endif

        //ensure that curves are not referenceing textures that no longer exist
        for(int i = 0; i < numberOfCurves; i++)
        {
            TrackBuildRPoint curve = _points[i];

            if (curve.trackTextureStyleIndex > textureIndex)
                curve.trackTextureStyleIndex--;
            if (curve.offroadTextureStyleIndex > textureIndex)
                curve.offroadTextureStyleIndex--;
            if (curve.bumperTextureStyleIndex > textureIndex)
                curve.bumperTextureStyleIndex--;
            if (curve.boundaryTextureStyleIndex > textureIndex)
                curve.boundaryTextureStyleIndex--;
            if (curve.bottomTextureStyleIndex > textureIndex)
                curve.bottomTextureStyleIndex--;
        }
    }

    public int numberOfTextures
    {
        get {return _textures.Count;}
    }

    public TrackBuildRTexture[] GetTexturesArray()
    {
        return _textures.ToArray();
    }

    public int numberOfPoints
    {
        get
        {
            if(_points.Count==0)
                return 0;
            return (_looped) ? _points.Count + 1 : _points.Count;
        }
    }

    public int realNumberOfPoints { get { return _points.Count; } }


    public bool tangentsGenerated { get { return _tangentsGenerated; } }
    public bool lightmapGenerated { get { return _lightmapGenerated; } }
    public bool optimised { get { return _optimised; } }

    public int numberOfCurves
    {
        get
        {
            if (_points.Count < 2)
                return 0;
            return numberOfPoints - 1;
        }
    }

    public bool loop
    {
        get { return _looped; }
        set
        {
            if(_looped!=value)
            {
                _looped = value;
                SetTrackDirty();
                if(!value)
                {//remove last curve
#if UNITY_EDITOR
                    DestroyImmediate(_points[numberOfPoints-1].holder);
#else
                    Destroy(_points[numberOfPoints-1].holder);
#endif
                }
                GUI.changed = true;
            }
        }
    }

    public float trackLength { get { return _trackLength; } }

    public float meshResolution
    {
        get
        {
            return _meshResolution;
        }
        set
        {
            _meshResolution = value;
        }
    }

    public bool disconnectBoundary {get {return _disconnectBoundary;} 
        set
        {
            _disconnectBoundary = value;

            if(value)
            {
                foreach(TrackBuildRPoint point in _points)
                {
                    if (point.leftForwardControlPoint == Vector3.zero)
                        point.leftForwardControlPoint = point.forwardControlPoint;
                    if (point.rightForwardControlPoint == Vector3.zero)
                        point.rightForwardControlPoint = point.forwardControlPoint;

                    point.MatchBoundaryValues();
                }
            }
        }
    }

    public bool showWireframe
    {
        get
        {
            return _showWireframe;
        } 
        set
        {
#if UNITY_EDITOR
            if(_showWireframe!=value)
            {
                foreach(TrackBuildRPoint curve in _points)
                {
                    if (curve.holder == null)
                        continue;
                    foreach (MeshRenderer holderR in curve.holder.GetComponentsInChildren<MeshRenderer>())
                        EditorUtility.SetSelectedWireframeHidden(holderR, !value);
                }
                _showWireframe = value;
            }
#endif
        }
    }

    public bool isDirty
    {
        get
        {
            foreach (TrackBuildRPoint point in _points)
                if (point.isDirty) return true;
            return false;
        }
    }

    public bool renderBoundaryWallReverse
    {
        get {return _renderBoundaryWallReverse;} 
        set
        {
            if (_renderBoundaryWallReverse != value)
            {
                ReRenderTrack();
                _renderBoundaryWallReverse = value;
            }
        }
    }

    public bool render
    {
        get {return _render;} 
        set
        {
            if(value != _render)
            {
                _render = value;
                gameObject.GetComponent<TrackBuildR>().ForceFullRecalculation();
            }
        }
    }

    public TrackBuildRPoint AddPoint(Vector3 position)
    {
#if UNITY_EDITOR
        TrackBuildRPoint point = Undo.AddComponent<TrackBuildRPoint>(gameObject);//ScriptableObject.CreateInstance<TrackBuildRPoint>();
#else
        TrackBuildRPoint point = gameObject.AddComponent<TrackBuildRPoint>();//ScriptableObject.CreateInstance<TrackBuildRPoint>();
#endif
        point.baseTransform = baseTransform;
        point.position = position;
        point.isDirty = true;
        _points.Add(point);
        RecalculateCurves();
        return point;
    }

    public void AddPoint(TrackBuildRPoint point)
    {
        point.baseTransform = baseTransform;
        _points.Add(point);
        RecalculateCurves();
    }

    public void InsertPoint(TrackBuildRPoint point, int index)
    {
        point.baseTransform = baseTransform;
        _points.Insert(index, point);
        RecalculateCurves();
    }

    public TrackBuildRPoint InsertPoint(int index)
    {
#if UNITY_EDITOR
        Undo.IncrementCurrentGroup();
        TrackBuildRPoint point = Undo.AddComponent<TrackBuildRPoint>(gameObject);//ScriptableObject.CreateInstance<TrackBuildRPoint>();
#else
        TrackBuildRPoint point = gameObject.AddComponent<TrackBuildRPoint>();//ScriptableObject.CreateInstance<TrackBuildRPoint>();
#endif
        point.baseTransform = baseTransform;
#if UNITY_EDITOR 
        Undo.RecordObject(this, "Remove Point"); 
#endif
        _points.Insert(index, point);
#if UNITY_EDITOR 
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#endif
        RecalculateCurves();
        return point;
    }

    public void RemovePoint(TrackBuildRPoint point)
    {
        if(_points.Count < 3)
        {
            Debug.Log("We can't see any point in allowing you to delete any more points so we're not going to do it.");
            return;
        }

#if UNITY_EDITOR
        Undo.IncrementCurrentGroup();
        _points.Remove(point);
        Undo.DestroyObjectImmediate(point.holder);
        Undo.DestroyObjectImmediate(point);
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
#else
        _points.Remove(point);
#endif
        RecalculateCurves();
    }

    //Sample nextNormIndex position on the entire curve based on time (0-1)
    public Vector3 GetTrackPosition(float t)
    {
        if(realNumberOfPoints<2)
        {
            Debug.LogError("Not enough points to define a curve");
            return Vector3.zero;
        }
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);

        return SplineMaths.CalculateBezierPoint(ct, pointA.position, pointA.forwardControlPoint, pointB.backwardControlPoint, pointB.position);
    }

    public float GetTrackWidth(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        float hermite = SplineMaths.CalculateHermite(ct);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);
        return Mathf.Lerp(pointA.width, pointB.width, hermite);
    }

    //    public float GetTrackCant(float t)
    //    {
    //        float curveT = 1.0f / numberOfCurves;
    //        int point = Mathf.FloorToInt(t / curveT);
    //        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
    //        float hermite = SplineMaths.CalculateHermite(ct);
    //        TrackBuildRPoint pointA = GetPoint(point);
    //        TrackBuildRPoint pointB = GetPoint(point + 1);
    //        return Mathf.LerpAngle(pointA.cant, pointB.cant, hermite);
    //    }

        public Quaternion GetTrackUp(float t)
        {
            float curveT = 1.0f / numberOfCurves;
            int point = Mathf.FloorToInt(t / curveT);
            float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
            float hermite = SplineMaths.CalculateHermite(ct);
            TrackBuildRPoint pointA = GetPoint(point);
            TrackBuildRPoint pointB = GetPoint(point + 1);
            return Quaternion.Slerp(pointA.trackUpQ, pointB.trackUpQ, hermite);
        }

    public float GetTrackCrownAngle(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        float hermite = SplineMaths.CalculateHermite(ct);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);
        return Mathf.LerpAngle(pointA.crownAngle, pointB.crownAngle, hermite);
    }

    public Vector3 GetTrackCross(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        float hermite = SplineMaths.CalculateHermite(ct);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);
        return Vector3.Slerp(pointA.trackCross, pointB.trackCross, hermite);
    }

    public float GetTrackPercentage(TrackBuildRPoint point)
    {
        int index = _points.IndexOf(point);
        return index / (float)numberOfPoints;
    }

    public float GetTrackPercentage(int pointIndex)
    {
        return pointIndex / (float)numberOfPoints;
    }

    public int GetNearestPointIndex(float trackPercentage)
    {
        return Mathf.RoundToInt(numberOfCurves * trackPercentage);
    }

    public int GetLastPointIndex(float trackPercentage)
    {
        return Mathf.FloorToInt(numberOfCurves * trackPercentage);
    }

    public int GetNextPointIndex(float trackPercentage)
    {
        return Mathf.CeilToInt(numberOfCurves * trackPercentage);
    }

    //Sample nextNormIndex position on the entire curve based on time (0-1)
    public Vector3 GetLeftBoundaryPosition(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);

        return SplineMaths.CalculateBezierPoint(ct, pointA.leftTrackBoundary, pointA.leftForwardControlPoint, pointB.leftBackwardControlPoint, pointB.leftTrackBoundary);
    }
    //Sample nextNormIndex position on the entire curve based on time (0-1)
    public Vector3 GetRightBoundaryPosition(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);

        return SplineMaths.CalculateBezierPoint(ct, pointA.rightTrackBoundary, pointA.rightForwardControlPoint, pointB.rightBackwardControlPoint, pointB.rightTrackBoundary);
    }

    public Vector3 GetTrackDirection(float t)
    {
        float curveT = 1.0f / numberOfCurves;
        int point = Mathf.FloorToInt(t / curveT);
        float ct = Mathf.Clamp01((t - point * curveT) * numberOfCurves);
        float hermite = SplineMaths.CalculateHermite(ct);
        TrackBuildRPoint pointA = GetPoint(point);
        TrackBuildRPoint pointB = GetPoint(point + 1);
        Quaternion trackDirectionA = Quaternion.LookRotation(pointA.trackDirection);
        Quaternion trackDirectionB = Quaternion.LookRotation(pointB.trackDirection);
        Quaternion trackDirectionQ = Quaternion.Slerp(trackDirectionA, trackDirectionB, hermite);
        return trackDirectionQ * Vector3.forward;
    }

    public void RecalculateCurves()
    {
        if (_points.Count < 2)//there is no track with only one point :o)
            return;

        if (isDirty)
        {
            _tangentsGenerated = false;
            _lightmapGenerated = false;
            _optimised = false;
        }

        //calculate approx bezier arc length per curve
        float roughArcLength = 0;
        for (int i = 0; i < realNumberOfPoints; i++)
        {
            TrackBuildRPoint pointA = GetPoint(i);
            TrackBuildRPoint pointB = GetPoint(i+1);
            roughArcLength += Vector3.Distance(pointA.position, pointA.forwardControlPoint);
            roughArcLength += Vector3.Distance(pointA.forwardControlPoint, pointB.backwardControlPoint);
            roughArcLength += Vector3.Distance(pointB.backwardControlPoint, pointB.position);
            _points[i].arcLength = roughArcLength;
            if(roughArcLength == 0)
            {
                DestroyImmediate(pointA);
                i--;
            }
        }

        for (int i = 0; i < realNumberOfPoints; i++)
        {
            TrackBuildRPoint pointA = GetPoint(i);
            TrackBuildRPoint pointB = GetPoint(i + 1);
            TrackBuildRPoint pointC = GetPoint(i - 1);
            pointA.nextPoint = pointB;
            pointA.lastPoint = pointC;
            pointA.pointName = "point " + i;
        }

        foreach (TrackBuildRPoint curve in _points)
        {
            if (!curve.curveIsDirty)//only recalculate modified points
                continue;

            if (curve.arcLength > 0)
            {
                TrackBuildRPoint pointA = curve;
                TrackBuildRPoint pointB = curve.nextPoint;

                //Build accurate arc length data into curve
                curve.center = Vector3.zero;
                Vector3 curveA = pointA.position;
                Vector3 curveP = pointA.forwardControlPoint;
                Vector3 curveQ = pointB.backwardControlPoint;
                Vector3 curveB = pointB.position;
                Vector3 curveALTB = pointA.leftTrackBoundary;
                Vector3 curveARTB = pointA.rightTrackBoundary;
                Vector3 curveBLTB = pointB.leftTrackBoundary;
                Vector3 curveBRTB = pointB.rightTrackBoundary;
                Vector3 curveALFCP = pointA.leftForwardControlPoint;
//                Vector3 curveALBCP = pointA.leftBackwardControlPoint;
                Vector3 curveARFCP = pointA.rightForwardControlPoint;
//                Vector3 curveARBCP = pointA.rightBackwardControlPoint;
//                Vector3 curveBLFCP = pointB.leftForwardControlPoint;
                Vector3 curveBLBCP = pointB.leftBackwardControlPoint;
//                Vector3 curveBRFCP = pointB.rightForwardControlPoint;
                Vector3 curveBRBCP = pointB.rightBackwardControlPoint;

                int _dataSize = Mathf.Max(Mathf.RoundToInt(curve.arcLength / meshResolution), 1);
                float normilisePercentAmount = 1.0f / (_dataSize * 10);
                float normalisePercent = 0;
                float targetMovement = curve.arcLength / (_dataSize - 1);

                List<float> normValues = new List<float>();
                normValues.Add(0);

                float currentLength = 0;
                float targetLength = targetMovement;
                float totalArcLength = 0;

                Vector3 pA = curveA, pB;
                for(; normalisePercent < 1.0f - normilisePercentAmount; normalisePercent += normilisePercentAmount)
                {
                    pB = SplineMaths.CalculateBezierPoint(normalisePercent, curveA, curveP, curveQ, curveB);
                    float arcLength = Vector3.Distance(pA, pB);

                    if(currentLength + arcLength > targetLength)
                    {
                        float lerpPoint = (targetLength - currentLength) / arcLength;
                        float normValue = Mathf.Lerp(normalisePercent, normalisePercent + normilisePercentAmount, lerpPoint);
                        normValues.Add(normValue);
                        curve.center += pB;

                        currentLength = targetLength;
                        targetLength += targetMovement;
                    }

                    currentLength += arcLength;
                    totalArcLength += arcLength;
                    pA = pB;
                }
                normValues.Add(1);

                int storedPointSize = normValues.Count;
                curve.normalisedT = normValues.ToArray();
                curve.arcLength = totalArcLength;
                curve.center /= (storedPointSize-2);
                curve.storedPointSize = storedPointSize;
                curve.clipArrayLeft = new bool[storedPointSize];
                curve.clipArrayRight = new bool[storedPointSize];

                curve.sampledPoints = new Vector3[storedPointSize];
                curve.sampledLeftBoundaryPoints = new Vector3[storedPointSize];
                curve.sampledRightBoundaryPoints = new Vector3[storedPointSize];
                curve.sampledWidths = new float[storedPointSize];
                curve.sampledCrowns = new float[storedPointSize];
                curve.sampledTrackDirections = new Vector3[storedPointSize];
                curve.sampledTrackUps = new Vector3[storedPointSize];
                curve.sampledTrackCrosses = new Vector3[storedPointSize];
                curve.sampledAngles = new float[storedPointSize];
                for (int p = 0; p < storedPointSize; p++)
                {
                    float tN = curve.normalisedT[p];
                    float tH = SplineMaths.CalculateHermite(tN);
                    curve.sampledPoints[p] = SplineMaths.CalculateBezierPoint(tN, curveA, curveP, curveQ, curveB);
                    curve.sampledLeftBoundaryPoints[p] = SplineMaths.CalculateBezierPoint(tN, curveALTB, curveALFCP, curveBLBCP, curveBLTB);
                    curve.sampledRightBoundaryPoints[p] = SplineMaths.CalculateBezierPoint(tN, curveARTB, curveARFCP, curveBRBCP, curveBRTB);
                    curve.sampledWidths[p] = Mathf.LerpAngle(pointA.width, pointB.width, tH);
                    curve.sampledCrowns[p] = Mathf.LerpAngle(pointA.crownAngle, pointB.crownAngle, tH);
                    curve.sampledTrackUps[p] = Quaternion.Slerp(pointA.trackUpQ, pointB.trackUpQ, tH) * Vector3.forward;

                    curve.clipArrayLeft[p] = true;
                    curve.clipArrayRight[p] = true;
                }
            }
        }
        for (int i = 0; i < numberOfCurves; i++)
        {
            TrackBuildRPoint curve = _points[i];
            if (!curve.curveIsDirty)//only recalculate modified points
                continue;
            if (curve.arcLength > 0)
            {
                int lastCurveIndex = (i > 0) ? i - 1 : (_looped) ? numberOfCurves - 1 : 0;
                int nextCurveIndex = (i < numberOfCurves - 1) ? i + 1 : (_looped) ? 0 : numberOfCurves - 1;
                TrackBuildRPoint lastcurve = _points[lastCurveIndex];
                TrackBuildRPoint nextcurve = _points[nextCurveIndex];

                int storedPointSize = curve.storedPointSize;
                for (int p = 0; p < storedPointSize; p++)
                {
                    int pA = p - 1;
                    int pB = p;
                    int pC = p + 1;
                    if(pA < 0) pA = 0;
                    if(pC >= storedPointSize) pC = storedPointSize - 1;

                    Vector3 sampledPointA = curve.sampledPoints[pA];
                    Vector3 sampledPointB = curve.sampledPoints[pB];
                    Vector3 sampledPointC = curve.sampledPoints[pC];

                    if(p == 0 && lastcurve != null && lastcurve.sampledPoints.Length > 1) 
                        sampledPointA = lastcurve.sampledPoints[lastcurve.storedPointSize - 2];//retrieve the penultimate point from the last curve
                    if(p == storedPointSize - 1 && nextcurve != null && nextcurve.sampledPoints.Length > 1)
                        sampledPointC = nextcurve.sampledPoints[1];//retrieve the second point from the next curve

                    Vector3 sampledTrackDirectionA = (sampledPointB - sampledPointA);
                    Vector3 sampledTrackDirectionB = (sampledPointC - sampledPointB);
                    Vector3 sampledTrackDirection = (sampledTrackDirectionA + sampledTrackDirectionB).normalized;
                    curve.sampledTrackDirections[pB] = sampledTrackDirection;
                    curve.sampledTrackCrosses[pB] = Vector3.Cross(curve.sampledTrackUps[pB], sampledTrackDirection);
                    curve.sampledAngles[pB] = Vector3.Angle(sampledTrackDirectionA, sampledTrackDirectionB) * -Mathf.Sign(Vector3.Dot((sampledTrackDirectionB - sampledTrackDirectionA), curve.sampledTrackCrosses[pB]));

                    curve.clipArrayLeft[pB] = true;
                    curve.clipArrayRight[pB] = true;
                }
            }
        }

        bool dirtyTextures = false;
        foreach (TrackBuildRTexture texture in _textures)
        {
            if (texture.isDirty)
                dirtyTextures = true;//if nextNormIndex point was dirty, ensure it's rerendered
            texture.isDirty = false;
        }

        foreach(TrackBuildRPoint point in _points)
            if (point.curveIsDirty || dirtyTextures)
                point.shouldReRender = true;//if nextNormIndex point was dirty, ensure it's rerendered

        //clean points
        foreach (TrackBuildRPoint point in _points)
            point.isDirty = false;//reset all points - data is no longer considered dirty

        //recalculate track length
        _trackLength = 0;
        foreach(TrackBuildRPoint curve in _points)
            _trackLength += curve.arcLength;
    }

    public float GetNearestPoint(Vector3 fromPostition)
    {
        int testPoints = 10;
        float testResolution = 1.0f / testPoints;
        float nearestPercentage = 0;
        float nextNearestPercentage = 0;
        float nearestPercentageSqrDistance = Mathf.Infinity;
        float nextNearestPercentageSqrDistance = Mathf.Infinity;
        for (float i = 0; i < 1; i += testResolution)
        {
            Vector3 point = GetTrackPosition(i);
            Vector3 difference = point - fromPostition;
            float newSqrDistance = Vector3.SqrMagnitude(difference);
            if (nearestPercentageSqrDistance > newSqrDistance)
            {
                nearestPercentage = i;
                nearestPercentageSqrDistance = newSqrDistance;
            }
        }
        nextNearestPercentage = nearestPercentage;
        nextNearestPercentageSqrDistance = nearestPercentageSqrDistance;
        int numberOfRefinments = Mathf.RoundToInt(Mathf.Pow(_trackLength * 10, 1.0f / 5.0f));
        Debug.Log(numberOfRefinments);
        for (int r = 0; r < numberOfRefinments; r++)
        {
            float refinedResolution = testResolution / testPoints;
            float startSearch = nearestPercentage - testResolution / 2;
            float endSearch = nearestPercentage + testResolution / 2;
            for (float i = startSearch; i < endSearch; i += refinedResolution)
            {
                Vector3 point = GetTrackPosition(i);
                Vector3 difference = point - fromPostition;
                float newSqrDistance = Vector3.SqrMagnitude(difference);
                if (nearestPercentageSqrDistance > newSqrDistance)
                {
                    nextNearestPercentage = nearestPercentage;
                    nextNearestPercentageSqrDistance = nearestPercentageSqrDistance;

                    nearestPercentage = i;
                    nearestPercentageSqrDistance = newSqrDistance;
                }
                else
                {
                    if (nextNearestPercentageSqrDistance > newSqrDistance)
                    {
                        nextNearestPercentage = i;
                        nextNearestPercentageSqrDistance = newSqrDistance;
                    }
                }
            }
            testResolution = refinedResolution;
        }
        float lerpvalue = nearestPercentageSqrDistance / (nearestPercentageSqrDistance + nextNearestPercentageSqrDistance);
        return Mathf.Clamp01(Mathf.Lerp(nearestPercentage, nextNearestPercentage, lerpvalue));
    }

    public void Clear()
    {

        int numCurves = numberOfCurves;
        for (int i = 0; i < numCurves; i++)
        {
            if (_points[i].holder != null)
            {
                DestroyImmediate(_points[i].holder);
            }
        }
        _points.Clear();
//        _textures.Clear();
//        InitTextures();
    }


    public TrackBuildRPoint GetPoint(int index)
    {
        if (_points.Count == 0)
            return null;
        if (!_looped)
        {
            return _points[Mathf.Clamp(index, 0, numberOfCurves)];
        }
        if (index >= numberOfCurves)
            index = index - numberOfCurves;
        if (index < 0)
            index = index + numberOfCurves;

        return _points[index];
    }


    private float SignedAngle(Vector3 from, Vector3 to, Vector3 up)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 cross = Vector3.Cross(up, direction);
        float dot = Vector3.Dot(from, cross);
        return Vector3.Angle(from, to) * Mathf.Sign(dot);
    }

    /// <summary>
    /// Set all point data as rendered
    /// </summary>
    public void TrackRendered()
    {
        foreach (TrackBuildRPoint point in _points)
        {
            point.shouldReRender = false;
        }
//        int numberOfPitlanPoints = pitlane.numberOfPoints;
//        for (int i = 0; i < numberOfPitlanPoints; i++)
//        {
//            pitlane[i].shouldReRender = false;
//        }
    }

    /// <summary>
    /// Mark the entire track as dirty so it will be recalculated/rebuilt
    /// </summary>
    public void SetTrackDirty()
    {
        foreach (TrackBuildRPoint point in _points)
        {
            point.isDirty = true;
        }
//        for (int i = 0; i < pitlane.numberOfPoints; i++)
//        {
//            pitlane[i].isDirty = true;
//        }
//        SetPitDirty();
    }

    /// <summary>
    /// Set all point data to rerender
    /// </summary>
    public void ReRenderTrack()
    {
        foreach (TrackBuildRPoint point in _points)
        {
            point.shouldReRender = true;
        }
//        for (int i = 0; i < pitlane.numberOfPoints; i++)
//        {
//            pitlane[i].shouldReRender = true;
//        }
//        ReRenderPit();
    }

//    /// <summary>
//    /// Mark the pit lane as dirty so it will be recalculated/rebuilt
//    /// </summary>
//    public void SetPitDirty()
//    {
//        for (int i = 0; i < pitlane.numberOfPoints; i++)
//        {
//            pitlane[i].isDirty = true;
//        }
//    }
//
//    /// <summary>
//    /// Set pit lane point data to rerender
//    /// </summary>
//    public void ReRenderPit()
//    {
//        for (int i = 0; i < pitlane.numberOfPoints; i++)
//        {
//            pitlane[i].shouldReRender = true;
//        }
//    }

    public void GenerateSecondaryUVSet()
    {
#if UNITY_EDITOR
        for (int i = 0; i < numberOfCurves; i++)
        {
            TrackBuildRPoint curve = _points[i];

            List<Mesh> unwrapList = new List<Mesh>();
            unwrapList.AddRange(curve.dynamicTrackMesh.meshes);
            unwrapList.AddRange(curve.dynamicOffroadMesh.meshes);
            unwrapList.AddRange(curve.dynamicBumperMesh.meshes);
            unwrapList.AddRange(curve.dynamicBoundaryMesh.meshes);
            unwrapList.AddRange(curve.dynamicBottomMesh.meshes);

            int count = unwrapList.Count;
            for(int m = 0; m < count; m++)
            {
                if (unwrapList[m].vertexCount==0)
                    continue;
                Unwrapping.GenerateSecondaryUVSet(unwrapList[m]);
            }
        }
        _lightmapGenerated = true;
#endif
    }

    public void OptimseMeshes()
    {
#if UNITY_EDITOR
        for (int i = 0; i < numberOfCurves; i++)
        {
            TrackBuildRPoint curve = _points[i];

            Mesh[] meshes = curve.dynamicTrackMesh.meshes;
            for (int m = 0; m < curve.dynamicTrackMesh.meshCount; m++)
                MeshUtility.Optimize(meshes[m]);

            meshes = curve.dynamicOffroadMesh.meshes;
            for (int m = 0; m < curve.dynamicOffroadMesh.meshCount; m++)
                MeshUtility.Optimize(meshes[m]);

            meshes = curve.dynamicBumperMesh.meshes;
            for (int m = 0; m < curve.dynamicBumperMesh.meshCount; m++)
                MeshUtility.Optimize(meshes[m]);

            meshes = curve.dynamicBoundaryMesh.meshes;
            for (int m = 0; m < curve.dynamicBoundaryMesh.meshCount; m++)
                MeshUtility.Optimize(meshes[m]);

            meshes = curve.dynamicBottomMesh.meshes;
            for (int m = 0; m < curve.dynamicBottomMesh.meshCount; m++)
                MeshUtility.Optimize(meshes[m]);
        }
        _optimised = true;
#endif
    }


#if UNITY_EDITOR
    public virtual string ToXML()
    {
        //todo track variables...
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<track>");
        sb.AppendLine("<textures>");
        foreach (TrackBuildRTexture texture in _textures)
            sb.Append(texture.ToXML());
        sb.AppendLine("</textures>");
        sb.AppendLine("<trackpoints>");
        foreach (TrackBuildRPoint point in _points)
            sb.Append(point.ToXML());
        sb.AppendLine("</trackpoints>");
        sb.AppendLine("<meshResolution>" + _meshResolution.ToString("F3") + "</meshResolution>");
        sb.AppendLine("<includeCollider>" + includeCollider + "</includeCollider>");
        sb.AppendLine("<includeColliderRoof>" + includeColliderRoof + "</includeColliderRoof>");
        sb.AppendLine("<trackColliderWallHeight>" + trackColliderWallHeight.ToString("F3") + "</trackColliderWallHeight>");
        sb.AppendLine("<trackBumpers>" + trackBumpers + "</trackBumpers>");
        sb.AppendLine("<bumperHeight>" + bumperHeight.ToString("F3") + "</bumperHeight>");
        sb.AppendLine("<bumperWidth>" + bumperWidth.ToString("F3") + "</bumperWidth>");
        sb.AppendLine("<bumperAngleThresold>" + bumperAngleThresold.ToString("F3") + "</bumperAngleThresold>");
        sb.AppendLine("<disconnectBoundary>" + _disconnectBoundary + "</disconnectBoundary>");
        sb.AppendLine("<renderBoundaryWallReverse>" + _renderBoundaryWallReverse + "</renderBoundaryWallReverse>");
        
        sb.AppendLine("</track>");
        return sb.ToString();
    }

    public virtual void FromXML(XmlNode node)
    {
        XmlNodeList textureNodes = node.SelectNodes("textures/texture");
        foreach(XmlNode textureNode in textureNodes)
        {
            TrackBuildRTexture newTexture = gameObject.AddComponent<TrackBuildRTexture>();
            newTexture.FromXML(textureNode);
            _textures.Add(newTexture);
        }

        XmlNodeList trackPointNodes = node.SelectNodes("trackpoints/trackpoint");
        foreach(XmlNode trackPointNode in trackPointNodes)
        {

            TrackBuildRPoint point = gameObject.AddComponent<TrackBuildRPoint>();
            point.FromXML(trackPointNode);
            point.baseTransform = baseTransform;
            point.isDirty = true;
            _points.Add(point);
        }

        if (node["meshResolution"] != null)
            _meshResolution = float.Parse(node["meshResolution"].FirstChild.Value);
        if (node["includeCollider"] != null)
            includeCollider = bool.Parse(node["includeCollider"].FirstChild.Value);
        if (node["includeColliderRoof"] != null)
            includeColliderRoof = bool.Parse(node["includeColliderRoof"].FirstChild.Value);
        if (node["trackColliderWallHeight"] != null)
            trackColliderWallHeight = float.Parse(node["trackColliderWallHeight"].FirstChild.Value);
        if (node["trackBumpers"] != null)
            trackBumpers = bool.Parse(node["trackBumpers"].FirstChild.Value);
        if (node["bumperHeight"] != null)
            bumperHeight = float.Parse(node["bumperHeight"].FirstChild.Value);
        if (node["bumperWidth"] != null)
            bumperWidth = float.Parse(node["bumperWidth"].FirstChild.Value);
        if (node["bumperAngleThresold"] != null)
            bumperAngleThresold = float.Parse(node["bumperAngleThresold"].FirstChild.Value);
        if (node["disconnectBoundary"] != null)
            _disconnectBoundary = bool.Parse(node["disconnectBoundary"].FirstChild.Value);
        if (node["renderBoundaryWallReverse"] != null)
            _renderBoundaryWallReverse = bool.Parse(node["renderBoundaryWallReverse"].FirstChild.Value);


        RecalculateCurves();
    }
#endif
    
#if UNITY_EDITOR
    public void FromKML(string coordinates)
    {
        Clear();
        string[] coorArray = coordinates.Split(new []{" ","\n","\t"}, StringSplitOptions.RemoveEmptyEntries);
        List<Vector3> kmlpoints = new List<Vector3>();
        Vector3 xyCenter = new Vector3();
        Vector3 lastCoord = Vector3.zero;
        int numberOfCoords = coorArray.Length;
        if(numberOfCoords == 0)
        {
            Debug.LogError("Track BuildR KML Import Error: Unable to extract a Track from this KML, please contact Jasper at email@jasperstocker.com for assistance");
            Debug.LogError("Coordinates "+ coordinates);
        }
        for (int i = 0; i < numberOfCoords; i++)
        {
            string coord = coorArray[i];
            if (coord == "")
                continue;

            string[] coordEntry = coord.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);

            if (coordEntry.Length < 3)
                continue;

            float KMLPointX = float.Parse(coordEntry[0]);
            float KMLPointY = float.Parse(coordEntry[2]);
            float KMLPointZ = float.Parse(coordEntry[1]);
            Vector3 newKMLPoint = new Vector3(KMLPointX, KMLPointY, KMLPointZ);
            if (_debugKML) Debug.Log(i + ". " + newKMLPoint);

            if (Vector3.Distance(newKMLPoint, lastCoord) < Mathf.Epsilon && lastCoord != Vector3.zero)
            {
                if(_debugKML) Debug.Log(i + ". skip duplicate point");
                continue;//skip duplicate point
            }

            
            kmlpoints.Add(newKMLPoint);
            xyCenter.x += newKMLPoint.x;
            xyCenter.z += newKMLPoint.z;

            lastCoord = newKMLPoint;
        }

        int numberOfKMLPoints = kmlpoints.Count;
        if (_debugKML) Debug.Log("number of KML points: " + numberOfKMLPoints);
        xyCenter.x *= 1.0f / numberOfKMLPoints;
        xyCenter.z *= 1.0f / numberOfKMLPoints;

        if (Vector3.Distance(kmlpoints[numberOfKMLPoints - 1], kmlpoints[0]) < 1.0f)
            kmlpoints.RemoveAt(numberOfKMLPoints - 1);
        numberOfKMLPoints = kmlpoints.Count;

        List<Vector3> eulerpoints = new List<Vector3>();
        float dataTrackLength = 0;
        Vector3 lastPoint = Vector3.zero;
        for (int i = 0; i < numberOfKMLPoints; i++)
        {
            Vector3 KMLPoint = kmlpoints[i];

            Vector3 newEulerPoint = GPSMaths.Simple(xyCenter, KMLPoint);
            newEulerPoint.y = KMLPoint.y;
            eulerpoints.Add(newEulerPoint);

            if(i > 0)
                dataTrackLength += Vector3.Distance(lastPoint, newEulerPoint);
            lastPoint = newEulerPoint;
        }


        if (dataTrackLength > 20000)
            if (!EditorUtility.DisplayDialog("Really Long Track!", "The Data is creating a track length around " + (dataTrackLength / 1000).ToString("F1") + "km long", "Continue", "Cancel"))
                return;

        if (dataTrackLength < 1)
            if (EditorUtility.DisplayDialog("No Track!", "The Data not producing a viable track", "Ok"))
                return;

        for(int i = 0; i < numberOfKMLPoints; i++)
        {
            TrackBuildRPoint point = gameObject.AddComponent<TrackBuildRPoint>();
            point.baseTransform = baseTransform;
            point.position = eulerpoints[i];
            point.isDirty = true;
            _points.Add(point);
        }

        for (int i = 0; i < numberOfKMLPoints; i++)
        {
            TrackBuildRPoint point = _points[i];
            Vector3 thisPosition = point.worldPosition;
            Vector3 lastPosition = GetPoint(i - 1).worldPosition;
            Vector3 nextPosition = GetPoint(i + 1).worldPosition;
            Vector3 backwardTrack = thisPosition - lastPosition;
            Vector3 forwardTrack = nextPosition - thisPosition;
            Vector3 controlDirection = (backwardTrack + forwardTrack).normalized;
            float forwardMag = forwardTrack.magnitude;
            float backwardMag = backwardTrack.magnitude;
            if (forwardMag == 0)
                forwardMag = backwardMag;
            if (backwardMag == 0)
                backwardMag = forwardMag;
            float controlMagnitude = Mathf.Min(forwardMag, backwardMag) * 0.1666f;
            point.forwardControlPoint = (controlDirection * controlMagnitude) + thisPosition;
        }
        InitTextures();
        RecalculateCurves();
    }
#endif
}
