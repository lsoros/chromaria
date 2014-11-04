using System;
using System.Xml;

using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Evolution.Xml;

namespace Chromaria.SharpNeatLib.NeatGenome.Xml
{
	public class XmlNeatGenomeReader : IGenomeReader
	{
		public IGenome Read(XmlElement xmlGenome)
		{
			return XmlNeatGenomeReaderStatic.Read(xmlGenome);
		}
	}
}
