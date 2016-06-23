using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	/// <summary>
	/// Compares the innovation ID of NeuronGenes.
	/// </summary>
	public class NeuronGeneComparer : IComparer<NeuronGene>
	{
        #region IComparer<NeuronGene> Members

        public int Compare(NeuronGene x, NeuronGene y)
        {
            // Test the most likely cases first.
            if (x.InnovationId < y.InnovationId)
                return -1;
            else if (x.InnovationId > y.InnovationId)
                return 1;
            else
                return 0;
        }

        #endregion
    }
}
