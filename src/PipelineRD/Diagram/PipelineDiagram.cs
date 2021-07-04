using DiagramBuilder.Flowcharts;
using DiagramBuilder.Html;

using FluentValidation;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PipelineRD.Diagram
{
    public class PipelineDiagram<TContext> : Pipeline<TContext>, IPipelineDiagram<TContext> where TContext : BaseContext
    {
        public override string Identifier => $"PipelineDiagram<{typeof(TContext).Name}>";

        private readonly DocumentationBuilder _builder;
        private readonly Flowchart _flowchart;
        private readonly IList<NodeR> _nodes;

        public PipelineDiagram(IServiceProvider serviceProvider, DocumentationBuilder documentationBuilder, string requestKey = null)
            : base(serviceProvider, requestKey)
        {
            _builder = documentationBuilder;
            _flowchart = new Flowchart(Identifier);
            _nodes = new List<NodeR>();
        }

        #region AddNext
        public override IPipelineDiagram<TContext> AddNext<TRequestStep>()
        {
            base.AddNext<TRequestStep>();

            var headStepIdentifier = HeadStep().Identifier;
            var node = new Node(headStepIdentifier);
            AddNodeR(node, ENodeType.Next);
            return this;
        }
        #endregion

        public override IPipelineDiagram<TContext> AddValidator<TRequest>(IValidator<TRequest> validator)
        {
            base.AddValidator(validator);
            return this;
        }

        public override IPipelineDiagram<TContext> AddValidator<TRequest>()
        {
            base.AddValidator<TRequest>();
            return this;
        }

        public override RequestStepResult Execute<TRequest>(TRequest request)
            => Execute(request, string.Empty);

        public override RequestStepResult Execute<TRequest>(TRequest request, string idempotencyKey)
        {
            ProcessNodes();

            var customDiagram = new HtmlCustomDiagram(_flowchart);
            customDiagram.AddPreClassDiagram(new HtmlClassDiagram("Request", request));
            _builder.AddDiagram(customDiagram);

            return base.Execute(request, idempotencyKey);
        }

        private void ProcessNodes()
        {
            foreach (var nodeR in _nodes.Where(n => n.Type != ENodeType.When))
            {
                if (HasNextNodeR(nodeR) == false)
                    continue;

                var nextNodeR = NextNodeR(nodeR);

                var node = nodeR.Node;
                var nextNode = nextNodeR.Node;

                if (nextNodeR.Type == ENodeType.When)
                {
                    _flowchart.Connect(nextNode)
                               .With(node, "Sim", NodeLinkType.DottedLineArrow);

                    if (HasNextNodeR(nextNodeR))
                    {
                        var next = NextNodeR(nextNodeR);
                        _flowchart.With(next.Node, "Não", NodeLinkType.DottedLineArrow);
                        _flowchart.Connect(node)
                                    .With(next.Node);
                    }
                }
                else
                {
                    _flowchart.Connect(node)
                                .With(nextNode);
                }
            }
        }

        private bool HasNextNodeR(NodeR currentNode) => NextNodeR(currentNode) != null;

        private NodeR NextNodeR(NodeR currentNode)
        {
            var index = _nodes.IndexOf(currentNode);
            var nextNodeIndex = index + 1;

            if (nextNodeIndex < _nodes.Count())
                return _nodes[nextNodeIndex];

            return null;
        }

        private void AddNodeR(Node node, ENodeType type) 
            => _nodes.Add(new NodeR(node, type));
    }
}
