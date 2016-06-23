using System;
using System.Collections;

using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Maths;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class GenomeFactory
	{
		/// <summary>
		/// Create a default minimal genome that describes a NN with the given number of inputs and outputs.
		/// </summary>
		/// <returns></returns>
		public static IGenome CreateGenome(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion)
		{
            IActivationFunction actFunct;
			NeuronGene neuronGene; // temp variable.
			NeuronGeneList inputNeuronGeneList = new NeuronGeneList(); // includes bias neuron.
			NeuronGeneList outputNeuronGeneList = new NeuronGeneList();
			NeuronGeneList neuronGeneList = new NeuronGeneList();
			ConnectionGeneList connectionGeneList = new ConnectionGeneList();

			// IMPORTANT NOTE: The neurons must all be created prior to any connections. That way all of the genomes
			// will obtain the same innovation ID's for the bias,input and output nodes in the initial population.
			// Create a single bias neuron.
            //TODO: DAVID proper activation function change to NULL?
            actFunct = ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid");
            neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Bias, actFunct);
			inputNeuronGeneList.Add(neuronGene);
			neuronGeneList.Add(neuronGene);

			// Create input neuron genes.
            //actFunct = ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
			for(int i=0; i<inputNeuronCount; i++)
			{
                //TODO: DAVID proper activation function change to NULL?
                neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Input, actFunct);
				inputNeuronGeneList.Add(neuronGene);
				neuronGeneList.Add(neuronGene);
			}

			// Create output neuron genes. 
            //actFunct = ActivationFunctionFactory.GetActivationFunction("NullFn");
			for(int i=0; i<outputNeuronCount; i++)
			{
                //actFunct = ActivationFunctionFactory.GetRandomActivationFunction(neatParameters);
                neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Output, actFunct);
                //TODO: DAVID proper activation function
                //neuronGene = new NeuronGene(idGenerator.NextInnovationId, NeuronType.Output, actFunct);
				outputNeuronGeneList.Add(neuronGene);
				neuronGeneList.Add(neuronGene);
			}

			// Loop over all possible connections from input to output nodes and create a number of connections based upon
			// connectionProportion.
			foreach(NeuronGene targetNeuronGene in outputNeuronGeneList)
			{
				foreach(NeuronGene sourceNeuronGene in inputNeuronGeneList)
				{
					// Always generate an ID even if we aren't going to use it. This is necessary to ensure connections
					// between the same neurons always have the same ID throughout the generated population.
					uint connectionInnovationId = idGenerator.NextInnovationId;

					if(Utilities.NextDouble() < connectionProportion)
					{	// Ok lets create a connection.
						connectionGeneList.Add(	new ConnectionGene(connectionInnovationId, 
							sourceNeuronGene.InnovationId,
							targetNeuronGene.InnovationId,
							(Utilities.NextDouble() * neatParameters.connectionWeightRange ) - neatParameters.connectionWeightRange/2.0));  // Weight 0 +-5
					}
				}
			}

			// Don't create any hidden nodes at this point. Fundamental to the NEAT way is to start minimally!
			return new NeatGenome(idGenerator.NextGenomeId, neuronGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
		}

		/// <summary>
		/// Construct a GenomeList. This can be used to construct a new Population object.
		/// </summary>
		/// <param name="evolutionAlgorithm"></param>
		/// <param name="inputNeuronCount"></param>
		/// <param name="outputNeuronCount"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		public GenomeList CreateGenomeList(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion, int length)
		{
			GenomeList genomeList = new GenomeList();
			
			for(int i=0; i<length; i++)
			{
				idGenerator.ResetNextInnovationNumber();

                IGenome gen = CreateGenome(neatParameters, idGenerator, inputNeuronCount, outputNeuronCount, connectionProportion); 
				genomeList.Add(gen);
			}

			return genomeList;
		}

		public static GenomeList CreateGenomeList(NeatGenome seedGenome, int length, NeatParameters neatParameters, IdGenerator idGenerator)
		{
			//Build the list.
			GenomeList genomeList = new GenomeList();
			
			// Use the seed directly just once.
			NeatGenome newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
			genomeList.Add(newGenome);

			// For the remainder we alter the weights.
			for(int i=1; i<length; i++)
			{
				newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
				
				// Reset the connection weights
				foreach(ConnectionGene connectionGene in newGenome.ConnectionGeneList)
					connectionGene.Weight += (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0;
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId,5,newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count-7)+7].InnovationId ,(Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0));
                //newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 6, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
				genomeList.Add(newGenome);
			}

            //

			return genomeList;
		}

        public static GenomeList CreateGenomeListAddedInputs(NeatGenome seedGenome, int length, NeatParameters neatParameters, IdGenerator idGenerator)
        {
            //Build the list.
            GenomeList genomeList = new GenomeList();

            // Use the seed directly just once.
            NeatGenome newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);
            //genomeList.Add(newGenome);

            // For the remainder we alter the weights.
            for (int i = 0; i < length; i++)
            {
                newGenome = new NeatGenome(seedGenome, idGenerator.NextGenomeId);

                // Reset the connection weights
                foreach (ConnectionGene connectionGene in newGenome.ConnectionGeneList)
                    connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0;
                newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 5, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
                newGenome.ConnectionGeneList.Add(new ConnectionGene(idGenerator.NextInnovationId, 6, newGenome.NeuronGeneList[Utilities.Next(newGenome.NeuronGeneList.Count - 7) + 7].InnovationId, (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange / 2.0));
                genomeList.Add(newGenome);
            }

            //

            return genomeList;
        }


		public static GenomeList CreateGenomeList(Population seedPopulation, int length, NeatParameters neatParameters, IdGenerator idGenerator)
		{
			//Build the list.
			GenomeList genomeList = new GenomeList();
			int seedIdx=0;
			
			for(int i=0; i<length; i++)
			{
				NeatGenome newGenome = new NeatGenome((NeatGenome)seedPopulation.GenomeList[seedIdx], idGenerator.NextGenomeId);

				// Reset the connection weights
				foreach(ConnectionGene connectionGene in newGenome.ConnectionGeneList)
					connectionGene.Weight = (Utilities.NextDouble() * neatParameters.connectionWeightRange) - neatParameters.connectionWeightRange/2.0;

				genomeList.Add(newGenome);

				if(++seedIdx >= seedPopulation.GenomeList.Count)
				{	// Back to first genome.
					seedIdx=0;
				}
			}
			return genomeList;
		}
	}
}
