namespace PCM.Core.Actions
{
    public class Goal
    {
        //action that simulates the agent once the goal has been achieved
        public Action simulatedAction;
        //action that simulates the agent approaching its goal
        public Action stepAction;

        public Goal Copy()
        {
            Goal newGoal = new()
            {
                simulatedAction = simulatedAction,
                stepAction = stepAction
            };
            return newGoal;
        }
    }
}