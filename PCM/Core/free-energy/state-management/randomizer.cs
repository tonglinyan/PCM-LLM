namespace PCM.Core.FreeEnergy.State
{
    public static class Randomizer
    {
        private static readonly Random rand = new();

        public enum DeltaType{
            FixedTime,
            DifferenceCounter
        }
        public class PropertyRandomizer
        {
            public int Agent;
            public double Magnitude;
            public List<(int, int)> Indices;
            public string Property;
            public DeltaType DeltaType;

            public long Delta;
            private long _lastRandomizationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            private double _lastRecordedFe = 0;
            private int differenceCounter = 0;


            public AgentState RandomizeIfNecessary(AgentState agent)
            {
                switch (DeltaType)
                {
                    case DeltaType.FixedTime:
                        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                        if (now - _lastRandomizationTime > Delta)
                        {
                            Console.WriteLine($"Randomizing {Agent} - {Property}");
                            _lastRandomizationTime = now;
                            return Randomize(agent, this);
                        }
                        break;
                        case DeltaType.DifferenceCounter:
                        double fe = (double)agent.stateFE;
                        var diff = Math.Abs(fe - _lastRecordedFe);
                        _lastRecordedFe = fe;
                        if(diff > 0.00001)
                            differenceCounter = 0;
                        else{
                            differenceCounter++;
                            Console.WriteLine(diff+" "+differenceCounter);
                        }
                        if(differenceCounter >= Delta){
                            Console.WriteLine($"Randomizing {Agent} - {Property}");
                            differenceCounter = 0;
                            return Randomize(agent, this);
                        }
                        break;

                }

                return agent;
            }
        }


        private static AgentState Randomize(AgentState agent, PropertyRandomizer pr)
        {
            var resultState = agent.ShallowCopy();
            var indices = pr.Indices;
            var magnitude = pr.Magnitude;
            switch (pr.Property)
            {
                case "preferences":
                    var randomPrefs = Core.Utils.Copy.Copy2DDouble(agent.preferences);
                    Randomize2Darray(randomPrefs, magnitude, 0, 1, indices);

                    resultState.preferences = randomPrefs;
                    break;
                case "tomUpdate":
                    var randomTomUpd = Core.Utils.Copy.Copy2DDouble(agent.tomInfluence);

                    Randomize2Darray(randomTomUpd, magnitude, 0, 1, indices);
                    resultState.tomUpdate = randomTomUpd;
                    break;
                case "tomInfluence":
                    var randomTomInf = Core.Utils.Copy.Copy2DDouble(agent.tomInfluence);

                    Randomize2Darray(randomTomInf, magnitude, 0, 1, indices);

                    resultState.tomInfluence = randomTomInf;
                    break;
                case "mutualLoveStep":
                    var randomMutualLove = Core.Utils.Copy.Copy2DDouble(agent.tomInfluence);

                    Randomize2Darray(randomMutualLove, magnitude, 0, 1, indices);

                    resultState.mutualLoveStep = randomMutualLove;
                    break;
                default:
                    throw new Exception($"Impossible to randomize unknown property :{pr.Property}");
            }

            return resultState;
        }
        private static void Randomize2Darray(double[][] array, double magnitude, double min, double max, List<(int, int)> indices)
        {
            if (indices == null)
                for (var i = 0; i < array.Length; i++)
                {
                    for (var j = 0; j < array[i].Length; j++)
                    {
                        array[i][j] += rand.NextDouble() * (magnitude * 2) - magnitude;
                        array[i][j] = EnsureBetween(array[i][j], min, max);
                    }
                }
            else
                foreach (var tuple in indices)
                {
                    Console.WriteLine(tuple.Item1 + " - " + tuple.Item2);
                    array[tuple.Item1][tuple.Item2] += rand.NextDouble() * (magnitude * 2) - magnitude;
                    array[tuple.Item1][tuple.Item2] = EnsureBetween(array[tuple.Item1][tuple.Item2], min, max);
                }
        }
        private static double EnsureBetween(double value, double min, double max) => value < min ? min : (value > max ? max : value);
    }
}