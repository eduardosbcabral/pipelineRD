using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using PipelineRD.Sample;

using Polly;

using System.Collections;
using System.Reflection;

using PipelineRD;
using PipelineRD.Cache;
using PipelineRD.Extensions;

IServiceCollection serviceCollection = new ServiceCollection();

//serviceCollection.AddScoped<AccountContext>();
//serviceCollection.AddScoped<InitializeAccountHandler>();
//serviceCollection.AddScoped<CreateAccountHandler>();
//serviceCollection.AddScoped<FinishAccountHandler>();
//serviceCollection.AddScoped<InitializeAccountRecoveryHandler>();
//serviceCollection.AddScoped<CreateAccountRecoveryHandler>();

serviceCollection.AddDistributedMemoryCache();

var serviceProvider = serviceCollection.BuildServiceProvider();
var memoryCache = serviceProvider.GetService<IDistributedCache>();

serviceCollection.UsePipelineRD(x =>
{
    x.SetupPipelineServices(y =>
    {
        y.InjectAll();
    });

    x.SetupCache(new PipelineRDCacheSettings()
    {
        KeyPreffix = "pipelinerdsample",
        TTLInMinutes = 5
    });
});


var idempotencyKey = string.Empty;
var request = new AccountRequest()
{
    Number = 1
};

for (var i = 0; i < 5; i++)
{
    var pipeline = new Pipeline<AccountContext, AccountRequest>(serviceProvider);
    pipeline.EnableCache();
    pipeline.WithHandler<InitializeAccountHandler>();
    pipeline.WithRecovery<InitializeAccountRecoveryHandler>();
    pipeline.WithHandler<CreateAccountHandler>();
    pipeline.WithRecovery<CreateAccountRecoveryHandler>();
    var policy = Policy.HandleResult<HandlerResult>(x => x.StatusCode == System.Net.HttpStatusCode.BadRequest)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));
    pipeline.WithPolicy(policy);
    pipeline.WithHandler<FinishAccountHandler>();

    var result = await pipeline.Execute(request, idempotencyKey);

    var memCacheField = typeof(MemoryDistributedCache).GetField("_memCache", BindingFlags.NonPublic | BindingFlags.Instance);
    var memCacheValue = memCacheField.GetValue(memoryCache);
    var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
    var collection = field.GetValue(memCacheValue) as ICollection;
    var items = new List<string>();
    if (collection != null)
        foreach (var item in collection)
        {
            var methodInfo = item.GetType().GetProperty("Key");
            var val = methodInfo.GetValue(item);
            items.Add(val.ToString());
            Console.WriteLine(val.ToString());
            request.Number = 2;
            idempotencyKey = val.ToString().Split(':')[2];
        }

    Console.WriteLine(result);
}

