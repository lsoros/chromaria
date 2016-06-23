using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class ErrorSign : IActivationFunction
    {

        #region IActivationFunction Members

        float error = .0001f;

        public double Calculate(double inputSignal)
        {
            if (inputSignal < 0)
            {
                if (inputSignal < -error)
                    return -1;
                else
                    return 0;
            }
            else if (inputSignal > 0)
            {
                if (inputSignal > error)
                    return 1;
                else
                    return 0;
            }
            else
                return 0;
        }

        public float Calculate(float inputSignal)
        {
            if (inputSignal < 0)
            {
                if (inputSignal > -error)
                    return 0;
                else
                    return -1;
            }
            else if (inputSignal > 0)
            {
                if (inputSignal < error)
                    return 0;
                else
                    return 1;
            }
            else
                return 0;
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
            get { return "Returns the sign of the input with some error around 0"; }
        }

        #endregion
    }
}
