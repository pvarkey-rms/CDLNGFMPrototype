using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class RITE : IEquatable<RITE>
    {
        //Needs to be read from Exposure Data... Architecture need to change, remove all multiplier arrays..
        public static int MaxNumOfBldgs;

        public long ExposureID { get; private set; }
        public HashSet<RITCharacteristic> RiskCharacteristics { get; private set; }
        public bool IsAggregate { get; private set; }   
        
        private double[] getMultiplierArr;
        public double[] GetMultiplierArr { get { return getMultiplierArr; } }
        public float Factor { get; private set; }        

        public int NumOfTrueBldgs { get; private set; } //this is numOfBldgs from EDS, physical location's buildings, only being used to compute TIV Per Building
        public int NumOfSampleBldgs { get; private set; } //this is the sample size for the aggregate location, which should not change per event

        public RITE(long _ID, int _numBldgs, int _numOfSamples, bool _isAggregate)
        {            
            RiskCharacteristics = new HashSet<RITCharacteristic>();
            ExposureID = _ID;
            NumOfTrueBldgs = _numBldgs;
            NumOfSampleBldgs = _numOfSamples;
            IsAggregate = _isAggregate;
            getMultiplierArr = GenerateMultiplierArr(NumOfSampleBldgs);
            Factor = ((float)NumOfTrueBldgs / (float)NumOfSampleBldgs); //TODO: check with Prototype?           
        }

        public void AddCharacteristic(long _ID, ExposureType _expType, double _tiv)
        {
            RITCharacteristic Char = new RITCharacteristic(_ID, _expType, _tiv);
            RiskCharacteristics.Add(Char);
            Char.ParentRITE = this;
        }

        public void AddCharacteristic(RITCharacteristic Char)
        {
            RiskCharacteristics.Add(Char);
            Char.ParentRITE = this;
        }        
        
        public int ActNumOfSampleBldgs
        {
            get
            {
                //if (IsAggregate)
                //    return NumOfSampleBldgs;
                //else
                //    return Math.Min(MaxNumOfBldgs, NumOfSampleBldgs);
                return NumOfSampleBldgs;
            }
        }
        
        public double GetTIV(long RITCharID)
        {
            return RiskCharacteristics.Where(ritChar => ritChar.ID == RITCharID).FirstOrDefault().TIV;
        }

        public static double[] GenerateMultiplierArr(int numOfBldgs)
        {
            double[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new double[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new double[MaxNumOfBldgs];
                Double temp = (double)(numOfBldgs / MaxNumOfBldgs);
                int n = (int)(Math.Floor(temp));
                int m = numOfBldgs - n * MaxNumOfBldgs;
                for (int i = 1; i <= m; i++)
                {
                    multiplierArr[i - 1] = n + 1;
                }
                for (int i = m + 1; i <= MaxNumOfBldgs; i++)
                {
                    multiplierArr[i - 1] = n;
                }
            }
            return multiplierArr;
        }


        public static double[] GenerateMultiplierArr_new(int numOfBldgs)
        {
            double[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new double[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new double[MaxNumOfBldgs];
                Double temp = (double)(numOfBldgs / MaxNumOfBldgs);
                for (int i = 1; i <= MaxNumOfBldgs; i++)
                {
                    multiplierArr[i - 1] = temp;
                }
            }
            return multiplierArr;
        }

        public bool Equals(RITE other)
        {
            if (other == null)
                return false;

            if (this.ExposureID == other.ExposureID)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            RITE riteObj = obj as RITE;
            if (riteObj == null)
                return false;
            else
                return Equals(riteObj);
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(this.ExposureID);
        }

        public override string ToString()
        {
            return this.ExposureID.ToString();
        }
    }
}
