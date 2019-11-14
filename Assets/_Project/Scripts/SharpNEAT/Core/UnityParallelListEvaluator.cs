using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Core;
using System.Collections;
using UnityEngine;
using SharpNeat.Phenomes;

namespace SharpNEAT.Core
{
    class UnityParallelListEvaluator<TGenome, TPhenome> : IGenomeListEvaluator<TGenome>
        where TGenome : class, IGenome<TGenome>
        where TPhenome : class
    {

        readonly IGenomeDecoder<TGenome, TPhenome> _genomeDecoder;
        IPhenomeEvaluator<TPhenome> _phenomeEvaluator;
        //readonly IPhenomeEvaluator<TPhenome> _phenomeEvaluator;
        Optimizer _optimizer;
        double maxNovelty = 0.0;
        double minNovelty = 1000000;

        #region Constructor

        /// <summary>
        /// Construct with the provided IGenomeDecoder and IPhenomeEvaluator.
        /// </summary>
        public UnityParallelListEvaluator(IGenomeDecoder<TGenome, TPhenome> genomeDecoder,
                                         IPhenomeEvaluator<TPhenome> phenomeEvaluator,
                                          Optimizer opt)
        {
            _genomeDecoder = genomeDecoder;
            _phenomeEvaluator = phenomeEvaluator;
            _optimizer = opt;
        }

        #endregion

        public ulong EvaluationCount
        {
            get { return _phenomeEvaluator.EvaluationCount; }
        }

        public bool StopConditionSatisfied
        {
            get { return _phenomeEvaluator.StopConditionSatisfied; }
        }

        public IEnumerator Evaluate(IList<TGenome> genomeList)
        {
			yield return Coroutiner.StartCoroutine(evaluateListIncrementally(genomeList, _optimizer.populationPartitions));
        }

		private IEnumerator evaluateListIncrementally(IList<TGenome> genomeList, int partitions) {
			Dictionary<TGenome, TPhenome> dict = new Dictionary<TGenome, TPhenome>();
			Dictionary<TGenome, FitnessInfo[]> fitnessDict = new Dictionary<TGenome, FitnessInfo[]>();
			int trials = _optimizer.trackChanger.tracks.Count();

			for (int i = 0; i < trials; i++)
			{
				//Utility.Log("Iteration " + (i + 1));
				_phenomeEvaluator.Reset();
				dict = new Dictionary<TGenome, TPhenome>();

				IList<IList<TGenome>> genomePartitions = Partition (genomeList, partitions);

				_optimizer.currentPartition = 1;
				foreach(IList<TGenome> genomeParition in genomePartitions) {
					_optimizer.carsDriving = true;

					foreach (TGenome genome in genomeParition)
					{
						TPhenome phenome = _genomeDecoder.Decode(genome);
						if (null == phenome)
						{   // Non-viable genome.
							genome.EvaluationInfo.SetFitness(0.0);
							genome.EvaluationInfo.AuxFitnessArr = null;
						}
						else
						{
							if (i == 0)
							{
								fitnessDict.Add(genome, new FitnessInfo[trials]);
							}
							dict.Add(genome, phenome);
							//if (!dict.ContainsKey(genome))
							//{
							//    dict.Add(genome, phenome);
							//    fitnessDict.Add(phenome, new FitnessInfo[trials]);
							//}
							Coroutiner.StartCoroutine(_phenomeEvaluator.Evaluate(phenome));
						}
					}

					int seconds = 0;
					do {
						yield return new WaitForSeconds(1);
						seconds++;
					} while(_optimizer.carsDriving && seconds < _optimizer.TrialDuration);

//					Debug.Log ("Partition " + _optimizer.currentPartition + " waited " + seconds + " seconds");
//					yield return new WaitForSeconds(_optimizer.TrialDuration);
					_optimizer.currentPartition ++;
				}


				foreach (TGenome genome in dict.Keys)
				{
					TPhenome phenome = dict[genome];
					if (phenome != null)
					{
						FitnessInfo fitnessInfo = _phenomeEvaluator.GetLastFitness(phenome);
						fitnessDict[genome][i] = fitnessInfo;
					}
				}

                _optimizer.trackChanger.nextTrack();
            }

            //TODO: Calculate Novelty now that all runs have completed.
            // Algorithm:   1. Find Nearest Neighbours in pop and archive
            //              2. sparseness = 1/k * sum(1->k, dist(x, mu)) - mu(i-th) is the i-th nearest neighbour)
            //              3. dist metric can use euclidean dist = (sqrt((x1 - mu1)^2 + (x2 - mu2)^2 ...))
            // Q:           How to access BC from here? :(

            // Algorithm:   1. sparseness_i = (s_i - s_min) / (s_max - s_min)
            if (_optimizer.runType == RunType.NoveltySearch || _optimizer.runType == RunType.Hybrid)
            {
                TGenome currentGenerationChamp = null;
                Dictionary<double, TGenome> genomesByNovelty = new Dictionary<double, TGenome>();
                //int numAddToArchive = 5;
                //int numAddToArchive = 10;
                int numAddToArchive = 15;
                foreach (TGenome genome in dict.Keys)
                {
                    TPhenome phenome = dict[genome];
                    if (phenome != null)
                    {
                        double novelty = 0;
                        novelty = _phenomeEvaluator.CalculateNoveltyScore(phenome);
                        genomesByNovelty[novelty] = genome;
                        if (novelty > maxNovelty)
                        {
                            maxNovelty = novelty;
                            currentGenerationChamp = genome;
                        }

                        if (novelty < minNovelty && novelty > 0)
                        {
                            minNovelty = novelty;
                        }
                        fitnessDict[genome][0]._auxFitnessArr[0]._value = novelty;
                    }
                }


                //normalise
                double denominator = (maxNovelty - minNovelty);
                foreach (TGenome genome in dict.Keys)
                {
                    var rawNovelty = fitnessDict[genome][0]._auxFitnessArr[0]._value;
                    double normalised = (rawNovelty - minNovelty) / denominator;
                    if (normalised > 1.0)
                    {
                        normalised = 1.0;
                    }
                    else if (normalised < 0.0)
                    {
                        normalised = 0.0;
                    }

                    if (_optimizer.runType == RunType.Hybrid)
                    {
                        double hybrid_score = (normalised + fitnessDict[genome][0]._fitness) / 2;
                        fitnessDict[genome][0]._auxFitnessArr[0]._value = hybrid_score;
                    }
                    else
                        fitnessDict[genome][0]._auxFitnessArr[0]._value = normalised;
                }

                List<double> noveltyScores = new List<double>(genomesByNovelty.Keys);
                noveltyScores.Sort();
                noveltyScores.Reverse();
                List<TGenome> genomesToAddToArchive = new List<TGenome>(noveltyScores.Take(numAddToArchive).Select(k => genomesByNovelty[k]));

                foreach (TGenome genome in genomesToAddToArchive)
                {
                    _optimizer.NoveltyArchive.Add((IBlackBox)dict[genome], _optimizer.ControllerBehaviourMap[(IBlackBox)dict[genome]]);
                    //Console.WriteLine("Added Genome with Novelty:", fitnessDict[genome][0]._auxFitnessArr[0]._value);
                }

                _optimizer.ControllerBehaviourMap.Clear();
            }

            foreach (TGenome genome in dict.Keys)
			{
				TPhenome phenome = dict[genome];
				if (phenome != null)
				{
					double fitness = 0;
                    double novelty = 0;
					
					for (int i = 0; i < trials; i++)
					{
						
						fitness += fitnessDict[genome][i]._fitness;
                        novelty += fitnessDict[genome][i]._auxFitnessArr[0]._value;
					}
					var fit = fitness;
					fitness /= trials; // Averaged fitness
					
					if (fit > _optimizer.StoppingFitness)
					{
						//  Utility.Log("Fitness is " + fit + ", stopping now because stopping fitness is " + _optimizer.StoppingFitness);
						//  _phenomeEvaluator.StopConditionSatisfied = true;
					}
					genome.EvaluationInfo.SetFitness(fitness);
                    genome.EvaluationInfo.SetNovelty(novelty);
					genome.EvaluationInfo.AuxFitnessArr = fitnessDict[genome][0]._auxFitnessArr;
				}
			}
		}

        public void Reset()
        {
            _phenomeEvaluator.Reset();
        }



		public static IList<TGenome>[] Partition<TGenome>(IList<TGenome> list, int totalPartitions)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			
			if (totalPartitions < 1)
				throw new ArgumentOutOfRangeException("totalPartitions");
			
			IList<TGenome>[] partitions = new IList<TGenome>[totalPartitions];
			
			int maxSize = (int)Math.Ceiling(list.Count / (double)totalPartitions);
			int k = 0;
			
			for (int i = 0; i < partitions.Length; i++)
			{
				partitions[i] = new List<TGenome>();
				for (int j = k; j < k + maxSize; j++)
				{
					if (j >= list.Count)
						break;
					partitions[i].Add(list[j]);
				}
				k += maxSize;
			}
			
			return partitions;
		}

    }
}
