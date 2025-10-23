namespace PCM.Core.FreeEnergy
{
    /// <summary>
    /// Constants used for the simulation
    /// </summary>
    public static class Constants
    {
        public static int nSigma = 100;
        public static int nPsyS = 1000;
        public static double SpatialStat_ClippingDistance = 10;
        public static double SpatialStat_GainFactor = 420; // The gain factor should be relative to total volume 4/3*PI*ClippingDistance^3 > Obsolete
        public static double sevuncertaingain = 3;
        public static double sensoryEvidenceUpdateWeight = 1;
        public static double sensoryEvidenceUpdateWeightForPlayer = 10;
        public static double neutralref = 0.5;
        public static double speed = 0.33;
        public static int depth = 1;
        public static string algSelection = "best";
        public static double minDist = 60; // Used to prevent an agent from getting too close to an entity. Simulates a circle collider
        public static bool goalPredict = false; // If true, predictions will take place using the goal prediction algorithm
    }
}