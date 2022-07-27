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

serviceCollection.UsePipelineRD(x =>
{
    x.AddPipelineServices(y =>
    {
        y.InjectAll();
    });
});

var serviceProvider = serviceCollection.BuildServiceProvider();

var memoryCache = serviceProvider.GetService<IDistributedCache>();

IPipelineRDCacheSettings cacheSettings = new PipelineRDCacheSettings()
{
    KeyPreffix = "pipelinerdsample",
    TTLInMinutes = 5
};

ICacheProvider cacheProvider = new CacheProvider(cacheSettings, memoryCache);

serviceCollection.AddSingleton(cacheProvider);



var idempotencyKey = string.Empty;
var request = new AccountRequest()
{
    Number = 1
};

for (var i = 0; i < 5; i++)
{
    var pipeline = new Pipeline<AccountContext, AccountRequest>(serviceProvider);
    pipeline.EnableCache(cacheProvider);
    pipeline.WithHandler<InitializeAccountHandler>();
    pipeline.WithRecovery<InitializeAccountRecoveryHandler>();
    pipeline.WithHandler<CreateAccountHandler>();
    pipeline.WithRecovery<CreateAccountRecoveryHandler>();
    pipeline.WithPolicy(
        Policy.HandleResult<HandlerResult>(
            x => x.StatusCode == System.Net.HttpStatusCode.BadRequest
        ).WaitAndRetry(3, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt))
    );
    pipeline.WithHandler<FinishAccountHandler>();

    var result = pipeline.Execute(request, idempotencyKey);

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
            //idempotencyKey = val.ToString().Split(':')[2];
        }

    Console.WriteLine(result);
}

