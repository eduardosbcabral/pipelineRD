# PipelineRD

A chain of responsability pattern implementation in .NET

Supports retry policy, sync and async steps, rollback, pipeline recovery by cache and visual documentation using diagrams.

Check the [wiki](https://github.com/eduardosbcabral/pipelineRD/wiki) for examples and how to use it.

### Installing PipelineRD

You should install [PipelineRD with NuGet](https://www.nuget.org/packages/PipelineRD):
 
    Install-Package PipelineRD
    
Or via the .NET Core command line interface:
    
    dotnet add package PipelineRD
    
Either commands, from Package Manager Console or .NET Core CLI, will download and install MediatR and all required dependencies.
    
PS: The R in the name stands for request and D for diagrams.
