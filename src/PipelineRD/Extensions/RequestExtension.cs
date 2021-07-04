using Newtonsoft.Json;

using System;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;

namespace PipelineRD.Extensions
{
    public static class RequestExtension
    {
        public static string GenerateHash<TRequest>(this TRequest request, string identifier)
        {
            var requestString = JsonConvert.SerializeObject(request);

            requestString = $"{identifier}: {requestString}";

            var encoding = new ASCIIEncoding();
            var key = encoding.GetBytes("072e77e426f92738a72fe23c4d1953b4");
            var hmac = new HMACSHA1(key);
            var bytes = hmac.ComputeHash(encoding.GetBytes(requestString));
            Console.WriteLine(ByteArrayToString(bytes));
            var result = Convert.ToBase64String(bytes);

            return result;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba);
        }
    }
}