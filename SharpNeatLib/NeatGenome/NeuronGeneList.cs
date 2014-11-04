using System;
using System.Collections.Generic;


namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class NeuronGeneList : List<NeuronGene>
	{
		static NeuronGeneComparer neuronGeneComparer = new NeuronGeneComparer();
		public bool OrderInvalidated=false;

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public NeuronGeneList()
		{}

        public NeuronGeneList(int count)
        {
            Capacity = (int)(count*1.5);
        }

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
		public NeuronGeneList(NeuronGeneList copyFrom)
		{
			int count = copyFrom.Count;
			Capacity = count;
			
			for(int i=0; i<count; i++)
				Add(new NeuronGene(copyFrom[i]));

//			foreach(NeuronGene neuronGene in copyFrom)
//				InnerList.Add(new NeuronGene(neuronGene));
		}

		#endregion

		#region Public Methods

        /// <summary>
        /// Inserts a NeuronGene into its correct (sorted) location within the gene list.
        /// Normally neuron genes can safely be assumed to have a new Innovation ID higher
        /// than all existing ID's, and so we can just call Add().
        /// This routine handles genes with older ID's that need placing correctly.
        /// </summary>
        /// <param name="neuronGene"></param>
        /// <returns></returns>
        public void InsertIntoPosition(NeuronGene neuronGene)
        {
            // Determine the insert idx with a linear search, starting from the end 
            // since mostly we expect to be adding genes that belong only 1 or 2 genes
            // from the end at most.
            int idx = Count - 1;
            for (; idx > -1; idx--)
            {
                if (this[idx].InnovationId < neuronGene.InnovationId)
                {	// Insert idx found.
                    break;
                }
            }
            Insert(idx + 1, neuronGene);
        }

		new public void Remove(NeuronGene neuronGene)
		{
			Remove(neuronGene.InnovationId);

			// This invokes a linear search. Invoke our binary search instead.
			//InnerList.Remove(neuronGene);
		}

		public void Remove(uint neuronId)
		{
			int idx = BinarySearch(neuronId);
			if(idx<0)
				throw new ApplicationException("Attempt to remove neuron with an unknown neuronId");
			else
				RemoveAt(idx);

//			// Inefficient scan through the neuron list.
//			// TODO: Implement a binary search method for NeuronList (Will generics resolve this problem anyway?).
//			int bound = List.Count;
//			for(int i=0; i<bound; i++)
//			{
//				if(((NeuronGene)List[i]).InnovationId == neuronId)
//				{
//					InnerList.RemoveAt(i);
//					return;
//				}
//			}
//			throw new ApplicationException("Attempt to remove neuron with an unknown neuronId");
		}

		public NeuronGene GetNeuronById(uint neuronId)
		{
			int idx = BinarySearch(neuronId);
			if(idx<0)
				return null;
			else
				return this[idx];

//			// Inefficient scan through the neuron list.
//			// TODO: Implement a binary search method for NeuronList (Will generics resolve this problem anyway?).
//			int bound = List.Count;
//			for(int i=0; i<bound; i++)
//			{
//				if(((NeuronGene)List[i]).InnovationId == neuronId)
//					return (NeuronGene)List[i];
//			}
//
//			// Not found;
//			return null;
		}

		public void SortByInnovationId()
		{
			Sort(neuronGeneComparer);
			OrderInvalidated=false;
		}

		public int BinarySearch(uint innovationId) 
		{            
			int lo = 0;
			int hi = Count-1;

			while (lo <= hi) 
			{
				int i = (lo + hi) >> 1;

				if(this[i].InnovationId<innovationId)
					lo = i + 1;
				else if(this[i].InnovationId>innovationId)
					hi = i - 1;
				else
					return i;


				// TODO: This is wrong. It will fail for large innovation numbers because they are of type uint.
				// Fortunately it's very unlikely anyone has reached such large numbers!
//				int c = (int)((NeuronGene)InnerList[i]).InnovationId - (int)innovationId;
//				if (c == 0) return i;
//
//				if (c < 0) 
//					lo = i + 1;
//				else 
//					hi = i - 1;
			}
			
			return ~lo;
		}

		// For debug purposes only.
//		public bool IsSorted()
//		{
//			uint prevId=0;
//			foreach(NeuronGene gene in InnerList)
//			{
//				if(gene.InnovationId<prevId)
//					return false;
//				prevId = gene.InnovationId;
//			}
//			return true;
//		}

		#endregion
	}
}
