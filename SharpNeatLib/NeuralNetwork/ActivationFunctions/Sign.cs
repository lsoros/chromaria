using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Sign : IActivationFunction
    {

        #region IActivationFunction Members

        public double Calculate(double inputSignal)
        {
            if (double.IsNaN(inputSignal))
                return 0;
            return Math.Sign(inputSignal);
        }

        public float Calculate(float inputSignal)
        {
            if(float.IsNaN(inputSignal))
                return 0;
            return Math.Sign(inputSignal);
        }

        public string FunctionId
        {
            get { return this.GetType().Name; }
        }

        public string FunctionString
        {
            get { return "Sign(x)"; }
        }

        public string FunctionDescription
        {
            get { return "Returns the sign of the input"; }
        }

        #endregion
    }
}
