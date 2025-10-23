namespace PCM.Core.FreeEnergy
{
    /// <summary>
    /// Execute a registered chain of functions (f<T> => <T>) and returns the
    /// last result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FunctionExecutor<T>
    {
        public List<double> _timerValues = new();
        public int _counter = 0;
        private readonly List<Func<T, T>> funcList = new();

        public T Execute(T p)
        {
            var tempP = p;

#if COMPUTE_TIME
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            _counter += 1;
#endif
            var i = 0;
            foreach (var f in funcList)
            {
#if COMPUTE_TIME
                watch.Restart();
#endif
                tempP = f(tempP);
#if COMPUTE_TIME
                _timerValues[i] += watch.Elapsed.TotalMilliseconds;
#endif
                i++;
            }
            return tempP;
        }
        public void AddFunction(Func<T, T> f)
        {
            _timerValues.Add(0);
            funcList.Add(f);
        }
    }
}