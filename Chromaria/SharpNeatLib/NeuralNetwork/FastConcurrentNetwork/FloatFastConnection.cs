using System;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public struct FloatFastConnection
	{
        
		public int sourceNeuronIdx;
		public int targetNeuronIdx;
		public float weight;
		public float signal;
        public bool hive;
        //For adaptation
        public float A, B, C, D, modConnection, learningRate;
	}
}
