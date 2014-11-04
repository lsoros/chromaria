using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    public class BipolarSigmoid : IActivationFunction
    {

        #region IActivationFunction Members

        public double Calculate(double inputSignal)
        {
            return (2.0 / (1.0 + Math.Exp(-4.9 * inputSignal))) - 1.0;
        }

        public float Calculate(float inputSignal)
        {
            return (2.0F / (1.0F + (float)Math.Exp(-4.9F * inputSignal))) - 1.0F;
        }

        public string FunctionId
        {
            get { return this.GetType().Name; }
        }

        public string FunctionString
        {
            get { return "2.0/(1.0 + exp(-4.9*inputSignal)) - 1.0"; }
        }

        public string FunctionDescription
        {
            get { return "bipolar steepend sigmoid"; }
        }

        #endregion
    }
}
