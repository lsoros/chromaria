using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Sine : IActivationFunction
    {
        #region IActivationFunction Members

        public double Calculate(double inputSignal)
        {
            return Math.Sin(2*inputSignal);
           
        }

        public float Calculate(float inputSignal)
        {
            return (float)Math.Sin(2*inputSignal);
        }

        public string FunctionId
        {
            get { return this.GetType().Name; }
        }

        public string FunctionString
        {
            get { return "Sin(2*inputSignal)"; }
        }

        public string FunctionDescription
        {
            get { return "Sin function with doubled period"; }
        }

        #endregion
    }
}
