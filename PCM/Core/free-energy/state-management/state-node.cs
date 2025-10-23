using static PCM.Core.Utils.Tree;

namespace PCM.Core.FreeEnergy.State
{
    public class AgentStateNode
    {
        public AgentState agentState { get; set; }
        public Actions.Action[] actions { get; set; }
        public double score { get; set; }
        public bool bestNode { get; set; }
        public List<Node<AgentStateNode>> otherAgents = new List<Node<AgentStateNode>> (); 


        public AgentStateNode(AgentState agentState, Actions.Action[] actions)
        {
            this.agentState = agentState;
            this.actions = actions;
            bestNode = false;
        }
        
        public AgentStateNode(Actions.Action[] actions){
            this.actions = actions;
            bestNode = false;
        }

        public void AddOtherAgents(Node<AgentStateNode> agentState){
            otherAgents.Add(agentState);
        }
    }
}