using System;
using System.Xml;

using Chromaria.SharpNeatLib.NeatGenome;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;

namespace Chromaria.SharpNeatLib.NeuralNetwork.Xml
{
	public class XmlNetworkWriterStatic
	{
        public static void Write(XmlNode parentNode, FloatFastConcurrentNetwork network, IActivationFunction activationFn)
        {
            //----- Start writing. Create document root node.
            XmlElement xmlNetwork = XmlUtilities.AddElement(parentNode, "network");
            XmlUtilities.AddAttribute(xmlNetwork, "activation-fn-id", activationFn.FunctionId);

            //----- Write Connections.
            XmlElement xmlConnections = XmlUtilities.AddElement(xmlNetwork, "connections");
            foreach (FloatFastConnection connectionGene in network.connectionArray)
                WriteConnection(xmlConnections, connectionGene);
        }
        //public static void Write(XmlNode parentNode, ModularNetwork network, IActivationFunction activationFn)
        //{
        //    //----- Start writing. Create document root node.
        //    XmlElement xmlNetwork = XmlUtilities.AddElement(parentNode, "network");
        //    if (activationFn != null)
        //    {
        //        XmlUtilities.AddAttribute(xmlNetwork, "activation-fn-id", activationFn.FunctionId);
        //    }
        //    //----- Write neurons.
        //    XmlElement xmlNeurons = XmlUtilities.AddElement(xmlNetwork, "neurons");
        //    foreach (NeuronGene neuronGene in genome.NeuronGeneList)
        //        WriteNeuron(xmlNeurons, neuronGene);
        //    //----- Write Connections.
        //    XmlElement xmlConnections = XmlUtilities.AddElement(xmlNetwork, "connections");
        //    foreach (FloatFastConnection connectionGene in network.connections)
        //        WriteConnection(xmlConnections, connectionGene);
        //}
		public static void Write(XmlNode parentNode, NeatGenome.NeatGenome genome, IActivationFunction activationFn)
		{
		//----- Start writing. Create document root node.
			XmlElement xmlNetwork = XmlUtilities.AddElement(parentNode, "network");
            if (activationFn != null)
            {
                XmlUtilities.AddAttribute(xmlNetwork, "activation-fn-id", activationFn.FunctionId);
                XmlUtilities.AddAttribute(xmlNetwork, "adaptable", "false");
            }
		//----- Write neurons.
			XmlElement xmlNeurons = XmlUtilities.AddElement(xmlNetwork, "neurons");
			foreach(NeuronGene neuronGene in genome.NeuronGeneList)
				WriteNeuron(xmlNeurons, neuronGene);

		//----- Write Connections.
			XmlElement xmlConnections = XmlUtilities.AddElement(xmlNetwork, "connections");
			foreach(ConnectionGene connectionGene in genome.ConnectionGeneList)
				WriteConnection(xmlConnections, connectionGene);
		}

		#region Private Methods

		private static void WriteNeuron(XmlElement xmlNeurons, NeuronGene neuronGene)
		{
			XmlElement xmlNeuron = XmlUtilities.AddElement(xmlNeurons, "neuron");

			XmlUtilities.AddAttribute(xmlNeuron, "id", neuronGene.InnovationId.ToString());
			XmlUtilities.AddAttribute(xmlNeuron, "type", XmlUtilities.GetNeuronTypeString(neuronGene.NeuronType));
            XmlUtilities.AddAttribute(xmlNeuron, "activationFunction", neuronGene.ActivationFunction.FunctionId);
            XmlUtilities.AddAttribute(xmlNeuron, "bias", neuronGene.Bias.ToString());
		}

		private static void WriteConnection(XmlElement xmlConnections, ConnectionGene connectionGene)
		{
			XmlElement xmlConnection = XmlUtilities.AddElement(xmlConnections, "connection");

			XmlUtilities.AddAttribute(xmlConnection, "src-id", connectionGene.SourceNeuronId.ToString() );
			XmlUtilities.AddAttribute(xmlConnection, "tgt-id", connectionGene.TargetNeuronId.ToString());
			XmlUtilities.AddAttribute(xmlConnection, "weight", connectionGene.Weight.ToString("R"));
		}

        private static void WriteConnection(XmlElement xmlConnections, FloatFastConnection connectionGene)
        {
            XmlElement xmlConnection = XmlUtilities.AddElement(xmlConnections, "connection");

            XmlUtilities.AddAttribute(xmlConnection, "src-id", connectionGene.sourceNeuronIdx.ToString());
            XmlUtilities.AddAttribute(xmlConnection, "tgt-id", connectionGene.targetNeuronIdx.ToString());
            XmlUtilities.AddAttribute(xmlConnection, "weight", connectionGene.weight.ToString("R"));
        }

		#endregion
	}
}
