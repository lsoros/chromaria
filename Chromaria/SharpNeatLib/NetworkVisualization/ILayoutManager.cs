using System;
using System.Drawing;

namespace Chromaria.SharpNeatLib.NetworkVisualization
{
	public interface ILayoutManager
	{
		void Layout(NetworkModel nm, Size areaSize);
	}
}
