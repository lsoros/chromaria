using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class ConnectionGeneList : List<ConnectionGene>
	{
		static ConnectionGeneComparer connectionGeneComparer = new ConnectionGeneComparer();
		//public bool OrderInvalidated=false;

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public ConnectionGeneList()
		{}

        public ConnectionGeneList(int count)
        {
            Capacity = (int)(count*1.5);
        }

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
		public ConnectionGeneList(ConnectionGeneList copyFrom)
		{
			int count = copyFrom.Count;
		    Capacity = count;
			for(int i=0; i<count; i++)
				Add(new ConnectionGene(copyFrom[i]));
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Inserts a ConnectionGene into its correct (sorted) location within the gene list.
		/// Normally connection genes can safely be assumed to have a new Innovation ID higher
		/// than all existing ID's, and so we can just call Add().
		/// This routine handles genes with older ID's that need placing correctly.
		/// </summary>
		/// <param name="connectionGene"></param>
		/// <returns></returns>
		public void InsertIntoPosition(ConnectionGene connectionGene)
		{
			// Determine the insert idx with a linear search, starting from the end 
			// since mostly we expect to be adding genes that belong only 1 or 2 genes
			// from the end at most.
			int idx=Count-1;
			for(; idx>-1; idx--)
			{
				if(this[idx].InnovationId < connectionGene.InnovationId)
				{	// Insert idx found.
					break;
				}
			}
			Insert(idx+1, connectionGene);
		}

		/*public void Remove(ConnectionGene connectionGene)
		{
			Remove(connectionGene.InnovationId);

			// This invokes a linear search. Invoke our binary search instead.
			//InnerList.Remove(connectionGene);
		}*/

		public void Remove(uint innovationId)
		{
			int idx = BinarySearch(innovationId);
			if(idx<0)
				throw new ApplicationException("Attempt to remove connection with an unknown innovationId");
			else
				RemoveAt(idx);
		}

		public void SortByInnovationId()
		{
			Sort(connectionGeneComparer);
			//OrderInvalidated=false;
		}

		public int BinarySearch(uint innovationId) 
		{            
			int lo = 0;
			int hi = Count-1;

			while (lo <= hi) 
			{
				int i = (lo + hi) >> 1;
				int c = (int)(this[i]).InnovationId - (int)innovationId;
				if (c == 0) return i;

				if (c < 0) 
					lo = i + 1;
				else 
					hi = i - 1;
			}
			
			return ~lo;
		}

		/// <summary>
		/// For debug purposes only. Don't call this in normal circumstances as it is an
		/// expensive O(n) operation.
		/// </summary>
		/// <returns></returns>
		public bool IsSorted()
		{
			uint prevId=0;
			foreach(ConnectionGene gene in this)
			{
				if(gene.InnovationId<prevId)
					return false;
				prevId = gene.InnovationId;
			}
			return true;
		}

		#endregion
	}
}
