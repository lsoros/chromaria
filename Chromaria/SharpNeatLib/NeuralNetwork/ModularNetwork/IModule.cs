using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    /// <summary>
    /// This interface describes functions that can be used as activation functions in the ModularNetwork.
    /// It is similar to IActivationFunction, but each module may have multiple inputs and outputs.
    /// </summary>
    /// <remarks>
    /// It could make sense for INetwork and IActivationFunction to inherit from IModule, but we'll start this way for lower impact.
    /// </remarks>
    public interface IModule
    {

        /// <summary>
        /// Calculates the value of the module given the inputs.
        /// If the module is a network, this method might run one activation or relax the network -- it's not decided right now.
        /// </summary>
        double[] Calculate(double[] inputSignal);

        /// <summary>
        /// Calculates the value of the module given the inputs.
        /// If the module is a network, this method might run one activation or relax the network -- it's not decided right now.
        /// </summary>
        /// <remarks>
        /// A float equivalent should be implemented as this provides approx. a 60% speed boost
        /// in the right circumstances. Partly through not having to cast to/from double and partly
        /// because floats are [sometimes] faster to calculate. They are also small and require less
        /// memory bus bandwidth and CPU cache.
        /// </remarks>
        float[] Calculate(float[] inputSignal);

        /// <summary>
        /// The number of inputs this IModule expects to get.
        /// </summary>
        int InputCount { get; }

        /// <summary>
        /// The number of outputs this IModule will produce.
        /// </summary>
        int OutputCount { get; }

        /// <summary>
        /// Unique ID. Stored in network XML to identify which function network the network is supposed to use.
        /// </summary>
        string FunctionId
        {
            get;
        }

        /// <summary>
        /// The function as a string in a platform agnostic form. For documentation purposes only -- this isn't actually compiled!
        /// </summary>
        string FunctionString
        {
            get;
        }

        /// <summary>
        /// A verbose, human-readable description of the activation function.
        /// </summary>
        string FunctionDescription
        {
            get;
        }

    }
}
