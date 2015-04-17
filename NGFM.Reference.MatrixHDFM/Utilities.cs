using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public static class Utilities
    {
        public static float[] ArrayAddition(float[] arr1, float[] arr2)
        {
            int len = arr1.Length;
            if (len != arr2.Length)
                throw new InvalidOperationException("ArrayAddition Error");

            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = arr1[i] + arr2[i];
            }

            return result;
        }

        public static float[] ArraySubtraction(float[] arr1, float[] arr2)
        {
            int len = arr1.Length;
            if (len != arr2.Length)
                throw new InvalidOperationException("ArraySubtraction Error");

            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = arr1[i] - arr2[i];
            }

            return result;
        }

        public static float[] ThreeArraySubtraction(float[] arr1, float[] arr2, float[] arr3)
        {
            int len = arr1.Length;
            if (len != arr2.Length)
                throw new InvalidOperationException("ArraySubtraction Error");

            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = arr1[i] - arr2[i] - arr3[i];
            }

            return result;
        }


        public static float[] ArrayMultiplication(float[] arr1, float[] arr2)
        {
            int len = arr1.Length;
            if (len != arr2.Length)
                throw new InvalidOperationException("ArrayMultiplication Error");

            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = arr1[i] * arr2[i];
            }

            return result;
        }


        public static float[] ArrayDivision(float[] arr1, float[] arr2)
        {
            int len = arr1.Length;
            if (len != arr2.Length)
                throw new InvalidOperationException("ArrayDivision Error");

            for (int i = 0; i < len; i++)
            { 
                if (arr2[i] == 0)
                    throw new InvalidOperationException("ArrayDIvisionError");            
            }
            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = arr1[i]/arr2[i];
            }

            return result;
        }


        public static float[] ArrayPropagate(float[] arr, int[] multiplier)
        {
            int len = arr.Length;
            if (len != multiplier.Length)
                throw new InvalidOperationException("ArrayPropagate Error");

            int newLen = multiplier.Sum();

            float[] result = new float[newLen];

            int k = 0;
            for (int i = 0; i < len; i++)
            {                
                for (int j = k; j < k + multiplier[i]; j++)
                {
                    result[j] = arr[i];
                }
                k += multiplier[i];
            }
            return result;
        }

        public static float[] ConstantSubtractArray(float a, float[] arr)
        {
            int len = arr.Length;
   
            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = a - arr[i];
            }

            return result;
        }

        public static float[] ConstantMultiplyArray(float a, float[] arr)
        {
            int len = arr.Length;

            float[] result = new float[len];

            for (int i = 0; i < len; i++)
            {
                result[i] = a*arr[i];
            }

            return result;
        }


        public static float[] SumArrayByPartition(float[] myArr, int[] partition)
        {
            float[] outArr = new float[partition.Length];
            float[] tempArr = new float[partition[0]];

            Array.Copy(myArr, 0, tempArr, 0, partition[0]);
            outArr[0] = tempArr.Sum();

            for (int i = 0; i < (partition.Length - 2); i++)
            {
                int gapBetween = partition[i + 1] - partition[i];
                outArr[i + 1] = 0; //no element between
                if (gapBetween == 1)
                    outArr[i + 1] = myArr[partition[i]];
                else
                {
                    tempArr = new float[partition[i + 1] - partition[i]];
                    Array.Copy(myArr, partition[i], tempArr, 0, gapBetween);
                    outArr[i + 1] = tempArr.Sum();
                }
            }
            return outArr;
        }

        public static float[] SumArrayByPartitionUsingFor(float[] myArr, int[] partition)
        {
            float[] outArr = new float[partition.Length];

            int kk = 0;
            for (int i = 0; i < partition.Length; i++)
            {
                for (int j = kk; j < partition[i]; j++)
                {
                    outArr[i] += myArr[j];                
                }
                kk += partition[i];                
            }
            return outArr;
        }

    }
}
