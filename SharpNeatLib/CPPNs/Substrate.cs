using System;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class Substrate
    {

        protected uint biasCount;
        protected uint inputCount;
        protected uint outputCount;
        protected uint hiddenCount;
        protected IActivationFunction activationFunction;

        public double weightRange;
        public double threshold;

        protected float inputDelta;
        protected float hiddenDelta;
        protected float outputDelta;

        protected NeuronGeneList neurons;

        public Substrate(uint inputCount, uint outputCount, uint hiddenCount, IActivationFunction function)
            : this(0 /*biasCount*/, inputCount, outputCount, hiddenCount, function) { }

        public Substrate(uint biasCount, uint inputCount, uint outputCount, uint hiddenCount, IActivationFunction function)
        {
            this.biasCount = biasCount;
            this.inputCount = inputCount;
            this.outputCount = outputCount;
            this.hiddenCount = hiddenCount;
            this.activationFunction = function;

            weightRange = HyperNEATParameters.weightRange;
            threshold = HyperNEATParameters.threshold;

            if (inputCount != 0)
                inputDelta = 2.0f / (inputCount);
            if (hiddenCount != 0)
                hiddenDelta = 2.0f / (hiddenCount);
            if (outputCount != 0)
                outputDelta = 2.0f / (outputCount);

            // SharpNEAT requires that the neuron list be in this order: bias|input|output|hidden
            neurons = new NeuronGeneList((int)(inputCount + outputCount + hiddenCount));

            // set up the bias nodes
            for (uint a = 0; a < biasCount; a++) {
                neurons.Add(new NeuronGene(a, NeuronType.Bias, ActivationFunctionFactory.GetActivationFunction("NullFn"), 0.0f));
            }

            // set up the input nodes
            for (uint a = 0; a < inputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount, NeuronType.Input, ActivationFunctionFactory.GetActivationFunction("NullFn"), 0.0f));
            }

            // set up the output nodes
            for (uint a = 0; a < outputCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount, NeuronType.Output, activationFunction, 1.0f));
            }

            // set up the hidden nodes
            for (uint a = 0; a < hiddenCount; a++) {
                neurons.Add(new NeuronGene(a + biasCount + inputCount + outputCount, NeuronType.Hidden, activationFunction, 0.5f));
            }
        }

        public INetwork generateNetwork(INetwork CPPN)
        {
            return generateGenome(CPPN).Decode(null);
        }

        public virtual NeatGenome.NeatGenome generateGenome(INetwork network)
        {
            int maxIterations = 2 * (network.TotalNeuronCount - (network.InputNeuronCount + network.OutputNeuronCount)) + 1;
            double epsilon = 0.0;

            uint firstBias = 0;
            uint lastBias = biasCount;
            uint firstInput = biasCount;
            uint lastInput = biasCount + inputCount;
            uint firstOutput = biasCount + inputCount;
            uint lastOutput = biasCount + inputCount + outputCount;
            uint firstHidden = biasCount + inputCount + outputCount;
            uint lastHidden = biasCount + inputCount + outputCount + hiddenCount;

            float[] coordinates = new float[4];
            float output;
            uint connectionCounter = 0;
            ConnectionGeneList connections = new ConnectionGeneList();

            // give bias inputs to all hidden and output nodes.
            // the source of the the link is located at (0,0), the target is each node, and the weight of the link is the second output of CPPN.
            coordinates[0] = 0;
            coordinates[1] = 0;
            for (uint bias = firstBias; bias < lastBias; bias++) {
                // link the bias to all hidden nodes.
                coordinates[2] = -1 + hiddenDelta / 2.0f;
                coordinates[3] = 0;
                for (uint hidden = firstHidden; hidden < lastHidden; hidden++) {
                    coordinates[2] += hiddenDelta;
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.RelaxNetwork(maxIterations, epsilon);
                    output = network.GetOutputSignal(1);

                    if (Math.Abs(output) > threshold) {
                        float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                        connections.Add(new ConnectionGene(connectionCounter++, bias, hidden, weight));
                    }
                }

                // link the bias to all output nodes.
                coordinates[2] = -1 + outputDelta / 2.0f;
                coordinates[3] = 1;
                for (uint outp = firstOutput; outp < lastOutput; outp++) {
                    coordinates[2] += outputDelta;
                    network.ClearSignals();
                    network.SetInputSignals(coordinates);
                    network.RelaxNetwork(maxIterations, epsilon);
                    output = network.GetOutputSignal(1);

                    if (Math.Abs(output) > threshold) {
                        float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                        connections.Add(new ConnectionGene(connectionCounter++, bias, outp, weight));
                    }
                }
            }

            if (hiddenCount > 0) {
                // link all input nodes to all hidden nodes.
                coordinates[0] = -1 + inputDelta / 2.0f;
                coordinates[1] = -1;
                coordinates[2] = -1 + hiddenDelta / 2.0f;
                coordinates[3] = 0;
                for (uint input = firstInput; input < lastInput; input++) {
                    coordinates[0] += inputDelta;
                    coordinates[2] = -1 + hiddenDelta / 2.0f;
                    for (uint hidden = firstHidden; hidden < lastHidden; hidden++) {
                        coordinates[2] += hiddenDelta;
                        network.ClearSignals();
                        network.SetInputSignals(coordinates);
                        network.RelaxNetwork(maxIterations, epsilon);
                        output = network.GetOutputSignal(0);

                        if (Math.Abs(output) > threshold) {
                            float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                            connections.Add(new ConnectionGene(connectionCounter++, input, hidden, weight));
                        }
                    }
                }

                // link all hidden nodes to all output nodes.
                coordinates[0] = -1 + hiddenDelta / 2.0f;
                coordinates[1] = 0;
                coordinates[2] = -1 + outputDelta / 2.0f;
                coordinates[3] = 1;
                for (uint hidden = firstHidden; hidden < lastHidden; hidden++) {
                    coordinates[0] += hiddenDelta;
                    coordinates[2] = -1 + outputDelta / 2.0f;
                    for (uint outp = firstOutput; outp < lastOutput; outp++) {
                        coordinates[2] += outputDelta;
                        network.ClearSignals();
                        network.SetInputSignals(coordinates);
                        network.RelaxNetwork(maxIterations, epsilon);
                        output = network.GetOutputSignal(0);

                        if (Math.Abs(output) > threshold) {
                            float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                            connections.Add(new ConnectionGene(connectionCounter++, hidden, outp, weight));
                        }
                    }
                }

            } else { // there are no hidden nodes, so connect each input to each output only.
                // link all input nodes to all output nodes.
                coordinates[0] = -1 + inputDelta / 2.0f;
                coordinates[1] = -1;
                coordinates[2] = -1 + outputDelta / 2.0f;
                coordinates[3] = 1;
                for (uint input = firstInput; input < lastInput; input++) {
                    coordinates[0] += inputDelta;
                    coordinates[2] = -1 + outputDelta / 2.0f;
                    for (uint outp = firstOutput; outp < lastOutput; output++) {
                        coordinates[2] += outputDelta;
                        network.ClearSignals();
                        network.SetInputSignals(coordinates);
                        network.RelaxNetwork(maxIterations, epsilon);
                        output = network.GetOutputSignal(0);

                        if (Math.Abs(output) > threshold) {
                            float weight = (float)(((Math.Abs(output) - (threshold)) / (1 - threshold)) * weightRange * Math.Sign(output));
                            connections.Add(new ConnectionGene(connectionCounter++, input, outp, weight));
                        }
                    }
                }
            }

            return new SharpNeatLib.NeatGenome.NeatGenome(0, neurons, connections, (int)inputCount, (int)outputCount);
        }

        
    }
}
