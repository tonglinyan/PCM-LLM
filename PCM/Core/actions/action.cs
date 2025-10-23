namespace PCM.Core.Actions
{
    public enum ActionType
    {
        Walk,
        Rotate,
        Grab,
        LetGo,
        Smile,
        Grimace,
        Idle,
        Stare,
        Spontaneous
    }

    public class Action
    {
        private readonly Func<int, FreeEnergy.State.AgentState, FreeEnergy.State.AgentState> _func;
        private readonly ActionType _type;
        private readonly int _targetId = 1;
        public Action(Func<int, FreeEnergy.State.AgentState, FreeEnergy.State.AgentState> func, ActionType type, int targetId = -1)
        {
            _func = func;
            _type = type;
            _targetId = targetId;
        }
        public FreeEnergy.State.AgentState Execute(int id, FreeEnergy.State.AgentState inputState)
        {
            return _func(id, inputState);
        }
        public ActionType GetActionType() => _type;
        static public ActionType[] CombinedActionToTypes(Action[] combinedAction) => combinedAction.Select(action => action.GetActionType()).ToArray();
        public int GetTargetId() => _targetId;
    }
}