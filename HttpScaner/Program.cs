using System.Collections.Concurrent;
using System.Net;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Information)
        .WriteTo.File("livehosts.txt"))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Warning)
        .WriteTo.File("deadhosts.txt"))
    .CreateLogger();

string filename = args[0];
Console.WriteLine($"--> reading urls from {filename}");

CancellationTokenSource cts = new CancellationTokenSource();
// cts.CancelAfter(TimeSpan.FromSeconds(30));
int cycle = 0;
HttpClientHandler clientHandler = new HttpClientHandler()
{
    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    AllowAutoRedirect = true,
    MaxConnectionsPerServer = 100,
    UseCookies = true,
    CookieContainer = new CookieContainer(25),
    PreAuthenticate = true,
    MaxResponseHeadersLength = 48
};
HttpClient client = new HttpClient(clientHandler);
client.Timeout = TimeSpan.FromSeconds(30);
const int opSize = 30;
client.DefaultRequestHeaders.Add("UserAgent", "PUTIN PIDARAS");

// while (!cts.IsCancellationRequested)
// {
List<Task> tasks = new List<Task>(opSize);
ConcurrentBag<string> live = new();
ConcurrentBag<string> dead = new();
foreach (var group in File.ReadAllLines(filename).Chunk(opSize))
{
    tasks.AddRange(group.Select(url => Task.Run(async () =>
    {
        try
        {
            HttpResponseMessage resp = await client.GetAsync(url, cts.Token);
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                live.Add(url);
                Log.Information("{Url} : OK", url);
            }
            else
            {
                dead.Add(url);
                Log.Warning("{Url} : DEAD", url);
            }

            resp.Dispose();
        }
        catch (Exception)
        {
            dead.Add(url);
            Log.Warning("{Url} : DEAD", url);
        }
    })));

    var results = Task.WhenAll(tasks);
    results.GetAwaiter().OnCompleted(() => { Console.WriteLine($"Completed hosts : {opSize * ++cycle}"); });
    tasks.Clear();
    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
}


// }

Console.WriteLine(">> Done");
cts.Dispose();
// Console.WriteLine(">> Live hosts:");
// foreach (var h in live)
// {
//     Console.WriteLine($"{h} : OK");
// }
//
// Console.WriteLine("<< Live hosts:");
// Console.WriteLine(">> Dead hosts:");
// foreach (var h in dead)
// {
//     Console.WriteLine($"{h} : OK");
// }

// static IEnumerable<IEnumerable<T>> GetUrls<T>(IReadOnlyCollection<T> fullList, int batchSize)
// {
//     int total = 0;
//     while (total < fullList.Count)
//     {
//         yield return fullList.Skip(total).Take(batchSize);
//         total += batchSize;
//     }
// }