using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace RMS.ContractGraphModel
{
    [Serializable]
    [ProtoContract]
    public class TermAllocationPosition
    {
        [ProtoMember(1)]
        public double R;

        [ProtoMember(2)]
        public double D;

        #region Constructors
        public TermAllocationPosition() : this(0.0, 0.0) { }
        public TermAllocationPosition(double r, double d)
        {
            R = r;
            D = d;
        }
        #endregion
    }

    public class TermAllocationPositionVectorized
    {
        [ProtoMember(1)]
        public double[] R_vector;

        [ProtoMember(2)]
        public double[] D_vector;

        public double[] FactorArray;

        #region Constructors
        public TermAllocationPositionVectorized() : this(0.0, 0.0) { }
        public TermAllocationPositionVectorized(double r, double d) : this(new double[]{r}, new double[]{d}, new double[]{1.0}){}
        public TermAllocationPositionVectorized(double[] r) : this(r, Enumerable.Repeat(0.0, r.Length).ToArray(), Enumerable.Repeat(1.0, r.Length).ToArray()) { }
        public TermAllocationPositionVectorized(double[] r, double[] d) : this(r, d, Enumerable.Repeat(1.0, r.Length).ToArray()) { }
        public TermAllocationPositionVectorized(double[] r, double[] d, double[] f)
        {
            R_vector = r;
            D_vector = d;
            FactorArray = f;
        }
        #endregion
    }

    //TODO: merge this class with TermAllocationPosition
    public class TermAllocationPositionVector
    {
        public class Component
        {
            public double R = 0.0;
            public double D = 0.0;
            public Component() : this(0.0, 0.0) { }
            public Component(Component c) : this(c.R, c.D) { }
            public Component(double r, double d)
            {
                R = r;
                D = d;
            }
            #region Operator Overloads
            public static Component operator +(Component component1, Component component2)
            {
                return new Component(component1.R + component2.R, component1.D + component2.D);
            }
            #endregion
        }

        public List<Component> components;
        public int NumBuildings = 1;

        private Component coalescence = null;

        #region API
        public void AddComponent(double r, double d)
        {
            AddComponent(new Component(r, d));
        }
        public void AddComponent(Component c)
        {
            components.Add(c);
            this.Coalesce();
        }
        #endregion

        #region Coalesced Properties
        public double R
        {
            get
            {
                if (components.Count == 0)
                    return 0.0;
                //if (coalescence == null) 
                //    this.Coalesce();
                return coalescence.R;
            }
            set
            {
                if (components.Count > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                components.First().R = value;
                this.Coalesce();
            }
        }
        public double D
        {
            get
            {
                if (components.Count == 0)
                    return 0.0;
                //if (coalescence == null)
                //    this.Coalesce();
                return coalescence.D;
            }
            set
            {
                if (components.Count > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                components.First().D = value;
                this.Coalesce();
            }
        }
        #endregion

        #region Constructors
        // TODO: change to <code>public TermExecutionPosition() : this(new List<Component>()) { }</code>; Test1EQ_AbsD_Franch_CDL is breaking test case
        public TermAllocationPositionVector() : this(0.0, 0.0) { }
        public TermAllocationPositionVector(double r, double d) : this(new Component(r, d)) { }
        public TermAllocationPositionVector(Component component) : this(new List<Component>(1) { component }) { }
        public TermAllocationPositionVector(List<Component> _components, int _NumBuildings = 1)
        {
            this.components = _components;
            this.NumBuildings = _NumBuildings;
        }

        public TermAllocationPositionVector(TermAllocationPositionVector CopyFromThis)
        {
            components = new List<Component>(CopyFromThis.components.Count);
            foreach (Component __component in CopyFromThis.components)
                components.Add(new Component(__component));

            NumBuildings = CopyFromThis.NumBuildings;
        }
        #endregion

        public void Coalesce()
        {
            if (NumBuildings < components.Count)
                throw new Exception("Number of buildings is lesser than size of multi-building vector!");

            coalescence = new Component();
            int TempNumBuildings = NumBuildings;
            int ComponentsCount = components.Count;
            foreach (Component component in components)
            {
                int NumBuildingsPerComponent = (int)Math.Ceiling((double)TempNumBuildings / (double)ComponentsCount--);
                coalescence.R += (component.R * NumBuildingsPerComponent);
                coalescence.D += (component.D * NumBuildingsPerComponent);
                TempNumBuildings -= NumBuildingsPerComponent;
            }
            components = new List<Component> { coalescence };
        }

        #region Operator Overloads
        public static TermAllocationPositionVector operator +(TermAllocationPositionVector subject1, TermAllocationPositionVector subject2)
        {
            List<Component> components = new List<Component>(Math.Max(subject1.components.Count, subject2.components.Count));
            for (int i = 0; i < subject1.components.Count; i++)
            {
                components.Insert(i, new Component(subject1.components[i]));
            }
            for (int i = 0; i < subject2.components.Count; i++)
            {
                if (components.Count <= i)
                    components.Insert(i, new Component(subject2.components[i]));
                else
                    components[i] += subject2.components[i];
            }
            return new TermAllocationPositionVector(components);

            //List<Component> components = subject1.components;
            //foreach (Component component in subject2.components)
            //{
            //    components.Add(component);
            //}
            //return new TermExecutionPosition(components);

            //return new TermExecutionPosition(subject1.S + subject2.S, subject1.D + subject2.D, subject1.X + subject2.X);
        }
        #endregion
    }

    // TODO: public class TermAllocationPositionVectorVectorized
}
