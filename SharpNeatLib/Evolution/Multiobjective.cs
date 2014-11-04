using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Novelty;
using System.Collections.Generic;
using System.Xml;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using System;


namespace Chromaria.SharpNeatLib.Multiobjective
{
	//information to rank each genome
	public class RankInformation
	{
		public	int domination_count; //when iterating, we count how many genomes dominate other genomes
		public	List<RankInformation> dominates; //who does this genome dominate
		public	int rank; //what is this genome's rank (i.e. what pareto front is it on)
		public  bool ranked;	//has this genome been ranked

		public RankInformation() {    
			dominates=new List<RankInformation>();
			reset();
		}
		
		public void reset() {
			rank=0;
			ranked=false;
			domination_count=0;
			dominates.Clear();
		}	
	}
	//class to assign multiobjective fitness to individuals (fitness based on what pareto front they are on)
	public class Multiobjective
	{
		
		int generation;
		public noveltyfixed nov;
		public GenomeList population;
		public bool doNovelty;
		List<RankInformation> ranks;
		
		public void measure_novelty()
		{
			int count = population.Count;
			nov.initialize(population);

			for (int i = 0; i< count; i++)
                    {
					  population[i].locality=0.0;
					  population[i].competition=0.0;
					}
					double max=0.0,min=100000000000.0;
					for (int i = 0; i< count; i++)
                    {
                    	double fit = nov.measureNovelty((NeatGenome.NeatGenome)population[i]);        

						population[i].objectives[population[i].objectives.Length-2] = fit+0.01;
                    	
                    	if(fit>max) max=fit;
                    	if(fit<min) min=fit;
                    				
					}
			Console.WriteLine("nov min: "+ min.ToString()+ " max:" + max.ToString());
		}
		
		//if genome x dominates y, increment y's dominated count, add y to x's dominated list
		public void update_domination(NeatGenome.NeatGenome x, NeatGenome.NeatGenome y,RankInformation r1,RankInformation r2)
		{
			if(dominates(x,y)) {
				r1.dominates.Add(r2);
				r2.domination_count++;
			}
		}
		
		//function to check whether genome x dominates genome y, usually defined as being no worse on all
		//objectives, and better at at least one
		public bool dominates(NeatGenome.NeatGenome x, NeatGenome.NeatGenome y) {
			bool better=false;
			double[] objx = x.objectives, objy = y.objectives;
			int sz = objx.Length;
			//if x is ever worse than y, it cannot dominate y
			//also check if x is better on at least one
			for(int i=0;i<sz-1;i++) {
				if(objx[i]<objy[i]) return false;
				if(objx[i]>objy[i]) better=true;
			}
			
			//genomic novelty check, disabled for now
			double thresh=0.1;
			if((objx[sz-1]+thresh)<(objy[sz-1])) return false;
			if((objx[sz-1]>(objy[sz-1]+thresh))) better=true;
			
			return better;
		}
		
		//distance function between two lists of objectives, used to see if two individuals are unique
		public double distance(double[] x, double[] y) {
			double delta=0.0;
			int len=x.Length;
			for(int i=0;i<len;i++) {
				double d=x[i]-y[i];
				delta+=d*d;
			}
			return delta;
		}
		
		public void printDistribution() {
		  String filename="dist"+generation+".txt";
		  string content="";
          
			XmlDocument archiveout = new XmlDocument();
            XmlPopulationWriter.WriteGenomeList(archiveout, population);
            archiveout.Save(filename);
 		
		  /*
		  for(int i=0;i<population.Count;i++) {
				for(int j=0;j<population[i].objectives.Length;j++)
		          content+=population[i].objectives[j]+" ";
				content+=population[i].Fitness+"\n";
		  }
		  System.IO.File.WriteAllText(filename,content);
		  */
			
		}
		
		//currently not used, calculates genomic novelty objective for protecting innovation
		//uses a rough characterization of topology, i.e. number of connections in the genome
		public void calculateGenomicNovelty() {
			double sum=0.0;
			int max_conn = 0;
		 foreach(IGenome x in population) {
				double minDist=10000000.0;

				NeatGenome.NeatGenome xx = (NeatGenome.NeatGenome)x;
		    	double difference=0.0;
				double delta=0.0;
				List<double> distances=new List<double>();
				if(xx.ConnectionGeneList.Count > max_conn)
					max_conn = xx.ConnectionGeneList.Count;
			//int ccount=xx.ConnectionGeneList.Count;
			foreach(IGenome y in population) {
					if(x==y)
						continue;
				NeatGenome.NeatGenome yy = (NeatGenome.NeatGenome)y;
				double d = xx.compat(yy,np);
				//if(d<minDist)
				//	minDist=d;
				distances.Add(d);
			}
			distances.Sort();
			int sz=Math.Min(distances.Count,10);
			double diversity=0.0;
			for(int i=0;i<sz;i++)
				diversity+=distances[i];
				xx.objectives[xx.objectives.Length-1] = diversity;
				sum+=diversity;
			}
			Console.WriteLine("Diversity: " + sum/population.Count + " " + max_conn);
		}
		
		//add an existing population from hypersharpNEAT to the multiobjective population maintained in
		//this class, step taken before evaluating multiobjective population through the rank function
		public void addPopulation(Population p) {
			for(int i=0;i<p.GenomeList.Count;i++)
		    {
		        bool blacklist=false;
				for(int j=0;j<population.Count;j++)
				{
				 if(distance(p.GenomeList[i].Behavior.objectives,population[j].objectives) < 0.001)
						blacklist=true;  //reject a genome if it is very similar to existing genomes in pop
				}
				if(!blacklist) { //add genome if it is unique
				//we might not need to make copies
				NeatGenome.NeatGenome copy=new NeatGenome.NeatGenome((NeatGenome.NeatGenome)p.GenomeList[i],0);
				copy.objectives = (double[])p.GenomeList[i].Behavior.objectives.Clone();
				population.Add(copy);    
				}	
				
			}
		}

		public void rankGenomes() {
		  int size = population.Count;
			
			calculateGenomicNovelty();
		 	if(doNovelty) {
				measure_novelty();
			}
			
		  //reset rank information
		  for(int i=0;i<size;i++) {
		    if(ranks.Count<(i+1))
				ranks.Add(new RankInformation());
			else
				ranks[i].reset();
		  }
			//calculate domination by testing each genome against every other genome
			for(int i=0;i<size;i++) {
				for(int j=0;j<size;j++) {
					update_domination((NeatGenome.NeatGenome)population[i],(NeatGenome.NeatGenome)population[j],ranks[i],ranks[j]);
				}
			}
			
			//successively peel off non-dominated fronts (e.g. those genomes no longer dominated by any in
			//the remaining population)
			List<int> front = new List<int>();
			int ranked_count=0;
			int current_rank=1;
			while(ranked_count < size) {
				//search for non-dominated front
				for(int i=0;i<size;i++)
				{
					//continue if already ranked
					if(ranks[i].ranked) continue;
					//if not dominated, add to front
					if(ranks[i].domination_count==0) {
						front.Add(i);
						ranks[i].ranked=true;
						ranks[i].rank=current_rank;
					}
				}
				
				int front_size = front.Count;
				Console.WriteLine("Front " + current_rank + " size: " + front_size);
				
				//now take all the non-dominated individuals, see who they dominated, and decrease
				//those genomes' domination counts, because we are removing this front from consideration
				//to find the next front of individuals non-dominated by the remaining individuals in
				//the population
				for(int i=0;i<front_size;i++) {
					RankInformation r = ranks[front[i]];
					foreach (RankInformation dominated in r.dominates) {
						dominated.domination_count--;
					}
				}
				
				ranked_count+=front_size;
				front.Clear();
				current_rank++;
			}
			
			//we save the last objective for potential use as genomic novelty objective
			int last_obj=population[0].objectives.Length-1;
			
			//fitness = popsize-rank (better way might be maxranks+1-rank), but doesn't matter
			//because speciation is not used and tournament selection is employed
			for(int i=0;i<size;i++) {
				population[i].Fitness = (size+1)-ranks[i].rank;//+population[i].objectives[last_obj]/100000.0;
			}
			
			population.Sort();
			generation++;
			if(generation%50==0)
			this.printDistribution();
		}
		
		
		//when we merge populations together, often the population will overflow, and we need to cut
		//it down. to do so, we just remove the last x individuals, which will be in the less significant
		//pareto fronts
		public GenomeList truncatePopulation(int size) {
			int to_remove=population.Count - size;
			Console.WriteLine("population size before: " + population.Count);
			Console.WriteLine("removing " + to_remove);
			if(to_remove>0)
	  			population.RemoveRange(size,to_remove);
						Console.WriteLine("population size after: " + population.Count);

			return population;
		}
		
		NeatParameters np;
		public Multiobjective (NeatParameters _np)
		{
			np=_np;
			population= new GenomeList();
			ranks=new List<RankInformation>();
			nov = new noveltyfixed(10.0);
			doNovelty=_np.noveltySearch;
			Console.WriteLine("multiobjective novelty " + doNovelty);
			generation=0;
		}
	}
}
