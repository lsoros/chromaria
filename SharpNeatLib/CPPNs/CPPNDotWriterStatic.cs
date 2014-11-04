using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.CPPNs
{
    public class CPPNDotWriterStatic
    {
        //saves a CPPN in dot file format. 
        //Assumes that inputs are X1, Y1, X2, Y2, Z
        public static void saveCPPNasDOT(SharpNeatLib.NeatGenome.NeatGenome genome, string filename)
        {
            StreamWriter SW = File.CreateText(filename);
            SW.WriteLine("digraph g { ");

            String activationType = "";

            foreach (NeuronGene neuron in genome.NeuronGeneList)
            {


                switch (neuron.NeuronType)
                {
                    case NeuronType.Bias: SW.WriteLine("N0 [shape=box, label=Bias]"); break;
                    case NeuronType.Input:

                        string str = "?";
                        switch (neuron.InnovationId)
                        {
                            case 1: str = "X1"; break;
                            case 2: str = "Y1"; break;
                            case 3: str = "X2"; break;
                            case 4: str = "Y2"; break;
                            case 5: str = "Z"; break;

                        }
                        SW.WriteLine("N" + neuron.InnovationId + "[shape=box label=" + str + "]");
                        break;
                    case NeuronType.Output: SW.WriteLine("N" + neuron.InnovationId + "[shape=triangle]"); break;
                    case NeuronType.Hidden:
                        if (neuron.ActivationFunction.FunctionDescription.Equals("bipolar steepend sigmoid")) activationType = "S";
                        if (neuron.ActivationFunction.FunctionDescription.Equals("bimodal gaussian")) activationType = "G";
                        if (neuron.ActivationFunction.FunctionDescription.Equals("Linear")) activationType = "L";
                        if (neuron.ActivationFunction.FunctionDescription.Equals("Sin function with doubled period")) activationType = "Si";
                        if (neuron.ActivationFunction.FunctionDescription.Equals("Returns the sign of the input")) activationType = "Sign";

                        SW.WriteLine("N" + neuron.InnovationId + "[shape=circle, label=N" + neuron.InnovationId + "_" + activationType + ", fillcolor=gray]");
                        break;
                }

            }

            foreach (ConnectionGene gene in genome.ConnectionGeneList)
            {
                SW.Write("N" + gene.SourceNeuronId + " -> N" + gene.TargetNeuronId + " ");

                if (gene.Weight > 0)
                    SW.WriteLine("[color=black] ");
                else if (gene.Weight < -0)
                    SW.WriteLine("[color=red] [arrowType=inv]");
            }

            foreach (ModuleGene mg in genome.ModuleGeneList)
            {
                foreach (uint sourceID in mg.InputIds)
                {
                    foreach (uint targetID in mg.OutputIds)
                    {
                        SW.Write("N" + sourceID + " -> N" + targetID + " ");

                        SW.WriteLine("[color=gray]");
                    }
                }
            }

            SW.WriteLine(" { rank=same; ");
            foreach (NeuronGene neuron in genome.NeuronGeneList)
            {
                if (neuron.NeuronType == NeuronType.Output)
                {
                    SW.WriteLine("N" + neuron.InnovationId);
                }
            }
            SW.WriteLine(" } ");


            SW.WriteLine(" { rank=same; ");
            foreach (NeuronGene neuron in genome.NeuronGeneList)
            {
                if (neuron.NeuronType == NeuronType.Input)
                {
                    SW.Write("N" + neuron.InnovationId + " ->");
                }
            }
            //Also the bias neuron on the same level
            SW.WriteLine("N0 [style=invis]");
            SW.WriteLine(" } ");

            SW.WriteLine("}");

            SW.Close();
        }

    }
}
