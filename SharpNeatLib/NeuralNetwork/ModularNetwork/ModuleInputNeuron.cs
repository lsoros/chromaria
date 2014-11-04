using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public class ModuleInputNeuron : IActivationFunction
	{

        public double Calculate(double inputSignal)
		{
			return inputSignal;
		}

		public float Calculate(float inputSignal)
		{
			return inputSignal;
		}

		public string FunctionId
		{
			get { return this.GetType().Name; }
		}

		public string FunctionString
		{
			get { return "Module input node."; }
		}

		public string FunctionDescription
		{
			get { return "This neuron is an input of a module, and should not be changed by neuron mutations -- only module mutations."; }
		}

    }
}
