using Assets.Car;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

public class Optimizer : MonoBehaviour
{
    private static NeatEvolutionAlgorithm<NeatGenome> _ea;
    private float accum;

    public readonly Dictionary<IBlackBox, UnitController> ControllerMap = new Dictionary<IBlackBox, UnitController>();
    public readonly Dictionary<IBlackBox, float[]> ControllerBehaviourMap = new Dictionary<IBlackBox, float[]>();
    public Dictionary<IBlackBox, float[]> NoveltyArchive = new Dictionary<IBlackBox, float[]>();

    private bool EARunning;
	private bool firstGeneration;

    private SimpleExperiment experiment;
    private double Novelty;
    private double MovingAverageNovelty;
    private double Fitness;
	private double MovingAverageFitness;
    private int frames;

    private uint Generation;
    private bool loadPopFromFile = true;

    private int NUM_INPUTS;
    private int NUM_OUTPUTS;
    private string popFileSavePath, champFileSavePath, champFitnessFileSavePath;
    private DateTime startTime;
    public float StoppingFitness; //Doesn't do anything ATM
    public RunType runType;
    public BehaviourCharacterisationType behaviourCharacterisation;
	private float timeLeft;
    public float TrialDuration;
	public uint MaxGenerations;

	public int populationPartitions;
	[HideInInspector] public int currentPartition;

    public GameObject Unit;
    public bool ConcurrentReverseUnits;
    public bool AutoStartingPositionsAndTargets;
    private readonly float updateInterval = 12;

    [HideInInspector] public GameObject[] tracks;
    [HideInInspector] public TrackChanger trackChanger;

	public uint StartGeneration;
	public string runIdOverride = "";
    public string sensorName;


	private string runId;
	private NeatGenome _globalChamp;
    private NeatGenome _globalChampFitness; //Only used for novelty search - this is the champ based on its fitness
	private XmlWriterSettings _xwSettings;

	[HideInInspector] public bool carsDriving;

    // Use this for initialization
    private void Start()
    {
        trackChanger = gameObject.GetComponent<TrackChanger>();

        startTime = DateTime.Now;
		runId = startTime.ToString();

		if (runIdOverride.Length > 0) {
			runId = runIdOverride;
		}

		//put xmls in run subfolder (to allow for multiple instances
		string runIdSubfolder = runId;

		foreach(char c in System.IO.Path.GetInvalidFileNameChars()) {
			runIdSubfolder = runIdSubfolder.Replace(c, '_');
		}

		populationPartitions = (populationPartitions < 1) ? 1 : populationPartitions;
        Utility.DebugLog = false;
        experiment = new SimpleExperiment();
        var xmlConfig = new XmlDocument();
        var textAsset = (TextAsset) Resources.Load("experiment.config");
        xmlConfig.LoadXml(textAsset.text);
        experiment.SetOptimizer(this);

        UnitController unit_controller = Unit.GetComponent<UnitController>();
        experiment.Initialize("Car Experiment", xmlConfig.DocumentElement, unit_controller.GetNumberOfInputsIntoNeat(), unit_controller.GetNumberOfOutputsNeededFromNeat());

        champFileSavePath = Application.persistentDataPath + string.Format("/{0}/{1}.champ.xml", runIdSubfolder, "driving");
        champFitnessFileSavePath = Application.persistentDataPath + string.Format("/{0}/{1}.champ.fitness.xml", runIdSubfolder, "driving");
        popFileSavePath = Application.persistentDataPath + string.Format("/{0}/{1}.pop.xml", runIdSubfolder, "driving");
		print(champFileSavePath);

		firstGeneration = true;

		_xwSettings = new XmlWriterSettings();
		_xwSettings.Indent = true;
		
		var dirInf = new DirectoryInfo(Application.persistentDataPath + string.Format("/{0}", runIdSubfolder));
		if (!dirInf.Exists)
		{
			Debug.Log("Creating subdirectory");
			dirInf.Create();
		}

        StartEA();
    }

    // Update is called once per frame
    private void Update()
    {
        //  evaluationStartTime += Time.deltaTime;
        if (EARunning)
        {
            timeLeft -= Time.deltaTime;
            accum += Time.timeScale/Time.deltaTime;
            ++frames;

            if (timeLeft <= 0.0)
            {
                var fps = accum / frames;
                timeLeft = updateInterval;
                accum = 0.0f;
                frames = 0;
                //   print("FPS: " + fps);
                if (fps < 10)
                {
                    if (Time.timeScale > 2)
                    {
                        Time.timeScale = Time.timeScale - 1;
                    }
                }
                else if (fps > 20)
                {
                    if (Time.timeScale < 30)
                    {
                        Time.timeScale = Time.timeScale + 1;
                    }
               } 
            }
            if (ControllerMap.Values.All(controller => controller.Stopped == true)) { //Checks if all cars have stopped
				carsDriving = false;
			}
        }

		if (Generation >= MaxGenerations) {
			StopEA();
			Application.Quit();
		}
    }

    public void StartEA()
    {
        Utility.DebugLog = true;
        if (loadPopFromFile)
        {
            _ea = experiment.CreateEvolutionAlgorithm(popFileSavePath);
        }
        else
        {
            _ea = experiment.CreateEvolutionAlgorithm();
        }

		WriteRunToDB ();

        _ea.UpdateEvent += ea_UpdateEvent;
        _ea.PausedEvent += ea_PauseEvent;

        var evoSpeed = 15;

        //   Time.fixedDeltaTime = 0.045f;
        Time.timeScale = evoSpeed;
        _ea.StartContinue();
        EARunning = true;
    }

    private void ea_UpdateEvent(object sender, EventArgs e)
    {
        bool newGlobalChamp = false;
        bool newGlobalFitnessChamp = false;

        //Set global best
        if (_globalChamp == null)
            newGlobalChamp = true;
        else
        {
            switch (runType)
            {
                //Greater than or equal to becuase I want the lastest fittest one
                case RunType.NoveltySearch:
                case RunType.Hybrid:
                    newGlobalChamp = _ea.CurrentChampGenome.EvaluationInfo.Novelty >= _globalChamp.EvaluationInfo.Novelty;
                    break;

                case RunType.Fitness:
                    newGlobalChamp = _ea.CurrentChampGenome.EvaluationInfo.Fitness >= _globalChamp.EvaluationInfo.Fitness;
                    break;
            }
        }

        if (newGlobalChamp)
        {
            _globalChamp = new NeatGenome(_ea.CurrentChampGenome, _ea.CurrentChampGenome.Id, _ea.CurrentChampGenome.BirthGeneration);
            _globalChamp.EvaluationInfo.SetNovelty(_ea.CurrentChampGenome.EvaluationInfo.Novelty);
            _globalChamp.EvaluationInfo.SetFitness(_ea.CurrentChampGenome.EvaluationInfo.Fitness);
        }

        if (runType == RunType.NoveltySearch || runType == RunType.Hybrid)
        {
            if (_globalChampFitness == null)
                newGlobalFitnessChamp = true;
            else
                newGlobalFitnessChamp = _ea.CurrentFitnessChampGenome.EvaluationInfo.Fitness >= _globalChampFitness.EvaluationInfo.Fitness;

            if (newGlobalFitnessChamp)
            {
                _globalChampFitness = new NeatGenome(_ea.CurrentFitnessChampGenome, _ea.CurrentFitnessChampGenome.Id, _ea.CurrentFitnessChampGenome.BirthGeneration);
                _globalChampFitness.EvaluationInfo.SetFitness(_ea.CurrentFitnessChampGenome.EvaluationInfo.Fitness);
                _globalChampFitness.EvaluationInfo.SetNovelty(_ea.CurrentFitnessChampGenome.EvaluationInfo.Novelty);

            }
        }
        
        Fitness = _ea.Statistics._maxFitness;
        Generation = _ea.CurrentGeneration + StartGeneration;
        var MovingAverageFitnessArr = _ea.Statistics._bestFitnessMA;
		MovingAverageFitness = MovingAverageFitnessArr.Mean;
        Novelty = _ea.Statistics._maxNovelty;
        var MovingAverageNoveltyArr = _ea.Statistics._bestNoveltyMA;
        MovingAverageNovelty = MovingAverageNoveltyArr.Mean;

        var log_score = (runType == RunType.NoveltySearch || runType == RunType.Hybrid) ? Novelty : Fitness;
        var log_ma = (runType == RunType.NoveltySearch || runType == RunType.Hybrid) ? MovingAverageNoveltyArr : MovingAverageFitnessArr;

        Utility.Log(string.Format("gen={0:N0} bestScore={1:N6}, movingAverage={2:N4} n_movingAverage: {3}, archive_size={4}, n_bc_map={5}",
            _ea.CurrentGeneration, log_score, log_ma.Mean, log_ma.Length, NoveltyArchive.Count, ControllerBehaviourMap.Count));


        // Save genomes to xml file.        
        using (var xw = XmlWriter.Create(popFileSavePath, _xwSettings))
		{
			experiment.SavePopulation(xw, _ea.GenomeList);
		}
        // Also save the best genome

        using (var xw = XmlWriter.Create(champFileSavePath, _xwSettings))
        {
            experiment.SavePopulation(xw, new[] { _globalChamp });
        }

        if (runType == RunType.NoveltySearch || runType == RunType.Hybrid)
        {
            using (var xw = XmlWriter.Create(champFitnessFileSavePath, _xwSettings))
            {
                experiment.SavePopulation(xw, new[] { _globalChampFitness });
            }
        }

		//prevents it from writing gen_0 as gen_1 in the DB.
		if (firstGeneration) {
			firstGeneration = false;
		} else {
            //Write Run Stats to DB
            if (runType == RunType.NoveltySearch || runType == RunType.Hybrid)
                WriteGenerationToDB(newGlobalChamp, newGlobalFitnessChamp);
            else
                WriteGenerationToDB(newGlobalChamp);
		}
		
	}
	
	private void ea_PauseEvent(object sender, EventArgs e)
	{
		Time.timeScale = 1;
		Utility.Log("Done ea'ing (and neat'ing)");

        var endTime = DateTime.Now;
        Utility.Log("Total time elapsed: " + (endTime - startTime));

        var stream = new StreamReader(popFileSavePath);


        EARunning = false;
    }

    public void StopEA()
    {
        if (_ea != null && _ea.RunState == RunState.Running)
        {
            _ea.Stop();
			firstGeneration = true;
		}
    }

    public void Evaluate(IBlackBox box)
    {
        trackChanger.resetObstacleSpawnsForCurrentTrack();
        if (AutoStartingPositionsAndTargets) {
            GameObject [] startPositions = trackChanger.getAllStartingPositions();

            var obj = Instantiate(Unit, startPositions[0].transform.position, startPositions[0].transform.rotation) as GameObject;
            var controller = obj.GetComponent<UnitController>();
            controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(Convert.ToInt32(startPositions[0].name.Substring(startPositions[0].name.Length - 1))));
            controller.SetRunType(runType);
            controller.SetBehaviourCharacterisation(behaviourCharacterisation);

            for (int pos = 1; pos < startPositions.Length; pos++)
            {
                int startIndex = Convert.ToInt32(startPositions[pos].name.Substring(startPositions[pos].name.Length - 1));
                GameObject startPosition = startPositions[pos];
                var cars = Instantiate(Unit, startPosition.transform.position, startPosition.transform.rotation) as GameObject;
                var car_controller = cars.GetComponent<UnitController>();
                car_controller.SetWaypoints(trackChanger.getSpecificTargetsForCurrentTrack(startIndex));
                controller.SetRunType(runType);
                controller.SetBehaviourCharacterisation(behaviourCharacterisation);

                Vector3 rotation = cars.transform.eulerAngles;
                Transform[] children = new Transform[cars.transform.childCount];

                int i = 0;
                foreach (Transform child in cars.transform)
                {
                    children[i] = child;
                    i++;
                }

                for (i = 0; i < children.Length; i++)
                {
                    children[i].SetParent(obj.transform);
                    children[i].name = children[i].name + "_start_" + startIndex;
                    children[i].rotation = Quaternion.AngleAxis(rotation.y, Vector3.up);
                    children[i].GetComponent<CarDriving>()._currentRotation = children[i].eulerAngles;
                }

                Destroy(cars);
                Destroy(car_controller);                
            }

            ControllerMap.Add(box, controller);
            controller.Activate(box);
        } else {
            var obj = Instantiate(Unit, new Vector3(0, 0.5f, 0), new Quaternion(0, 0, 0, 0)) as GameObject;
            var controller = obj.GetComponent<UnitController>();
            controller.SetWaypoints(trackChanger.getTargetsForCurrentTrack(false));
            controller.SetRunType(runType);
            controller.SetBehaviourCharacterisation(behaviourCharacterisation);

            //Hack to get collective working
            if (ConcurrentReverseUnits)
            {
                GameObject ConcurrentReverseUnitStart = trackChanger.getReverseStart();
                if (ConcurrentReverseUnitStart == null)
                {
                    throw new Exception("Reverse Start Position not defined for this track! Either disable reverse units or define start location");
                }
                else {
                    var rev_obj = Instantiate(Unit, ConcurrentReverseUnitStart.transform.position, ConcurrentReverseUnitStart.transform.rotation) as GameObject;
                    var rev_controller = rev_obj.GetComponent<UnitController>();
                    rev_controller.SetWaypoints(trackChanger.getTargetsForCurrentTrack(true));
                    rev_controller.SetRunType(runType);
                    rev_controller.SetBehaviourCharacterisation(behaviourCharacterisation);

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
            }

            ControllerMap.Add(box, controller);
            controller.Activate(box);
        }
    }

    public void StopEvaluation(IBlackBox box)
    {
		var ct = ControllerMap[box];
            
        if(runType == RunType.NoveltySearch || runType == RunType.Hybrid)
            ControllerBehaviourMap.Add(box, ct.GetBehaviourCharacterisation());

    	Destroy(ct.gameObject);
        ControllerMap.Remove(box);
    }

    public void RunBest()
    {
		GUI.Label(new Rect(Screen.width/2, Screen.height/2, 150, 70), "Please select track");

        Time.timeScale = 1;

        NeatGenome genome = null;


        // Try to load the genome from the XML document.
        try
        {
            using (var xr = XmlReader.Create(champFileSavePath))
                genome =
                    NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false,
                        (NeatGenomeFactory) experiment.CreateGenomeFactory())[0];
        }
        catch (Exception e1)
        {
            print(champFileSavePath + " Error loading genome from file!\nLoading aborted.\n"
                                     + e1.Message + "\nJoe: " + champFileSavePath);
            return;
        }

        // Get a genome decoder that can convert genomes to phenomes.
        var genomeDecoder = experiment.CreateGenomeDecoder();

        // Decode the genome into a phenome (neural network).
        var phenome = genomeDecoder.Decode(genome);

        Evaluate(phenome);
    }

    public float GetNovelty(IBlackBox box)
    {
        if (ControllerMap.ContainsKey(box))
        {
            return ControllerMap[box].GetNovelty();
        }
        return -1;
    }

    public float GetFitness(IBlackBox box)
    {
        if (ControllerMap.ContainsKey(box))
        {
            return ControllerMap[box].GetFitness();
        }
        return 0;
    }

    private void OnGUI()
	{
//		if (EARunning) {
//			if (GUI.Button(new Rect(10, 10, 100, 40), "Stop EA"))
//			{
//				StopEA();
//			}
//		}

        if (!EARunning)
        {
			if (GUI.Button(new Rect(10, 10, 100, 40), "Start EA"))
			{
				StartEA();
			}

            if (GUI.Button(new Rect(10, 60, 100, 40), "Run best"))
            {
                RunBest();
            }

            if (GUI.Button(new Rect(10, 110, 250, 40), "Loading Population From File: " + loadPopFromFile))
            {
                loadPopFromFile = !loadPopFromFile;
            }

			if (GUI.Button(new Rect(10, 160, 50, 40), "Prev"))
			{
                trackChanger.previousTrack();
            }

            GUI.Label(new Rect(66, 170, 50, 40), "track");

			if (GUI.Button(new Rect(100, 160, 50, 40), "Next"))
			{
                trackChanger.nextTrack();
			}

        }
        else
        {
            GUI.Label(new Rect(Screen.width - 140, Screen.height - 80, 150, 70),
			          string.Format("Population Size: {0}\n{1}\ns/Generation: {2:0.00}", _ea.GenomeList.Count, DateTime.Now - startTime, ((DateTime.Now - startTime).TotalMilliseconds / (int)Generation)/1000));
        }

        string label = "";

        if (runType == RunType.Fitness)
            label = string.Format("Generation: {0}\nPartition: {1} of {2}\nMax Fitness: {3:0.00}\nMean Best Fitness (Generational): {4:0.00}", Generation, currentPartition, populationPartitions, Fitness, MovingAverageFitness);
        else if (runType == RunType.NoveltySearch)
            label = string.Format("Generation: {0}\nPartition: {1} of {2}\nMax Novelty: {3:0.00}\nMean Best Novelty (Generational): {4:0.00}", Generation, currentPartition, populationPartitions, Novelty, MovingAverageNovelty);
        else if (runType == RunType.Hybrid)
            label = string.Format("Generation: {0}\nPartition: {1} of {2}\nMax Hybrid: {3:0.00}\nMean Best Hybrid (Generational): {4:0.00}", Generation, currentPartition, populationPartitions, Novelty, MovingAverageNovelty);
        else
            throw new NotImplementedException("Label for Run Type not implement");


        GUI.Label(new Rect(10, Screen.height - 80, 250, 70), label);

    }

	private void WriteRunToDB() {
		gameObject.GetComponent<DatabaseLogging> ().LogNewRun (runId, _ea.GenomeList.Count, TrialDuration, trackChanger.tracks.Length, sensorName);
	}

    private void updateGenerationInDB()
    {
        gameObject.GetComponent<DatabaseLogging>().UpdateRunGenerationCount(runId, Convert.ToInt32(Generation));
    }

    private void WriteGenerationToDB(bool newNovChamp, bool newFitChamp)
    {
        updateGenerationInDB();
        gameObject.GetComponent<DatabaseLogging>().LogCurrentGeneration(runId,
                                                                        Convert.ToInt32(Generation),
                                                                        _ea.Statistics._meanFitness,
                                                                        _ea.CurrentChampGenome.EvaluationInfo.Fitness,
                                                                        _globalChamp.EvaluationInfo.Fitness,
                                                                        _ea.CurrentFitnessChampGenome.EvaluationInfo.Fitness,
                                                                        _globalChampFitness.EvaluationInfo.Fitness,
                                                                        _ea.Statistics._meanNovelty,
                                                                        _ea.CurrentChampGenome.EvaluationInfo.Novelty,
                                                                        _ea.CurrentFitnessChampGenome.EvaluationInfo.Novelty,
                                                                        _globalChamp.EvaluationInfo.Novelty,
                                                                        _globalChampFitness.EvaluationInfo.Novelty,
                                                                        newNovChamp,
                                                                        newFitChamp,
                                                                        _ea.CurrentChampGenome.Complexity,
                                                                        _ea.CurrentFitnessChampGenome.Complexity,
                                                                        _ea.Statistics._meanComplexity,
                                                                        _ea.Statistics._maxComplexity);
        foreach (NeatGenome genome in _ea.GenomeList) 
            gameObject.GetComponent<DatabaseLogging>().LogCurrentGenerationGenomePopulation(runId,
                                                                        Convert.ToInt32(Generation),
                                                                        Convert.ToInt32(genome.Id),
                                                                        Convert.ToInt32(genome.BirthGeneration),
                                                                        genome.Complexity,
                                                                        genome.EvaluationInfo.Fitness,
                                                                        genome.EvaluationInfo.Novelty);

        if (File.Exists(champFileSavePath) && File.Exists(champFitnessFileSavePath))
        {
            string novChampXML = File.ReadAllText(champFileSavePath);
            string fitChampXML = File.ReadAllText(champFitnessFileSavePath);
            gameObject.GetComponent<DatabaseLogging>().UpdateRunWithChampXML(runId, novChampXML, fitChampXML);
        }
    }
	
	private void WriteGenerationToDB(bool newChamp) {
        updateGenerationInDB();
        gameObject.GetComponent<DatabaseLogging>().LogCurrentGeneration (runId, 
		                                                                  Convert.ToInt32(Generation), 
		                                                                  _ea.Statistics._meanFitness,
                                                                          _ea.Statistics._maxFitness,
                                                                          _globalChamp.EvaluationInfo.Fitness,
                                                                          newChamp,
		                                                                  _ea.CurrentChampGenome.Complexity,
		                                                                  _ea.Statistics._meanComplexity,
		                                                                  _ea.Statistics._maxComplexity);

        foreach (NeatGenome genome in _ea.GenomeList)
            gameObject.GetComponent<DatabaseLogging>().LogCurrentGenerationGenomePopulation(runId,
                                                                        Convert.ToInt32(Generation),
                                                                        Convert.ToInt32(genome.Id),
                                                                        Convert.ToInt32(genome.BirthGeneration),
                                                                        genome.Complexity,
                                                                        genome.EvaluationInfo.Fitness);

            if (newChamp && File.Exists (champFileSavePath)) {
			string champXML = File.ReadAllText(champFileSavePath);
			gameObject.GetComponent<DatabaseLogging> ().UpdateRunWithChampXML(runId, champXML);
        }

		//		double elapsed = (DateTime.Now - startTime).TotalSeconds;
		//
		//		gameObject.GetComponent<DatabaseLogging> ().LogCurrentGeneration (runId, 
		//		                                                                  Convert.ToInt32(_ea.CurrentGeneration),
		//		                                                                  elapsed/_ea.CurrentGeneration, _ea.Statistics._evaluationsPerSec);
	}
}