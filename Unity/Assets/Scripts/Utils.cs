using System;
using UnityEngine;

namespace Assets.Scripts
{
    public class Utils
    {
        public static double[] Vector3ToDoubleArray(Vector3 vector)
        {
            return new double[] { vector.x, vector.y, vector.z };
        }

        public static T[] ResizeArray<T>(T[] array, int[] newDimensions, T value)
        {
            int originalSize = array.Length;
            T[] newArray = new T[newDimensions[0]];
            for (int i = 0; i < Math.Min(originalSize, newDimensions[0]); i++)
            {
                newArray[i] = array[i];
            }
            for (int i = originalSize; i < newDimensions[0]; i++)
            {
                newArray[i] = value;
            }
            return newArray;
        }

        public static T[][] ResizeMatrix<T>(T[][] array, int[] newDimensions, T value)
        {
            int originalSize = array.Length;
            T[][] newArray = new T[newDimensions[0]][];
            for (int i = 0; i < Math.Min(originalSize, newDimensions[0]); i++)
            {
                newArray[i] = ResizeArray(array[i], new int[] { newDimensions[1] }, value);
            }
            for (int i = originalSize; i < newDimensions[0]; i++)
            {
                newArray[i] = ResizeArray(new T[] { }, new int[] { newDimensions[1] }, value);
            }
            return newArray;
        }

        public static T[][][] Resize3DArray<T>(T[][][] array, int[] newDimensions, T value)
        {
            int originalSize = array.Length;
            T[][][] newArray = new T[newDimensions[0]][][];
            for (int i = 0; i < Math.Min(originalSize, newDimensions[0]); i++)
            {
                newArray[i] = ResizeMatrix(array[i], new int[] { newDimensions[1], newDimensions[2] }, value);
            }
            for (int i = originalSize; i < newDimensions[0]; i++)
            {
                newArray[i] = ResizeMatrix(new T[][] { }, new int[] { newDimensions[1], newDimensions[2] }, value);
            }
            return newArray;
        }

        public class ReferenceWrapper<T>
        {
            public T Value { get; set; }

            public ReferenceWrapper(T initialValue)
            {
                Value = initialValue;
            }
        }
    }

    public class Matrix
    {
        private double[,] data;
        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            data = new double[rows, columns];
        }

        public Matrix(int rows, int columns, double[] a)
        {
            Rows = rows;
            Columns = columns;
            data = new double[rows, columns];
            int indice = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    data[i, j] = a[indice];
                    indice++;
                }
            }
        }

        public double this[int row, int col]
        {
            get { return data[row, col]; }
            set { data[row, col] = value; }
        }

        public static Matrix Add(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new InvalidOperationException("Matrices must have the same dimensions to be added.");

            Matrix result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            }
            return result;
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {

            if (a.Columns != b.Rows)
                throw new InvalidOperationException("Matrix A's columns must match Matrix B's rows.");

            Matrix result = new Matrix(a.Rows, b.Columns);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    result[i, j] = 0;
                    for (int k = 0; k < a.Columns; k++)
                    {
                        result[i, j] += a[i, k] * b[k, j];
                    }
                }
            }
            return result;
        }

        public static Matrix PointwisedMultiply(Matrix a, Matrix b)
        {

            if (a.Rows != b.Rows || a.Columns != b.Columns)
                throw new InvalidOperationException("Matrices must have the same dimensions to be added.");

            Matrix result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] * b[i, j];
                }
            }
            return result;
        }

        public static double ValuewiseSumUp(Matrix a)
        {
            double result = 0;
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result += a[i, j];
                }
            }
            return result;
        }

        public static Matrix Normalization(Matrix a)
        {
            Matrix result = new Matrix(a.Rows, a.Columns);
            double sum = ValuewiseSumUp(a);
            if (sum == 0) return a;

            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result[i, j] = a[i, j] / sum;
                }
            }
            return result;
        }

        public static double[] MatrixToArray(Matrix matrix)
        {
            double[] data = new double[matrix.Rows * matrix.Columns];
            int indices = 0;
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Columns; j++)
                {
                    data[indices] = matrix[i, j];
                }
            }
            return data;
        }

        public static void PrintMatrix(Matrix matrix)
        {
            string result = "";
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Columns; j++)
                {
                    result += " " + matrix[i, j];
                }
                result += "\n";
            }
            Debug.Log(result);
        }

        public static void PrintArray(double[][] array)
        {
            string result = "Preference \n";
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array[0].Length; j++)
                {
                    result += array[i][j] + "\t";
                }
                result += "\t";
            }
            Debug.Log(result);
        }

        public static void PrintArray1(double[] array, string text)
        {
            string result = text + "\n";
            for (int j = 0; j < array.Length; j++)
            {
                result += array[j] + "\t";
            }

            Debug.Log(result);
        }
    }
}