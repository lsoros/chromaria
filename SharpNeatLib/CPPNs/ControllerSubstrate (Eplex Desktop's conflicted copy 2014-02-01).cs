using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.NeatGenome;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class ControllerSubstrate : Substrate
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
        public ControllerSubstrate(uint input, uint output, uint hidden, IActivationFunction function, uint rgbSize=10, uint hX=5, uint hY=2) : base(input, output, hidden, function)
        {
            rgbOneDimension = rgbSize;
            colorArraySize = rgbOneDimension * rgbOneDimension;
            headingArraySize = hX*hY;
            headingX = hX;
            headingY = hY;

            neurons.Clear();

            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(biasCount + inputCount + outputCount + hiddenCount));

            // set up the bias nodes
            for (uint a = 0; a < biasCount; a++) {
                neurons.Add(new NeuronGene(a, NeuronType.Bias, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }

            // set up the input nodes
            for (uint a = 0; a < inputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }

            // set up the output nodes
            for (uint a = 0; a < outputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount, NeuronType.Output, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }

            // set up the hidden nodes
            for (uint a = 0; a < hiddenCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount + outputCount, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid")));
            }
        }

        public override NeatGenome.NeatGenome generateGenome(INetwork network)
        {
            float[] coordinates = new float[4];
            float outputR, outputG, outputB, outputHD, outputRGB, outputHDP;
            uint connectionCounter = 0;
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;
            ConnectionGeneList connections = new ConnectionGeneList();

            xDelta = 2.0f / (rgbOneDimension);
            yDelta = 2.0f / (rgbOneDimension);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;
            coordinates[2] = -1 + xDelta / 2.0f;
            coordinates[3] = -1 + yDelta / 2.0f;

            // Query the CPPN for the connections between the RGB input and hidden layers
            for (uint x = 0; x < rgbOneDimension; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1 + yDelta / 2.0f;
                for (uint y = 0; y < rgbOneDimension; y++, coordinates[1] += yDelta)
                {
                    // Reset the x2 coordinate and then loop through all possible values of x2
                    coordinates[2] = -1 + xDelta / 2.0f;
                    for (uint x2 = 0; x2 < rgbOneDimension; x2++, coordinates[2] += xDelta)
                    {
                        // Reset the y2 coordinate then loop through all possible values of y2
                        coordinates[3] = -1 + yDelta / 2.0f;
                        for (uint y2 = 0; y2 < rgbOneDimension; y2++, coordinates[3] += yDelta)
                        {   
                            // Set the CPPN inputs, activate the CPPN, and read the output signals
                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);
                            outputR = network.GetOutputSignal(0);
                            outputG = network.GetOutputSignal(1);
                            outputB = network.GetOutputSignal(2);

                            // Calculate the weight of the R->RGB connection based on the CPPN output
                            if (Math.Abs(outputR) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputR));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, x + rgbOneDimension * y, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("R Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                            // Calculate the weight of the G->RGB connection based on the CPPN output
                            if (Math.Abs(outputG) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputG) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputG));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("G Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                            // Calculate the weight of the B->RGB connection based on the CPPN output
                            if (Math.Abs(outputB) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputB) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputB));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + 2 * colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("B Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                        }
                    }
                }
            }

            xDelta = 2.0f / (headingX);
            yDelta = 2.0f / (headingY);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;
            coordinates[2] = -1 + xDelta / 2.0f;
            coordinates[3] = -1 + yDelta / 2.0f;

            // Query the CPPN for the connections between the H input and hidden layers
            for (uint x = 0; x < headingX; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1 + yDelta / 2.0f;
                for (uint y = 0; y < headingY; y++, coordinates[1] += yDelta)
                {
                    // Reset the x2 coordinate and then loop through all possible values of x2
                    coordinates[2] = -1 + xDelta / 2.0f;
                    for (uint x2 = 0; x2 < headingX; x2++, coordinates[2] += xDelta)
                    {
                        // Reset the y2 coordinate then loop through all possible values of y2
                        coordinates[3] = -1 + yDelta / 2.0f;
                        for (uint y2 = 0; y2 < headingY; y2++, coordinates[3] += yDelta)
                        {
                            // Set the CPPN inputs, activate the CPPN, and read the output signals
                            network.ClearSignals();
                            network.SetInputSignals(coordinates);
                            network.MultipleSteps(iterations);
                            outputHD = network.GetOutputSignal(3);

                            // Calculate the weight of the HD->HDP connection based on the CPPN output
                            if (Math.Abs(outputHD) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputHD) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHD));
                                connections.Add(new ConnectionGene(connectionCounter++, 3*colorArraySize + (x + headingX * y), (x2 + headingX * y2) + inputCount + outputCount + colorArraySize, weight));
                                //Console.WriteLine("HD Generated connection from " + (3 * colorArraySize + (x + headingX * y)) + " to " + ((x2 + headingX * y2) + inputCount + outputCount + colorArraySize));
                            }
                        }
                    }
                }
            }

            xDelta = 2.0f / (rgbOneDimension);
            yDelta = 2.0f / (rgbOneDimension);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;
            coordinates[2] = -1 + outputDelta / 2.0f;
            coordinates[3] = 0;


            // Query the CPPN for the connections between the RGB hidden and output layers
            for (uint x = 0; x < rgbOneDimension; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1 + yDelta / 2.0f;
                for (uint y = 0; y < rgbOneDimension; y++, coordinates[1] += yDelta)
                {
                    coordinates[2] = -1 + outputDelta / 2.0f;
                    for (uint outputs = 0; outputs < outputCount; outputs++, coordinates[2] += outputDelta)
                    {
                        // Set the CPPN inputs, activate the CPPN, and read the output signal
                        network.ClearSignals();
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);
                        outputRGB = network.GetOutputSignal(4);

                        // Calculate the weight of the RGB->O connection based on the CPPN output
                        if (Math.Abs(outputRGB) > threshold)
                        {
                            float weight = (float)(((Math.Abs(outputRGB) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputRGB));
                            connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, outputs + inputCount, weight));
                            //Console.WriteLine("INT Generated connection from " + ((x + rgbOneDimension * y) + inputCount + outputCount) + " to " + (outputs + inputCount));
                        }
                    }
                }
            }


            xDelta = 2.0f / (headingX);
            yDelta = 2.0f / (headingY);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;
            coordinates[2] = -1 + outputDelta / 2.0f;
            coordinates[3] = 0;

            // Query the CPPN for the connections between the HD hidden and output layers
            for (uint x = 0; x < headingX; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1 + yDelta / 2.0f;
                for (uint y = 0; y < headingY; y++, coordinates[1] += yDelta)
                {
                    coordinates[2] = -1 + outputDelta / 2.0f;
                    for (uint outputs = 0; outputs < outputCount; outputs++, coordinates[2] += outputDelta)
                    {
                        // Set the CPPN inputs, activate the CPPN, and read the output signal
                        network.ClearSignals();
                        coordinates[2] = -0.5f;
                        coordinates[3] = 0.5f;
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);
                        outputHDP = network.GetOutputSignal(5);

                        // Calculate the weight of the RGB->O connection based on the CPPN output
                        if (Math.Abs(outputHDP) > threshold)
                        {
                            float weight = (float)(((Math.Abs(outputHDP) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHDP));
                            connections.Add(new ConnectionGene(connectionCounter++, (x + headingX * y) + inputCount + outputCount + colorArraySize, 310, weight));
                            //Console.WriteLine("HDP Generated connection from " + ((x + headingX * y) + inputCount + outputCount + colorArraySize) + " to " + (outputs + inputCount));
                        }

                        // Set the CPPN inputs, activate the CPPN, and read the output signal
                        network.ClearSignals();
                        coordinates[2] = 0.0f;
                        coordinates[3] = 0.5f;
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);
                        outputHDP = network.GetOutputSignal(5);

                        // Calculate the weight of the RGB->O connection based on the CPPN output
                        if (Math.Abs(outputHDP) > threshold)
                        {
                            float weight = (float)(((Math.Abs(outputHDP) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHDP));
                            connections.Add(new ConnectionGene(connectionCounter++, (x + headingX * y) + inputCount + outputCount + colorArraySize, 311, weight));
                            //Console.WriteLine("HDP Generated connection from " + ((x + headingX * y) + inputCount + outputCount + colorArraySize) + " to " + (outputs + inputCount));
                        }

                        // Set the CPPN inputs, activate the CPPN, and read the output signal
                        network.ClearSignals();
                        coordinates[2] = 0.5f;
                        coordinates[3] = 0.5f;
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);
                        outputHDP = network.GetOutputSignal(5);

                        // Calculate the weight of the RGB->O connection based on the CPPN output
                        if (Math.Abs(outputHDP) > threshold)
                        {
                            float weight = (float)(((Math.Abs(outputHDP) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHDP));
                            connections.Add(new ConnectionGene(connectionCounter++, (x + headingX * y) + inputCount + outputCount + colorArraySize, 312, weight));
                            //Console.WriteLine("HDP Generated connection from " + ((x + headingX * y) + inputCount + outputCount + colorArraySize) + " to " + (outputs + inputCount));
                        }

                        // Set the CPPN inputs, activate the CPPN, and read the output signal
                        network.ClearSignals();
                        coordinates[2] = 0.0f;
                        coordinates[3] = -0.5f;
                        network.SetInputSignals(coordinates);
                        network.MultipleSteps(iterations);
                        outputHDP = network.GetOutputSignal(5);

                        // Calculate the weight of the RGB->O connection based on the CPPN output
                        if (Math.Abs(outputHDP) > threshold)
                        {
                            float weight = (float)(((Math.Abs(outputHDP) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHDP));
                            connections.Add(new ConnectionGene(connectionCounter++, (x + headingX * y) + inputCount + outputCount + colorArraySize, 313, weight));
                            //Console.WriteLine("HDP Generated connection from " + ((x + headingX * y) + inputCount + outputCount + colorArraySize) + " to " + (outputs + inputCount));
                        }
                    }
                }
            }
            
            // Return a genome combining the already-specified neurons with the CPPN-generated connections
            return new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)inputCount, (int)outputCount);
        }
    }
}
