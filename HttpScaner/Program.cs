using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HttpScaner;

var filename = args[0];
string?[] urls = File.ReadAllLines(filename);
Console.WriteLine($"reading urls from {filename}");
HttpClientHandler clientHandler = new HttpClientHandler();
clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
var tasks = new List<Task<TargetResponse>>();

foreach (string? url in urls)
{
    tasks.Add(Task.Run(async () =>
    {
        using (HttpClient client = new HttpClient(clientHandler))
        {
            var res = new TargetResponse() { Target = url, Status = false };
            try
            {
                if (client != null)
                {
                    var resp = await client.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                    {
                        res.Status = true;
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
                // Console.WriteLine($"{e}");
            }

            return res;
        }
    }));
}


var responses = await Task.WhenAll(tasks.ToArray());


foreach (var r in responses)
{
    Console.WriteLine($"{r.Target} {r.Status}");
}