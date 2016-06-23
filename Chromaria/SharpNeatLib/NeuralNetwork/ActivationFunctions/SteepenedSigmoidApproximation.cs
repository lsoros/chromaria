using System;
using System.Collections.Generic;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
	public class SteepenedSigmoidApproximation : IActivationFunction
	{

        //static Dictionary<float,float> hash = new Dictionary<float, float>();
        //static Object locked = new object();

        static SteepenedSigmoidApproximation()
        {
           // for (float f = -1; f <= 1; f += 0.01f)
           //     hash.Add(f, doCalculate(f));
        }


        public double Calculate(double inputSignal)
		{
			const double one = 1.0;
			const double one_half = 0.5; 

			if(inputSignal<-1.0)
			{
				return 0.0;
			}
			else if(inputSignal<0.0)
			{
				return (inputSignal+one)*(inputSignal+one)*one_half;
			}
			else if(inputSignal<1.0)
			{
				return 1.0-(inputSignal-one)*(inputSignal-one)*one_half;
			}
			else
			{
				return 1.0;
			}
		}

        public float Calculate(float inputSignal)
        {
          /*  inputSignal = (float)Math.Round(inputSignal, 2);
            if(SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                return hash[inputSignal];
            return doCalculate(inputSignal);*/
            const float one = 1.0F;
            const float one_half = 0.5F;
            if (inputSignal < -1.0F)
            {
                return 0.0F;
            }
            else if (inputSignal < 0.0F)
            {
                //				float d=inputSignal+four;
                //				return d*d*one_32nd;
                //if (SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                //    return SteepenedSigmoidApproximation.hash[inputSignal];
                float f = (inputSignal + one) * (inputSignal + one) * one_half;
                /*lock (SteepenedSigmoidApproximation.locked)
                {
                    if(!SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                        SteepenedSigmoidApproximation.hash.Add(inputSignal, f);
                }*/
                return f;
            }
            else if (inputSignal < 1.0F)
            {
                //if (SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                //    return SteepenedSigmoidApproximation.hash[inputSignal];
                //				float d=inputSignal-four;
                //				return 1.0F-d*d*one_32nd;
                float f = 1.0F - (inputSignal - one) * (inputSignal - one) * one_half;
                /* lock (SteepenedSigmoidApproximation.locked)
                 {
                     if (!SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                         SteepenedSigmoidApproximation.hash.Add(inputSignal, f);
                 }*/
                return f;
            }
            else
            {
                return 1.0F;
            }
        }

        public static float doCalculate(float inputSignal)
		{
			const float one = 1.0F;
			const float one_half = 0.5F;
            inputSignal = (float)Math.Round(inputSignal,2);
			if(inputSignal<-1.0F)
			{
				return 0.0F;
			}
			else if(inputSignal<0.0F)
			{
//				float d=inputSignal+four;
//				return d*d*one_32nd;
                //if (SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                //    return SteepenedSigmoidApproximation.hash[inputSignal];
                float f = (inputSignal + one) * (inputSignal + one) * one_half;
                /*lock (SteepenedSigmoidApproximation.locked)
                {
                    if(!SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                        SteepenedSigmoidApproximation.hash.Add(inputSignal, f);
                }*/
				return f;
			}
			else if(inputSignal<1.0F)
			{
                //if (SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                //    return SteepenedSigmoidApproximation.hash[inputSignal];
//				float d=inputSignal-four;
//				return 1.0F-d*d*one_32nd;
				float f =  1.0F-(inputSignal-one)*(inputSignal-one)*one_half;
               /* lock (SteepenedSigmoidApproximation.locked)
                {
                    if (!SteepenedSigmoidApproximation.hash.ContainsKey(inputSignal))
                        SteepenedSigmoidApproximation.hash.Add(inputSignal, f);
                }*/
                return f;
			}
			else
			{
				return 1.0F;
			}
		}

		/// <summary>
		/// Unique ID. Stored in network XML to identify which function network the network is supposed to use.
		/// </summary>
		public string FunctionId
		{
			get
			{
				return this.GetType().Name;
			}
		}

		/// <summary>
		/// The function as a string in a platform agnostic form. For documentation purposes only, this isn;t actually compiled!
		/// </summary>
		public string FunctionString
		{
			get
			{
				return "";
			}
		}


		/// <summary>
		/// A human readable / verbose description of the activation function.
		/// </summary>
		public string FunctionDescription
		{
			get
			{
				return "";
			}
		}
	}
}
