using System;
namespace Chromaria.SharpNeatLib.NeuralNetwork
{
    class Max : IModule
    {

        #region IModule Members

        public double[] Calculate(double[] inputSignal)
        {
            double retval = double.NegativeInfinity;
            foreach (double input in inputSignal) {
                retval = Math.Max(retval, input);
            }
            return new double[] { retval };
        }

        public float[] Calculate(float[] inputSignal)
        {
            float retval = float.NegativeInfinity;
            foreach (float input in inputSignal) {
                retval = Math.Max(retval, input);
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
            get { return "max(x, y)"; }
        }

        public string FunctionDescription
        {
            get { return "A continuous function analogous to a logical OR of the inputs, if inputs of min_val are taken to be false, and max_val are true."; }
        }

        #endregion

    }
}
