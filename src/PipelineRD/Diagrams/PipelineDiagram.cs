using DiagramBuilder.Flowcharts;

using Microsoft.Extensions.DependencyInjection;

using FluentValidation;

using Polly;

using System;
using System.Collections.Generic;
using System.Linq;
using DiagramBuilder.Html;
using System.Linq.Expressions;

namespace PipelineRD.Diagrams
{
    public class PipelineDiagram<TContext> : IPipeline<TContext> where TContext : BaseContext
    {
        public string Identifier => $"Pipeline({typeof(TContext).Name})";

        public TContext Context => throw new NotImplementedException();

        public string CurrentRequestStepIdentifier => throw new NotImplementedException();

        public IReadOnlyCollection<IRequestStep<TContext>> Steps => throw new NotImplementedException();

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
        }

        public IPipeline<TContext> EnableRecoveryRequestByHash()
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> DisableRecoveryRequestByHash()
        {
            throw new NotImplementedException();
        }

        public RequestStepResult Execute<TRequest>(TRequest request) where TRequest : IPipelineRequest
            => Execute(request, string.Empty);

        public RequestStepResult Execute<TRequest>(TRequest request, string idempotencyKey) where TRequest : IPipelineRequest
        {
            ProcessNodes();

            _flowchart.SetName($"{Identifier} - {typeof(TRequest).Name}");

            var customDiagram = new HtmlCustomDiagram(_flowchart);
            customDiagram.AddPreClassDiagram(new HtmlClassDiagram("Request", request));
            _builder.AddDiagram(customDiagram);

            return null;
        }

        public RequestStepResult ExecuteFromSpecificRequestStep(string requestStepIdentifier)
        {
            throw new NotImplementedException();
        }

        public RequestStepResult ExecuteNextRequestStep()
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> AddNext<TRequestStep>() where TRequestStep : IRequestStep<TContext>
        {
            var requestStep = (IRequestStep<TContext>)_serviceProvider.GetService<TRequestStep>();
            if (requestStep == null)
            {
                throw new NullReferenceException("[PipelineDiagram] Request step not found.");
            }

            var node = new Node(requestStep.GetType().Name);
            AddNodeR(node, ENodeType.Next);
            return this;
        }

        public IPipeline<TContext> AddValidator<TRequest>(IValidator<TRequest> validator) where TRequest : IPipelineRequest
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> AddValidator<TRequest>() where TRequest : IPipelineRequest
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> WithPolicy(Policy<RequestStepResult> policy)
        {
            throw new NotImplementedException();
        }

        public IPipeline<TContext> When(Expression<Func<TContext, bool>> condition)
        {
            var expressionBody = ExpressionBody(condition);
            AddAtPreviousNodeR(new Node(expressionBody, NodeShapes.Rhombus), ENodeType.When);
            return this;
        }

        public IPipeline<TContext> When<TCondition>()
        {
            var instance = (ICondition<TContext>)_serviceProvider.GetService<TCondition>();
            if (instance == null)
            {
                throw new PipelineException("[PipelineDiagram] Could not find the condition. Try adding to the dependency injection container.");
            }

            var expressionBody = ExpressionBody(instance.When());
            AddNodeR(new Node(expressionBody, NodeShapes.Rhombus), ENodeType.When);
            return this;
        }

        public IPipeline<TContext> AddRollback(IRollbackRequestStep<TContext> rollbackStep)
        {
            if (rollbackStep == null)
            {
                throw new NullReferenceException("[PipelineDiagram] Request step not found.");
            }

            var node = new Node(rollbackStep.GetType().Name);
            AddNodeR(node, ENodeType.Rollback);
            return this;
        }

        public IPipeline<TContext> AddRollback<TRollbackRequestStep>() where TRollbackRequestStep : IRollbackRequestStep<TContext>
        {
            var rollbackStep = (IRollbackRequestStep<TContext>)_serviceProvider.GetService<TRollbackRequestStep>();
            return AddRollback(rollbackStep);
        }

        public IPipeline<TContext> AddFinally<TRequestStep>() where TRequestStep : IRequestStep<TContext>
        {
            throw new NotImplementedException();
        }

        public void ExecuteRollback()
        {
            throw new NotImplementedException();
        }

        private string ExpressionBody<T>(Expression<Func<T, bool>> exp)
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
            foreach (var nodeR in _nodes)
            {
                if (HasNextNodeR(nodeR) == false)
                    continue;

                var nextNodeR = NextNodeR(nodeR);

                var node = nodeR.Node;
                var nextNode = nextNodeR.Node;

                if(nextNodeR.Type == ENodeType.Rollback)
                {
                    _flowchart.Connect(node)
                      .With(nextNode, NodeLinkType.LineArrow);
                }
                else if (nodeR.Type == ENodeType.When)
                {
                    _flowchart.Connect(node)
                        .With(nextNode, "Yes", NodeLinkType.DottedLineArrow);

                    if (HasNextNodeR(nextNodeR))
                    {
                        var next = NextNodeR(nextNodeR);
                        _flowchart.With(next.Node, "No", NodeLinkType.DottedLineArrow);
                        _flowchart.Connect(nextNode)
                            .With(next.Node, NodeLinkType.ThickLineArrow);
                    }
                }
                else if(nodeR.Type == ENodeType.Rollback)
                {
                    //var previousRollbackNodeR = PreviousNodeRByType(nodeR, ENodeType.Rollback);
                    //if(previousRollbackNodeR != null)
                    //{
                    //    _flowchart.Connect(nodeR.Node)
                    //        .With(previousRollbackNodeR.Node, NodeLinkType.ThickLineArrow);
                    //}

                    var previousNode = PreviousNodeR(nodeR);
                    if (previousNode != null)
                    {
                        _flowchart.Connect(previousNode.Node)
                            .With(nextNode, NodeLinkType.ThickLineArrow);
                    }
                }
                else
                {
                    var previousNode = PreviousNodeR(nodeR);
                    if(previousNode != null)
                    {
                        if(previousNode.Type != ENodeType.When)
                        {
                            _flowchart.Connect(node)
                                .With(nextNode);
                        }
                    }
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

        private NodeR PreviousNodeRByType(NodeR currentNode, ENodeType type)
        {
            var index = _nodes.IndexOf(currentNode);

            if (index > 0)
            {
                var previousNodes = _nodes.Take(index)?.Where(x => x.Type == type);
                return previousNodes?.LastOrDefault();
            }

            return null;
        }

        private void AddNodeR(Node node, ENodeType type)
            => _nodes.Add(new NodeR(node, type));

        private void AddAtPreviousNodeR(Node node, ENodeType type)
            => _nodes.Insert(_nodes.Count - 1 > 0 ? _nodes.Count - 1 : 0, new NodeR(node, type));
    }
}
