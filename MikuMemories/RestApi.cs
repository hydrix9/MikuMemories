using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace MikuMemories
{
    public class RestApi
    {
        private static SemaphoreSlim _postRequestSemaphore = new SemaphoreSlim(1, 1);

        public static async Task<string> PostRequest(string url, string json)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;

            await _postRequestSemaphore.WaitAsync(); // Acquire the semaphore

            try
            {
                response = await httpClient.PostAsync(url, content);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                _postRequestSemaphore.Release(); // Release the semaphore
                return null;
            }

            _postRequestSemaphore.Release(); // Release the semaphore

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine("Response content:");
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return null;
            }
        }

    }
}