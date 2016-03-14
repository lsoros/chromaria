using System;
using System.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;

namespace Chromaria.SharpNeatLib.NeatGenome.Xml
{
	public class XmlGenomeWriterStatic
	{

        public static void Write(XmlNode parentNode, NeatGenome genome)
		{
			//----- Start writing. Create document root node.
			XmlElement xmlGenome = XmlUtilities.AddElement(parentNode, "genome");
			XmlUtilities.AddAttribute(xmlGenome, "id", genome.GenomeId.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "species-id", genome.SpeciesId.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "age", genome.GenomeAge.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "fitness", genome.Fitness.ToString("0.00"));
			XmlUtilities.AddAttribute(xmlGenome, "realfitness", genome.RealFitness.ToString("0.00"));
            XmlUtilities.AddAttribute(xmlGenome, "adaptable", genome.networkAdaptable.ToString());
            XmlUtilities.AddAttribute(xmlGenome, "modulated", genome.networkModulatory.ToString());

            //----- Write neurons.
            XmlElement xmlNeurons = XmlUtilities.AddElement(xmlGenome, "neurons");
            foreach (NeuronGene neuronGene in genome.NeuronGeneList)
                WriteNeuron(xmlNeurons, neuronGene);

            //----- Write modules.
            XmlElement xmlModules = XmlUtilities.AddElement(xmlGenome, "modules");
            foreach (ModuleGene moduleGene in genome.ModuleGeneList)
                WriteModule(xmlModules, moduleGene);

            //----- Write connections.
			XmlElement xmlConnections = XmlUtilities.AddElement(xmlGenome, "connections");
			foreach(ConnectionGene connectionGene in genome.ConnectionGeneList)
				WriteConnectionGene(xmlConnections, connectionGene);
		    
			
		    //----- Write beahavior
		    if(genome.Behavior!=null)
		    {
                XmlElement xmlBehavior = XmlUtilities.AddElement(xmlGenome, "behavior");
		        if(genome.Behavior.behaviorList!=null)
		        {
                    XmlElement xmlBehaviorList = XmlUtilities.AddElement(xmlBehavior, "list");
                    WriteBehavior(xmlBehaviorList, genome.objectives, genome.Behavior);        
		        }
		    }
		}

		/// <param name="activationFn">Not strictly part of a genome. But it is useful to document which function
		/// the genome is supposed to run against when decoded into a network.</param>
		public static void Write(XmlNode parentNode, NeatGenome genome, IActivationFunction activationFn)
		{
			//----- Start writing. Create document root node.
			XmlElement xmlGenome = XmlUtilities.AddElement(parentNode, "genome");
			XmlUtilities.AddAttribute(xmlGenome, "id", genome.GenomeId.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "species-id", genome.SpeciesId.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "age", genome.GenomeAge.ToString());
			XmlUtilities.AddAttribute(xmlGenome, "fitness", genome.Fitness.ToString("0.00"));
			XmlUtilities.AddAttribute(xmlGenome, "activation-fn-id", activationFn.FunctionId);

			//----- Write neurons.
			XmlElement xmlNeurons = XmlUtilities.AddElement(xmlGenome, "neurons");
			foreach(NeuronGene neuronGene in genome.NeuronGeneList)
				WriteNeuron(xmlNeurons, neuronGene);

            //----- Write modules.
            XmlElement xmlModules = XmlUtilities.AddElement(xmlGenome, "modules");
            foreach (ModuleGene moduleGene in genome.ModuleGeneList)
                WriteModule(xmlModules, moduleGene);

			//----- Write Connections.
			XmlElement xmlConnections = XmlUtilities.AddElement(xmlGenome, "connections");
			foreach(ConnectionGene connectionGene in genome.ConnectionGeneList)
				WriteConnectionGene(xmlConnections, connectionGene);

		    //----- Write beahavior
		    if(genome.Behavior!=null)
		    {
                XmlElement xmlBehavior = XmlUtilities.AddElement(xmlGenome, "behavior");
		        if(genome.Behavior.behaviorList!=null)
		        {
                    XmlElement xmlBehaviorList = XmlUtilities.AddElement(xmlBehavior, "list");
		            WriteBehavior(xmlBehaviorList, genome.objectives, genome.Behavior);        
		        }
		    }

		}

		#region Private Static Methods
        private static void WriteBehavior(XmlElement xmlBehavior, double[] obj,BehaviorType behavior)
        {
            string outstring = "";
                
            for(int i=0;i<behavior.behaviorList.Count;i++)
            {
                outstring += behavior.behaviorList[i].ToString() + ",";   
            }

            XmlUtilities.AddAttribute(xmlBehavior,"vals",outstring);
        }

		private static void WriteNeuron(XmlElement xmlNeurons, NeuronGene neuronGene)
		{
			XmlElement xmlNeuron = XmlUtilities.AddElement(xmlNeurons, "neuron");

            XmlUtilities.AddAttribute(xmlNeuron, "id", neuronGene.InnovationId.ToString());
			XmlUtilities.AddAttribute(xmlNeuron, "type", XmlUtilities.GetNeuronTypeString(neuronGene.NeuronType));
            XmlUtilities.AddAttribute(xmlNeuron, "activationFunction", neuronGene.ActivationFunction.FunctionId);
			XmlUtilities.AddAttribute(xmlNeuron, "bias", neuronGene.Bias.ToString());
            XmlUtilities.AddAttribute(xmlNeuron, "layer", neuronGene.Layer.ToString());
		}

        private static void WriteModule(XmlElement xmlModules, ModuleGene moduleGene)
        {
            XmlElement xmlModule = XmlUtilities.AddElement(xmlModules, "module");

            XmlUtilities.AddAttribute(xmlModule, "id", moduleGene.InnovationId.ToString());
            XmlUtilities.AddAttribute(xmlModule, "function", moduleGene.Function.FunctionId);

            int index = 0;
            foreach (uint inputId in moduleGene.InputIds) {
                XmlElement inputGene = XmlUtilities.AddElement(xmlModule, "input");
                XmlUtilities.AddAttribute(inputGene, "id", inputId.ToString());
                XmlUtilities.AddAttribute(inputGene, "order", (index++).ToString());
            }

            index = 0;
            foreach (uint outputId in moduleGene.OutputIds) {
                XmlElement outputGene = XmlUtilities.AddElement(xmlModule, "output");
                XmlUtilities.AddAttribute(outputGene, "id", outputId.ToString());
                XmlUtilities.AddAttribute(outputGene, "order", (index++).ToString());
            }
        }

		private static void WriteConnectionGene(XmlElement xmlConnections, ConnectionGene connectionGene)
		{
			XmlElement xmlConnectionGene = XmlUtilities.AddElement(xmlConnections, "connection");

			XmlUtilities.AddAttribute(xmlConnectionGene, "innov-id", connectionGene.InnovationId.ToString());
			XmlUtilities.AddAttribute(xmlConnectionGene, "src-id", connectionGene.SourceNeuronId.ToString());
			XmlUtilities.AddAttribute(xmlConnectionGene, "tgt-id", connectionGene.TargetNeuronId.ToString());
			XmlUtilities.AddAttribute(xmlConnectionGene, "weight", connectionGene.Weight.ToString("R"));
            XmlUtilities.AddAttribute(xmlConnectionGene, "learningrate", connectionGene.learningRate.ToString("R"));
            XmlUtilities.AddAttribute(xmlConnectionGene, "A", connectionGene.A.ToString("R"));
            XmlUtilities.AddAttribute(xmlConnectionGene, "B", connectionGene.B.ToString("R"));
            XmlUtilities.AddAttribute(xmlConnectionGene, "C", connectionGene.C.ToString("R"));
            XmlUtilities.AddAttribute(xmlConnectionGene, "D", connectionGene.D.ToString("R"));
		}

		#endregion

    }
}
