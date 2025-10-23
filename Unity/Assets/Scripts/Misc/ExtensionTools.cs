using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

static public class ExtensionTools
{
    static public T GetRandomElement<T>(this T[] array)
    {
        if ((array == null) || (array.Length == 0)) return default(T);
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    static public T GetRandomElement<T>(this List<T> list)
    {
        if ((list == null) || (list.Count == 0)) return default(T);
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    static public T[] GetFilledArray<T>(int size, T defaultValue)
    {
        T[] result = new T[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = defaultValue;
        }
        return result;
    }

    static public T[][] GetFilledMatrix<T>(int firstDimension, int secondDimension, T defaultValue)
    {
        T[][] result = new T[firstDimension][];
        for(int i = 0; i < firstDimension; i++)
        {
            result[i] = GetFilledArray(secondDimension, defaultValue);
        }
        return result;
    }

    static public T[] ExtendArray<T>(T[] array, int newSize, T defaultValue)
    {
        if (newSize <= array.Length) return array;

        T[] newArray = new T[newSize];
        array.CopyTo(newArray, 0);

        for(int i = array.Length; i < newSize; i++)
        {
            if(i > array.Length)
            {
                newArray[i] = defaultValue;
            }
        }

        return newArray;
    }

    static public T[][] ExtendMatrix<T>(T[][] matrix, int firstDimension, int secondDimension, T defaultValue)
    {
        T[][] newMatrix = new T[firstDimension][];

        for(int i = 0; i < firstDimension; i++)
        {
            if(i < matrix.Length)
            {
                newMatrix[i] = ExtendArray(matrix[i], secondDimension, defaultValue);
            }
            else
            {
                newMatrix[i] = GetFilledArray(secondDimension, defaultValue);
            }
        }

        return newMatrix;
    }

    static private System.Numerics.Vector3 ToUnscaledPCMVector3(this Vector3 vector, bool isSize)
    {
        return new System.Numerics.Vector3()
        {
            X = isSize ? vector.x : -vector.x,
            Y = vector.y,
            Z = vector.z
        };
    }

    static private Vector3 ToUnityVector3(this System.Numerics.Vector3 vector, bool isSize)
    {
        return new Vector3()
        {
            x = isSize ? vector.X : -vector.X,
            y = vector.Y,
            z = vector.Z
        };
    }
    static public double[] ToScaledArray(double[] vector, bool isSize)
    {
        var temp = ToUnscaledArray(vector, isSize);
        return new double[] { temp[0] * 1e2f, temp[1] * 1e2f, temp[2] * 1e2f }; 
    }

    static public double[] ToUnscaledArray(double[] vector, bool isSize)
    {
        return new double[] { isSize ? vector[0] : vector[0], vector[1], vector[2]};
    }

    static public double[] Vector3ToArray(Vector3 vector) {
        return new double[] {vector.x, vector.y, vector.z};
    }

    static public System.Numerics.Vector3 ToScaledPCMVector3(this Vector3 vector, bool isSize)
    {
        return vector.ToUnscaledPCMVector3(isSize) * 1e2f; // meters to cm
    }

    static public Vector3 FromPCMVector3(this System.Numerics.Vector3 vector, bool isSize)
    {
        return vector.ToUnityVector3(isSize) * 1e-2f; // cm to meters
    }

    static public Vector3 FromPCMVector3Unscale(this System.Numerics.Vector3 vector, bool isSize)
    {
        return vector.ToUnityVector3(isSize); // cm to meters
    }

    static public Vector3 FromArrayToVector3(double[] position)
    {
        return new Vector3((float)position[0], (float)position[1], (float)position[2]);
    }

    static public Vector3 VectorBetweenTwoPoints(Vector3 origin, Vector3 target, bool horizontal)
    {
        Vector3 result = target - origin;
        result.y = horizontal ? 0 : result.y;
        result.Normalize();

        return result;
    }

    static public float AngleBetweenVectorAndPoint(Vector3 forward, Vector3 origin, Vector3 target, bool horizontal)
    {
        Vector3 targetForward = VectorBetweenTwoPoints(origin, target, horizontal);
        float a = Vector3.Dot(forward, targetForward);
        return a;
    }

    
    static public Core.Interfacing.Body PCMBodyFromUnityData(Vector3 worldSpaceCenter, Vector3 worldSpaceSize, Vector3 worldSpaceForward, Vector3 worldSpaceOrientationOrigin)
    {
        worldSpaceForward.Normalize();

        System.Numerics.Vector3 pcmCenter = worldSpaceCenter.ToScaledPCMVector3(false);
        System.Numerics.Vector3 pcmSize = worldSpaceSize.ToScaledPCMVector3(true);
        System.Numerics.Vector3 pcmForward = worldSpaceForward.ToUnscaledPCMVector3(false);
        System.Numerics.Vector3 pcmOrientationOrigin = worldSpaceOrientationOrigin.ToScaledPCMVector3(false);

        Core.Interfacing.Body bodyData = new()
        {
            Width = pcmSize.X,
            Height = pcmSize.Y,
            Depth = pcmSize.Z,
            Center = pcmCenter,
            Orientation = pcmForward,
            OrientationOrigin = pcmOrientationOrigin,
        };
        return bodyData;
    }

    static public GameObject DuplicateGameObject(GameObject originalGameObject, params System.Type[] components)
    {
        string newGameObjectName = string.Format("{0} (Duplicate)", originalGameObject.name);
        GameObject newGameObject = new GameObject(newGameObjectName);
        newGameObject.transform.position = originalGameObject.transform.position;
        newGameObject.transform.rotation = originalGameObject.transform.rotation;
        newGameObject.transform.localScale = originalGameObject.transform.localScale;

        foreach (System.Type componentType in components)
        {
            Component originalComponent = originalGameObject.GetComponent(componentType);
            Component newComponent = newGameObject.GetComponent(componentType);
            if(newComponent == null)
            {
                newComponent = newGameObject.AddComponent(componentType);
            }

            FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(newComponent, field.GetValue(originalComponent));
            }
        }

        return newGameObject;
    }

    static public Transform RecursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            else
            {
                Transform found = RecursiveFindChild(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    static public void RepairSkinnedMeshRenderer(SkinnedMeshRenderer target, SkinnedMeshRenderer reference, Transform targetArmature)
    {
        Transform[] targetBones = new Transform[reference.bones.Length];
        for (int i = 0; i < reference.bones.Length; i++)
        {
            string boneName = reference.bones[i].name;
            Transform myBone = RecursiveFindChild(targetArmature, boneName);
            targetBones[i] = myBone;
        }
        target.bones = targetBones;
    }

    static public double MultivariateNormalPDF(Vector3 agentCenter, Vector3 center, Vector3 sigma)
    {
        double[] x = Vector3ToArray(agentCenter);
        double[] mean = Vector3ToArray(center);
        double[,] covariance = {
            { sigma.x, 0.0, 0.0 },
            { 0.0, sigma.y, 0.0 },
            { 0.0, 0.0, sigma.z }
        };

        int k = x.Length; 

        double detSigma = Determinant(covariance);
        double[,] invSigma = Inverse(covariance);
        double[] diff = Subtract(x, mean);

        double quadForm = DotProduct(MatrixVectorMultiply(invSigma, diff), diff);

        double normalization = 1.0 / (Math.Pow(2 * Math.PI, k / 2.0) * Math.Sqrt(detSigma));
        double exponent = Math.Exp(-0.5 * quadForm);

        return normalization * exponent;
    }

    static double[] Subtract(double[] a, double[] b)
    {
        double[] result = new double[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] - b[i];
        }
        return result;
    }

    static double DotProduct(double[] a, double[] b)
    {
        double result = 0.0;
        for (int i = 0; i < a.Length; i++)
        {
            result += a[i] * b[i];
        }
        return result;
    }

    static double[] MatrixVectorMultiply(double[,] matrix, double[] vector)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double[] result = new double[rows];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i] += matrix[i, j] * vector[j];
            }
        }
        return result;
    }

    static double Determinant(double[,] matrix)
    {
        return matrix[0, 0] * (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]) -
               matrix[0, 1] * (matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]) +
               matrix[0, 2] * (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]);
    }

    static double[,] Inverse(double[,] matrix)
    {
        double det = Determinant(matrix);
        if (det == 0) throw new InvalidOperationException("Matrix is singular and cannot be inverted.");

        double[,] result = new double[3, 3];

        result[0, 0] = (matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1]) / det;
        result[0, 1] = (matrix[0, 2] * matrix[2, 1] - matrix[0, 1] * matrix[2, 2]) / det;
        result[0, 2] = (matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1]) / det;

        result[1, 0] = (matrix[1, 2] * matrix[2, 0] - matrix[1, 0] * matrix[2, 2]) / det;
        result[1, 1] = (matrix[0, 0] * matrix[2, 2] - matrix[0, 2] * matrix[2, 0]) / det;
        result[1, 2] = (matrix[0, 2] * matrix[1, 0] - matrix[0, 0] * matrix[1, 2]) / det;

        result[2, 0] = (matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0]) / det;
        result[2, 1] = (matrix[0, 1] * matrix[2, 0] - matrix[0, 0] * matrix[2, 1]) / det;
        result[2, 2] = (matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0]) / det;

        return result;
    }
}

