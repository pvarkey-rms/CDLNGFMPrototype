using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jint;

namespace RMS.Prototype.NGFM
{
    class JintHarness : IJavaScriptHarness
    {
        private Jint.Engine JintEngine;

        public void Construct(params string[] JavascriptSourceFiles)
        {
            JintEngine = new Jint.Engine();
            foreach (string JavascriptSourceFile in JavascriptSourceFiles)
            {
                JintEngine.Execute(System.IO.File.ReadAllText(JavascriptSourceFile));
            }
        }

        public void Destruct()
        {
        }

        public object Execute(string FunctionInvocation)
        {
            return JintEngine.Execute(FunctionInvocation).GetCompletionValue();
        }

        public Dictionary<string, object> Parse(string strCDL)
        {
            return (Dictionary<string, object>)AsPOJO((Jint.Native.JsValue)Execute("grammarAst.parse('" + strCDL + "')"));
        }

        private object AsPOJO(Jint.Native.JsValue JsValue)
        {
            if (JsValue.Type == Jint.Runtime.Types.Boolean)
                return Jint.Runtime.TypeConverter.ToBoolean(JsValue);

            else if (JsValue.Type == Jint.Runtime.Types.Number)
            {
                double AsDouble = Jint.Runtime.TypeConverter.ToNumber(JsValue);
                int AsInt = Jint.Runtime.TypeConverter.ToInt32(JsValue);
                var diff = Math.Abs(AsDouble - AsInt);
                if (diff < 0.00000001)
                    return AsInt;
                else
                    return AsDouble;
            }

            else if (JsValue.Type == Jint.Runtime.Types.String)
                return Jint.Runtime.TypeConverter.ToString(JsValue);

            else // if (JsValue.Type == Jint.Runtime.Types.Object)
            {
                Jint.Native.Object.ObjectInstance AsObject =
                    JsValue.TryCast<Jint.Native.Object.ObjectInstance>();

                if (AsObject is Jint.Native.Array.ArrayInstance)
                {
                    int length = Jint.Runtime.TypeConverter.ToInt32(AsObject.Get("length"));
                    object[] array = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        array[i] = AsPOJO(AsObject.Get(i.ToString()));
                    }
                    return array;
                }

                else
                {
                    Dictionary<string, object> AsDictionary = new Dictionary<string, object>();

                    foreach (string Property in AsObject.Properties.Keys)
                    {
                        AsDictionary.Add(Property, AsPOJO(AsObject.Get(Property)));
                    }

                    return AsDictionary;
                }
            }
        }
    }
}
