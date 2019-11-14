using UnityEngine;
using System.Collections;
using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using SharpNeat.DistanceMetrics;
using System.Linq;

public class SimpleEvaluator : IPhenomeEvaluator<IBlackBox> {

	ulong _evalCount;
	bool _stopConditionSatisfied;
	Optimizer optimizer;
	FitnessInfo fitness;

	Dictionary<IBlackBox, FitnessInfo> dict = new Dictionary<IBlackBox, FitnessInfo>();

	public ulong EvaluationCount
	{
		get { return _evalCount; }
	}

	public bool StopConditionSatisfied
	{
		get { return _stopConditionSatisfied; }
	}

	public SimpleEvaluator(Optimizer se)
	{
		this.optimizer = se;
	}

	public IEnumerator Evaluate(IBlackBox box)
	{
		if (optimizer != null)
		{
			optimizer.Evaluate(box);
//			yield return new WaitForSeconds(optimizer.TrialDuration);

			int seconds = 0;
			do {
				yield return new WaitForSeconds(1);
				seconds++;
			} while(!optimizer.ControllerMap[box].Stopped && seconds < optimizer.TrialDuration);

            float nov = optimizer.GetNovelty(box); //This won't have anything here
            float fit = optimizer.GetFitness(box);
            optimizer.StopEvaluation(box);
		   
			FitnessInfo fitness = new FitnessInfo(fit, nov);
			dict.Add(box, fitness);
		}
	}

    public float CalculateNoveltyScore(IBlackBox individual)
    {
        EuclideanDistanceMetric edm = new EuclideanDistanceMetric();
        float[] currentBC = optimizer.ControllerBehaviourMap[individual];
        int k_nearest = 15;

        List<float> sparseness = new List<float>();

        // Compare with current population
        foreach (IBlackBox other in optimizer.ControllerBehaviourMap.Keys)
        {
            if (other == individual)
                continue;

            float[] otherBC = optimizer.ControllerBehaviourMap[other];
            sparseness.Add((float)edm.MeasureDistance(currentBC, otherBC));
        }

        // Compare with archive
        foreach (float[] otherBC in optimizer.NoveltyArchive.Values)
            sparseness.Add((float)edm.MeasureDistance(currentBC, otherBC));

        sparseness.Sort();
        var novelty = sparseness.Take(k_nearest).Sum() / k_nearest; //15 nearest neighbours
        return novelty;
    }

    public void Reset()
	{
		this.fitness = FitnessInfo.Zero;
		dict = new Dictionary<IBlackBox, FitnessInfo>();
	}

	public FitnessInfo GetLastFitness()
	{
		return this.fitness;
	}


	public FitnessInfo GetLastFitness(IBlackBox phenome)
	{
		if (dict.ContainsKey(phenome))
		{
			FitnessInfo fit = dict[phenome];
			dict.Remove(phenome);
		   
			return fit;
		}
		
		return FitnessInfo.Zero;
	}
}
