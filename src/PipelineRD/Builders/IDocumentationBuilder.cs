namespace PipelineRD.Builders
{
    public interface IDocumentationBuilder
    {
        void UseStatic(string folder);
        void UsePath(string path);
    }
}
