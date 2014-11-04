using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.NeatGenome;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class PlanterSubstrate : Substrate
    {
        /// <summary>
        /// The number of neurons in ONE color array (R, G, or B, but not all three combined).
        /// </summary>
        public uint rgbOneDimension;
        public uint headingX;
        public uint headingY;
        public uint colorArraySize;

        /// <summary>
        /// The number of neurons in the entire heading array.
        /// </summary>
        public uint headingArraySize;

        public float xDelta, yDelta;

        // ControllerSubstrate constructor
        public PlanterSubstrate(uint input, uint output, uint hidden, IActivationFunction function, uint rgbSize = 10, uint hX = 5, uint hY = 2)
            : base(input, output, hidden, function)
        {
            rgbOneDimension = rgbSize;
            colorArraySize = rgbOneDimension * rgbOneDimension;
            headingArraySize = hX * hY;
            headingX = hX;
            headingY = hY;

            neurons.Clear();

            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(inputCount + outputCount + hiddenCount));

            // set up the bias nodes
            for (uint a = 0; a < biasCount; a++)
            {
                neurons.Add(new NeuronGene(a, NeuronType.Bias, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }

            // set up the input nodes
            for (uint a = 0; a < inputCount; a++)
            {
                neurons.Add(new NeuronGene(a + biasCount, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn")));
            }

            // set up the output nodes
            for (uint a = 0; a < outputCount; a++)
            {
                neurons.Add(new NeuronGene(a + biasCount + inputCount, NeuronType.Output, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }

            // set up the hidden nodes
            for (uint a = 0; a < hiddenCount; a++)
            {
                neurons.Add(new NeuronGene(a + biasCount + inputCount + outputCount, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }
        }

        public override NeatGenome.NeatGenome generateGenome(INetwork network=null)
        {
            uint connectionCounter = 0;
            ConnectionGeneList connections = new ConnectionGeneList();

            /*
            // Connect the R,G,B inputs to the RGB layer.
            // R should encourage planting, while G and B should inhibit it.
            for (uint x = 0; x < rgbOneDimension; x++)
            {
                for (uint y = 0; y < rgbOneDimension; y++)
                {
                    for (uint x2 = 0; x2 < rgbOneDimension; x2++)
                    {
                        for (uint y2 = 0; y2 < rgbOneDimension; y2++)
                        {
                            // R
                            connections.Add(new ConnectionGene(connectionCounter++, x + rgbOneDimension * y, (x2 + rgbOneDimension * y2) + inputCount + outputCount, 1.0));

                            // G
                            connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, -1.0));

                            // B
                            connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + 2 * colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, -1.0));
                        }
                    }
                }
            }

            // Connect the heading inputs to the heading integration layer with weight 1.0
            for (uint x = 0; x < headingX; x++)
            {
                for (uint y = 0; y < headingY; y++)
                {
                    for (uint x2 = 0; x2 < headingX; x2++)
                    {
                        for (uint y2 = 0; y2 < headingY; y2++)
                        {
                            connections.Add(new ConnectionGene(connectionCounter++, 3 * colorArraySize + (x + headingX * y), (x2 + headingX * y2) + inputCount + outputCount + colorArraySize, 1.0));
                        }
                    }
                }
            }

            // Add the connections between the RGB hidden and output layers (but only the planting output, #4)
            for (uint x = 0; x < rgbOneDimension; x++)
            {
                for (uint y = 0; y < rgbOneDimension; y++)
                {
                    connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, 313, 1.0));
                }
            }

            // Add the connections between the heading hidden and output layers (but only the straight output, #2)
            for (uint x = 0; x < headingX; x++)
            {
                for (uint y = 0; y < headingY; y++)
                {
                    connections.Add(new ConnectionGene(connectionCounter++, (x + headingX * y) + inputCount + outputCount + colorArraySize, 311, 1.0));
                }
            }
            */

            // Return a genome combining the already-specified neurons with the CPPN-generated connections
            return new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)inputCount, (int)outputCount);
        }
    }
}
