using System;
using System.Xml;
using Chromaria.SharpNeatLib.Evolution;

namespace Chromaria.SharpNeatLib.Evolution.Xml
{
	public interface IGenomeReader
	{
		IGenome Read(XmlElement xmlGenome);
	}
}
