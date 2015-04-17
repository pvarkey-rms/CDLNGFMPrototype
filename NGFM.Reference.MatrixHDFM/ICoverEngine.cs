using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public interface ICoverEngine
    {
        void ApplyCoverLayer(ICoverLevelFinancialInfo FinancialInfo, int StartPosition, ICoverState TotalCoverState);
        float ApplyTopCoverLayer(ICoverLevelFinancialInfo FinancialInfo, int StartPosition, ICoverState TotalCoverState, int[] LeafTopCoverList, float[] FactorVector);
    }



    public class CoverEngine : ICoverEngine
    {
        public void ApplyCoverLayer(ICoverLevelFinancialInfo FinancialInfo, int TotalStatePosition, ICoverState TotalCoverState)
        {
            float[] Attachment = FinancialInfo.GetCodedAttachment();
            float[] Limit = FinancialInfo.GetCodedLimit();
            float[] Share = FinancialInfo.GetCodedShare();
            float[] SubjectLoss = TotalCoverState.GetSubjectLoss();
            float[] Payout = TotalCoverState.GetPayout();
            FunctionType functionType = FinancialInfo.PayoutFunctionType;

            int EndPosition = Share.Count();

            if (functionType == FunctionType.Constant)
            {
                float E = 0;

                for (int i = 0; i < EndPosition; i++)
                {
                    E = SubjectLoss[TotalStatePosition] - Attachment[i];
                    Payout[TotalStatePosition] = Share[i] * Limit[i] / 2 * (E / Math.Abs(E) + 1);
                    TotalStatePosition++;
                }
            }
            else if (functionType == FunctionType.Regular)
            {
                for (int i = 0; i < EndPosition; i++)
                {
                    Payout[TotalStatePosition] = Share[i] * Math.Min(Limit[i], Math.Max(0, SubjectLoss[TotalStatePosition] - Attachment[i]));
                    TotalStatePosition++;
                }
            }
            else if (functionType == FunctionType.Min)
            {
                for (int i = 0; i < EndPosition; i++)
                {
                    Payout[TotalStatePosition] = Share[i] * Math.Min(Limit[i], Math.Max(0, SubjectLoss[TotalStatePosition]));
                    TotalStatePosition++;
                }
            }
            else
            {
                float[] F = FinancialInfo.GetPayoutMultiplier();
                float E = 0;
                for (int i = 0; i < EndPosition; i++)
                {
                    E = SubjectLoss[TotalStatePosition] - Attachment[i];
                    if (E <= 0)
                        Payout[TotalStatePosition] = 0;
                    else
                        Payout[TotalStatePosition] = Share[i] * (16 * F[i] * F[i] - 14 * F[i] - 1) * ((F[i] - 1) * F[i] * Limit[i] + (2 * F[i] - 1) * Math.Min((SubjectLoss[TotalStatePosition] - Attachment[i] * F[i]), Limit[i])) * (Math.Max(0, E) / Math.Abs(E));
                    TotalStatePosition++;
                }
                //float E = 0;
                //for (int i = 0; i < EndPosition; i++)
                //{
                //    E = SubjectLoss[TotalStatePosition] - Attachment[i];
                //    //Payout[TotalStatePosition] = Multiplier[i] * Share[i] * Math.Min(Limit[i], Math.Max(0, E)) + (1 - Multiplier[i]) * Limit[i] / 2 * (E / Math.Abs(E) + 1);
                //    if (E <= 0)
                //    {
                //        Payout[TotalStatePosition] = Multiplier[i] * Share[i] * Math.Min(Limit[i], Math.Max(0, E));
                //    }
                //    else
                //        Payout[TotalStatePosition] = Multiplier[i] * Share[i] * Math.Min(Limit[i], E) + (1 - Multiplier[i]) * Limit[i] / 2 * (E / Math.Abs(E) + 1);
                //    TotalStatePosition++;
                //}
            }

        }

        public float ApplyTopCoverLayer(ICoverLevelFinancialInfo FinancialInfo, int TotalStatePosition, ICoverState TotalCoverState, int[] LeafTopCoverList, float[] FactorVector)
        {
            float[] Attachment = FinancialInfo.GetCodedAttachment();
            float[] Limit = FinancialInfo.GetCodedLimit();
            float[] Share = FinancialInfo.GetCodedShare();
            float[] SubjectLoss = TotalCoverState.GetSubjectLoss();
            float[] Payout = TotalCoverState.GetPayout();
            float TotalPayout = 0;
            FunctionType functionType = FinancialInfo.PayoutFunctionType;

            //float[] Factors = FinancialInfo.GetFactor();
            int[] factorsIndex = FinancialInfo.GetFactorIndex();
            int counter = factorsIndex.Count();
            float[] factors = new float[counter];
            for (int i = 0; i < counter; i++)
            {

                int uniqueIndex = factorsIndex[i];
                if (uniqueIndex == -1)
                    factors[i] = 1;
                else
                    factors[i] = FactorVector[uniqueIndex];
            }
                   
            int EndPosition = Share.Count();

            if (functionType == FunctionType.Constant)
            {
                float[] Multiplier = FinancialInfo.GetPayoutMultiplier();
                float E = 0;
                for (int i = 0; i < EndPosition; i++)
                {
                    E = SubjectLoss[TotalStatePosition] - Attachment[i];
                    Payout[TotalStatePosition] = Limit[i] / 2 * (E / Math.Abs(E) + 1);
                    TotalPayout += Payout[TotalStatePosition] * factors[i];
                    TotalStatePosition++;
                }
            }
            else if (functionType == FunctionType.Regular)
            {
                for (int i = 0; i < EndPosition; i++)
                {
                    Payout[TotalStatePosition] = Share[i] * Math.Min(Limit[i], Math.Max(0, SubjectLoss[TotalStatePosition] - Attachment[i]));
                    TotalPayout += Payout[TotalStatePosition] * factors[i];
                    TotalStatePosition++;
                }
            }
            else if (functionType == FunctionType.Min)
            {
                for (int i = 0; i < EndPosition; i++)
                {
                    Payout[TotalStatePosition] = Share[i] * Math.Min(Limit[i], Math.Max(0, SubjectLoss[TotalStatePosition]));
                    TotalPayout += Payout[TotalStatePosition] * factors[i];
                    TotalStatePosition++;
                }
            }
            else
            {
                float[] F = FinancialInfo.GetPayoutMultiplier();
                float E = 0;
                for (int i = 0; i < EndPosition; i++)
                {
                    E = SubjectLoss[TotalStatePosition] - Attachment[i];

                    if (E <= 0)
                        Payout[TotalStatePosition] = 0;
                    else
                        Payout[TotalStatePosition] = (16 * F[i] * F[i] - 14 * F[i] - 1) * ((F[i] - 1) * F[i] * Limit[i] + (2 * F[i] - 1) * Math.Min((SubjectLoss[TotalStatePosition] - Attachment[i] * F[i]), Limit[i])) * (Math.Max(0, E) / Math.Abs(E));
                    TotalPayout += Payout[TotalStatePosition] * factors[i];
                    //Payout[TotalStatePosition] = Multiplier[i] * Share[i] * Math.Min(Limit[i], Math.Max(0, E)) + (1 - Multiplier[i]) * Limit[i] / 2 * (E / Math.Abs(E) + 1);

                    //if (E <= 0)
                    //{
                    //    Payout[i] = Multiplier[i] * Share[i] * Math.Min(Limit[i], Math.Max(0, E));
                    //    TotalPayout += Payout[TotalStatePosition];
                    //}
                    //else
                    //{
                    //    Payout[i] = Multiplier[i] * Share[i] * Math.Min(Limit[i], E) + (1 - Multiplier[i]) * Limit[i];
                    //    TotalPayout += Payout[TotalStatePosition];
                    //}

                    TotalStatePosition++;
                }
            }

            int NumOfLeafTop = LeafTopCoverList.Count();
            if (NumOfLeafTop > 0)
            {
                for (int i = 0; i < NumOfLeafTop; i++)
                {
                    TotalPayout += Payout[i] * factors[i];
                }
            }
            return TotalPayout;
        }

    }
}
