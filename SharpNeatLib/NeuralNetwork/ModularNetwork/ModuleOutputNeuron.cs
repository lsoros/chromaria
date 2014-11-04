using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public class ModuleOutputNeuron : IActivationFunction
	{

        public double Calculate(double inputSignal)
		{
			return 0;
		}

		public float Calculate(float inputSignal)
		{
			return 0;
		}

		public string FunctionId
		{
			get { return this.GetType().Name; }
		}

		public string FunctionString
		{
			get { return "Module output node."; }
		}

		public string FunctionDescription
		{
			get { return "This neuron is an output of a module, and should not be changed by neuron mutations -- only module mutations."; }
		}

    }
}
