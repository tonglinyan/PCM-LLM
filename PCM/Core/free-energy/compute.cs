using PCM.Core.FreeEnergy.State;

namespace PCM.Core.FreeEnergy
{

    public abstract class FECompute
    {
        protected readonly FunctionExecutor<AgentState> funcExec = new();
        protected FECompute() => Init();
        public AgentState ComputeFOC(AgentState s)
        {
            return funcExec.Execute(s);
        }
        protected abstract void Init();
    }
    
    public class FreeEnergy : FECompute
    {
        public static (double fe, double energ, double entrop) neutralFe;

        private static readonly string[] _dbgFuncNames = new string[]
        {
                "Compute Certainty",
                "Update other preferences",
                "Update self preferences",
                "Update preferences w/ mutual love",
                "Pseudo spatial statistic",
                "Update emotions",
                "State Fe"
        };

        protected override void Init()
        {
            //Precompute the neutral free energy, used later
            var (mvnpd_psi_s_j_m, mvnpd_psi_mu_j) = Utils.ComputeJoints(0);
            neutralFe = Utils.ComputeFe(0.5, 0.5,
                mvnpd_psi_s_j_m, mvnpd_psi_mu_j);
            //define the function chain
            funcExec.AddFunction(StateModifier.ComputeCertaintyUncertainty);
            funcExec.AddFunction(StateModifier.UpdateOtherprefV2);
            funcExec.AddFunction(StateModifier.UpdateSelfPref);
            funcExec.AddFunction(StateModifier.UpdatePreferencesWithMutualLove);
            funcExec.AddFunction(StateModifier.SpatialStatAround);
            funcExec.AddFunction(StateModifier.UpdateEmotions);
            funcExec.AddFunction(StateModifier.ComputeStateFe);
        }

        /// <summary>
        /// Debug - Check execution time for each part of the FE computation
        /// </summary>
        public void _DisplayExecutionTime()
        {
            Console.WriteLine("--- Execution Time ---");
            Console.WriteLine($"Computed FOC {funcExec._counter} times");
            for (int i = 0; i < funcExec._timerValues.Count; i++)
            {
                Console.WriteLine("[" + _dbgFuncNames[i] + "]: " + funcExec._timerValues[i] + "ms");
            }
        }

    }
}