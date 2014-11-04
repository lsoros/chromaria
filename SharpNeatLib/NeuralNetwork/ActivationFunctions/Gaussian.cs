using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Gaussian : IActivationFunction
    {
        #region IActivationFunction Members

        public double Calculate(double inputSignal)
        {
            return Math.Exp(-Math.Pow(inputSignal, 2));
        }

        public float Calculate(float inputSignal)
        {
            return (float)(Math.Exp(-Math.Pow(inputSignal, 2)));
        }

        public string FunctionId
        {
            get { return this.GetType().Name; }
        }

        public string FunctionString
        {
            get { return "2*e^(-(input*2.5)^2) - 1"; }
        }

        public string FunctionDescription
        {
            get { return "bimodal gaussian"; }
        }

        #endregion
    }
}
