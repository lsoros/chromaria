using System;
using System.Collections.Generic;
using Chromaria.SharpNeatLib.Evolution;

namespace Chromaria.SharpNeatLib.Evolution
{
	/// <summary>
	/// Sort by Fitness(Descending). Genomes with like fitness are then sorted by genome size(Ascending).
	/// This means the selection routines are more liley to select the fit AND the smallest first.
	/// </summary>
	public class GenomeComparer : IComparer<IGenome>
	{

        #region IComparer<IGenome> Members

        public int Compare(IGenome x, IGenome y)
        {
            double fitnessDelta = y.Fitness - x.Fitness;
            if (fitnessDelta < 0.0D)
                return -1;
            else if (fitnessDelta > 0.0D)
                return 1;

            long ageDelta = x.GenomeAge - y.GenomeAge;

            // Convert result to an int.
            if (ageDelta < 0)
                return -1;
            else if (ageDelta > 0)
                return 1;

            return 0;
        }

        #endregion
    }
}
