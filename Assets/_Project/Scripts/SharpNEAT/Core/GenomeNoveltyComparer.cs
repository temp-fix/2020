using System.Collections.Generic;

namespace SharpNeat.Core
{
    /// <summary>
    /// Sort genomes, highest novelty first. Genomes with equal novelty are secondary sorted by age 
    /// (youngest first). Used by the selection routines to select the fittest and youngest genomes.
    /// </summary>
    public class GenomeNoveltyComparer<TGenome> : IComparer<TGenome>
        where TGenome : class, IGenome<TGenome>
    {
        /// <summary>
        /// Pre-built comparer.
        /// </summary>
        public static readonly GenomeNoveltyComparer<TGenome> Singleton = new GenomeNoveltyComparer<TGenome>();
        #region IComparer<TGenome> Members

        /// <summary>
        /// Sort genomes, highest novelty first. Genomes with equal novelty are secondary sorted by age (youngest first).
        /// Used by the selection routines to select the fittest and youngest genomes.
        /// </summary>
        public int Compare(TGenome x, TGenome y)
        {
            // Primary sort - highest novelty first.
            if (x.EvaluationInfo.Novelty > y.EvaluationInfo.Novelty)
            {
                return -1;
            }
            if (x.EvaluationInfo.Novelty < y.EvaluationInfo.Novelty)
            {
                return 1;
            }

            // Noveltyes are equal.
            // Secondary sort - youngest first. Younger genomes have a *higher* BirthGeneration.
            if (x.BirthGeneration > y.BirthGeneration)
            {
                return -1;
            }
            if (x.BirthGeneration < y.BirthGeneration)
            {
                return 1;
            }

            // Genomes are equal.
            return 0;
        }

        #endregion
    }
}
