using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace RMS.ContractGraphModel
{
    public class TermExecutionPosition
    {
        public double[] S_vector { get; private set; }
        public double[] D_vector { get; private set; }
        public double[] X_vector { get; private set; }

        public int NumBuildings = 1;
        public double Factor = 1.0;
        public double[] FactorArray;

        private double S_coalescence;
        private double D_coalescence;
        private double X_coalescence;
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
        public double D
        {
            get
            {
                if (D_vector.Length == 0)
                    return 0.0;
                return D_coalescence;
            }
            set
            {
                if (D_vector.Length > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                D_vector = new double[] { value };
                D_coalescence = value;
            }
        }
        public double X
        {
            get
            {
                if (X_vector.Length == 0)
                    return 0.0;
                return X_coalescence;
            }
            set
            {
                if (X_vector.Length > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                X_vector = new double[] { value } ;
                X_coalescence = value;
            }
        }
        #endregion

        #region Constructors
        public TermExecutionPosition() : this(0.0, 0.0, 0.0) { }
        public TermExecutionPosition(double s) : this(s, 0.0, 0.0) { }
        public TermExecutionPosition(double s, double d, double x) : 
            this(new double[]{s}, new double[]{d}, new double[]{x}, 1.0) { }
        public TermExecutionPosition(double[] _S, double _Factor = 1.0, int _NumBuildings = 1)
        {
            if (_S == null)
                throw new NullReferenceException("Illegal construction for term execution position using null array(s)!");
            Init(_S, Enumerable.Repeat(0.0, _S.Length).ToArray(), Enumerable.Repeat(0.0, _S.Length).ToArray(), _Factor, _NumBuildings);
        }
        public TermExecutionPosition(double[] _S, double[] _D, double[] _X, double _Factor = 1.0, int _NumBuildings = 1)
        {
            if (_S == null || _D == null || _X == null)
                throw new NullReferenceException("Illegal construction for term execution position using null array(s)!");
            if (_S.Length != _D.Length || _D.Length != _X.Length)
                throw new NullReferenceException("Illegal construction for term execution position using unequal array(s)!");
            Init(_S, _D, _X, _Factor, _NumBuildings);
        }
        private void Init(double[] _S, double[] _D, double[] _X, double _Factor = 1.0, int _NumBuildings = 1)
        {
            this.S_vector = _S;
            this.D_vector = _D;
            this.X_vector = _X;
            this.Factor = _Factor;
            this.FactorArray = Enumerable.Repeat(Factor, S_vector.Length).ToArray();
            this.NumBuildings = _NumBuildings;
        }
        public TermExecutionPosition(double[] _S, double[] _D, double[] _X, double[] _FactorArray = null, int _NumBuildings = 1)
        {
            if (_S == null || _D == null || _X == null)
                throw new NullReferenceException("Illegal construction for term execution position using null array(s)!");
            if (_S.Length != _D.Length || _D.Length != _X.Length)
                throw new NullReferenceException("Illegal construction for term execution position using unequal array(s)!");
            Init(_S, _D, _X, _FactorArray, _NumBuildings);
        }
        private void Init(double[] _S, double[] _D, double[] _X, double[] _FactorArray = null, int _NumBuildings = 1)
        {
            this.S_vector = _S;
            this.D_vector = _D;
            this.X_vector = _X;
            this.Factor = _FactorArray.Max();
            this.FactorArray = _FactorArray;
            this.NumBuildings = _NumBuildings;
        }

        public TermExecutionPosition(TermExecutionPosition CopyFromThis)
        {
            int Count = CopyFromThis.S_vector.Length;
            S_vector = new double[Count];
            D_vector = new double[Count];
            X_vector = new double[Count];

            for (int i = 0; i < Count; i++)
            {
                S_vector[i] = CopyFromThis.S_vector[i];
                D_vector[i] = CopyFromThis.D_vector[i];
                X_vector[i] = CopyFromThis.X_vector[i];
            }

            Factor = CopyFromThis.Factor;
            FactorArray = CopyFromThis.FactorArray;
            NumBuildings = CopyFromThis.NumBuildings;
        }
        #endregion

        public void Coalesce() // @FACTORCHANGES : public void Coalesce_()
        {
            if (coalesced)
                return;

            S_coalescence = S_vector.Zip(FactorArray, (x, y) => x * y).Sum();
            D_coalescence = D_vector.Zip(FactorArray, (x, y) => x * y).Sum();
            X_coalescence = X_vector.Zip(FactorArray, (x, y) => x * y).Sum();
            // @FACTORARRAY REPLACE
            //S_coalescence = S_vector.Sum() * Factor;
            //D_coalescence = D_vector.Sum() * Factor;
            //X_coalescence = X_vector.Sum() * Factor;

            S_vector = new double[] { S_coalescence };
            D_vector = new double[] { D_coalescence };
            X_vector = new double[] { X_coalescence };

            Factor = 1.0;
            FactorArray = new double[] { 1.0 };

            coalesced = true;
        }

        public void Coalesce_() // @FACTORCHANGES : public void Coalesce()
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
                D_coalescence = D_vector[0];
                X_coalescence = X_vector[0];
                S_vector = new double[] { S_coalescence };
                D_vector = new double[] { D_coalescence };
                X_vector = new double[] { X_coalescence };
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
                D_coalescence += (D_vector[i] * NumBuildingsPerComponent);
                X_coalescence += (X_vector[i] * NumBuildingsPerComponent);
                TempNumBuildings -= NumBuildingsPerComponent;
            }
            S_vector = new double[]{S_coalescence};
            D_vector = new double[]{D_coalescence};
            X_vector = new double[]{X_coalescence};
            coalesced = true;
        }

        #region Operator Overloads
        // TODO: must only support equal sized vectors or vector of size 1 added to any other vector
        public static TermExecutionPosition operator +(TermExecutionPosition subject1, TermExecutionPosition subject2)
        {
            int Count = Math.Max(subject1.S_vector.Length, subject2.S_vector.Length);

            double[] S_Vector_Sum = new double[Count];
            double[] D_Vector_Sum = new double[Count];
            double[] X_Vector_Sum = new double[Count];

            for (int i = 0; i < subject1.S_vector.Length; i++)
            {
                S_Vector_Sum[i] = subject1.S_vector[i];
                D_Vector_Sum[i] = subject1.D_vector[i];
                X_Vector_Sum[i] = subject1.X_vector[i];
            }

            for (int i = 0; i < subject2.S_vector.Length; i++)
            {
                S_Vector_Sum[i] += subject2.S_vector[i];
                D_Vector_Sum[i] += subject2.D_vector[i];
                X_Vector_Sum[i] += subject2.X_vector[i];
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
                        FactorArray[i] = (subject1.FactorArray[i] == 1.0) ? subject2.FactorArray[i] : Math.Max(subject1.FactorArray[i], subject2.FactorArray[i]);
                }
            }

            return new TermExecutionPosition(S_Vector_Sum, D_Vector_Sum, X_Vector_Sum,
                FactorArray, 
                Math.Max(subject1.NumBuildings, subject2.NumBuildings));
        }
        #endregion
    }
}
