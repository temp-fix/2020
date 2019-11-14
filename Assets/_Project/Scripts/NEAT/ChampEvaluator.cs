using Assets.Car;
using Mono.Data.SqliteClient;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class ChampEvaluator : MonoBehaviour
{
    private readonly Dictionary<IBlackBox, UnitController> ControllerMap = new Dictionary<IBlackBox, UnitController>();
    private SimpleExperiment experiment;

    private string champFileSavePath;
    private readonly float updateInterval = 12;
    private DatabaseLogging dbLogging;
    public GameObject[] cars;
    private TrackChanger trackChanger;
    private int currentCameraIndex;
    private int currentCarIndex;
    private bool IsChecking;

    private string playPauseLabel = "";

    public string champColumnInDB = "champ_xml";
    public string champSubfolder = "champ_eval";
    public string run_database = "";
    private List<GameObject> cameras;
    public GameObject mainCamera;
    public string[] RunID;
    private XmlDocument xmlConfig;
    public bool AutoStart;
    private int trials = 0;
    public int TrialsToRun = 5;
    private int currentRun = 0;
    public int TrialDuration = 60;
    private int seconds;
    private int trialDurationFrames;
    // Use this for initialization

    private void Awake()
    {
        RunID = new string[20] {
"4/17/2019 12:48:09 AM",
"4/17/2019 12:48:20 AM",
"4/17/2019 6:35:05 AM",
"4/17/2019 6:35:15 AM",
"4/17/2019 9:28:42 AM",
"4/17/2019 9:29:07 AM",
"4/17/2019 12:22:32 PM",
"4/17/2019 12:22:45 PM",
"4/17/2019 3:16:24 PM",
"4/17/2019 6:10:02 PM",
"4/17/2019 9:03:34 PM",
"5/4/2019 10:06:16 AM",
"5/4/2019 1:03:49 PM",
"5/4/2019 6:52:23 PM",
"5/4/2019 9:46:17 PM",
"5/5/2019 12:39:56 AM",
"5/5/2019 3:33:41 AM",
"5/5/2019 6:27:44 AM",
"5/5/2019 9:21:56 AM",
"5/5/2019 12:16:06 PM"
        };

        trackChanger = gameObject.GetComponent<TrackChanger>();
        dbLogging = gameObject.GetComponent<DatabaseLogging>();
    }

    private void Start()
    {
        IsChecking = false;
        cameras = new List<GameObject>();
        cameras.Add(mainCamera);
        Utility.DebugLog = false;
        experiment = new SimpleExperiment();
        xmlConfig = new XmlDocument();
        var textAsset = (TextAsset)Resources.Load("experiment.config");
        xmlConfig.LoadXml(textAsset.text);

        if (RunID.Length == 0)
        {
            var dirInf = new DirectoryInfo(Application.persistentDataPath + string.Format("/{0}", champSubfolder));
            if (!dirInf.Exists)
            {
                Debug.Log("Creating subdirectory");
                dirInf.Create();
            }

            foreach (GameObject car in cars)
            {
                string champFile = Application.persistentDataPath + string.Format("/{0}/{1}.champ.xml", champSubfolder, car.name);
                if (!File.Exists(champFile))
                {
                    File.Create(champFile);
                }
            }
        }

        playPauseLabel = "Pause";
        Time.timeScale = 3;
        seconds = 0;
        trialDurationFrames = TrialDuration * 50;
    }

    private void FixedUpdate()
    {
        if (seconds >= trialDurationFrames)
        {
            foreach (var controller in ControllerMap.Values)
                controller.Stop();
        }

        if (IsChecking)
            seconds++;
    }

    private void Update()
    {
        if (IsChecking)
        {
            if (ControllerMap.Values.All(controller => controller.Stopped == true))
            { //Checks if all cars have stopped
                IsChecking = false;
                LogAllVehicleFitnessesToDb();
                RemoveAllCars();

                if (currentRun < RunID.Length - 1)
                    currentRun++;
                else
                {
                    trackChanger.nextTrack();
                    currentRun = 0;

                    if (trackChanger.CurrentTrackIndex == 0)
                        trials++;
                }

                if (trials < TrialsToRun)
                {
                    AddAll();
                    seconds = 0;
                }
            }
        } else {
            if (AutoStart)
            {
                if (trials < TrialsToRun)
                    AddAll();
                else
                    Application.Quit();

                //Time.timeScale = 5;
                //playPauseLabel = "Pause";
            }
        }
    }

    private void LogAllVehicleFitnessesToDb()
    {
        Dictionary<NEATCarInputHandler, float> fitness = new Dictionary<NEATCarInputHandler, float>();

        foreach (UnitController controller in ControllerMap.Values)
        {
            NEATCarInputHandler[] individualCars = controller.gameObject.GetComponentsInChildren<NEATCarInputHandler>();

            foreach (NEATCarInputHandler car in individualCars)
            {
                fitness.Add(car, car.GetFitness());
            }
        }

        //dbLogging.LogEvaluation("360", RunID[currentRun], sensorInputMinimums, sensorInputAverages, sensorInputMaximums, fitness.Values.Average());
        dbLogging.LogEvaluation(run_database, RunID[currentRun], cars[currentCarIndex].name, fitness.Values.Average(), trials + 1, trackChanger.CurrentTrack.name);
    }

    private void LogAllVehiclesToDatabaseRadar()
    {
        List<float> sensorInputMinimums = new List<float>();
        List<float> sensorInputAverages = new List<float>();
        List<float> sensorInputMaximums = new List<float>();
        Dictionary<NEATCarInputHandler, float> fitness = new Dictionary<NEATCarInputHandler, float>();

        foreach (UnitController controller in ControllerMap.Values)
        {
            NEATCarInputHandler[] individualCars = controller.gameObject.GetComponentsInChildren<NEATCarInputHandler>();

            foreach (NEATCarInputHandler car in individualCars)
            {
                fitness.Add(car, car.GetFitness());
            }

            foreach (string key in controller.SensorInputs.Keys)
            {
                sensorInputMinimums.Add(controller.SensorInputs[key].Min());
                sensorInputAverages.Add(controller.SensorInputs[key].Average());
                sensorInputMaximums.Add(controller.SensorInputs[key].Max());
            }
        }

        dbLogging.LogEvaluation("360", RunID[currentRun], sensorInputMinimums, sensorInputAverages, sensorInputMaximums, fitness.Values.Average());
    }

    private void LogAllVehiclesToDatabase()
    {
        foreach (UnitController controller in ControllerMap.Values)
        {
            float dist = controller.gameObject.GetComponent<DistanceTravelledCalculator>().DistanceTravelled;

            //remove leading "BMW_"
            string sensor = controller.gameObject.name.Substring(4);
            //remove trailing "(Clone)"
            sensor = sensor.Substring(0, sensor.Length - 7);

            string startingMarker = controller.gameObject.GetComponent<DistanceTravelledCalculator>().StartingMarker;

            List<float> sensorInputAverages = new List<float>();

            foreach (string key in controller.SensorInputs.Keys)
            {
                sensorInputAverages.Add(controller.SensorInputs[key].Sum() / controller.gameObject.GetComponent<DistanceTravelledCalculator>().timesteps);
            }

            dbLogging.LogEvaluation(sensor, startingMarker, dist, "", sensorInputAverages);
        }
    }

    private void RemoveAllCars()
    {
        List<IBlackBox> keysToRemove = new List<IBlackBox>();

        foreach (IBlackBox key in ControllerMap.Keys)
        {
            Destroy(ControllerMap[key].gameObject);
            keysToRemove.Add(key);
        }

        foreach (IBlackBox key in keysToRemove)
        {
            ControllerMap.Remove(key);
        }
    }

    public void AddChamp()
    {
        //GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 150, 70), "Please select track");

        NeatGenome genome = null;
        UnitController unit_controller = cars[currentCarIndex].GetComponent<UnitController>();
        experiment.Initialize("Car Experiment", xmlConfig.DocumentElement, unit_controller.GetNumberOfInputsIntoNeat(), unit_controller.GetNumberOfOutputsNeededFromNeat());

        if (RunID.Length == 0)
        {
            champFileSavePath = Application.persistentDataPath + string.Format("/{0}/{1}.champ.xml", champSubfolder, cars[currentCarIndex].name);
            // Try to load the genome from the XML document.
            try
            {
                using (var xr = XmlReader.Create(champFileSavePath))
                    genome =
                        NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false,
                                                               (NeatGenomeFactory)experiment.CreateGenomeFactory())[0];
            }
            catch (Exception e1)
            {
                print(champFileSavePath + " Error loading genome from file!\nLoading aborted.\n" + e1.Message + "\nJoe: " + champFileSavePath);
                return;
            }
        } else
        {
            IDataReader _dbr;
            //string _constr = "URI = file:" + Application.persistentDataPath + string.Format("/{0}.db", trackChanger.CurrentTrack.name);
            string _constr = "URI = file:" + Application.persistentDataPath + string.Format("/{0}.db", run_database);
            IDbConnection _connection = new SqliteConnection(_constr);
            IDbCommand _command = _connection.CreateCommand();
            _connection.Open();

            _command.CommandText = "select " + champColumnInDB + " from simulation_results where run_id = '"+ RunID[currentRun] + "'; ";
            _dbr = _command.ExecuteReader();
            _dbr.Read();
            string xmlString = _dbr.GetString(0);
            //xmlString = xmlString.Replace("\\\"", "\"");
            xmlString = xmlString.Replace('\"', '\'');
            xmlString = xmlString.Replace('\r', ' ');
            xmlString = xmlString.Replace('\n', ' ');

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString);
            genome = NeatGenomeXmlIO.LoadCompleteGenomeList(xml, false, (NeatGenomeFactory)experiment.CreateGenomeFactory())[0];
        

            _command.Dispose();
            _command = null;
            _connection.Close();
            _connection = null;
        }

        // Get a genome decoder that can convert genomes to phenomes.
        var genomeDecoder = experiment.CreateGenomeDecoder();


        if (trackChanger.CurrentTrack.name == "straight_hilly")
        {

            var phenome = genomeDecoder.Decode(genome);

            var obj = Instantiate(cars[currentCarIndex], new Vector3(0, 0.5f, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
            var controller = obj.GetComponent<UnitController>();
            controller.SetWaypoints(trackChanger.getTargetsForCurrentTrack(false));
            controller.SetRunType(RunType.Evaluation);

            GameObject ConcurrentReverseUnitStart = trackChanger.getReverseStart();
            if (ConcurrentReverseUnitStart == null)
            {
                throw new Exception("Reverse Start Position not defined for this track! Either disable reverse units or define start location");
            }
            else
            {
                var rev_obj = Instantiate(cars[currentCarIndex], ConcurrentReverseUnitStart.transform.position, ConcurrentReverseUnitStart.transform.rotation) as GameObject;
                var rev_controller = rev_obj.GetComponent<UnitController>();
                rev_controller.SetWaypoints(trackChanger.getTargetsForCurrentTrack(true));
                rev_controller.SetRunType(RunType.Evaluation);

                Vector3 rotation = rev_obj.transform.eulerAngles;
                Transform[] children = new Transform[rev_obj.transform.childCount];

                int i = 0;
                foreach (Transform child in rev_obj.transform)
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

                //Have to investigate way to add reverse cars to the UnitController Equation....
                //ControllerMap.Add(box, rev_controller); 
                Destroy(rev_obj);
                Destroy(rev_controller);
            }

            ControllerMap.Add(phenome, controller);
            controller.Activate(phenome);

        } else {
            // Decode the genome into a phenome (neural network).
            if (trackChanger.getTargetsForCurrentTrack(false).Length > 0)
            {
                foreach (GameObject startingMarker in trackChanger.getTargetsForCurrentTrack(false))
                {
                    var phenome = genomeDecoder.Decode(genome);

                    var obj = Instantiate(cars[currentCarIndex], startingMarker.transform.position, startingMarker.transform.rotation) as GameObject;
                    var controller = obj.GetComponent<UnitController>();

                    ControllerMap.Add(phenome, controller);

                    controller.Activate(phenome);

                    obj.GetComponent<DistanceTravelledCalculator>().StartingMarker = startingMarker.name;
                    //add camera to list of cams
                    foreach (Transform child in obj.transform)
                    {
                        if (child.name == "car_cam")
                        {
                            cameras.Add(child.gameObject);
                        }
                    }
                }
            }
            else
            {

                GameObject[] startPositions = trackChanger.getAllStartingPositions();
                var phenome = genomeDecoder.Decode(genome);

                var obj = Instantiate(cars[currentCarIndex], startPositions[0].transform.position, startPositions[0].transform.rotation) as GameObject;
                var controller = obj.GetComponent<UnitController>();
                controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(Convert.ToInt32(startPositions[0].name.Substring(startPositions[0].name.Length - 1))));
                controller.SetRunType(RunType.Evaluation);

                for (int pos = 1; pos < startPositions.Length; pos++)
                {
                    int startIndex = Convert.ToInt32(startPositions[pos].name.Substring(startPositions[pos].name.Length - 1));
                    GameObject startPosition = startPositions[pos];
                    var cars_obj = Instantiate(cars[currentCarIndex], startPosition.transform.position, startPosition.transform.rotation) as GameObject;
                    var car_controller = cars_obj.GetComponent<UnitController>();
                    car_controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(startIndex));
                    car_controller.SetRunType(RunType.Evaluation);

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

                ControllerMap.Add(phenome, controller);
                controller.Activate(phenome);

            }
        }

        IsChecking = true;
    }

    private void AddAll()
    {
        for (int i = 0; i < cars.Length; i++)
        {
            currentCarIndex = i;
            trackChanger.resetObstacleSpawnsForCurrentTrack();
            AddChamp();
        }
    }

    private void playPause()
    {
        if (Time.timeScale > 0)
        {
            Time.timeScale = 0;
            playPauseLabel = "Play";
        }
        else {
            Time.timeScale = 3;
            playPauseLabel = "Pause";
        }
    }

    public void disableAllCameras()
    {
        cameras.ForEach(camera => camera.SetActive(false));
    }

    private void OnGUI()
    {

        if (GUI.Button(new Rect(10, 10, 100, 40), "Add Champ"))
        {
            AddChamp();
        }

        if (GUI.Button(new Rect(170, 10, 100, 40), "Add All"))
        {
            AddAll();
        }

        if (GUI.Button(new Rect(115, 10, 50, 40), playPauseLabel))
        {
            playPause();
        }

        //=================

        if (GUI.Button(new Rect(10, 60, 50, 40), "Prev"))
        {
            currentCarIndex--;
            if (currentCarIndex < 0)
            {
                currentCarIndex = cars.Length - 1;
            }
            cars[currentCarIndex].SetActive(true);
        }

        GUI.Label(new Rect(66, 70, 150, 40), cars[currentCarIndex].name);

        if (GUI.Button(new Rect(150, 60, 50, 40), "Next"))
        {
            currentCarIndex++;
            if (currentCarIndex > cars.Length - 1)
            {
                currentCarIndex = 0;
            }
            cars[currentCarIndex].SetActive(true);
        }

        //=================


        if (GUI.Button(new Rect(10, 110, 50, 40), "Prev"))
        {
            currentCameraIndex--;
            if (currentCameraIndex < 0)
            {
                currentCameraIndex = cameras.Count - 1;
            }

            disableAllCameras();
            cameras[currentCameraIndex].SetActive(true);
        }

        GUI.Label(new Rect(66, 120, 50, 40), "cam");

        if (GUI.Button(new Rect(100, 110, 50, 40), "Next"))
        {
            currentCameraIndex++;
            if (currentCameraIndex > cameras.Count - 1)
            {
                currentCameraIndex = 0;
            }
            disableAllCameras();
            cameras[currentCameraIndex].SetActive(true);
        }

        //=================


        if (GUI.Button(new Rect(10, 160, 50, 40), "Prev"))
        {
            trackChanger.previousTrack();
        }

        GUI.Label(new Rect(66, 170, 50, 40), "track");

        if (GUI.Button(new Rect(100, 160, 50, 40), "Next"))
        {
            trackChanger.nextTrack();
        }

        //=================

    }

}