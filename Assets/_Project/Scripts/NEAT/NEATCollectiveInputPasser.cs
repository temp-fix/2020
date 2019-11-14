using Assets.Car;
using UnityEngine;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using System.Linq;
using System;

public class NEATCollectiveInputPasser : UnitController
{ 
    private NEATCarInputHandler[] carscripts = new NEATCarInputHandler [0];
    private int frame;
    private float novelty;
    private BehaviourCharacterisationType bcType;
    private float[] BehaviourCharacterisation;

    private void Awake()
    {
        GetCarScripts();
    }

    private void GetCarScripts()
    {
        carscripts = GetComponentsInChildren<NEATCarInputHandler>();
    }

    public override void Activate(IBlackBox box)
    {
        GetCarScripts();
        foreach (NEATCarInputHandler carscript in carscripts)
            carscript.Activate(box);
    }

    public override int GetNumberOfOutputsNeededFromNeat()
    {
        if (carscripts.Length == 0)
            GetCarScripts();

        return carscripts[0].GetNumberOfOutputsNeededFromNeat();
    }

    public override int GetNumberOfInputsIntoNeat()
    {
        if (carscripts.Length == 0)
            GetCarScripts();

        return carscripts[0].GetNumberOfInputsIntoNeat();
    }

    public override float GetFitness()
    {
        float fitness = 0;

        foreach (NEATCarInputHandler carscript in carscripts)
            fitness += carscript.GetFitness();

        //List<float> completeDistanceList = new List<float>();
        //foreach (List<float> distanceList in distancesBetweenCars.Values)
        //    completeDistanceList.AddRange(distanceList);

        //return fitness / carscripts.Length / completeDistanceList.Average();
        return fitness / carscripts.Length;
    }

    public override float GetNovelty()
    {
        //float novelty = -1;
        //foreach (NEATCarInputHandler carscript in carscripts)
        //{
        //    float currentNovelty = carscript.GetNovelty();
        //    if (currentNovelty > -1)
        //        novelty += currentNovelty;
        //}

        //return (novelty > -1) ? (novelty / carscripts.Length) : -1;
        return novelty;
    }

    public override void SetWaypoints(GameObject[] waypoints)
    {
        if (carscripts.Length == 0)
            GetCarScripts();

        foreach (NEATCarInputHandler carscript in carscripts)
            carscript.SetWaypoints(waypoints);
    }

    public override bool Stopped
    {
        get
        {
            bool allStopped = true;

            foreach (NEATCarInputHandler carscript in carscripts)
                if (!carscript.Stopped)
                    allStopped = false;

            return allStopped;
        }
    }

    public override void Stop()
    {
        foreach (NEATCarInputHandler carscript in carscripts)
            carscript.Stop();
    }

    public override Dictionary<string, List<float>> SensorInputs
    {
        get
        {
            Dictionary<string, List<float>> sensorAverages = new Dictionary<string, List<float>>();

            foreach (NEATCarInputHandler car in carscripts)
            {
                foreach (string sensor in car.SensorInputs.Keys)
                {
                    if (!sensorAverages.ContainsKey(sensor))
                        sensorAverages.Add(sensor, new List<float>());

                    sensorAverages[sensor].Add(car.SensorInputs[sensor].Sum() / car.SensorInputs[sensor].Count);
                }
            }

            return sensorAverages;
        }
    }

    private void Start()
    {
        frame = 0;
    }

    private void FixedUpdate()
    {
        frame++;

        if (carscripts.Length > 1 && bcType == BehaviourCharacterisationType.CohesionAndSpeed && frame % 50 == 0)
        {
            float speed = 0.0f;
            float distanceBetweenCars = 0.0f;

            for (int i = 0; i < carscripts.Length - 1; i++)
            {
                speed += carscripts[i].gameObject.GetComponent<CarDriving>().CurrentSpeed;

                for (int j = 1; j < carscripts.Length; j++)
                {
                    distanceBetweenCars += Vector3.Distance(carscripts[i].gameObject.transform.position, carscripts[j].gameObject.transform.position);
                }
            }

            int timestep = frame / 50 - 1;

            if (timestep * 2 < BehaviourCharacterisation.Length) {
                BehaviourCharacterisation[timestep] = Mathf.Round(distanceBetweenCars * 100f) / 100f;
                BehaviourCharacterisation[BehaviourCharacterisation.Length / 2 + timestep] = Mathf.Round(speed * 100f) / 100f;
              }
        }
    }

    public override void SetBehaviourCharacterisation(BehaviourCharacterisationType type)
    {
        bcType = type;
        if (carscripts.Length == 0)
            GetCarScripts();

        foreach (NEATCarInputHandler carscript in carscripts)
            carscript.SetBehaviourCharacterisation(type);

        if (type == BehaviourCharacterisationType.CohesionAndSpeed)
            BehaviourCharacterisation = Enumerable.Repeat(0.0f, 200).ToArray(); //Initialise BC with 0.0 values
    }

    public override void SetRunType(RunType type)
    {
        if (carscripts.Length == 0)
            GetCarScripts();

        foreach (NEATCarInputHandler carscript in carscripts)
            carscript.SetRunType(type);
    }

    public override void SetNovelty(float novelty)
    {
        this.novelty = novelty;
    }

    public override float[] GetBehaviourCharacterisation()
    {
        if (carscripts.Length == 0)
            GetCarScripts();

        if (bcType != BehaviourCharacterisationType.CohesionAndSpeed) {
            List<float[]> characterisations = new List<float[]>();
            int characterisationLength = carscripts[0].GetBehaviourCharacterisation().Length;

            for (int i = 0; i < carscripts.Length; i++)
            {
                var current = carscripts[i].GetBehaviourCharacterisation();
                if (current.Length != characterisationLength)
                    throw new Exception("Characterisation lengths vary!!!");

                characterisations.Add(current);
            }

            characterisations.Sort(new BehaviourCharacterisationComparer());

            List<float> combined = new List<float>();

            foreach (float[] c in characterisations)
                combined.AddRange(c);

            return combined.ToArray<float>();
        }

        return BehaviourCharacterisation;
    }
}

class BehaviourCharacterisationComparer : IComparer<float[]>
{
    public int Compare(float[] x, float[] y)
    {
        return (x.Sum()).CompareTo(y.Sum());
    }
}