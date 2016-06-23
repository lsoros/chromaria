using System;
using System.Collections.Generic;
using System.Text;

namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Modulus : IActivationFunction 
    {

        static int factor = 5;
        double ddelta = (2.0 / factor);
        float fdelta = 2.0f / factor;
        static float constant = 10000000;

        #region IActivationFunction Members

        public float Calculate(float inputSignal, int fact)
        {
            float delta = 2.0f / fact;
            inputSignal += 51;
            inputSignal *= constant;
            inputSignal = (int)inputSignal % (int)(delta * constant);
            inputSignal /= constant;
            inputSignal *= fact;
            return inputSignal - 1;
        }

        public double Calculate(double inputSignal, int fact)
        {
            double delta = 2.0 / fact;
            inputSignal += 51;
            inputSignal *= constant;
            inputSignal = (int)inputSignal % (int)(delta * constant);
            inputSignal /= constant;
            inputSignal *= fact;
            return inputSignal - 1;
        }


        public double Calculate(double inputSignal)
        {
            //shift to 0-max#
            inputSignal = ((51 + inputSignal));

            //find modulus (inputSignal>0 <ddelta)
            inputSignal *= constant;
            inputSignal = (int)inputSignal % (int)(ddelta * constant);
            //while (inputSignal > ddelta)
            //    inputSignal -= ddelta;
            inputSignal /= constant;
            inputSignal = inputSignal * (factor);

            return (inputSignal)-1;
        }

        public float Calculate(float inputSignal)
        {
            //shift to 0-max#
            inputSignal = ((51 + inputSignal));

            //find modulus (inputSignal>0 <ddelta)
            inputSignal *= constant;
            inputSignal = (int)inputSignal % (int)(fdelta * constant);
            //while (inputSignal > ddelta)
            //    inputSignal -= ddelta;
            inputSignal /= constant;
            inputSignal = inputSignal * (factor);

            return (inputSignal) - 1;
        }

        public string FunctionId
        {
            get
            {
                return this.GetType().Name;
            }
        }

        public string FunctionString
        {
            get { return "Mod " + factor.ToString(); }
        }

        public string FunctionDescription
        {
            get { return "Modulus"; }
        }

        #endregion
    }
}
