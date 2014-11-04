using System;
namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class MaxAbs : IModule
    {

        #region IModule Members

        public double[] Calculate(double[] inputSignal)
        {
            double retval = 0;
            foreach (double input in inputSignal) {
                retval = Math.Max(retval, Math.Abs(input));
            }
            return new double[] { retval };
        }

        public float[] Calculate(float[] inputSignal)
        {
            float retval = 0;
            foreach (float input in inputSignal) {
                retval = Math.Max(retval, Math.Abs(input));
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
            get { return "max(abs(x), abs(y))"; }
        }

        public string FunctionDescription
        {
            get { return "A continuous function analogous to a logical OR of the inputs, if inputs of 0 are taken to be false, and 1 or -1 are true."; }
        }

        #endregion

    }
}
