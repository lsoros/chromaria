using System;
using System.Threading;
using System.Collections.Generic;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Experiments;

namespace Chromaria.SharpNeatLib.Experiments
{
    /// <summary>
    /// An implementation of IPopulationEvaluator that evaluates all new genomes(EvaluationCount==0)
    /// within the population using multiple threads, using an INetworkEvaluator provided at construction time.
    /// 
    /// This class provides an IPopulationEvaluator for use within the EvolutionAlgorithm by simply
    /// providing an INetworkEvaluator to its constructor. This usage is intended for experiments
    /// where the genomes are evaluated independently of each other (e.g. not simultaneoulsy in 
    /// a simulated world) using a fixed evaluation function that can be described by an INetworkEvaluator.
    /// </summary>
    public class MultiThreadedPopulationEvaluator : IPopulationEvaluator
    {
        public INetworkEvaluator networkEvaluator;
        IActivationFunction activationFn;
        //private static Semaphore sem = new Semaphore(2, 2);
        private static Semaphore sem = new Semaphore(HyperNEATParameters.numThreads, HyperNEATParameters.numThreads);
        private static Semaphore sem2 = new Semaphore(1, 1);

        ulong evaluationCount = 0;

        #region Constructor

        public MultiThreadedPopulationEvaluator(INetworkEvaluator networkEvaluator, IActivationFunction activationFn)
        {
            this.networkEvaluator = networkEvaluator;
            this.activationFn = activationFn;

        }

        #endregion

        #region IPopulationEvaluator Members

        public void EvaluatePopulation(Population pop, EvolutionAlgorithm ea)
        {
            int count = pop.GenomeList.Count;
            evalPack e;
            IGenome g;
            int i;

            for (i = 0; i < count; i++)
            {
                //Console.WriteLine(i);
                sem.WaitOne();
                g = pop.GenomeList[i];
                e = new evalPack(networkEvaluator, activationFn, g, i % HyperNEATParameters.numThreads,(int)ea.Generation);
                ThreadPool.QueueUserWorkItem(new WaitCallback(evalNet), e);
                // Update master evaluation counter.
                evaluationCount++;
            }
            //Console.WriteLine("waiting for last threads..");
           for (int j = 0; j < HyperNEATParameters.numThreads; j++)
            {
           		sem.WaitOne();
              //  Console.WriteLine("waiting");
			}
            for (int j = 0; j < HyperNEATParameters.numThreads; j++)
            {
				//Console.WriteLine("releasing");
       
                sem.Release();
            }
            //Console.WriteLine("generation done...");
            //calulate novelty scores...
            if(ea.NeatParameters.noveltySearch)
            {
                if(ea.NeatParameters.noveltySearch)
                {
                    ea.CalculateNovelty();
                }
            }

        }



        public ulong EvaluationCount
        {
            get
            {
                return evaluationCount;
            }
        }

        public string EvaluatorStateMessage
        {
            get
            {	// Pass on the network evaluator's message.
                return networkEvaluator.EvaluatorStateMessage;
            }
        }

        public bool BestIsIntermediateChampion
        {
            get
            {	// Only relevant to incremental evolution experiments.
                return false;
            }
        }

        public bool SearchCompleted
        {
            get
            {	// This flag is not yet supported in the main search algorithm.
                return false;
            }
        }

        public static void evalNet(Object input)
        {

            evalPack e = (evalPack)input;

            if (e.g == null || (!HyperNEATParameters.reevaluateEveryGeneration && e.g.EvaluationCount != 0))
            {
                sem.Release();
                return;
            }
            sem2.WaitOne();
            INetwork network = e.g.Decode(e.Activation);
            sem2.Release();
            if (network == null)
            {	// Future genomes may not decode - handle the possibility.
                e.g.Fitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;
                e.g.RealFitness = e.g.Fitness;
            }
            else
            {
                BehaviorType behavior;
                e.g.Fitness = Math.Max(e.NetworkEvaluator.threadSafeEvaluateNetwork(network,sem2,out behavior,e.ThreadNumber), EvolutionAlgorithm.MIN_GENOME_FITNESS);
                e.g.Behavior = behavior;
                e.g.RealFitness = e.g.Fitness;
            }

            // Reset these genome level statistics.
            e.g.TotalFitness += e.g.Fitness;
            e.g.EvaluationCount += 1;
            sem.Release();
        }

        #endregion
    }

    class evalPack
    {
        INetworkEvaluator networkEvaluator;
        IActivationFunction activationFn;
        IGenome genome;
        int threadnumber;
		int generation;
        public evalPack(INetworkEvaluator n, IActivationFunction a, IGenome g,int t,int gen)
        {

            networkEvaluator = n;
            activationFn = a;
            genome = g;
            threadnumber = t;
			generation=gen;
        }
		public int Generation
		{
			get
			{
				return generation;
			}
		}
        public int ThreadNumber
        {
            get
            {
                return threadnumber;
            }
        }
        public INetworkEvaluator NetworkEvaluator
        {
            get
            {
                return networkEvaluator;
            }
        }

        public IActivationFunction Activation
        {
            get
            {
                return activationFn;
            }
        }

        public IGenome g
        {
            get
            {
                return genome;
            }
        }

    }
}
