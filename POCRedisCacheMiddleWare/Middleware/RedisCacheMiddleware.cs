using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace POCRedisCacheMiddleWare
{
    public class RedisCacheMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRedisCacheService _redisCacheService;        

        public RedisCacheMiddleware(RequestDelegate next, IRedisCacheService redisCacheService)
        {
            _next = next;
            _redisCacheService = redisCacheService;
        }
        public async Task Invoke(HttpContext context)
        {
            var cacheKey = GenerateCacheKeyFromRequest(context.Request);

            var cachedResponse = await _redisCacheService.GetCachedResponseAsync(cacheKey);


            if (!string.IsNullOrEmpty(cachedResponse))
            {
               // var jsonString = "{\"foo\":1,\"bar\":false}";
                byte[] data = Encoding.UTF8.GetBytes(cachedResponse);
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await context.Response.Body.WriteAsync(data, 0, data.Length);
                //context.Response.Body
                //  context.Response = new HttpResponse { StatusCodes = "200" };
                //var contentResult = new ContentResult
                //{
                //    Content = cachedResponse,
                //    ContentType = "application/json",
                //    StatusCode = 200
                //};
                //context.Response.
                //context.Response = contentResult;
              //  await context.Response.WriteAsync(cachedResponse);
                return;
            }

            //Copy a pointer to the original response body stream
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                //...and use that for the temporary response body
                context.Response.Body = responseBody;

                //Continue down the Middleware pipeline, eventually returning to this class
                await _next(context);
                if (context.Response != null && context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
                {
                    context.Response.Body.Seek(0, SeekOrigin.Begin);

                    //...and copy it into a string
                    // var body = JsonConvert.DeserializeObject<dynamic>(new StreamReader(stream).ReadToEnd());
                    string text = await new StreamReader(context.Response.Body).ReadToEndAsync();
                    var body = JsonConvert.DeserializeObject<dynamic>(text);
                    await _redisCacheService.CacheResponseAsync(cacheKey, body, 600);
                    //We need to reset the reader for the response so that the client can read it.
                    context.Response.Body.Seek(0, SeekOrigin.Begin);
                }
                    //Format the response from the server
                    // var response = await FormatResponse(context.Response);
                    

                //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                await responseBody.CopyToAsync(originalBodyStream);
            }

            //var originalBody = context.Response.Body;

            //var responseBody = new MemoryStream();
            //context.Response.Body = responseBody;

            //await _next(context);

            //if (context.Response != null && context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
            //{
            //    using (Stream stream = context.Response.Body)
            //    {
            //        stream.Seek(0, SeekOrigin.Begin);
            //        var body = JsonConvert.DeserializeObject<dynamic>(new StreamReader(stream).ReadToEnd());
            //        await _redisCacheService.CacheResponseAsync(cacheKey, body, 600);
            //    }
            //}
            //responseBody.Seek(0, SeekOrigin.Begin);
            //await responseBody.CopyToAsync(originalBody);

            //    //Copy a pointer to the original response body stream
            //    var originalBodyStream = context.Response.Body;
            //using (var responseBody = new MemoryStream())
            //{
            //    //...and use that for the temporary response body
            //    context.Response.Body = responseBody;

            //    //Continue down the Middleware pipeline, eventually returning to this class
            //    await _next(context);

            //    //Format the response from the server
            //    var response = await FormatResponse(context.Response);

            //    //TODO: Save log to chosen datastore

            //    //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
            //    await responseBody.CopyToAsync(originalBodyStream);
            //}

            ////   await _redisCacheService.CacheResponseAsync(cacheKey, "testData", 600);
            //if (context.Response != null )
            //{
            //    await _redisCacheService.CacheResponseAsync(cacheKey, context.Response, 600);
            //}

            // await context.Response.WriteAsync("hello");
        }

        //public async Task Invoke(HttpContext context)
        //{
           

        //    var originalBody = context.Response.Body;

        //    var responseBody = new MemoryStream();
        //    context.Response.Body = responseBody;

        //    await _next(context);

        //    if (context.Response != null && context.Response.ContentType != null && context.Response.ContentType.Contains("application/json"))
        //    {
                                   
        //            responseBody.Seek(0, SeekOrigin.Begin);
        //            string json = new StreamReader(responseBody).ReadToEnd();
        //            var body = JsonConvert.DeserializeObject<dynamic>(json);

        //            // If success is false, then change the response status code to 400
        //            if (body.success == false)
        //            {
        //                context.Response.StatusCode = 400;
                       
        //            }                                
        //    }            

        //    responseBody.Seek(0, SeekOrigin.Begin);
        //    await responseBody.CopyToAsync(originalBody);
        //}
   
    private async Task<string> FormatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response, including the status code (e.g. 200, 404, 401, etc.)
            return $"{response.StatusCode}: {text}";
        }
        private static string GenerateCacheKeyFromRequest(HttpRequest request)
        {
            var keyBuilder = new StringBuilder();

            keyBuilder.Append($"{request.Path}");

            foreach (var (key, value) in request.Query.OrderBy(x => x.Key))
            {
                keyBuilder.Append($"|{key}-{value}");
            }

            return keyBuilder.ToString();
        }
    }
}
