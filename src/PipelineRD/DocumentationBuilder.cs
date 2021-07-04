using DiagramBuilder.Html;
using System.Collections.Generic;
using System.Linq;

namespace PipelineRD
{
    public class DocumentationBuilder
    {
        private readonly IList<HtmlCustomDiagram> _diagrams;

        public DocumentationBuilder()
        {
            _diagrams = new List<HtmlCustomDiagram>();
        }

        public void AddDiagram(HtmlCustomDiagram customDiagram)
        {
            _diagrams.Add(customDiagram);
        }

        public void Compile()
        {
            var path = @"C:\Users\Eduardo\source\repos\docs";
            new HtmlBuilder().BuildDocumentation(path, _diagrams.ToArray());
        }
    }
}