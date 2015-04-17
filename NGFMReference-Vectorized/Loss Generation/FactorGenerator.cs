using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;

namespace NGFMReference
{
    public class FactorGenerator
    {

        public FactorInfo GetFactor(RITExposure location)
        {
            float randomFactor; //  num of samples/agg location # of buildings
            int currSeed = (int)location.ExposureID;
            var RandGen = new Random(currSeed);
            int NumOfSamples = RandGen.Next(1, 10);

            randomFactor = 1; //TODO: do we need this?

            return new FactorInfo(NumOfSamples, randomFactor);
        }

        public FactorInfo GetFactor(RITExposure location, int eventID)
        {
            float randomFactor; //  num of samples/agg location # of buildings
            int currSeed = ((int)location.ExposureID * eventID) % Int32.MaxValue;

            var RandGen = new Random(currSeed);
            int NumOfSamples = RandGen.Next(1, 10);

            randomFactor = 1; //TODO: do we need this?

            return new FactorInfo(NumOfSamples, randomFactor);
        }

        public int ConstantIntHackedFactor()
        {
            return 2;
        }

       //public FactorInfo GetFactor(RITExposure location)  //for testing
       //{
       //    return new FactorInfo(location.CommonCharacteristics.NumBuildings*2, 1);
       //}

    }

    public class FactorInfo
    {
        public int NumOfSamples {get; set;}
        public double Factor {get; set;}

        public FactorInfo(int _NumOfSamples, double _Factor)
        {
            NumOfSamples = _NumOfSamples;
            Factor = _Factor;
        }
    }

}
