using System;
using System.Collections.Generic;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class ModuleGene
	{

		#region Constructor

        public ModuleGene(uint innovationId, IModule function, List<uint> inputs, List<uint> outputs)
        {
            this.InnovationId = innovationId;
            this.Function = function;
            this.InputIds = inputs;
            this.OutputIds = outputs;
        }

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
		public ModuleGene(ModuleGene copyFrom)
		{
			this.InnovationId = copyFrom.InnovationId;
            this.Function = copyFrom.Function;
            this.InputIds = copyFrom.InputIds;
            this.OutputIds = copyFrom.OutputIds;
        }

		#endregion

		#region Properties

        // Although this id is allocated from the global innovation ID pool, modules do not participate 
        // in compatibility measurements. It is still used as a unique ID to distinguish between modules.
        public uint InnovationId { get; set; }

        public IModule Function { get; set; }

        public List<uint> InputIds { get; set; }

        public List<uint> OutputIds { get; set; }
        
        #endregion

    }
}
