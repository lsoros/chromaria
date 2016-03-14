using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria.SharpNeatLib.NeatGenome
{
    public class CombinedGenome
    {
        public int fileNumber { get; set; }
        public NeatGenome MorphologyCPPNGenome { get; set; }
        public NeatGenome ControllerCPPNGenome { get; set; }

        // Default constructor
        public CombinedGenome(NeatGenome morphlogyCPPNGenome_in, NeatGenome controllerCPPNGenome_in)
        {
            MorphologyCPPNGenome = morphlogyCPPNGenome_in;
            ControllerCPPNGenome = controllerCPPNGenome_in;
        }
    }
}
