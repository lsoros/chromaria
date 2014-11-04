using System;
namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Multiply : IModule
    {

        #region IModule Members

        public double[] Calculate(double[] inputSignal)
        {
            double retval = 1;
            foreach (double input in inputSignal) {
                retval *= input;
            }
            return new double[] { retval };
        }

        public float[] Calculate(float[] inputSignal)
        {
            float retval = 1F;
            foreach (float input in inputSignal) {
                retval *= input;
            }
            return new float[] { retval };
        }

        public int InputCount
        {
            get { return _inputCount; }
            set { _inputCount = value;}
        }
        private int _inputCount = 2;

        public int OutputCount
        {
            get { return 1; }
        }

        public string FunctionId
        {
            get { return GetType().Name; }
        }

        public string FunctionString
        {
            get { return "x * y"; }
        }

        public string FunctionDescription
        {
            get { return "Multiplies all its inputs together."; }
        }

        #endregion

    }
}
