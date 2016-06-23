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
        public uint colorArraySize;

        /// <summary>
        /// The number of neurons in the entire heading array.
        /// </summary>
        public uint headingArraySize;

        public float xDelta, yDelta;

        // ControllerSubstrate constructor
        public ControllerSubstrate(uint input, uint output, uint hidden, IActivationFunction function, uint rgbSize=10) : base(input, output, hidden, function)
        {
            rgbOneDimension = rgbSize;
            colorArraySize = rgbOneDimension * rgbOneDimension;
            headingArraySize = 8;

            neurons.Clear();

            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(inputCount + outputCount + hiddenCount));

            // set up the bias nodes
            for (uint a = 0; a < biasCount; a++) {
                neurons.Add(new NeuronGene(a, NeuronType.Bias, ActivationFunctionFactory.GetActivationFunction("Linear"),0.0f));
            }

            // set up the input nodes
            for (uint a = 0; a < inputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("Linear"),0.0f));
            }

            // set up the output nodes
            for (uint a = 0; a < outputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount, NeuronType.Output, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"),1.0f));
            }

            // set up the hidden nodes
            for (uint a = 0; a < hiddenCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount + outputCount, NeuronType.Hidden, ActivationFunctionFactory.GetActivationFunction("BipolarSigmoid"),0.5f));
            }
        }

        public override NeatGenome.NeatGenome generateGenome(INetwork network)
        {
            float[] coordinates = new float[4];
            float outputR_RGB, outputG_RGB, outputB_RGB, outputHeading_HiddenH, outputRGB_DIR, outputRGB_P, outputHiddenH_DIR, outputHiddenH_P;
            uint connectionCounter = 0;
            int iterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;
            ConnectionGeneList connections = new ConnectionGeneList();

            // Query the CPPN for the connections between the RGB input and hidden layers
            xDelta = 2.0f / (rgbOneDimension);
            yDelta = 2.0f / (rgbOneDimension);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;
            coordinates[2] = -1 + xDelta / 2.0f;
            coordinates[3] = -1 + yDelta / 2.0f;

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
                            outputR_RGB = network.GetOutputSignal(0);
                            outputG_RGB = network.GetOutputSignal(1);
                            outputB_RGB = network.GetOutputSignal(2);

                            // Calculate the weight of the R->RGB connection based on the CPPN output
                            if (Math.Abs(outputR_RGB) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputR_RGB) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputR_RGB));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, x + rgbOneDimension * y, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("R Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                            // Calculate the weight of the G->RGB connection based on the CPPN output
                            if (Math.Abs(outputG_RGB) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputG_RGB) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputG_RGB));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("G Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                            // Calculate the weight of the B->RGB connection based on the CPPN output
                            if (Math.Abs(outputB_RGB) > threshold)
                            {
                                float weight = (float)(((Math.Abs(outputB_RGB) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputB_RGB));
                                //float weight = 0.0f;
                                connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + 2 * colorArraySize, (x2 + rgbOneDimension * y2) + inputCount + outputCount, weight));
                                //Console.WriteLine("B Generated connection from " + (x + rgbOneDimension * y) + " to " + ((x2 + rgbOneDimension * y2) + inputCount + outputCount));
                            }

                        }
                    }
                }
            }

            // Query the CPPN for the connections between the H input and hidden layers
            
            /*
            uint headingX = 3;
            uint headingY = 3;
            xDelta = 1.0f;
            yDelta = 1.0f;

            coordinates[0] = -1.0f;
            coordinates[1] = -1.0f;
            coordinates[2] = -1.0f;
            coordinates[3] = -1.0f;

            for (uint x = 0; x < headingX; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1.0f;
                for (uint y = 0; y < headingY; y++, coordinates[1] += yDelta)
                {
                    // Reset the x2 coordinate and then loop through all possible values of x2
                    coordinates[2] = -1.0f;
                    for (uint x2 = 0; x2 < headingX; x2++, coordinates[2] += xDelta)
                    {
                        // Reset the y2 coordinate then loop through all possible values of y2
                        coordinates[3] = -1.0f;
                        for (uint y2 = 0; y2 < headingY; y2++, coordinates[3] += yDelta)
                        {
                            // Don't query for (0,0) - this substrate does not have a node in the center of the heading plane
                            if (!((coordinates[0] == 0.0f && coordinates[1] == 0.0f) || (coordinates[2] == 0.0f && coordinates[3] == 0.0f)))
                            {
                                // Set the CPPN inputs, activate the CPPN, and read the output signals
                                network.ClearSignals();
                                network.SetInputSignals(coordinates);
                                network.MultipleSteps(iterations);
                                outputHeading_HiddenH = network.GetOutputSignal(3);

                                // Calculate the weight of the HD->HDP connection based on the CPPN output
                                if (Math.Abs(outputHeading_HiddenH) > threshold)
                                {
                                    float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                                    connections.Add(new ConnectionGene(connectionCounter++, 3 * colorArraySize + (x + headingX * y), (x2 + headingX * y2) + inputCount + outputCount + colorArraySize, weight));
                                    //Console.WriteLine("HD Generated connection from " + (3 * colorArraySize + (x + headingX * y)) + " to " + ((x2 + headingX * y2) + inputCount + outputCount + colorArraySize));
                                }
                            }
                        }
                    }
                }
            }*/

            
            xDelta = 1.0f;
            uint headingX = 3;

            coordinates[0] = -1.0f;
            coordinates[1] = -1.0f;

            // Determine all connections from H row 1
            for (uint src = 300; src < 303; src++, coordinates[0] += xDelta)
            {
                // Reset the x2 coordinate and then loop through all possible values of x2
                coordinates[2] = -1.0f;
                for (uint tgt = 412; tgt < 415; tgt++, coordinates[2] += xDelta)
                {
                    // Query the CPPN for the connections between H row 1 and HDP row 1
                    coordinates[3] = -1.0f;

                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }

                    // Query the CPPN for the connections between H row 1 and HDP row 3
                    coordinates[3] = 1.0f;

                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt+5, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + (tgt + 5) + " with weight " + weight);
                    }
                }
            }

            coordinates[0] = -1.0f;
            coordinates[1] = 1.0f;

            // Determine all connections from H row 3
            for (uint src = 305; src < 308; src++, coordinates[0] += xDelta)
            {
                // Reset the x2 coordinate and then loop through all possible values of x2
                coordinates[2] = -1.0f;
                for (uint x2 = 0; x2 < headingX; x2++, coordinates[2] += xDelta)
                {
                    // Query the CPPN for the connections between H row 3 and HDP row 1
                    coordinates[3] = -1.0f;

                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, 412 + x2, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + (412 + x2) + " with weight " + weight);
                    }

                    // Query the CPPN for the connections between H row 3 and HDP row 3
                    coordinates[3] = 1.0f;

                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, 417 + x2, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + (417 + x2) + " with weight " + weight);
                    }
                }
            }

            // Determine the connections for the H and HDP middle rows

            // Query the CPPN for the connections between H row 1 and HDP row 2
            coordinates[0] = -1.0f;
            coordinates[1] = -1.0f;
            coordinates[3] = 0.0f;
            for (uint src = 300; src < 303; src++, coordinates[0] += xDelta)
            {
                coordinates[2] = -1.0f;
                for (uint tgt = 415; tgt < 417; tgt++, coordinates[2] += 2.0f)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }
                }
            }

            // Query the CPPN for the connections between H row 2 and HDP row 2
            coordinates[0] = -1.0f;
            coordinates[1] = 0.0f;
            coordinates[3] = 0.0f;

            for (uint src = 303; src < 305; src++, coordinates[0] += 2.0f)
            {
                coordinates[2] = -1.0f;
                for (uint tgt = 415; tgt < 417; tgt++, coordinates[2] += 2.0f)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }
                }
            }

            
            // Query the CPPN for the connections between H row 3 and HDP row 2
            coordinates[0] = -1.0f;
            coordinates[1] = 1.0f;
            coordinates[3] = 0.0f;
            for (uint src = 305; src < 308; src++, coordinates[0] += xDelta)
            {
                coordinates[2] = -1.0f;
                for (uint tgt = 415; tgt < 417; tgt++, coordinates[2] += 2.0f)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }
                }
            }

            // Query the CPPN for the connections between H row 2 and HDP row 1
            coordinates[1] = 0.0f;
            coordinates[2] = -1.0f;
            for (uint src = 303; src < 305; src++, coordinates[2] += 2.0f)
            {
                coordinates[0] = -1.0f;
                coordinates[3] = -1.0f;
                for (uint tgt = 412; tgt < 415; tgt++, coordinates[0] += xDelta)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }
                }

                // Query the CPPN for the connections between H row 2 and HDP row 3
                coordinates[0] = -1.0f;
                coordinates[3] = 1.0f;
                for (uint tgt = 417; tgt < 420; tgt++, coordinates[0] += xDelta)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signals
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHeading_HiddenH = network.GetOutputSignal(3);

                    // Calculate the weight of the HD->HDP connection based on the CPPN output
                    if (Math.Abs(outputHeading_HiddenH) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHeading_HiddenH) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHeading_HiddenH));
                        connections.Add(new ConnectionGene(connectionCounter++, src, tgt, weight));
                        //Console.WriteLine("Generated connection from " + src + " to " + tgt + " with weight " + weight);
                    }
                }
            }

            // Query the CPPN for the connections between the RGB hidden layer and movement output nodes
            xDelta = 2.0f / (rgbOneDimension);
            yDelta = 2.0f / (rgbOneDimension);

            coordinates[0] = -1 + xDelta / 2.0f;
            coordinates[1] = -1 + yDelta / 2.0f;

            for (uint x = 0; x < rgbOneDimension; x++, coordinates[0] += xDelta)
            {
                // Reset the y1 coordinate and then loop through all possible values of y1
                coordinates[1] = -1 + yDelta / 2.0f;
                for (uint y = 0; y < rgbOneDimension; y++, coordinates[1] += yDelta)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = -1.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputRGB_DIR = network.GetOutputSignal(4);

                    // Calculate the weight of the RGB->L connection based on the CPPN output
                    if (Math.Abs(outputRGB_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputRGB_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputRGB_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, 308, weight));
                        //Console.WriteLine("RGB Generated connection from " + ((x + rgbOneDimension * y) + inputCount + outputCount) + " to 310");
                    }

                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = 0.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputRGB_DIR = network.GetOutputSignal(4);
                    outputRGB_P = network.GetOutputSignal(5);

                    // Calculate the weight of the RGB->S connection based on the CPPN output
                    if (Math.Abs(outputRGB_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputRGB_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputRGB_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, 309, weight));
                        //Console.WriteLine("RGB Generated connection from " + ((x + rgbOneDimension * y) + inputCount + outputCount) + " to 311");
                    }

                    // Calculate the weight of the RGB->P connection based on the CPPN output
                    if (Math.Abs(outputRGB_P) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputRGB_P) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputRGB_P));
                        connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, 311, weight));
                        //Console.WriteLine("RGB Generated connection from " + ((x + rgbOneDimension * y) + inputCount + outputCount) + " to 313");
                    }

                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = 1.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputRGB_DIR = network.GetOutputSignal(4);

                    // Calculate the weight of the RGB->R connection based on the CPPN output
                    if (Math.Abs(outputRGB_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputRGB_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputRGB_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, (x + rgbOneDimension * y) + inputCount + outputCount, 310, weight));
                        //Console.WriteLine("RGB Generated connection from " + ((x + rgbOneDimension * y) + inputCount + outputCount) + " to 312");
                    }
                }
            }

            // Query the CPPN for the connections between the Heading hidden and output layers
            xDelta = 1.0f;
            yDelta = 1.0f;

            coordinates[0] = -1.0f;

            // Determine the connections for heading hidden rows 1 and 3
            for (uint src = 412; src < 415; src++, coordinates[0] += xDelta)
            {
                uint srcOffset = 0;
                for (coordinates[1] = -1.0f; coordinates[1] < 3.0f; coordinates[1] += 2.0f)
                {
                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = -1.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHiddenH_DIR = network.GetOutputSignal(6);

                    // Calculate the weight of the RGB->O connection based on the CPPN output
                    if (Math.Abs(outputHiddenH_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, src+srcOffset, 308, weight));
                        //Console.WriteLine("Generated connection from " + (src+srcOffset) + " to 308 with weight " + weight);
                    }

                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = 0.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHiddenH_DIR = network.GetOutputSignal(6);
                    outputHiddenH_P = network.GetOutputSignal(7);

                    // Calculate the weight of the RGB->O connection based on the CPPN output
                    if (Math.Abs(outputHiddenH_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, src + srcOffset, 309, weight));
                        //Console.WriteLine("Generated connection from " + (src + srcOffset) + " to 309 with weight " + weight);
                    }

                    // Calculate the weight of the RGB->O connection based on the CPPN output
                    if (Math.Abs(outputHiddenH_P) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHiddenH_P) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_P));
                        connections.Add(new ConnectionGene(connectionCounter++, src + srcOffset, 311, weight));
                        //Console.WriteLine("Generated connection from " + (src + srcOffset) + " to 311 with weight " + weight);
                    }

                    // Set the CPPN inputs, activate the CPPN, and read the output signal
                    network.ClearSignals();
                    coordinates[2] = 1.0f;
                    coordinates[3] = 0.0f;
                    network.SetInputSignals(coordinates);
                    network.MultipleSteps(iterations);
                    outputHiddenH_DIR = network.GetOutputSignal(6);

                    // Calculate the weight of the RGB->O connection based on the CPPN output
                    if (Math.Abs(outputHiddenH_DIR) > threshold)
                    {
                        float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                        connections.Add(new ConnectionGene(connectionCounter++, src + srcOffset, 310, weight));
                        //Console.WriteLine("Generated connection from " + (src + srcOffset) + " to 310 with weight " + weight);
                    }

                    srcOffset+=5;
                }
            }

            coordinates[0] = -1.0f;
            coordinates[1] = 0.0f;

            // Determine the connections for heading hidden row 2
            for (uint src = 415; src < 417; src++, coordinates[0] += 2.0f)
            {
                // Set the CPPN inputs, activate the CPPN, and read the output signal
                network.ClearSignals();
                coordinates[2] = -1.0f;
                coordinates[3] = 0.0f;
                network.SetInputSignals(coordinates);
                network.MultipleSteps(iterations);
                outputHiddenH_DIR = network.GetOutputSignal(6);

                // Calculate the weight of the RGB->O connection based on the CPPN output
                if (Math.Abs(outputHiddenH_DIR) > threshold)
                {
                    float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                    connections.Add(new ConnectionGene(connectionCounter++, src, 308, weight));
                    //Console.WriteLine("Generated connection from " + src + " to 308 with weight " + weight);
                }

                // Set the CPPN inputs, activate the CPPN, and read the output signal
                network.ClearSignals();
                coordinates[2] = 0.0f;
                coordinates[3] = 0.0f;
                network.SetInputSignals(coordinates);
                network.MultipleSteps(iterations);
                outputHiddenH_DIR = network.GetOutputSignal(6);
                outputHiddenH_P = network.GetOutputSignal(7);

                // Calculate the weight of the RGB->O connection based on the CPPN output
                if (Math.Abs(outputHiddenH_DIR) > threshold)
                {
                    float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                    connections.Add(new ConnectionGene(connectionCounter++, src, 309, weight));
                    //Console.WriteLine("Generated connection from " + src + " to 309 with weight " + weight);
                }

                // Calculate the weight of the RGB->O connection based on the CPPN output
                if (Math.Abs(outputHiddenH_P) > threshold)
                {
                    float weight = (float)(((Math.Abs(outputHiddenH_P) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_P));
                    connections.Add(new ConnectionGene(connectionCounter++, src, 311, weight));
                    //Console.WriteLine("Generated connection from " + src + " to 311 with weight " + weight);
                }

                // Set the CPPN inputs, activate the CPPN, and read the output signal
                network.ClearSignals();
                coordinates[2] = 1.0f;
                coordinates[3] = 0.0f;
                network.SetInputSignals(coordinates);
                network.MultipleSteps(iterations);
                outputHiddenH_DIR = network.GetOutputSignal(6);

                // Calculate the weight of the RGB->O connection based on the CPPN output
                if (Math.Abs(outputHiddenH_DIR) > threshold)
                {
                    float weight = (float)(((Math.Abs(outputHiddenH_DIR) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(outputHiddenH_DIR));
                    connections.Add(new ConnectionGene(connectionCounter++, src, 310, weight));
                    //Console.WriteLine("Generated connection from " + src + " to 310 with weight " + weight);
                }
            }
            
            
            // Return a genome combining the already-specified neurons with the CPPN-generated connections
            return new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)inputCount, (int)outputCount);
        }
    }
}
