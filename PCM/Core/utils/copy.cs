namespace PCM.Core.Utils
{
    public static class Copy
    {
        public interface ICopyable<T>
        {
            public T Copy();
        }
        public static T[] CopyArray<T>(ICopyable<T>[] array)
        {
            var r = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
                r[i] = array[i].Copy();
            return r;
        }

        public static double[] Copy1DDouble(double[] array)
        {
            var ilen = array.Length;
            var newer = new double[ilen];
            Array.Copy(array, newer, ilen);
            return newer;
        }

        public static T[] Copy1DArray<T>(T[] array){
            var ilen = array.Length;
            var newer = new T[ilen];
            Array.Copy(array, newer, ilen);
            return newer;
        }
        // public static int[] Copy1DInt(int[] array)
        // {
        //     var ilen = array.Length;
        //     var newer = new int[ilen];
        //     Array.Copy(array, newer, ilen);
        //     return newer;
        // }

        public static double[][] Copy2DDouble(double[][] array)
        {
            var len = array.Length;
            var r = new double[len][];
            for (int i = 0; i < len; i++)
            {
                r[i] = Copy1DDouble(array[i]);
            }
            return r;
        }
    }
}