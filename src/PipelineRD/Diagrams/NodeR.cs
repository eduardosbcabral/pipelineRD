using DiagramBuilder.Flowcharts;

namespace PipelineRD.Diagrams
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
