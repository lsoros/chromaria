// noveltyhistogram.cs created with MonoDevelop
// User: joel at 2:22 AM 7/23/2009
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.Novelty
{
	public class noveltyhistogram
	{	
		List<int> bins;
		int[] buffer;
		List<int> add_queue;
		
		public noveltyhistogram(List<int> _bins)
		{
			bins=_bins;
			add_queue = new List<int>();
			int size = 1;
			foreach (int i in bins)
				size*=i;
			buffer = new int[size];
		}

		public void update_histogram()
		{
			foreach (int i in add_queue)
			{
				buffer[i]+=1;
			}
			add_queue.Clear();
		}
		
		public double query_point(List<double> pt, bool add_to_queue)
		{
			double sparseness;
			int index = 0;
			int multiplier = 1;

			for(int x=0;x<bins.Count;x++)
			{
			    index += ((int) (pt[x]*(bins[x]-1)+0.49)) * multiplier;
				multiplier*=bins[x];
			}
			
			if(add_to_queue)
				add_queue.Add(index);
			
			sparseness = 1000.0 / (1.0 + (double)buffer[index]);
			return sparseness;
		}
	}
}
