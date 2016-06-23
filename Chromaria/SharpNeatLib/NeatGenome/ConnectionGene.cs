using System;

namespace Chromaria.SharpNeatLib.NeatGenome
{
	public class ConnectionGene
	{
		uint	innovationId;
		uint	sourceNeuronId;
		uint	targetNeuronId;
        public bool hive;
//		bool	enabled;
        public float A, B, C, D, modConnection, learningRate;
		double	weight;
		bool	fixedWeight=false;
        public float[] coordinates; //coordinates of the neurons connected by this connection. used to requery the CPPN
		
		/// <summary>
		/// Used by the connection mutation routine to flag mutated connections so that they aren't
		/// mutated more than once.
		/// </summary>
		bool	isMutated=false;

		#region Constructor

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="copyFrom"></param>
		public ConnectionGene(ConnectionGene copyFrom)
		{
			this.innovationId = copyFrom.innovationId;
			this.sourceNeuronId = copyFrom.sourceNeuronId;
			this.targetNeuronId = copyFrom.targetNeuronId;
//			this.enabled = copyFrom.enabled;
			this.weight = copyFrom.weight;
			this.fixedWeight = copyFrom.fixedWeight;
            this.hive = copyFrom.hive;
		}

		public ConnectionGene(uint innovationId, uint sourceNeuronId, uint targetNeuronId, double weight)
		{
			this.innovationId = innovationId;
			this.sourceNeuronId = sourceNeuronId;
			this.targetNeuronId = targetNeuronId;
//			this.enabled = enabled;
			this.weight = weight;
		}

        public ConnectionGene(uint innovationId, uint sourceNeuronId, uint targetNeuronId, double weight, ref float[] _coordinates, float A, float B, float C, float D, float modConnection, float learningRate, bool h = false)
            :this(innovationId, sourceNeuronId, targetNeuronId, weight)
        {
            coordinates = new float[_coordinates.Length];
            this.A = A;
            this.B = B;
            this.C = C;
            this.D = D;
            this.modConnection = modConnection;
            this.learningRate = learningRate;
            this.hive = h;
            Array.Copy(_coordinates, coordinates, _coordinates.Length);
           // coordinates = _coordinates;
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

		public uint SourceNeuronId
		{
			get
			{
				return sourceNeuronId;
			}
			set
			{
				sourceNeuronId = value;
			}
		}

		public uint TargetNeuronId
		{
			get
			{
				return targetNeuronId;
			}
			set
			{
				targetNeuronId = value;
			}
		}

//		public bool	Enabled
//		{
//			get
//			{
//				return enabled;
//			}
//			set
//			{
//				enabled = value;
//			}
//		}

		public double Weight
		{
			get
			{
				return weight;
			}
			set
			{
				weight = value;
			}
		}

		public bool FixedWeight
		{
			get
			{
				return fixedWeight;
			}
			set
			{
				fixedWeight = value;
			}
		}

		public bool IsMutated
		{
			get
			{
				return isMutated;
			}
			set
			{
				isMutated = value;
			}
		}

		#endregion
	}
}
