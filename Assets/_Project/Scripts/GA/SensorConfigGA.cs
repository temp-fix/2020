using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class SensorConfigGA : MonoBehaviour
{
    private const float updateInterval = 12;

    private readonly int _totalNumberOfExperiments = 1;

    private GameObject _currentCar;
    private int _currentTrack;
    private SensorConfigChamp _experimentChamp;
    private int _experimentNo;
    private bool _GARunning;
    private int _generation;
    private SensorConfigChamp _generationChamp;
    private SensorConfigChamp _globalChamp;
    private SensorConfigGenome[] _initalRandom;
    private int _maxTracks;

    //Holds referances to the gameobject that the SensorConfigIndividual script (Individual) is attached to
    private GameObject[] _population;
    private int _run;
    private XmlWriterSettings _xwSettings;
    private float accum;

    //The car onto which the sensor config is placed
    public GameObject Car;

    private int CurrentPopAge;
    public SensorDimensions dimensions;
    private int frames;
    public int MaxGenerations;
    public int MaxNumberOfRuns; //min of 1
    private int MaxPopulationAge; //Increases every frame, min of 2
    public int TrialDuration;

    public GameObject ObstSpawner;
    private string popFileSavePath, champFileSavePath;
    public int PopulationSize; //min of 2

    private string runId;

    public int StartingTimeScale;
    public int MaxTimeScale = 10;

    private float timeLeft;


    private void Start()
    {
        champFileSavePath = Application.persistentDataPath + string.Format("/{0}.champ.xml", "sensors");
        popFileSavePath = Application.persistentDataPath + string.Format("/{0}.pop.xml", "sensors");
        _xwSettings = new XmlWriterSettings();
        _xwSettings.Indent = true;
        _maxTracks = gameObject.GetComponent<TrackChanger>().tracks.Count();

        gameObject.GetComponent<TrackChanger>().SetObstacleLayerToObstacles();

        MaxPopulationAge = (int) (TrialDuration / Time.fixedDeltaTime);
        //Sanitize inputs
        if (MaxPopulationAge <= 1)
        {
            MaxPopulationAge = 2;
        }

        if (PopulationSize <= 1)
        {
            PopulationSize = 2;
        }

        if (MaxNumberOfRuns < 1)
        {
            MaxNumberOfRuns = 1;
        }

        if (MaxTimeScale < 1)
        {
            MaxTimeScale = 1;
        }

        _experimentNo = 1;
        _run = 1;

        runId = _run + ": " + _experimentNo + " @ " + DateTime.Now;


        _GARunning = false;

        StartGA();
    }

    private void StartGA()
    {
        WriteRunToDB();

        CurrentPopAge = 1;
        _generation = 1;

        _population = new GameObject[PopulationSize];
        _initalRandom = new SensorConfigGenome[PopulationSize];

        NewRunSet();
        InitPopulation();

        _GARunning = true;

        StartingTimeScale = (StartingTimeScale < 1) ? 1 : StartingTimeScale; //must be at least 1
        Time.timeScale = StartingTimeScale; // set the time scale
    }

    private void InitPopulation()
    {
        for (var indi = 0; indi < _population.Length; indi++)
        {

            GameObject indi_gameObj = MakeNewIndividualGameObjFromGenome(_initalRandom[indi]);

            _population[indi] = indi_gameObj;
        }

        PutNewPopOnNewCar();
    }

    private void PutNewPopOnNewCar()
    {
        //Destroy the old car
        Destroy(_currentCar);

        //Will hold all of the configs
        var pop_gameObj = new GameObject("Population: Sensor Configs");

        foreach (GameObject indi in _population)
        {
            indi.transform.SetParent(pop_gameObj.transform);
            indi.transform.localPosition = Vector3.zero;
        }

        //Make new car gameObj
        GameObject car_gameObj = CreateNewCar();

        //Attach the population to the new car
        pop_gameObj.transform.SetParent(car_gameObj.transform);
        pop_gameObj.transform.localPosition = Vector3.zero;

        _currentCar = car_gameObj;
    }

    private GameObject CreateNewCar()
    {
        //Make gameObj
        var car_gameObj = Instantiate(Car, transform.position, transform.rotation) as GameObject;

        //Set this transform as the parent obj for the car, makes things neat.
        car_gameObj.transform.SetParent(transform);

        if (!car_gameObj.activeSelf)
        {
            car_gameObj.SetActive(true);
        }

        //Set the way points for the driving AI
        car_gameObj.GetComponent<CarDrivingAIControl>()
            .SetCheckPoints(gameObject.GetComponent<TrackChanger>().getTargetsForCurrentTrack(false));

        return car_gameObj;
    }

    private GameObject MakeNewIndividualGameObjFromGenome(SensorConfigGenome genome)
    {
        //Make gameObj
        var indi_game_obj = new GameObject("Individual: Sensor Config");

        //Attach the individual script to the sensor conifg game object and set its the phenome using the given genome
        indi_game_obj.AddComponent<SensorConfigIndividual>().SetSensorConfigPhenomeUsingGenomeSurface(genome, Car);
        //indi_game_obj.AddComponent<SensorConfigIndividual>().SetSensorConfigPhenomeUsingGenome(genome);

        //Recenter the game object to the parent transform
        indi_game_obj.transform.localPosition = Vector3.zero;

        return indi_game_obj;
    }

    private void Update()
    {
        if (_GARunning)
        {
            timeLeft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            if (timeLeft <= 0.0)
            {
                float fps = accum / frames;
                timeLeft = updateInterval;
                accum = 0.0f;
                frames = 0;
                //   print("FPS: " + fps);
                if (fps < 5)
                {
                    //                    Time.timeScale = Time.timeScale - 1;
                }
                else if (fps > 20)
                {
                    Time.timeScale = Time.timeScale + 1;
                }

                if (Time.timeScale < 1)
                {
                    Time.timeScale = 1;
                }

                if (Time.timeScale > MaxTimeScale)
                {
                    Time.timeScale = MaxTimeScale;
                }
            }
        }
    }

    public void EndRun()
    {
        CurrentPopAge = MaxPopulationAge - 1;
    }

    private void FixedUpdate()
    {
        if (_GARunning)
        {
            if (CurrentPopAge != MaxPopulationAge)
            {
                //if (_currentCar.GetComponent<CarDrivingAIControl>().checkPointNum == gameObject.GetComponent<TrackChanger>().getTargetsForCurrentTrack().Length - 1)
                //    CurrentPopAge = MaxPopulationAge - 1;
                CurrentPopAge++;
            }
            else
            {
                CurrentPopAge = 1;


                //After the MaxGenerations, go on to the next experiment
                if (_generation == MaxGenerations)
                {
                    //Collect gen data
                    GenerationData();

                    if (_experimentNo == _totalNumberOfExperiments) //end of all experiments
                    {
                        _experimentNo = 0; //will be incriemnted below

                        if (_run == MaxNumberOfRuns)
                        {
                            print("----------- FINISHED :D ----------");

                            //max runs have occured -> end
                            Application.Quit();
#if UNITY_EDITOR
                            EditorApplication.isPlaying = false;
#endif
                        }
                        else if (_run % 5 == 0)
                        {
                            print("New set");

                            //A set of 5 runs have occured
                            NewRunSet();
                        }

                        _run++;
                        print("new run! " + _run);
                    }

                    _experimentNo++;
                    print("New experiment! No:" + _experimentNo + ", run: " + _run);

                    //End of experiment, write champ to DB
                    WriteRunChampToDB(); //TODO: check that the experiment best is written

                    //Clear experiment best
                    _experimentChamp = null;

                    //Reinitialise RunID and make new entry
                    runId = _run + ": " + _experimentNo + " @ " + DateTime.Now;
                    WriteRunToDB();

                    _generation = 1;

                    InitPopulation();
                }
                else
                {
                    NextGen();
                    //                    gameObject.GetComponent<TrackChanger>().nextTrack();
                    //
                    //                    if (_currentTrack == _maxTracks - 1)
                    //                    {
                    //                        _currentTrack = 0;
                    //                        NextGen();
                    //                    }
                    //                    else
                    //                    {
                    //                        _currentTrack ++;
                    //                        PutNewPopOnNewCar();
                    //                    }
                }
            }
        }
    }

    private void NewRunSet()
    {
        //Make a new set of random genomes
        for (var indi = 0; indi < _population.Length; indi++)
        {
            _initalRandom[indi] = new SensorConfigGenome(dimensions);
        }

        //Spawn the obstacles
        if (ObstSpawner != null)
        {
            ObstSpawner.GetComponent<SpawnObstacles>().SpawnNewSetOfObstacles();
        }
    }

    private void OnApplicationQuit()
    {
        if (_globalChamp != null)
        {
            WriteRunChampToDB();
            print(_globalChamp.ToString());
        }
    }

    private static void PrintPhenome(SensorConfigIndividual phe)
    {
        string retrn = phe.Genes.Genome.Aggregate("", (current, f) => current + (f + " "));
        print(retrn + " fit:" + phe.Fitness);
    }

    private void WritePopulationToFile()
    {
        using (XmlWriter xw = XmlWriter.Create(popFileSavePath, _xwSettings))
        {
            SensorConfigXmlIO.WriteComplete(xw, _population);
        }
    }

    private void WriteChampToFile()
    {
        using (XmlWriter xw = XmlWriter.Create(champFileSavePath, _xwSettings))
        {
            SensorConfigXmlIO.Write(xw, _population[0].GetComponent<SensorConfigIndividual>());
        }
    }

    private void WriteRunToDB()
    {
        gameObject.GetComponent<DatabaseLogging>().LogNewRun(runId, MaxGenerations, PopulationSize);
    }

    private void WriteGenerationToDB(bool newChamp)
    {
        gameObject.GetComponent<DatabaseLogging>().LogCurrentGeneration(
            runId,
            _generation,
            _population.Average(individual => individual.GetComponent<SensorConfigIndividual>().Fitness),
            _generationChamp.Fitness,
            _experimentChamp.Fitness,
            newChamp);
    }

    private void WriteRunChampToDB()
    {
        //TODO: which champ is being saved here?
        if (File.Exists(champFileSavePath))
        {
            string champXML = File.ReadAllText(champFileSavePath);
            gameObject.GetComponent<DatabaseLogging>().UpdateRunWithChampXML(runId, champXML);
        }
    }

    private void NextGen()
    {
        //Collect geneation data
        GenerationData();

        //A new generation begins
        _generation++;

        //Various experiments
        switch (_experimentNo)
        {
            default:
                Application.Quit();
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
                break;

            //Base GA
            case 1:
                _population = MakeNewPopByDoingGeneticOperations(
                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                    () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                    (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                    genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                    );
                break;
                //
                //            //Testing selection
                //            case 2:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_Random(_population),
                //                    (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //                    genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //                    );
                //                break;
                //            case 3:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_FitnessProportionate(_population),
                //                    (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //                    genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //                    );
                //                break;
                //
                //            //Testing Recomb
                //            case 4:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //                    SensorConfigGAOperators.Recomb_OnePointCrossover,
                //                    genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //                    );
                //                break;
                //            case 5:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //                    SensorConfigGAOperators.Recomb_Local,
                //                    genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //                    );
                //                break;
                //
                //            //Testing mutation
                //            case 6:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //                    (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //                    genome => SensorConfigGAOperators.Mutation_Random(0.05f, genome, dimensions)
                //                    );
                //                break;
                //            case 7:
                //                _population = MakeNewPopByDoingGeneticOperations(
                //                    () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //                    () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //                    (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //                    genome => SensorConfigGAOperators.Mutation_Gaussian(genome, dimensions)
                //                    );
                //                break;

                ////Testing tourni size
                //case 1:
                //    _population = MakeNewPopByDoingGeneticOperations(
                //        () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //        () => SensorConfigGAOperators.Selection_Tournament(5, 1.0f, _population),
                //        (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //        genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //        );
                //    break;

                //case 2:
                //    _population = MakeNewPopByDoingGeneticOperations(
                //        () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //        () => SensorConfigGAOperators.Selection_Tournament(20, 1.0f, _population),
                //        (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //        genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //        );
                //    break;

                ////mut rates
                //case 3:
                //    _population = MakeNewPopByDoingGeneticOperations(
                //        () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //        () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //        (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //        genome => SensorConfigGAOperators.Mutation_Gaussian(genome, dimensions, 3f)
                //        );
                //    break;

                //case 4:
                //    _population = MakeNewPopByDoingGeneticOperations(
                //        () => SensorConfigGAOperators.Replacement_Elitist(1, _population),
                //        () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //        (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //        genome => SensorConfigGAOperators.Mutation_Gaussian(genome, dimensions, 0f)
                //        );
                //    break;

                ////Testing elite
                //case 5:
                //    _population = MakeNewPopByDoingGeneticOperations(
                //        () => SensorConfigGAOperators.Replacement_Elitist(5, _population),
                //        () => SensorConfigGAOperators.Selection_Tournament(10, 1.0f, _population),
                //        (parent_A, parent_B) => SensorConfigGAOperators.Recomb_Uniform(parent_A, parent_B, 0.6f),
                //        genome => SensorConfigGAOperators.Mutation_Breeder(10f, 4.0f, genome, dimensions)
                //        );
                //    break;
        }

        PutNewPopOnNewCar();
    }

    private void GenerationData()
    {
        Array.Sort(_population,
            (A, B) =>
                B.GetComponent<SensorConfigIndividual>()
                    .Fitness.CompareTo(A.GetComponent<SensorConfigIndividual>().Fitness));

        //Set generation best
        _generationChamp = MakeChamp(_population[0].GetComponent<SensorConfigIndividual>());

        //Write Population to XML
        WritePopulationToFile();
        WriteChampToFile();

        //Set best for expermient
        var new_experi_champ = false;
        if (_experimentChamp == null ||
            _generationChamp.Fitness >= _experimentChamp.Fitness)
        //Greater than or equal to becuase I want the lastest fittest one
        {
            _experimentChamp = MakeChamp(_population[0].GetComponent<SensorConfigIndividual>());
            new_experi_champ = true;

            if (_globalChamp == null || _experimentChamp.Fitness >= _globalChamp.Fitness)
            {
                _globalChamp = MakeChamp(_population[0].GetComponent<SensorConfigIndividual>());

                print("New global best found: " + _globalChamp);
            }
        }

        //Print the stats for this generation
        PrintGenerationStats(_population);

        //Write Run Stats to DB
        WriteGenerationToDB(new_experi_champ);
    }

    private GameObject[] MakeNewPopByDoingGeneticOperations(
        Func<SensorConfigGenome[]> replacement_method,
        Func<SensorConfigIndividual> selection_method,
        Func<SensorConfigIndividual, SensorConfigIndividual, SensorConfigGenome> recomb_method,
        Func<SensorConfigGenome, SensorConfigGenome> mutation_method
        )
    {
        //Create new pop
        var new_population = new GameObject[_population.Length];

        //Get the elites
        SensorConfigGenome[] elite_genomes = replacement_method();

        // Add the genome of the fittest genomes to the new pop
        for (var elite_cnt = 0; elite_cnt < elite_genomes.Length; elite_cnt++)
        {
            new_population[elite_cnt] = MakeNewIndividualGameObjFromGenome(elite_genomes[elite_cnt]);
        }

        // Start adding offspring from where the elite ended
        for (int offspring_cnt = elite_genomes.Length; offspring_cnt < new_population.Length; offspring_cnt++)
        {
            //Parent selection
            SensorConfigIndividual parent_A = selection_method();
            SensorConfigIndividual parent_B;
            var tries = 0;
            do
            {
                if (tries == 50)
                {
                    print("probz a prob");
                    //if all the genomes are the same, th wihile loop will crash unity. This is here to prevent that.
                    parent_B = selection_method();
                    break;
                }
                tries++;
                parent_B = selection_method();
            } while (parent_A == parent_B); //A and B cannot be the same.

            //Recombine to produce offspring:
            SensorConfigGenome offspring_genome = recomb_method(parent_A, parent_B);

            //Mutate offspring: 
            SensorConfigGenome mutated_ofspring = mutation_method(offspring_genome);

            //Add to the new pop
            new_population[offspring_cnt] = MakeNewIndividualGameObjFromGenome(mutated_ofspring);
        }

        return new_population;
    }

    private float SimilarityIndex()
    {
        //TODO: do, not finished

        var equal_idxs = new List<int>();
        var no_of_equals = 0;

        //comparing outer to inner
        for (var outer = 0; outer < _population.Length - 1; outer++)
        {
            if (equal_idxs.Contains(outer))
            {
                break;
            }

            int size_before = equal_idxs.Count;

            for (int inner = outer + 1; inner < _population.Length; inner++)
            {
                if (equal_idxs.Contains(inner))
                {
                    break;
                }

                SensorConfigProperties[] gene_outer = _population[outer].GetComponent<SensorConfigIndividual>().Genes.Genome;
                SensorConfigProperties[] gene_inner = _population[inner].GetComponent<SensorConfigIndividual>().Genes.Genome;

                Assert.AreEqual(gene_outer.Length, gene_inner.Length);

                var equal = true;
                for (var i = 0; i < gene_outer.Length; i++)
                {
                    //Check equal if still equal
                    if (equal)
                    {
                        equal = (Math.Abs(gene_outer[i].angles.x - gene_inner[i].angles.x) < 0.01f &&
                                 Math.Abs(gene_outer[i].angles.y - gene_inner[i].angles.y) < 0.01f);
                    }
                    else
                    {
                        //No need to continue if not equal
                        break;
                    }
                }

                if (equal)
                {
                    no_of_equals++;
                    //Add the index to the array list
                    equal_idxs.Add(inner);
                }
            }


            int size_after = equal_idxs.Count;
            if (size_before != size_after)
            {
                //There were equal genes equal to outer's gene
                equal_idxs.Add(outer);
            }
        }

        //Return the number of times there were equal genes, div by the size of the pop to give a rating between 0 and 1.
        return equal_idxs.Count / (float)_population.Length;
    }

    private SensorConfigChamp MakeChamp(SensorConfigIndividual indi)
    {
        return new SensorConfigChamp(indi.Genes, indi.Fitness);
    }

    private void PrintGenerationStats(GameObject[] pop)
    {
        float avg_fitness = pop.Average(individual => individual.GetComponent<SensorConfigIndividual>().Fitness);
        print(
            "Generation " + _generation +
            ":\nAverage fitness: " + avg_fitness +
            "\nGeneration champ: " + _generationChamp +
            "\nExperiment champ: " + _experimentChamp
            );
    }
}