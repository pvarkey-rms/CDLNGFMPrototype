using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class RITCharacteristic : IEquatable<RITCharacteristic>
    {
        public long ID { get; private set; }
        public ExposureType ExpType { get; private set; }
        public double TIV { get; private set; }
        public RITE ParentRITE {get; set;}

        public RITCharacteristic(long _ID, ExposureType _expType, double _tiv)
        {
            ID = _ID;
            ExpType = _expType;
            TIV = _tiv;
        }

        //Override IEquatable
        public bool Equals(RITCharacteristic other)
        {
            if (other == null)
                return false;

            if (this.ID == other.ID)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            RITCharacteristic charObj = obj as RITCharacteristic;
            if (charObj == null)
                return false;
            else
                return Equals(charObj);
        }

        public override int GetHashCode()
        {
            return Convert.ToInt32(this.ID);
        }

    }
}
