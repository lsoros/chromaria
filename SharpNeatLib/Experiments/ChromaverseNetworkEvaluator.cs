using Chromaria.SharpNeatLib.NeuralNetwork;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Chromaria.SharpNeatLib.Experiments
{
    class ChromariaNetworkEvaluator : INetworkEvaluator
    {
        private String evaluatorStateMessage;
        
        /// <summary>
        /// Evaluates the argument INetwork.
        /// The network brought into the function will be the actual decoded network, not the CPPN.
        /// </summary>
        /// <param name="network"></param>
        /// <returns>Fitness of the network.</returns>
        public double EvaluateNetwork(INetwork network, out BehaviorType behavior)
        {
            // Set the global NetworkToEvaluate parameter
            // (This is better than requiring the Simulator class to always take a network as a parameter
            // so that it can also be used for multi-creature experiments.)
            //Chromaria.Simulator.networkToBeEvaluated = network;
            //Chromaria.Simulator.sim.Run();

            behavior = new BehaviorType();
            return 0.0;
        }

        /// <summary>
        /// THIS FUNCTION IS CURRENTLY NOT IMPLEMENTED IN A THREADSAFE MANNER.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="sem"></param>
        /// <returns>Fitness of the network.</returns>
        public double threadSafeEvaluateNetwork(INetwork network, Semaphore sem, out BehaviorType behavior, int thread)
        {
            // Set the global NetworkToEvaluate parameter
            // (This is better than requiring the Simulator class to always take a network as a parameter
            // so that it can also be used for multi-creature experiments.)
            //Chromaria.Simulator.networkToBeEvaluated = network;
            //using (Simulator simulator = new Simulator())
            //{
                //simulator.Run();
            //}

            behavior = new BehaviorType();
            return 0.0;
        }

        /// <summary>
        /// A human readable message that describes the state of the evaluator. This is useful if the
        /// evaluator has several modes (e.g. difficulty levels in incremenetal evolution) and we want 
        /// to let the user know what mode the evaluator is in.
        /// </summary>
        public string EvaluatorStateMessage
        {
            get
            {
                return evaluatorStateMessage;
            }
        }
    }
}
