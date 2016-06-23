using Chromaria.SharpNeatLib;
using Chromaria.SharpNeatLib.Evolution;
using Chromaria.SharpNeatLib.NeuralNetwork;
using Chromaria.SharpNeatLib.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chromaria.SharpNeatLib.NeatGenome
{
    public class CPPNGenomeFactory : GenomeFactory
    {
        // This class contains a GenomeFactory that generates CPPNs. 
        // The CPPNGenomeFactory turns these generated CPPNs into genomes that represent neural controllers.
        GenomeFactory cppnGenerator;
        GenomeList listOfCppnGenomes;

        /// <summary>
        /// CPPNGenomeFactory constructor. Used to generate a minimal CPPN
        /// </summary>
        /// <param name="neatParameters"></param>
        /// <param name="idGenerator"></param>
        /// <param name="inputNeuronCount"></param>
        /// <param name="outputNeuronCount"></param>
        /// <param name="connectionProportion"></param>
        public CPPNGenomeFactory(NeatParameters neatParameters, IdGenerator idGenerator, int inputNeuronCount, int outputNeuronCount, float connectionProportion)
        {
            cppnGenerator = new GenomeFactory();
            listOfCppnGenomes = cppnGenerator.CreateGenomeList(neatParameters, idGenerator, inputNeuronCount, outputNeuronCount, connectionProportion, Chromaria.Simulator.populationSize);
        }

        /// <summary>
        /// Construct a GenomeList. This can be used to construct a new Population object.
        /// </summary>
        /// <param name="evolutionAlgorithm"></param>
        /// <param name="inputNeuronCount"></param>
        /// <param name="outputNeuronCount"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public GenomeList CreateGenomeList()
        {
            GenomeList genomeList = new GenomeList();

            INetwork cppn;
            foreach (NeatGenome cppnGenome in listOfCppnGenomes)
            {
                // Decode the CPPN genome into a CPPN
                cppn = cppnGenome.Decode(new BipolarSigmoid());

                // Use the CPPN in tandem with the HyperNEAT substrate to generate a genome for the controller
                // and add the controller's genome to the genome list
                genomeList.Add(Chromaria.Simulator.controllerSubstrate.generateGenome(cppn));
            }

            return genomeList;
        }
    }
}
