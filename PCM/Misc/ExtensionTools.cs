namespace PCM.Misc
{
    static public class ExtensionTools
    {
        private static readonly Random random = new();

        static public T GetRandomElement<T>(this T[] array)
        {
            if ((array == null) || (array.Length == 0)) return default(T);
            return array[random.Next(0, array.Length)];
        }

        static public T GetRandomElement<T>(this List<T> list)
        {
            if ((list == null) || (list.Count == 0)) return default(T);
            return list[random.Next(0, list.Count)];
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
            for (int i = 0; i < firstDimension; i++)
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

            for (int i = array.Length; i < newSize; i++)
            {
                if (i > array.Length)
                {
                    newArray[i] = defaultValue;
                }
            }

            return newArray;
        }

        static public T[][] ExtendMatrix<T>(T[][] matrix, int firstDimension, int secondDimension, T defaultValue)
        {
            T[][] newMatrix = new T[firstDimension][];

            for (int i = 0; i < firstDimension; i++)
            {
                if (i < matrix.Length)
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
    }
}
