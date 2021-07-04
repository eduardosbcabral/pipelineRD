using DiagramBuilder.Flowcharts;

namespace PipelineRD.Diagram
{
    internal class NodeR
    {
        public Node Node { get; set; }
        public ENodeType Type { get; set; }

        public NodeR(Node node, ENodeType type)
        {
            Node = node;
            Type = type;
        }
    }
}
