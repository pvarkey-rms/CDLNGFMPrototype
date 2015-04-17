using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{

    public class TermEngine1 : ITermEngine
    {
        //This TermEngine is for SublimitIsNetofDed && DedisAbsorbable
        //Apply interaction on term levels
        public void ApplyInteraction(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {
            if (!finTermInfo.HasPercentDed)
            {

                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();


                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();
                float[] rRatio = levelstate.GetAllocateRatioR();
                float[] dRatio = levelstate.GetAllocateRatioD();
                //Nina:
                float dedFromBelow;
                float recoveFromBelow;
                float deltaD;

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        //ded[j] = Math.Max(ded[j], (Math.Min(subjectloss[j], tempDed) - excess[j]));
                        //ded[j] = Math.Max(ded[j], (tempDed - excess[j]));
                        //ded[j] = Math.Max(ded[j], Math.Min(subjectloss[j], codeDed[j]) - excess[j]);
                        ded[j] = Math.Max(ded[j], Math.Min(subjectloss[j], tempDed) - excess[j]);
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j] - ded[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...Including Max Ded
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], Math.Min(subjectloss[j], tempDed) - excess[j]);
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);

                        excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j] - ded[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }



                }
            }
            else
                throw new NotSupportedException("Cannot currently handle maximum or percent deductibles");
        }

        //Apply interaction on lowest level
        public void InteractionOnLowestTermLevel(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {
            if (!finTermInfo.HasPercentDed)
            {
                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        // Main aplication of financial terms, and interaction...

                        //float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j] - ded[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];
                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        // Main aplication of financial terms, and interaction...

                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j] - ded[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }
                }
            }
            else
                throw new NotSupportedException("Cannot currently handle percent deductibles");
        }
    }

    public class TermEngine2 : ITermEngine
    {
        //!NetOfDeductible && DedIsAbsorbable
        //Apply interaction on term levels
        public void ApplyInteraction(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {
            if (!finTermInfo.HasMaxDed && !finTermInfo.HasPercentDed)
            {
                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();
                float[] rRatio = levelstate.GetAllocateRatioR();
                float[] dRatio = levelstate.GetAllocateRatioD();

                float dedFromBelow;
                float recoveFromBelow;
                float deltaD;

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(excess[j], Math.Max(0, subjectloss[j] - codeLim[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        //excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j]));
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed - excess[j]);
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];


                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(excess[j], Math.Max(0, subjectloss[j] - codeLim[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        //excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j]));
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed - excess[j]);
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];


                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
            }
            else
                throw new NotSupportedException("Cannot currently handle maximum or percent deductibles");
        }

        //Apply interaction on lowest level
        public void InteractionOnLowestTermLevel(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {

            if (!finTermInfo.HasMaxDed && !finTermInfo.HasPercentDed)
            {

                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {

                        // Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(0, tempDed - excess[j]);
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {

                        // Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(0, tempDed - excess[j]);
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }

                }
            }
            else
                throw new NotSupportedException("Cannot currently handle percent deductibles");
        }
    }

    public class TermEngine3 : ITermEngine
    {
        //NetOfDeductible && !DedIsAbsorbable
        //Apply interaction on term levels
        public void ApplyInteraction(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {

            if (!finTermInfo.HasPercentDed)
            {
                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();
                float[] rRatio = levelstate.GetAllocateRatioR();
                float[] dRatio = levelstate.GetAllocateRatioD();
                //Nina:
                float dedFromBelow;
                float recoveFromBelow;
                float deltaD;

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];

                        //Main aplication of financial terms, and interaction...

                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed);
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j] - ded[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(excess[j], Math.Max(0, subjectloss[j] - codeLim[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        //excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j]));
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed - excess[j]);
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];


                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }

                }
            }
            else
                throw new NotSupportedException("Cannot currently handle maximum or percent deductibles");
        }

        //Apply interaction on lowest level
        public void InteractionOnLowestTermLevel(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {

            if (!finTermInfo.HasMaxDed && !finTermInfo.HasPercentDed)
            {

                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {

                        // Main aplication of financial terms, and interaction...

                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j] - ded[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {

                        // Main aplication of financial terms, and interaction...

                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j] - ded[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }


                }
            }
            else
                throw new NotSupportedException("Cannot currently handle maximum or percent deductibles");
        }

    }

    public class TermEngine4 : ITermEngine
    {
        //!SublimitIsNetofDed && !DedisAbsorbable
        //Apply interaction on term levels
        public void ApplyInteraction(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {

            if (!finTermInfo.HasMaxDed && !finTermInfo.HasPercentDed)
            {
                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();
                float[] rRatio = levelstate.GetAllocateRatioR();
                float[] dRatio = levelstate.GetAllocateRatioD();
                //Nina:
                float dedFromBelow;
                float recoveFromBelow;
                float deltaD;

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(excess[j], Math.Max(0, subjectloss[j] - codeLim[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        //excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j]));
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed);
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        //Save ded and recoverable from below
                        dedFromBelow = ded[j];
                        recoveFromBelow = recov[j];
                        //Nina: Recoverable from below need to be added since when dont aggregate R during aggregation?
                        //recoveFromBelow = ChildrenLevelState.GetRecoverable().Sum() + ChildrenAriteLevelState.GetRecoverable().Sum();

                        /////Initial All lines

                        //Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(excess[j], Math.Max(0, subjectloss[j] - codeLim[j]));
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        //excess[j] = Math.Max(excess[j], (subjectloss[j] - codeLim[j]));
                        float tempDed = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Max(ded[j], tempDed);
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                        //Allocation Ratio Calculation
                        deltaD = ded[j] - dedFromBelow;
                        if (deltaD >= 0)
                        {
                            rRatio[j] = (recoveFromBelow == 0) ? 0 : (1 - deltaD / recoveFromBelow);
                            dRatio[j] = 1;
                        }
                        else
                        {
                            rRatio[j] = 1;
                            dRatio[j] = (dedFromBelow == 0) ? 0 : (1 + deltaD / dedFromBelow);
                        }
                    }
                }
            }
            else
                throw new NotSupportedException("Cannot currently handle percent deductibles");
        }

        //Apply interaction on lowest level
        public void InteractionOnLowestTermLevel(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize)
        {

            if (!finTermInfo.HasMaxDed && !finTermInfo.HasPercentDed)
            {

                float[] codeDed = finTermInfo.GetCodedMinDeds();
                float[] codedMaxDed = finTermInfo.GetCodedMaxDeds();
                float[] codeLim = finTermInfo.GetCodedLimits();

                int[] franchiseMinDedFlag = finTermInfo.GetFranchiseMinDedFlags();
                int[] franchiseMaxDedFlag = finTermInfo.GetFranchiseMaxDedFlags();

                float[] subjectloss = levelstate.GetSubjectLoss();
                float[] excess = levelstate.GetExcess();
                float[] ded = levelstate.GetDeductible();
                float[] recov = levelstate.GetRecoverable();

                if (!finTermInfo.HasMaxDed)
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        // Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }
                }
                else
                {
                    for (int j = 0; j < levelsize; j++)
                    {
                        // Main aplication of financial terms, and interaction...
                        excess[j] = Math.Max(0, subjectloss[j] - codeLim[j]);
                        excess[j] = Math.Min(subjectloss[j] - ded[j], excess[j]);
                        ded[j] = (subjectloss[j] <= codeDed[j]) ? subjectloss[j] : codeDed[j] * franchiseMinDedFlag[j];
                        ded[j] = Math.Min(ded[j], Math.Min(subjectloss[j], codedMaxDed[j]));
                        ded[j] = Math.Min(subjectloss[j] - excess[j], ded[j]);
                        recov[j] = subjectloss[j] - excess[j] - ded[j];

                    }
                }
            }
            else
                throw new NotSupportedException("Cannot currently handle percent deductibles");
        }
    }

    public interface ITermEngine
    {
        void ApplyInteraction(ILevelState termstate, ILevelFinancialInfo finTermInfo, int levelsize);
        void InteractionOnLowestTermLevel(ILevelState levelstate, ILevelFinancialInfo finTermInfo, int levelsize);
    }
}
