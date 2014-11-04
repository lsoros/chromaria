using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.Experiments
{
    class ChromariaPopulationEvaluator : SingleFilePopulationEvaluator
    {
        public ChromariaPopulationEvaluator(INetworkEvaluator evaluator)
        {
            networkEvaluator = evaluator;
        }

        public override void EvaluatePopulation(Population pop, EvolutionAlgorithm ea)
        {
            // Evaluate in single-file each genome within the population. 
			// Only evaluate new genomes (those with EvaluationCount==0).
			int count = pop.GenomeList.Count;
            for (int i = 0; i < count; i++)
            {
                // Grab the next individual out of the population
                IGenome g = pop.GenomeList[i];
                if (g.EvaluationCount != 0)
                    continue;

                // The current genome is a CPPN genome, not a network genome
                // So, decode the CPPN genome into a CPPN, use the CPPN to generate an ANN,
                // then run the networkEvaluator on the ANN
                INetwork cppn = g.Decode(activationFn);
                if (cppn == null)
                {	// Future genomes may not decode - handle the possibility.
                    g.Fitness = EvolutionAlgorithm.MIN_GENOME_FITNESS;
                }
                else
                {
                    //BehaviorType behavior;
                    //INetwork network = Chromaria.Simulator.controllerSubstrate.generateGenome(cppn).Decode(activationFn);
                    g.Fitness = Math.Max(0.0f, EvolutionAlgorithm.MIN_GENOME_FITNESS);
                    //if (Chromaria.Simulator.plantingWasValid)
                        //Chromaria.Simulator.successfulPlanterGenome = g;
                    g.RealFitness = g.Fitness;
                }

                // Reset these genome level statistics.
                g.TotalFitness = g.Fitness;
                g.EvaluationCount = 1;

                // Update master evaluation counter.
                evaluationCount++;

                // Close the XML tag for this individual
                //Chromaria.Simulator.xmlWriter.WriteEndElement();
            }
            if (ea.NeatParameters.noveltySearch)
            {
                if (ea.NeatParameters.noveltySearch && ea.noveltyInitialized)
                {
                    ea.CalculateNovelty();
                }
            }
        }
    }
}
