using DiagramBuilder.Flowcharts;

using Microsoft.Extensions.DependencyInjection;

using Polly;

using System;
using System.Collections.Generic;
using System.Linq;
using DiagramBuilder.Html;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PipelineRD.Diagrams
{
    internal class PipelineDiagram<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
        public string Identifier => $"Pipeline({typeof(TContext).Name})";

        public TContext Context => throw new NotImplementedException();

        public string CurrentRequestStepIdentifier => throw new NotImplementedException();

        public IReadOnlyCollection<IStep<TContext>> Steps => throw new NotImplementedException();

        public IServiceProvider GetServiceProvider() => _serviceProvider;

        private readonly DocumentationBuilder _builder;
        private readonly Flowchart _flowchart;
        private readonly IList<NodeR> _nodes;
        private readonly IServiceProvider _serviceProvider;

        public PipelineDiagram(IServiceProvider serviceProvider, DocumentationBuilder documentationBuilder)
        {
            _serviceProvider = serviceProvider;
            _builder = documentationBuilder;
            _flowchart = new Flowchart(Identifier);
            _nodes = new List<NodeR>();

            var node = new Node("Begin", NodeShapes.Circle);
            AddNodeR(node, ENodeType.Next);
        }

        public void SetRequestKey(string requestKey) { }

        public IPipeline<TContext> EnableRecoveryRequestByHash()
        {
            return this;
        }

        public IPipeline<TContext> DisableRecoveryRequestByHash()
        {
            return this;
        }

        public Task<RequestStepResult> Execute<TRequest>(TRequest request)
            => Execute(request, string.Empty);

        public Task<RequestStepResult> Execute<TRequest>(TRequest request, string idempotencyKey)
        {
            var node = new Node("Finish", NodeShapes.Circle);
            AddNodeR(node, ENodeType.Next);

            ProcessNodes();

            _flowchart.SetName($"{Identifier} - {typeof(TRequest).Name}");

            var customDiagram = new HtmlCustomDiagram(_flowchart);
            customDiagram.AddPreClassDiagram(new HtmlClassDiagram("Request", request));
            _builder.AddDiagram(customDiagram);

            return Task.FromResult(new RequestStepResult());
        }

        public Task<RequestStepResult> ExecuteFromSpecificRequestStep(string requestStepIdentifier) => null;

        public Task<RequestStepResult> ExecuteNextRequestStep() => null;

        public IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IStep<TContext>
        {
            var node = new Node(RemoveInterfaceNamePreffix(typeof(TRequestStep)));
            AddNodeR(node, ENodeType.Next);
            return this;
        }

        public IPipeline<TContext> AddNext<TRequestStep>(IStep<TContext> requestStep) where TRequestStep : IStep<TContext>
        {
            var node = new Node(RemoveInterfaceNamePreffix(typeof(TRequestStep)));
            AddNodeR(node, ENodeType.Next);
            return this;
        }

        public IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy)
        {
            var currentNode = _nodes.LastOrDefault(x => x.Type == ENodeType.Next);
            if(currentNode != null)
            {
                currentNode.Node.Text += " - With Policy";
            }
            return this;
        }

        public IPipeline<TContext> WithPolicy(AsyncPolicy<RequestStepResult> policy)
        {
            var currentNode = _nodes.LastOrDefault(x => x.Type == ENodeType.Next);
            if (currentNode != null)
            {
                currentNode.Node.Text += " - With Policy";
            }
            return this;
        }

        public IPipeline<TContext> When(Expression<Func<TContext, bool>> condition)
        {
            var expressionBody = ExpressionBody(condition);
            AddAtPreviousNodeR(new Node(expressionBody, NodeShapes.Rhombus), ENodeType.When);
            return this;
        }

        public IPipeline<TContext> When<TCondition>() where TCondition : ICondition<TContext>
        {
            var instance = (ICondition<TContext>)Activator.CreateInstance(typeof(TCondition));
            var expressionBody = ExpressionBody(instance.When());
            AddNodeR(new Node(expressionBody, NodeShapes.Rhombus), ENodeType.When);
            return this;
        }

        public IPipeline<TContext> AddRollback(IRollbackStep<TContext> rollbackStep)
        {
            var node = new Node(RemoveInterfaceNamePreffix(rollbackStep?.GetType()));
            AddNodeR(node, ENodeType.Rollback);
            return this;
        }

        public IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackStep<TContext>
        {
            var node = new Node(RemoveInterfaceNamePreffix(typeof(TRollbackRequestStep)));
            AddNodeR(node, ENodeType.Next);
            return this;
        }

        public IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IStep<TContext>
        {
            var node = new Node(RemoveInterfaceNamePreffix(typeof(TRequestStep)));
            AddNodeR(node, ENodeType.Next);
            return this;
        }

        public Task ExecuteRollback() { return null; }

        private static string ExpressionBody<T>(Expression<Func<T, bool>> exp)
        {
            string expBody = (exp).Body.ToString();

            var paramName = exp.Parameters[0].Name;
            var paramTypeName = "[Context]"; //exp.Parameters[0].Type.Name;

            expBody = expBody.Replace(paramName + ".", paramTypeName + ".")
                         .Replace("\"", "#quot;")
                         .Replace("AndAlso", "&&").Replace("OrElse", "||");

            return $"\"{expBody}\"";
        }

        private void ProcessNodes()
        {
            foreach(var nodeR in _nodes)
            {
                if (!HasNextNodeR(nodeR))
                    break;

                var nextNodeR = NextNodeR(nodeR);
                var currentNode = nodeR.Node;
                var nextNode = nextNodeR.Node;

                if (nodeR.Type == ENodeType.Rollback)
                {
                    var previousNode = PreviousNodeR(nodeR);
                    if (previousNode != null)
                    {
                        _flowchart.Connect(previousNode.Node)
                            .With(nextNode);
                    }

                    continue;
                }

                if (nodeR.Type == ENodeType.When)
                {
                    _flowchart.Connect(currentNode)
                        .With(nextNode, "Yes", NodeLinkType.DottedLineArrow);

                    if (HasNextNodeR(nextNodeR))
                    {
                        var next = NextNodeR(nextNodeR);
                        _flowchart.With(next.Node, "No", NodeLinkType.DottedLineArrow);
                    }

                    continue;
                }

                if (nextNodeR.Type == ENodeType.Next)
                {
                    _flowchart.Connect(currentNode)
                        .With(nextNode);
                } 
                else if(nextNodeR.Type == ENodeType.Rollback)
                {
                    _flowchart.Connect(currentNode)
                      .With(nextNode, NodeLinkType.ThickLineArrow);
                }
                else if(nextNodeR.Type == ENodeType.When)
                {
                    _flowchart.Connect(currentNode)
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

        private NodeR PreviousNodeR(NodeR currentNode)
        {
            var index = _nodes.IndexOf(currentNode);

            if(index > 0)
            {
                var previousNodeIndex = index - 1;

                if (previousNodeIndex < _nodes.Count())
                    return _nodes[previousNodeIndex];
            }

            return null;
        }

        private void AddNodeR(Node node, ENodeType type)
            => _nodes.Add(new NodeR(node, type));

        private void AddAtPreviousNodeR(Node node, ENodeType type)
            => _nodes.Insert(_nodes.Count - 1 > 0 ? _nodes.Count - 1 : 0, new NodeR(node, type));

        private string RemoveInterfaceNamePreffix(Type type)
        {
            var nodeName = type?.Name;
            return nodeName.FirstOrDefault().ToString() == "I" ? nodeName.Substring(1, nodeName.Length-1) : nodeName;
        }
    }
}
