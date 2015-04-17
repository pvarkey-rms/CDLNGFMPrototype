using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class AllocatedLossAdaptor
    {
        IRITEindexMapper Mapper;

        public AllocatedLossAdaptor(IRITEindexMapper _mapper)
        {
            Mapper = _mapper;
        }

        public List<AtomicLoss> GetAllocatedLossList(float[] AllocatedLossVector, int[] GULossIndicies, uint[] TimeStamps, Declarations contractDeclarations)
        {
            List<AtomicLoss> AllocatedLosses = new List<AtomicLoss>();
            int NumOfArites = AllocatedLossVector.Length;

            for (int i = 0; i < NumOfArites; i++)
            {
                if (AllocatedLossVector[i] > 0)
                {
                    AtomicLoss loss = new AtomicLoss();
                    loss.ExposureID = Mapper.GetRITEIDFromIndex(GULossIndicies[i]);
                    loss.Subperil = Mapper.GetSubPerilFromIndex(GULossIndicies[i]);
                    loss.Loss = AllocatedLossVector[i];
                    RITCharacteristic RitChar = Mapper.GetRITCharObject(loss.ExposureID);
                    loss.RITE = RitChar.ParentRITE;
                    loss.ExpType = RitChar.ExpType;

                    //Get TimeStamp using Mapper:
                    int ContractYear = contractDeclarations.Inception.Year;
                    uint timestamp = TimeStamps[GULossIndicies[i]];
                    DateTime ActualTimeStamp = new DateTime(ContractYear, 1, 1);
                    ActualTimeStamp = ActualTimeStamp.AddDays((double)timestamp - 1);
                    loss.timestamp = ActualTimeStamp;
                    AllocatedLosses.Add(loss);
                }
            }

            return AllocatedLosses;
        } 

    }
}
