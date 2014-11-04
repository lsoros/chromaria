using System;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class NeuronGene
	{
		// Although this id is allocated from the global innovation ID pool, neurons do not participate 
		// in compatibility measurements and so it is not used as an innovation ID. It is used as a unique
		// ID to distinguish between neurons.
		uint innovationId;
		NeuronType neuronType;
        IActivationFunction activationFunction;
        public float Bias { get; set; }
        public float TimeConstant { get; set; }
        public float Layer { get; set; }

		#region Constructor

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
		public NeuronGene(NeuronGene copyFrom)
		{
			this.innovationId = copyFrom.innovationId;
			this.neuronType = copyFrom.neuronType;
            this.activationFunction = copyFrom.activationFunction;
            this.TimeConstant = copyFrom.TimeConstant;
            this.Layer = copyFrom.Layer;
		}

		public NeuronGene(uint innovationId, NeuronType neuronType, IActivationFunction activationFunction, float layer = -1)
		{
			this.innovationId = innovationId;
			this.neuronType = neuronType;
            this.activationFunction = activationFunction;
            this.TimeConstant = 1;
            this.Layer = layer;
		}

		#endregion

		#region Properties

		public uint InnovationId
		{
			get
			{
				return innovationId;
			}
			set
			{
				innovationId = value;
			}
		}

		public NeuronType NeuronType
		{
			get
			{
				return neuronType;
			}
            set
            {
                neuronType = value;
            }
		}

        public IActivationFunction ActivationFunction
        {
            get
            {
                return activationFunction;
            }
            set
            {
                activationFunction = value;
            }
        }

		#endregion
	}
}
