using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    class CoverExecutionPosition
    {
        public double[] S_vector { get; private set; }
        public double[] P_vector { get; private set; }

        public int NumBuildings = 1;
        public double Factor = 1.0;
        public double[] FactorArray;

        private double S_coalescence;
        private double P_coalescence;

        private bool coalesced = false;

        #region Coalesced Properties
        public double S 
        {   
            get 
            {
                if (S_vector.Length == 0)
                    return 0.0;
                return S_coalescence;
            }
            set
            {
                if (S_vector.Length > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                S_vector = new double[] { value };
                S_coalescence = value;
            }
        }
        public double P
        {
            get
            {
                if (P_vector.Length == 0)
                    return 0.0;
                return P_coalescence;
            }
            set
            {
                if (P_vector.Length > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                P_vector = new double[] { value };
                P_coalescence = value;
            }
        }
        #endregion

        #region Constructors
        public CoverExecutionPosition() : this(0.0, 0.0) { }
        public CoverExecutionPosition(double s) : this(s, 0.0) { }
        public CoverExecutionPosition(double s, double p) : 
            this(new double[]{s}, new double[]{p}, 1.0) { }
        public CoverExecutionPosition(double s, double p, double f) :
            this(new double[] { s }, new double[] { p }, f) { }
        public CoverExecutionPosition(double[] _S, double _Factor = 1.0, int _NumBuildings = 1)
        {
            if (_S == null)
                throw new NullReferenceException("Illegal construction for cover execution position using null array(s)!");
            Init(_S, Enumerable.Repeat(0.0, _S.Length).ToArray(), _Factor, _NumBuildings);
        }
        public CoverExecutionPosition(double[] _S, double[] _P, double _Factor = 1.0, int _NumBuildings = 1)
        {
            if (_S == null || _P == null)
                throw new NullReferenceException("Illegal construction for cover execution position using null array(s)!");
            if (_S.Length != _P.Length)
                throw new NullReferenceException("Illegal construction for cover execution position using unequal array(s)!");
            Init(_S, _P, _Factor, _NumBuildings);
        }
        private void Init(double[] _S, double[] _P, double _Factor = 1.0, int _NumBuildings = 1)
        {
            this.S_vector = _S;
            this.P_vector = _P;
            this.Factor = _Factor;
            this.FactorArray = Enumerable.Repeat(Factor, S_vector.Length).ToArray();
            this.NumBuildings = _NumBuildings;
        }
        public CoverExecutionPosition(double[] _S, double[] _P, double[] _FactorArray = null, int _NumBuildings = 1)
        {
            if (_S == null || _P == null)
                throw new NullReferenceException("Illegal construction for cover execution position using null array(s)!");
            if (_S.Length != _P.Length)
                throw new NullReferenceException("Illegal construction for cover execution position using unequal array(s)!");
            Init(_S, _P, _FactorArray, _NumBuildings);
        }
        private void Init(double[] _S, double[] _P, double[] _FactorArray = null, int _NumBuildings = 1)
        {
            this.S_vector = _S;
            this.P_vector = _P;
            this.Factor = _FactorArray.Max();
            this.FactorArray = _FactorArray;
            this.NumBuildings = _NumBuildings;
        }

        public CoverExecutionPosition(CoverExecutionPosition CopyFromThis)
        {
            int Count = CopyFromThis.S_vector.Length;
            S_vector = new double[Count];
            P_vector = new double[Count];

            for (int i = 0; i < Count; i++)
            {
                S_vector[i] = CopyFromThis.S_vector[i];
                P_vector[i] = CopyFromThis.P_vector[i];
            }

            Factor = CopyFromThis.Factor;
            NumBuildings = CopyFromThis.NumBuildings;
        }
        #endregion

        public void Coalesce() // @FACTORCHANGES : public void Coalesce_()
        {
            if (coalesced)
                return;

            S_coalescence = S_vector.Zip(FactorArray, (x, y) => x * y).Sum();
            P_coalescence = P_vector.Zip(FactorArray, (x, y) => x * y).Sum();
            // @FACTORARRAY REPLACE
            //S_coalescence = S_vector.Sum() * Factor;
            //P_coalescence = P_vector.Sum() * Factor;

            S_vector = new double[] { S_coalescence };
            P_vector = new double[] { P_coalescence };

            Factor = 1.0;
            FactorArray = new double[] { 1.0 };

            coalesced = true;
        }

        public void Coalesce_()
        {
            if (NumBuildings < S_vector.Length)
                throw new Exception("Number of buildings is lesser than size of multi-building vector!");

            Coalesce(NumBuildings);
        }

        public void Coalesce(int _NumBuildings)
        {
            if (coalesced)
                return;
            if (_NumBuildings == 1)
            {
                S_coalescence = S_vector[0];
                P_coalescence = P_vector[0];
                S_vector = new double[] { S_coalescence };
                P_vector = new double[] { P_coalescence };
                coalesced = true;
                return;
            }
            int TempNumBuildings = _NumBuildings;
            int ComponentsCount = S_vector.Length;
            int OriginalComponentsCount = S_vector.Length;
            for (int i = 0; i < OriginalComponentsCount; i++)
            {
                int NumBuildingsPerComponent = (int)Math.Ceiling((double)TempNumBuildings / (double)ComponentsCount--);
                S_coalescence += (S_vector[i] * NumBuildingsPerComponent);
                P_coalescence += (P_vector[i] * NumBuildingsPerComponent);
                TempNumBuildings -= NumBuildingsPerComponent;
            }
            S_vector = new double[] { S_coalescence };
            P_vector = new double[] { P_coalescence };
            coalesced = true;
        }

        #region Operator Overloads
        public static CoverExecutionPosition operator +(CoverExecutionPosition subject1, CoverExecutionPosition subject2)
        {
            int Count = Math.Max(subject1.S_vector.Length, subject2.S_vector.Length);

            double[] S_Vector_Sum = new double[Count];
            double[] P_Vector_Sum = new double[Count];

            for (int i = 0; i < subject1.S_vector.Length; i++)
            {
                S_Vector_Sum[i] = subject1.S_vector[i];
                P_Vector_Sum[i] = subject1.P_vector[i];
            }

            for (int i = 0; i < subject2.S_vector.Length; i++)
            {
                S_Vector_Sum[i] += subject2.S_vector[i];
                P_Vector_Sum[i] += subject2.P_vector[i];
            }

            double Factor = (subject1.Factor == 1.0) ? subject2.Factor : Math.Max(subject1.Factor, subject2.Factor);
            double[] FactorArray = null;
            if (subject1.FactorArray == null || (subject1.FactorArray.Length < subject2.FactorArray.Length))
                FactorArray = subject2.FactorArray;
            else
            {
                FactorArray = new double[subject1.FactorArray.Length];
                for (int i = 0; i < subject1.FactorArray.Length; i++)
                {
                    if (i >= subject2.FactorArray.Length)
                        FactorArray[i] = subject1.FactorArray[i];
                    else
                        FactorArray[i] = (subject1.FactorArray[i] == 1.0) ? 
                            subject2.FactorArray[i] : Math.Max(subject1.FactorArray[i], subject2.FactorArray[i]);
                }
            }
            //double[] FactorArray = new double[subject1.FactorArray.Length];
            //for (int i = 0; i < subject1.FactorArray.Length; i++)
            //{
            //    FactorArray[i] = (subject1.FactorArray[i] == 1.0) ? 
            //        subject2.FactorArray[i] : Math.Max(subject1.FactorArray[i], subject2.FactorArray[i]);
            //}

            return new CoverExecutionPosition(S_Vector_Sum, P_Vector_Sum,
                FactorArray,
                Math.Max(subject1.NumBuildings, subject2.NumBuildings));
        }
        #endregion
    }
}
