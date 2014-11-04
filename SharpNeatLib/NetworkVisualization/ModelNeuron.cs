using System;
using System.Drawing;
using Chromaria.SharpNeatLib.NeuralNetwork;

namespace Chromaria.SharpNeatLib.NetworkVisualization
{
	public class ModelNeuron
	{
		bool hasPositionInfo=false;
		Point position;

		NeuronType	neuronType;
		uint		id;
		bool		omitFromBitmap=false;
        IActivationFunction function;

		ModelConnectionList inConnectionList = new ModelConnectionList();	
		ModelConnectionList outConnectionList = new ModelConnectionList();	

		Object auxPaintingData;

		#region Constructor
		
		public ModelNeuron(NeuronType neuronType, uint id, IActivationFunction funct)
		{
			this.neuronType = neuronType;
			this.id = id;
            this.function = funct;
		}

		#endregion

		#region Properties

		public bool HasPositionInfo
		{
			get
			{
				return hasPositionInfo;
			}
			set
			{
				hasPositionInfo = value;
			}
		}

		public Point Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
			}
		}

		public NeuronType NeuronType
		{
			get
			{
				return neuronType;
			}
		}

		public uint Id
		{
			get
			{
				return id;
			}
		}

		public bool OmitFromBitmap
		{
			get
			{
				return omitFromBitmap;
			}
			set
			{
				omitFromBitmap = value;
			}
		}

		public ModelConnectionList InConnectionList
		{
			get
			{
				return inConnectionList;
			}
		}

		public ModelConnectionList OutConnectionList
		{
			get
			{
				return outConnectionList;
			}
		}

        public IActivationFunction ActivationFunction
        {
            get
            {
                return function;
            }
        }   

		/// <summary>
		/// Auxilliary data that can be attached during the painting routines to aid painting.
		/// </summary>
		public object AuxPaintingData
		{
			get
			{
				return auxPaintingData;
			}
			set
			{
				auxPaintingData = value;
			}
		}

		#endregion
	}
}
