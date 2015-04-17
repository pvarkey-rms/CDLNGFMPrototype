using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class RITE : IEquatable<RITE>
    {
        const int MaxNumOfBldgs = 250;
        public long ExposureID { get; private set; }
        public HashSet<RITCharacteristic> RiskCharacteristics { get; private set; }
        
        private int[] getMultiplierArr;
        public int[] GetMultiplierArr { get { return getMultiplierArr; } }

        public RITE(long _ID, int _numBldgs)
        {
            RiskCharacteristics = new HashSet<RITCharacteristic>();
            ExposureID = _ID;
            NumOfBldgs = _numBldgs;
            getMultiplierArr = GenerateMultiplierArr(NumOfBldgs);
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

        public int NumOfBldgs { get; private set; }
        public int ActNumOfBldgs
        {
            get
            {
                return Math.Min(MaxNumOfBldgs, NumOfBldgs);
            }
        }
        
        public double GetTIV(long RITCharID)
        {
            return RiskCharacteristics.Where(ritChar => ritChar.ID == RITCharID).FirstOrDefault().TIV;
        }

        public static int[] GenerateMultiplierArr(int numOfBldgs)
        {
            int[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new int[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new int[MaxNumOfBldgs];
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
