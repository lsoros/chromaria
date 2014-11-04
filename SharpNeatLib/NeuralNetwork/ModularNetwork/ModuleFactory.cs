using System.Collections.Generic;
using System.Reflection;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    public static class ModuleFactory
    {

        public static double[] probabilities;

        public static IModule[] functions;

        public static Dictionary<string, IModule> cache = new Dictionary<string, IModule>();


        public static void setProbabilities(Dictionary<string, double> probs)
        {
            probabilities = new double[probs.Count];
            functions = new IModule[probs.Count];
            int counter = 0;
            foreach (KeyValuePair<string, double> funct in probs) {
                probabilities[counter] = funct.Value;
                functions[counter] = GetByName(funct.Key);
                counter++;
            }
        }


        public static IModule GetRandom()
        {
            return functions[Maths.RouletteWheel.SingleThrow(probabilities)];
        }


        public static IModule GetByName(string functionId)
        {
            IModule retval = null;
            if (cache.ContainsKey(functionId)) {
                retval = cache[functionId];
            } else {
                try {
                    retval = CreateByName(functionId);
                    cache.Add(functionId, retval);
                } catch {
                    // we'd be better off logging this but it's easier to ignore it for now.
                }
            }
            return retval;
        }


        private static IModule CreateByName(string functionId)
        {
            // For now the function ID is the name of a class that implements IModule.
            string className = typeof(ActivationFunctionFactory).Namespace + '.' + functionId;
            return Assembly.GetExecutingAssembly().CreateInstance(className) as IModule;
        }

    }
}
