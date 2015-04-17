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
    public class TermExecutionPosition_ObsoleteV1
    {
        [Serializable]
        [ProtoContract]
        public class Component
        {
            [ProtoMember(1)]
            public double S = 0.0;

            [ProtoMember(2)]
            public double D = 0.0;

            [ProtoMember(3)]
            public double X = 0.0;

            public Component() : this(0.0, 0.0, 0.0) { }
            public Component(Component c) : this(c.S, c.D, c.X) { }
            public Component(double s, double d, double x)
            {
                S = s;
                D = d;
                X = x;
            }
            #region Operator Overloads
            public static Component operator +(Component component1, Component component2)
            {
                return new Component(component1.S + component2.S, component1.D + component2.D, component1.X + component2.X);
            }
            #endregion
        }

        [ProtoMember(4)]
        public List<Component> components;

        [ProtoMember(5)]
        public int NumBuildings = 1;

        [ProtoMember(6)]
        private Component coalescence = null;
        
        #region Coalesced Properties
        public double S 
        {   
            get 
            {
                if (components.Count == 0)
                    return 0.0;
                //if (coalescence == null) 
                //    this.Coalesce();
                return coalescence.S;
            }
            set
            {
                if (components.Count > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                components.First().S = value;
                //this.Coalesce();
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
                //this.Coalesce();
            }
        }
        public double X
        {
            get
            {
                if (components.Count == 0)
                    return 0.0;
                //if (coalescence == null)
                //    this.Coalesce();
                return coalescence.X;
            }
            set
            {
                if (components.Count > 1)
                    throw new Exception("Cannot set execution position property if more than 1 component!");
                components.First().X = value;
                //this.Coalesce();
            }
        }
        #endregion

        #region Constructors
        // TODO: change to <code>public TermExecutionPosition() : this(new List<Component>()) { }</code>; Test1EQ_AbsD_Franch_CDL is breaking test case
        public TermExecutionPosition_ObsoleteV1() : this(0.0, 0.0, 0.0) { }
        public TermExecutionPosition_ObsoleteV1(double s, double d, double x) : this(new Component(s, d, x)) { }
        public TermExecutionPosition_ObsoleteV1(Component component) : this(new List<Component>(1) { component }) { }
        public TermExecutionPosition_ObsoleteV1(List<Component> _components, int _NumBuildings = 1)
        {
            this.components = _components;
            this.NumBuildings = _NumBuildings;
        }

        public TermExecutionPosition_ObsoleteV1(TermExecutionPosition_ObsoleteV1 CopyFromThis)
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

            Coalesce(NumBuildings);
        }

        public void Coalesce(int _NumBuildings)
        {
            if (coalescence != null)
                return;
            coalescence = new Component();
            int TempNumBuildings = _NumBuildings;
            int ComponentsCount = components.Count;
            foreach (Component component in components)
            {
                int NumBuildingsPerComponent = (int)Math.Ceiling((double)TempNumBuildings / (double)ComponentsCount--);
                coalescence.S += (component.S * NumBuildingsPerComponent);
                coalescence.D += (component.D * NumBuildingsPerComponent);
                coalescence.X += (component.X * NumBuildingsPerComponent);
                TempNumBuildings -= NumBuildingsPerComponent;
            }
            components = new List<Component> { coalescence };
        }

        #region Operator Overloads
        public static TermExecutionPosition_ObsoleteV1 operator +(TermExecutionPosition_ObsoleteV1 subject1, TermExecutionPosition_ObsoleteV1 subject2)
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
            return new TermExecutionPosition_ObsoleteV1(components, Math.Max(subject1.NumBuildings, subject2.NumBuildings));

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
}
