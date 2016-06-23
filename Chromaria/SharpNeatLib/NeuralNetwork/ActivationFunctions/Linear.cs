using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public class Linear : IActivationFunction
	{
		public double Calculate(double inputSignal)
		{
            if (Double.IsNegativeInfinity(inputSignal))
                return -1.0;
            else if (Double.IsPositiveInfinity(inputSignal))
                return 1.0;
            else
                return inputSignal;
		}

		public float Calculate(float inputSignal)
		{
            if (float.IsNegativeInfinity(inputSignal))
                return -1.0f;
            else if (float.IsPositiveInfinity(inputSignal))
                return 1.0f;
            else
                return inputSignal;
		}

		/// <summary>
		/// Unique ID. Stored in network XML to identify which function network the network is supposed to use.
		/// </summary>
		public string FunctionId
		{
			get
			{
				return this.GetType().Name;
			}
		}

		/// <summary>
		/// The function as a string in a platform agnostic form. For documentation purposes only, this isn;t actually compiled!
		/// </summary>
		public string FunctionString
		{
			get
			{
                return "(x+1)/2 [min=0, max=1]";
			}
		}

		/// <summary>
		/// A human readable / verbose description of the activation function.
		/// </summary>
		public string FunctionDescription
		{
			get
			{
                return "Linear";
			}
		}
	}
}
