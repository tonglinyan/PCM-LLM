using PCM.Core.FreeEnergy.State;
namespace PCM.Core.Utils
{
    public static class Tree
    {
        public class Node<T>
        {

            public T value;
            public List<Node<T>> children = new();
            private int _totalChildren = 0;
            public Node<T> parent;
            public int depth = 0;
            public int id = -1;

            public int TotalChildren { get => _totalChildren; }
            public int DirectChildren { get => children.Count; }

            public Node(T value)
            {
                this.value = value;
            }

            private void _incrementChildren(){
                _totalChildren += 1;
                parent?._incrementChildren();
            }
            public void AddChild(Node<T> child)
            {
                children.Add(child);
                child.parent = this;
                child.depth = depth + 1;
                _incrementChildren();
            }
            /// <summary>
            /// Returns the lineage of the node.
            /// The first element of the list is the node, the second the father, the third the grandfather etc.
            /// </summary>
            /// <returns>List of ancestors</returns>
            public List<Node<T>> GetLineage()
            {
                var lineage = new List<Node<T>>();
                var node = this;
                while (node != null)
                {
                    lineage.Add(node);
                    node = node.parent;
                }
                return lineage;
            }

            public static List<Node<AgentStateNode>> GetLineageOfBestPath(Node<AgentStateNode> node){
                var lineage = new List<Node<AgentStateNode>>();
                while (node != null)
                {
                    node.value.bestNode = true;
                    lineage.Add(node);
                    node = node.parent;
                }
                return lineage;
            }

            public static int GetIdleCount(Node<Actions.Action[]> node)
            {
                var idleCount = 0;
                while (node != null)
                {
                    foreach(var action in node.value)
                    {
                        var actionType = action.GetActionType();
                        if (actionType == Actions.ActionType.Idle)
                            idleCount++;
                    }
                    node = node.parent;
                }
                return idleCount;
            }
            
            public static int GetIdleCount(Node<AgentStateNode> node)
            {
                var idleCount = 0;
                while (node != null)
                {
                    foreach(var action in node.value.actions)
                    {
                        var actionType = action.GetActionType();
                        if (actionType == Actions.ActionType.Idle)
                            idleCount++;
                    }
                    node = node.parent;
                }
                return idleCount;
            }            

            public Node<T> GetChild(int i) => children[i];

            public Node<T> Clone()
            {
                return new Node<T>(this.value);
            }
        }

        public class WeightedNode<T> : Node<T>
        {
            public double weight;
            public WeightedNode(T value, double weight) : base(value)
            {
                this.weight = weight;
            }

        }
    }
}