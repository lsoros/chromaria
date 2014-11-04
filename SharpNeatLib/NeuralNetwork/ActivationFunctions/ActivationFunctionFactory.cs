using System;
using System.Collections;
using System.Reflection;

using System.Collections.Generic;
using System.Diagnostics;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{

	public static class ActivationFunctionFactory
	{
        public static double[] probabilities;
        public static IActivationFunction[] functions;

        public static void setProbabilities(Dictionary<string,double> probs)
        {
            probabilities = new double[probs.Count];
            functions = new IActivationFunction[probs.Count];
            int counter = 0;
            foreach (KeyValuePair<string, double> funct in probs)
            {
                probabilities[counter] = funct.Value;
                functions[counter]= GetActivationFunction(funct.Key);
                counter++;
            }

        }

		public static Hashtable activationFunctionTable = new Hashtable();

		public static IActivationFunction GetActivationFunction(string functionId)
		{
            IActivationFunction activationFunction = (IActivationFunction)ActivationFunctionFactory.activationFunctionTable[functionId];
			if(activationFunction==null)
			{
				activationFunction = CreateActivationFunction(functionId);
				activationFunctionTable.Add(functionId, activationFunction);
			}
			return activationFunction;
		}

		private static IActivationFunction CreateActivationFunction(string functionId)
		{
			// For now the function ID is the name of a class that implements IActivationFunction.
			string className = typeof(ActivationFunctionFactory).Namespace + '.' + functionId;
			return (IActivationFunction)Assembly.GetExecutingAssembly().CreateInstance(className);
		}

        public static IActivationFunction GetRandomActivationFunction(Evolution.NeatParameters np)
        {
            Dictionary<string, double> actFunDictionary = new Dictionary<string, double>(4);
            actFunDictionary.Add("Linear", Chromaria.Simulator.pLinear);
            actFunDictionary.Add("Gaussian", Chromaria.Simulator.pGaussian);
            actFunDictionary.Add("BipolarSigmoid", Chromaria.Simulator.pBipolarSigmoid);
            actFunDictionary.Add("Sine", Chromaria.Simulator.pSine);
            ActivationFunctionFactory.setProbabilities(actFunDictionary);
            return functions[Maths.RouletteWheel.SingleThrow(probabilities)];
        }
	}
}
