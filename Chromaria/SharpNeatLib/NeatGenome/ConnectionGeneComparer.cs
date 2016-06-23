using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	/// <summary>
	/// Compares the innovation ID of ConnectionGenes.
	/// </summary>
	public class ConnectionGeneComparer : IComparer<ConnectionGene>
	{

        #region IComparer<ConnectionGene> Members

        public int Compare(ConnectionGene x, ConnectionGene y)
        {
            // Test the most likely cases first.
            if ((x).InnovationId < (y).InnovationId)
                return -1;
            else if ((x).InnovationId > (y).InnovationId)
                return 1;
            else
                return 0;
        }

        #endregion
    }
}
