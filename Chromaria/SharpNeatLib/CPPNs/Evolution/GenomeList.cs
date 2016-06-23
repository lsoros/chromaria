using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.Evolution
{

	public class GenomeList : List<IGenome>
	{
		static GenomeComparer genomeComparer = new GenomeComparer();
		static PruningModeGenomeComparer pruningModeGenomeComparer = new PruningModeGenomeComparer();

		new public void Sort()
		{
			Sort(genomeComparer);
		}

		/// <summary>
		/// This perfroms a secondary sort on genome size (ascending order), so that small genomes
		/// are more likely to be selected thus aiding a pruning phase.
		/// </summary>
		public void Sort_PruningMode()
		{
			Sort(pruningModeGenomeComparer);
		}

	}
}
