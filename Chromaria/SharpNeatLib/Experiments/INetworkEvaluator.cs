using System;
using System.Threading;
using Chromaria.SharpNeatLib.NeuralNetwork;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.Experiments
{
	/// <summary>
	/// A simple interface that describes a class that can evaluate a single INetwork.
	/// Typically this interface can be passed to the constructor of 
	/// SingleFilePopulationEvaluator to provide an IPopulationEvaluator to the
	/// EvolutionAlgorithm. See comments on SingleFilePopulationEvaluator for more information.
	/// </summary>
	public interface INetworkEvaluator
	{	
		/// <summary>
		/// Evaluates the argument INetwork.
		/// </summary>
		/// <param name="network"></param>
		/// <returns>Fitness of the network.</returns>
		double EvaluateNetwork(INetwork network, out BehaviorType behavior);

        /// <summary>
        /// Evaluates the argument INetwork which is a CPPN, and decodes the CPPN in a thread safe manner.
        /// </summary>
        /// <param name="network"></param>
        /// <param name="sem"></param>
        /// <returns>Fitness of the network.</returns>
        double threadSafeEvaluateNetwork(INetwork network, Semaphore sem,out BehaviorType behavior,int thread);

		/// <summary>
		/// A human readable message that describes the state of the evaluator. This is useful if the
		/// evaluator has several modes (e.g. difficulty levels in incremenetal evolution) and we want 
		/// to let the user know what mode the evaluator is in.
		/// </summary>
		string EvaluatorStateMessage
		{
			get;
		}
	}
}
