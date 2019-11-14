using UnityEngine;
using System.Collections.Generic;
using System.Data;
using Mono.Data.SqliteClient;
using System;

public enum SimulationType { Sensor, Controller, PerformanceTesting, Evaluation };

public class DatabaseLogging : MonoBehaviour
{

    public SimulationType simulation;
    private string databaseName = "";
    private string _constr = "URI=file:";
    private IDbConnection _dbc;
    private IDbCommand _dbcm;
    private IDataReader _dbr;
    public string databaseNameOverride;

    void Awake()
    {
        databaseName = simulation.ToString();
        if (databaseNameOverride.Length > 0)
            databaseName = databaseNameOverride;

        string databaseFilename = Application.persistentDataPath + string.Format("/{0}.db", databaseName);
        _constr += databaseFilename;

        switch (simulation)
        {
            case SimulationType.Sensor:
                CreateDBAndTablesSensor();
                break;

            case SimulationType.Controller:
                if (gameObject.GetComponent<Optimizer>().runType == RunType.NoveltySearch || gameObject.GetComponent<Optimizer>().runType == RunType.Hybrid)
                    CreateDBAndTablesControllerNoveltySearchOrHybrid();
                else
                    CreateDBAndTablesController();
                break;

            case SimulationType.PerformanceTesting:
                CreateDBAndTablesPerformanceTest();
                break;

            case SimulationType.Evaluation:
                //CreateDBAndTablesEvaluations();
                //CreateDBAndTablesEvaluationsRadar();
                CreateDBAndTablesEvaluationsFinal();
                break;
        }
    }

    private void CreateDBAndTablesPerformanceTest()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SIMULATION_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "CREATE TABLE SIMULATION_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL,"
                    + " GENERATIONS_RUN INT NOT NULL, "
                    + " POPULATION_SIZE INT NOT NULL, "
                    + " TRIAL_DURATION INT NOT NULL, "
                    + " TRACKS INT NOT NULL "
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FITNESS_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "CREATE TABLE FITNESS_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL, "
                    + " GENERATION INT NOT NULL,"
                    + " GENERATION_TOOK DOUBLE NOT NULL, "
                    + " EVALS_PER_SEC INT NOT NULL "
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    private void CreateDBAndTablesEvaluationsRadar()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EVAL_RESULTS_RADAR';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "CREATE TABLE EVAL_RESULTS_RADAR " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " CONFIG VARCHAR NOT NULL, "
                    + " RUN_ID VARCHAR NOT NULL, "
                    + " DISTANCE DOUBLE NOT NULL, "
                    + " S1_MIN DOUBLE NOT NULL, "
                    + " S2_MIN DOUBLE NOT NULL, "
                    + " S3_MIN DOUBLE NOT NULL, "
                    + " S4_MIN DOUBLE NOT NULL, "
                    + " S5_MIN DOUBLE NOT NULL, "
                    + " S6_MIN DOUBLE NOT NULL, "
                    + " S1_AVG DOUBLE NOT NULL, "
                    + " S2_AVG DOUBLE NOT NULL, "
                    + " S3_AVG DOUBLE NOT NULL, "
                    + " S4_AVG DOUBLE NOT NULL, "
                    + " S5_AVG DOUBLE NOT NULL, "
                    + " S6_AVG DOUBLE NOT NULL, "
                    + " S1_MAX DOUBLE NOT NULL, "
                    + " S2_MAX DOUBLE NOT NULL, "
                    + " S3_MAX DOUBLE NOT NULL, "
                    + " S4_MAX DOUBLE NOT NULL, "
                    + " S5_MAX DOUBLE NOT NULL,  "
                    + " S6_MAX DOUBLE NOT NULL  "
                    + ");";

            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    private void CreateDBAndTablesEvaluations()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EVAL_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "CREATE TABLE EVAL_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " CONFIG VARCHAR NOT NULL, "
                    + " START_POS INT NOT NULL, "
                    + " DIST DOUBLE NOT NULL, "
                    + " CHAMP_XML TEXT NOT NULL, "
                    + " S_1 DOUBLE NOT NULL, "
                    + " S_2 DOUBLE NOT NULL, "
                    + " S_3 DOUBLE NOT NULL, "
                    + " S_4 DOUBLE NOT NULL, "
                    + " S_5 DOUBLE NOT NULL, "
                    + " S_6 DOUBLE NOT NULL, "
                    + " S_7 DOUBLE NOT NULL, "
                    + " S_8 DOUBLE NOT NULL, "
                    + " S_9 DOUBLE NOT NULL, "
                    + " S_10 DOUBLE NOT NULL "
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    private void CreateDBAndTablesEvaluationsFinal()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='EVAL_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "CREATE TABLE EVAL_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_DB VARCHAR NOT NULL, "
                    + " RUN_ID VARCHAR NOT NULL, "
                    + " TRACK_ID VARCHAR NOT NULL, "
                    + " N_CARS INT NOT NULL, "
                    + " TRIAL INT NOT NULL, "
                    + " FITNESS DOUBLE NOT NULL "
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    private void CreateDBAndTablesController()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SIMULATION_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating SIMULATION_RESULTS TABLE");
            sql = "CREATE TABLE SIMULATION_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL,"
                    + " GENERATIONS_RUN INT NOT NULL, "
                    + " POPULATION_SIZE INT NOT NULL, "
                    + " TRIAL_DURATION INT NOT NULL, "
                    + " TRACKS INT NOT NULL, "
                    + " CHAMP_SENSOR_XML TEXT, "
                    + " CHAMP_XML TEXT"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FITNESS_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating FITNESS_LOGGING TABLE");
            sql = "CREATE TABLE FITNESS_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL, "
                    + " GENERATION INT NOT NULL,"
                    + " AVERAGE_FITNESS DOUBLE NOT NULL, "
                    + " CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " GLOBAL_CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " NEW_CHAMP_FOUND INT NOT NULL, "
                    + " CHAMP_COMPLEXITY DOUBLE NOT NULL, "
                    + " AVERAGE_COMPLEXITY DOUBLE NOT NULL, "
                    + " MAX_COMPLEXITY DOUBLE NOT NULL"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='GENERATION_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating GENERATION_LOGGING TABLE");
            sql = "CREATE TABLE GENERATION_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL, "
                    + " GENERATION INT NOT NULL,"
                    + " GENOME_ID INT NOT NULL,"
                    + " GENOME_BIRTH_GEN INT NOT NULL, "
                    + " GENOME_COMPLEXITY DOUBLE NOT NULL, "
                    + " GENOME_FITNESS DOUBLE NOT NULL"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    private void CreateDBAndTablesControllerNoveltySearchOrHybrid()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SIMULATION_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating SIMULATION_RESULTS TABLE");
            sql = "CREATE TABLE SIMULATION_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL,"
                    + " GENERATIONS_RUN INT NOT NULL, "
                    + " POPULATION_SIZE INT NOT NULL, "
                    + " TRIAL_DURATION INT NOT NULL, "
                    + " TRACKS INT NOT NULL, "
                    + " CHAMP_SENSOR_XML TEXT, "
                    + " FIT_CHAMP_XML TEXT, "
                    + " NOV_CHAMP_XML TEXT"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FITNESS_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating FITNESS_LOGGING TABLE");
            sql = "CREATE TABLE FITNESS_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL, "
                    + " GENERATION INT NOT NULL,"
                    + " AVERAGE_FITNESS DOUBLE NOT NULL, "
                    + " NOV_CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " FIT_CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " NOV_GLOBAL_CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " FIT_GLOBAL_CHAMP_FITNESS DOUBLE NOT NULL, "
                    + " AVERAGE_NOVELTY DOUBLE NOT NULL, "
                    + " NOV_CHAMP_NOVELTY DOUBLE NOT NULL, "
                    + " NOV_GLOBAL_CHAMP_NOVELTY DOUBLE NOT NULL, "
                    + " FIT_CHAMP_NOVELTY DOUBLE NOT NULL, "
                    + " FIT_GLOBAL_CHAMP_NOVELTY DOUBLE NOT NULL, "
                    + " NOV_NEW_CHAMP_FOUND INT NOT NULL, "
                    + " FIT_NEW_CHAMP_FOUND INT NOT NULL, "
                    + " NOV_CHAMP_COMPLEXITY DOUBLE NOT NULL, "
                    + " FIT_CHAMP_COMPLEXITY DOUBLE NOT NULL, "
                    + " AVERAGE_COMPLEXITY DOUBLE NOT NULL, "
                    + " MAX_COMPLEXITY DOUBLE NOT NULL"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='GENERATION_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating GENERATION_LOGGING TABLE");
            sql = "CREATE TABLE GENERATION_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                    + " RUN_ID STRING NOT NULL, "
                    + " GENERATION INT NOT NULL,"
                    + " GENOME_ID INT NOT NULL,"
                    + " GENOME_BIRTH_GEN INT NOT NULL, "
                    + " GENOME_COMPLEXITY DOUBLE NOT NULL, "
                    + " GENOME_FITNESS DOUBLE NOT NULL, "
                    + " GENOME_NOVELTY DOUBLE NOT NULL"
                    + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }
        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogNewRun(string runId, int popSize, float trialDuration, int numTracks, string sensor_xml)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT RUN_ID FROM SIMULATION_RESULTS WHERE RUN_ID='" + runId + "';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            sql = "INSERT INTO SIMULATION_RESULTS (RUN_ID, GENERATIONS_RUN, POPULATION_SIZE, TRIAL_DURATION, TRACKS, CHAMP_SENSOR_XML) VALUES ('"
                + runId
                    + "', '"
                    + 0
                    + "', '"
                    + popSize
                    + "', '"
                    + trialDuration
                    + "', '"
                    + numTracks
                    + "', '"
                    + sensor_xml
                    + "');";

            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void UpdateRunGenerationCount(string runId, int generation)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "UPDATE SIMULATION_RESULTS SET GENERATIONS_RUN = '" + generation + "' WHERE RUN_ID = '" + runId + "';";


        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogCurrentGeneration(string runId, int generation, double popFitness, double champFitness, double globalChampFitness, bool newChampFound,
                                     double champComplexity, double avgComplexity, double maxComplexity)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO FITNESS_LOGGING (RUN_ID, GENERATION, AVERAGE_FITNESS, CHAMP_FITNESS, GLOBAL_CHAMP_FITNESS, NEW_CHAMP_FOUND, " +
            "CHAMP_COMPLEXITY, AVERAGE_COMPLEXITY, MAX_COMPLEXITY) VALUES ('"
            + runId
                + "', '"
                + generation
                + "', '"
                + popFitness
                + "', '"
                + champFitness
                + "', '"
                + globalChampFitness
                + "', '"
                + newChampFound
                + "', '"
                + champComplexity
                + "', '"
                + avgComplexity
                + "', '"
                + maxComplexity
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogCurrentGeneration(string runId, int generation, double popFitness, double novChampFitness, double globalNovChampFitness, double fitChampFitness, double globalFitChampFitness, double popNovelty,
                                     double novChampNovelty, double fitChampNovelty, double globalNovChampNovelty, double globalFitChampNovelty, bool newNovChampFound, bool newFitChampFound,
                                     double novChampComplexity, double fitChampComplexity, double avgComplexity, double maxComplexity)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO FITNESS_LOGGING" +
            " (RUN_ID, GENERATION, AVERAGE_FITNESS, NOV_CHAMP_FITNESS, FIT_CHAMP_FITNESS, NOV_GLOBAL_CHAMP_FITNESS, FIT_GLOBAL_CHAMP_FITNESS, AVERAGE_NOVELTY, NOV_CHAMP_NOVELTY, NOV_GLOBAL_CHAMP_NOVELTY, " +
            "FIT_CHAMP_NOVELTY, FIT_GLOBAL_CHAMP_NOVELTY, NOV_NEW_CHAMP_FOUND, FIT_NEW_CHAMP_FOUND, NOV_CHAMP_COMPLEXITY, FIT_CHAMP_COMPLEXITY, AVERAGE_COMPLEXITY, MAX_COMPLEXITY) VALUES ('"
                + runId
                + "', '"
                + generation
                + "', '"
                + popFitness
                + "', '"
                + novChampFitness
                + "', '"
                + fitChampFitness
                + "', '"
                + globalNovChampFitness
                + "', '"
                + globalFitChampFitness
                + "', '"
                + popNovelty
                + "', '"
                + novChampNovelty
                + "', '"
                + globalNovChampNovelty
                + "', '"
                + fitChampNovelty
                + "', '"
                + globalFitChampNovelty
                + "', '"
                + newNovChampFound
                + "', '"
                + newFitChampFound
                + "', '"
                + novChampComplexity
                + "', '"
                + fitChampComplexity
                + "', '"
                + avgComplexity
                + "', '"
                + maxComplexity
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }


    public void LogCurrentGenerationGenomePopulation(
        string runId,
        int generation,
        int genomeId,
        int genomeBirthGeneration,
        double genomeComplexity,
        double genomeFitness)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO GENERATION_LOGGING (RUN_ID, GENERATION, GENOME_ID, GENOME_BIRTH_GEN, GENOME_COMPLEXITY, GENOME_FITNESS" +
            ") VALUES ('"
                + runId
                + "', '"
                + generation
                + "', '"
                + genomeId
                + "', '"
                + genomeBirthGeneration
                + "', '"
                + genomeComplexity
                + "', '"
                + genomeFitness
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogCurrentGenerationGenomePopulation(
        string runId,
        int generation,
        int genomeId,
        int genomeBirthGeneration,
        double genomeComplexity,
        double genomeFitness,
        double genomeNovelty)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO GENERATION_LOGGING (RUN_ID, GENERATION, GENOME_ID, GENOME_BIRTH_GEN, GENOME_COMPLEXITY, GENOME_FITNESS, GENOME_NOVELTY" +
            ") VALUES ('"
                + runId
                + "', '"
                + generation
                + "', '"
                + genomeId
                + "', '"
                + genomeBirthGeneration
                + "', '"
                + genomeComplexity
                + "', '"
                + genomeFitness
                + "', '"
                + genomeNovelty
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogCurrentGeneration(string runId, int generation, double generationTime, int evalsPerSec)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO FITNESS_LOGGING (RUN_ID, GENERATION, GENERATION_TOOK, EVALS_PER_SEC) VALUES ('"
                + runId
                + "', '"
                + generation
                + "', '"
                + generationTime
                + "', '"
                + evalsPerSec
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    //==== SENSOR STUFF ====

    private void CreateDBAndTablesSensor()
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='SIMULATION_RESULTS';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating SIMULATION_RESULTS TABLE");
            sql = "CREATE TABLE SIMULATION_RESULTS " + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                + " RUN_ID STRING NOT NULL,"
                + " MAX_GENERATIONS INT NOT NULL, "
                + " POPULATION_SIZE INT NOT NULL, "
                + " TRACK VARCHAR NOT NULL, "
                + " CHAMP_XML TEXT"
                + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='FITNESS_LOGGING';";
        _dbr = _command.ExecuteReader();

        if (!_dbr.Read())
        {
            print("Creating FITNESS_LOGGING TABLE");
            sql = "CREATE TABLE FITNESS_LOGGING" + "("
                + " ID INTEGER PRIMARY KEY AUTOINCREMENT,"
                + " RUN_ID STRING NOT NULL, "
                + " GENERATION INT NOT NULL,"
                + " AVERAGE_FITNESS DOUBLE NOT NULL, "
                + " CHAMP_FITNESS DOUBLE NOT NULL, "
                + " GLOBAL_CHAMP_FITNESS DOUBLE NOT NULL, "
                + " NEW_CHAMP_FOUND INT NOT NULL"
                + ");";
            _command.CommandText = sql;
            _command.ExecuteNonQuery();
        }

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogNewRun(string runId, int maxGenerations, int popSize)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();
        sql = "INSERT INTO SIMULATION_RESULTS (RUN_ID, MAX_GENERATIONS, POPULATION_SIZE, TRACK) VALUES ('"
            + runId
                + "', '"
                + maxGenerations
                + "', '"
                + popSize
                + "', '"
                + Application.loadedLevelName
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogCurrentGeneration(string runId, int generation, float popFitness, float champFitness, float globalChampFitness, bool newChampFound)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO FITNESS_LOGGING (RUN_ID, GENERATION, AVERAGE_FITNESS, CHAMP_FITNESS, GLOBAL_CHAMP_FITNESS, NEW_CHAMP_FOUND) VALUES ('"
            + runId
                + "', '"
                + generation
                + "', '"
                + popFitness
                + "', '"
                + champFitness
                + "', '"
                + globalChampFitness
                + "', '"
                + newChampFound
                + "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void UpdateRunWithChampXML(string runId, string champ)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "UPDATE SIMULATION_RESULTS SET CHAMP_XML = '" + champ + "' WHERE RUN_ID = '" + runId + "';";
        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void UpdateRunWithChampXML(string runId, string novChamp, string fitChamp)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "UPDATE SIMULATION_RESULTS SET NOV_CHAMP_XML = '" + novChamp + "', FIT_CHAMP_XML = '" + fitChamp + "' WHERE RUN_ID = '" + runId + "';";
        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogEvaluation(string sensorConfig, string champId, List<float> sensorMin, List<float> sensorAvg, List<float> sensorMax, float distance)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO EVAL_RESULTS_RADAR (CONFIG, RUN_ID, DISTANCE, S1_MIN, S2_MIN, S3_MIN, S4_MIN, S5_MIN, S6_MIN, " +
            "S1_AVG, S2_AVG, S3_AVG, S4_AVG, S5_AVG, S6_AVG, S1_MAX, S2_MAX, S3_MAX, S4_MAX, S5_MAX, S6_MAX) VALUES ('"
            + sensorConfig
            + "', '"
            + champId
            + "', '"
            + distance;

        foreach (float min in sensorMin)
            sql += "', '" + min;

        for (int i = sensorMin.Count; i < 6; i++)
            sql += "', '-1";

        foreach (float avg in sensorAvg)
            sql += "', '" + avg;

        for (int i = sensorAvg.Count; i < 6; i++)
            sql += "', '-1";

        foreach (float max in sensorMax)
            sql += "', '" + max;

        for (int i = sensorMax.Count; i < 6; i++)
            sql += "', '-1";

        sql += "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogEvaluation(string sensorConfiguration, string startingMarker, float distance, string champXml, List<float> sensorDurations)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO EVAL_RESULTS (CONFIG, START_POS, DIST, CHAMP_XML, S_1, S_2, S_3, S_4, S_5, S_6, S_7, S_8, S_9, S_10) VALUES ('"
            + sensorConfiguration
            + "', '"
            + startingMarker
            + "', '"
            + distance
            + "', '"
            + champXml;


        foreach (float sensorDuration in sensorDurations)
        {
            sql += "', '" + sensorDuration;
        }

        sql += "');";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }

    public void LogEvaluation(string database, string runId, string carCount, double fitness, int currentTrialCount, string trackId)
    {
        IDbConnection _connection = new SqliteConnection(_constr);
        IDbCommand _command = _connection.CreateCommand();
        string sql;
        _connection.Open();

        sql = "INSERT INTO EVAL_RESULTS (RUN_DB, RUN_ID, TRACK_ID, N_CARS, TRIAL, FITNESS) VALUES ('"
            + database
            + "', '"
            + runId
            + "', '"
            + trackId
            + "', '"
            + carCount
            + "' , '"
            + currentTrialCount
            + "', "
            + fitness;

        sql += ");";

        _command.CommandText = sql;
        _command.ExecuteNonQuery();

        _command.Dispose();
        _command = null;
        _connection.Close();
        _connection = null;
    }
}
