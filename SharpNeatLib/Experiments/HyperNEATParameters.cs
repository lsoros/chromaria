using System;
using System.Collections.Generic;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.Experiments
{
    public class HyperNEATParameters
    {
        public static double threshold = 0;
        public static double weightRange = 0;
        public static int numThreads = 1;
        public static IActivationFunction substrateActivationFunction = null;
        public static Dictionary<string, double> activationFunctions = new Dictionary<string, double>();
        public static Dictionary<string, double> modules = new Dictionary<string, double>();
        public static Dictionary<string, string> parameters = new Dictionary<string, string>();
        //public static System.IO.StreamWriter distOutput = new System.IO.StreamWriter("distances.txt");
        //If you're doing a nondeterministic domain set this variable to true otherwise only new genomes will be evaluated.
        public static bool reevaluateEveryGeneration = true;

        static HyperNEATParameters()
        {
            loadParameterFile();
        }

        public static void forceLoad()
        {

        }

        public static void loadParameterFile()
        {
            try
            {
                System.IO.StreamReader input = new System.IO.StreamReader(@"params.txt");
                string[] line;
                double probability;
                bool readingActivation = false;
                bool readingModules = false;
                while (!input.EndOfStream)
                {
                    line = input.ReadLine().Split(' ');
                    if (line[0].Equals("StartActivationFunctions"))
                    {
                        readingActivation = true;
                    }
                    else if (line[0].Equals("EndActivationFunctions"))
                    {
                        readingActivation = false;

                    } else if (line[0].Equals("StartModules")) {
                        readingModules = true;

                    } else if (line[0].Equals("EndModules")) {
                        readingModules = false;

                    } else {
                        if (readingActivation)
                        {
                            double.TryParse(line[1], out probability);
                            activationFunctions.Add(line[0], probability);

                        } else if (readingModules) {
                            double.TryParse(line[1], out probability);
                            modules.Add(line[0], probability);

                        } else {
                            parameters.Add(line[0].ToLower(), line[1]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                System.Console.WriteLine("Error reading config file, check file location and formation");
            }

            ActivationFunctionFactory.setProbabilities(activationFunctions);
            ModuleFactory.setProbabilities(modules);

            setParameterDouble("threshold", ref threshold);
            setParameterDouble("weightrange", ref weightRange);
            setParameterInt("numberofthreads", ref numThreads);
            setSubstrateActivationFunction();
        }

        private static void setSubstrateActivationFunction()
        {
            string parameter=getParameter("substrateactivationfunction");
            if(parameter!=null)
                substrateActivationFunction=ActivationFunctionFactory.GetActivationFunction(parameter);
        }

        public static string getParameter(string parameter)
        {
            if (parameters.ContainsKey(parameter))
                return parameters[parameter];
            else
                return null;
        }

        public static void setParameterDouble(string parameter, ref double target)
        {
            parameter = getParameter(parameter.ToLower());
            if (parameter != null)
                double.TryParse(parameter, out target);
        }

        public static void setParameterInt(string parameter, ref int target)
        {
            parameter = getParameter(parameter.ToLower());
            if (parameter != null)
                int.TryParse(parameter, out target);
        }
    }
}
