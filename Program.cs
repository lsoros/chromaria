using Chromaria.SharpNeatLib.CPPNs;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;
using Chromaria.SharpNeatLib.Experiments;
using Chromaria.SharpNeatLib.Maths;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeatGenome.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Chromaria
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // Running modes: only select one of these at a time
            bool noveltySearch = false;
            bool manualMode = false; // Allows you to control a creature with the keyboard
            bool demoFromXML = false; // Loads one genome from XML and runs it repeatedly
            bool generateStats = false; // Outputs color stats for visualization

            // Read run mode in from params file
            try
            {
                string line;
                using (StreamReader sr = new StreamReader("chromaria-params.txt"))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.StartsWith("----<<< NOVELTY SEARCH >>>----"))
                            break;

                        if (line.StartsWith("[x]"))
                        {
                            if (line.Contains("Replay existing run"))
                                Simulator.replayRun = true;
                            else if (line.Contains("Demo individual"))
                                demoFromXML = true;
                            else if (line.Contains("New novelty search"))
                                noveltySearch = true;
                            else if (line.Contains("Debug mode"))
                                manualMode = true;
                            else if (line.Contains("Generate stats for visualization"))
                                generateStats = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            // Run the simulator in the specified mode
            if (noveltySearch)
                using (NoveltySearchRun sim = new NoveltySearchRun()) { sim.Run(); }
            else if (manualMode)
                using (ManualRun sim = new ManualRun()) { sim.Run(); }
            else if (demoFromXML)
                using (DemoFromXML sim = new DemoFromXML()) { sim.Run(); }
            else if(generateStats)
                using (GenerateStats sim = new GenerateStats()) { sim.Run(); }
            else
                using (MainLoop sim = new MainLoop()) { sim.Run(); }
        }
    }
}

