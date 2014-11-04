using System;
using System.Xml;

using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Xml;

namespace Chromaria.SharpNeatLib.Evolution.Xml
{
	public class XmlPopulationWriter
	{
	    public static void WriteGenomeList(XmlNode parentNode, GenomeList g)
	    {
	        XmlElement xmlPopulation = XmlUtilities.AddElement(parentNode,"population");
	        
			foreach(IGenome genome in g)
			{
				genome.Write(xmlPopulation);
			}
	    }
	    
		public static void Write(XmlNode parentNode, Population p, IActivationFunction activationFn)
		{
		
			XmlElement xmlPopulation = XmlUtilities.AddElement(parentNode, "population");
			XmlUtilities.AddAttribute(xmlPopulation, "activation-fn-id", activationFn.FunctionId);

			foreach(IGenome genome in p.GenomeList)
			{
				genome.Write(xmlPopulation);
			}
		}
	}
}
