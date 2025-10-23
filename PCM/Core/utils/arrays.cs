using System.Numerics;
namespace PCM.Core.Utils
{
    public static class Arrays {
        /// <summary>
        /// In place multiplication of an array by a number
        /// </summary>
        /// <param name="array"></param>
        /// <param name="val"></param>
        public static void IPMult2DArray(double[][] array, double val)
        {
            for (int i = 0; i < array.Length; i++)
                for (int j = 0; j < array[i].Length; j++)
                    array[i][j] *= val;
        }
        /// <summary>
        /// Create a double array from a Vector3
        /// </summary>
        /// <param name="v">vector</param>
        /// <returns></returns>
        public static double[] Vector3ToDoubleArray(Vector3 v)
        {
            double[] res = new double[3];
            res[0] = v.X;
            res[1] = v.Y;
            res[2] = v.Z;
            return res;
        }
        /// <summary>
        /// Create a Vector3 from a double array
        /// </summary>
        /// <param name="v">array</param>
        /// <returns></returns>
        public static Vector3 DoubleArrayToVector3(double[] v)
        {
            if (v.Length != 3)
                throw new Exception("Size need to be 3");
            return new Vector3((float)v[0], (float)v[1], (float)v[2]);
        }

        public static double[] OpArrays(double[] arr1, double[] arr2, Func<double, double, double> f)
        {
            return arr1.Zip(arr2, f).ToArray();
        }
        public static double[] SubArrays(double[] arr1, double[] arr2)
        {
            if (arr1.Length != arr2.Length)
                throw new Exception("Array must be the same size");
            var arr = new double[arr1.Length];
            for (var i = 0; i < arr1.Length; i++)
                arr[i] = arr1[i] - arr2[i];
            return arr;
            //return OpArrays(arr1, arr2, (v1, v2) => v1 - v2);
        }
        public static double[] AddArrays(double[] arr1, double[] arr2)
        {
            //return OpArrays(arr1, arr2, (v1, v2) => v1 + v2);
            var arr = new double[arr1.Length];
            for (var i = 0; i < arr1.Length; i++)
                arr[i] = arr1[i] + arr2[i];
            return arr;
        }
        public static double[] MultArrays(double[] arr1, double[] arr2)
        {
            //return OpArrays(arr1, arr2, (v1, v2) => v1 * v2);
            var arr = new double[arr1.Length];
            for (var i = 0; i < arr1.Length; i++)
                arr[i] = arr1[i] * arr2[i];
            return arr;
        }
        public static double[] DivArrays(double[] arr1, double[] arr2)
        {
            return OpArrays(arr1, arr2, (v1, v2) => v1 / v2);
        }
        
        /// <summary>
        /// Normalize array 
        /// </summary>
        public static IEnumerable<double> NormalizeArray(IEnumerable<double> array)
        {
            var sum = array.Sum();
            return array.Select(v =>
            {
                return v / sum;
            });
        }

        public static double[][] Normalize2DArray(double[][] array)
        {
            return array.Select(v =>
            {
                return NormalizeArray(v).ToArray();
            }).ToArray();
        }

        public static double[][][] Normalize3DArray(double[][][] array)
        {
            return array.Select(v =>
            {
                return Normalize2DArray(v);
            }).ToArray();
        }

        /// <summary>
        /// In place multiplication of an array by a number
        /// </summary>
        /// <param name="array"></param>
        /// <param name="val"></param>
        public static double[] MultArray(double[] array, double val)
        {
            double[] r = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                r[i] = array[i] * val;
            return r;
        }

        public static T[,] To2D<T>(T[][] source)
        {
            try
            {
                int FirstDim = source.Length;
                int SecondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

                var result = new T[FirstDim, SecondDim];
                for (int i = 0; i < FirstDim; ++i)
                    for (int j = 0; j < SecondDim; ++j)
                        result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            } 
        }
    }
}