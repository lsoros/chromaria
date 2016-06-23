using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public struct IntegerFastConnection
	{
		public int sourceNeuronIdx;
		public int targetNeuronIdx;
		public int weight;
		public int signal;
	}
}
