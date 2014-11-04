using System;
using System.Collections.Generic;
using System.Xml;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;

namespace Chromaria.SharpNeatLib.NeatGenome.Xml
{
	public class XmlNeatGenomeReaderStatic
	{
		public static NeatGenome Read(XmlDocument doc)
		{
			XmlElement xmlGenome = (XmlElement)doc.SelectSingleNode("genome");
			if(xmlGenome==null)
				throw new Exception("The genome XML is missing the root 'genome' element.");

			return Read(xmlGenome);
		}

        //public static 

		public static NeatGenome Read(XmlElement xmlGenome)
		{
			int inputNeuronCount=0;
			int outputNeuronCount=0;

			uint id = uint.Parse(XmlUtilities.GetAttributeValue(xmlGenome, "id"));

			//--- Read neuron genes into a list.
			NeuronGeneList neuronGeneList = new NeuronGeneList();
			XmlNodeList listNeuronGenes = xmlGenome.SelectNodes("neurons/neuron");
			foreach(XmlElement xmlNeuronGene in listNeuronGenes)
			{
				NeuronGene neuronGene = ReadNeuronGene(xmlNeuronGene);

				// Count the input and output neurons as we go.
				switch(neuronGene.NeuronType)
				{
					case NeuronType.Input:
						inputNeuronCount++;
						break;
					case NeuronType.Output:
						outputNeuronCount++;
						break;
				}

				neuronGeneList.Add(neuronGene);
			}

            //--- Read module genes into a list.
            List<ModuleGene> moduleGeneList = new List<ModuleGene>();
            XmlNodeList listModuleGenes = xmlGenome.SelectNodes("modules/module");
            foreach (XmlElement xmlModuleGene in listModuleGenes) {
                moduleGeneList.Add(ReadModuleGene(xmlModuleGene));
            }

			//--- Read connection genes into a list.
			ConnectionGeneList connectionGeneList = new ConnectionGeneList();
			XmlNodeList listConnectionGenes = xmlGenome.SelectNodes("connections/connection");
			foreach(XmlElement xmlConnectionGene in listConnectionGenes)
				connectionGeneList.Add(ReadConnectionGene(xmlConnectionGene));
			
			//return new NeatGenome(id, neuronGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
            NeatGenome g = new NeatGenome(id, neuronGeneList, moduleGeneList, connectionGeneList, inputNeuronCount, outputNeuronCount);
            g.Behavior = ReadBehavior(xmlGenome.SelectSingleNode("list"));
            g.Behavior.objectives = new double[6];
            g.objectives = new double[6];
            return g;
		}

		private static NeuronGene ReadNeuronGene(XmlElement xmlNeuronGene)
		{
			uint id = uint.Parse(XmlUtilities.GetAttributeValue(xmlNeuronGene, "id"));
            float layer = (float)Convert.ToDouble(XmlUtilities.GetAttributeValue(xmlNeuronGene, "layer"));
			NeuronType neuronType = XmlUtilities.GetNeuronType(XmlUtilities.GetAttributeValue(xmlNeuronGene, "type"));
            string activationFn = XmlUtilities.GetAttributeValue(xmlNeuronGene, "activationFunction");
			return new NeuronGene(id, neuronType, ActivationFunctionFactory.GetActivationFunction(activationFn), layer);	
		}

        private static ModuleGene ReadModuleGene(XmlElement xmlModuleGene)
        {
            uint id = uint.Parse(XmlUtilities.GetAttributeValue(xmlModuleGene, "id"));
            string function = XmlUtilities.GetAttributeValue(xmlModuleGene, "function");

            XmlNodeList inputNodes = xmlModuleGene.GetElementsByTagName("input");
            uint[] inputs = new uint[inputNodes.Count];
            foreach (XmlNode inp in inputNodes) {
                inputs[int.Parse(XmlUtilities.GetAttributeValue(inp, "order"))] = uint.Parse(XmlUtilities.GetAttributeValue(inp, "id"));
            }

            XmlNodeList outputNodes = xmlModuleGene.GetElementsByTagName("output");
            uint[] outputs = new uint[outputNodes.Count];
            foreach (XmlNode outp in outputNodes) {
                outputs[int.Parse(XmlUtilities.GetAttributeValue(outp, "order"))] = uint.Parse(XmlUtilities.GetAttributeValue(outp, "id"));
            }

            return new ModuleGene(id, ModuleFactory.GetByName(function), new List<uint>(inputs), new List<uint>(outputs));
        }

        private static ConnectionGene ReadConnectionGene(XmlElement xmlConnectionGene)
		{
			uint innovationId = uint.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "innov-id"));
			uint sourceNeuronId = uint.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "src-id"));
			uint targetNeuronId = uint.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "tgt-id"));
			double weight = double.Parse(XmlUtilities.GetAttributeValue(xmlConnectionGene, "weight"));
	
			return new ConnectionGene(innovationId, sourceNeuronId, targetNeuronId, weight);
		}

        private static BehaviorType ReadBehavior(XmlNode xmlBehavior)
        {
            if (xmlBehavior == null)
                return new BehaviorType();
            List<double> l = new List<double>();
            string[] s = XmlUtilities.GetAttributeValue(xmlBehavior, "list").Split(new char[] { ',' });
            BehaviorType b = new BehaviorType();
            foreach (string str in s)
            {
                if(str!="")
                    l.Add(double.Parse(str));
            }
            b.behaviorList = l;
            return b;
        }
	}
}
