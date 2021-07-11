namespace PipelineRD.Builders
{
    public interface IPipelineServicesBuilder
    {
        void InjectContexts();
        void InjectSteps();
        void InjectPipelines();
        void InjectRequestValidators();
        void InjectPipelineInitializers();
        void InjectPipelineBuilders();
        void InjectAll();
    }
}
