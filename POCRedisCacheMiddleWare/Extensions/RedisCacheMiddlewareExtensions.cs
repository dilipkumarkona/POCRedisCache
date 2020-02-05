using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace POCRedisCacheMiddleWare
{
   public static class RedisCacheMiddlewareExtensions
    {
        public static IApplicationBuilder UseRedisCacheMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RedisCacheMiddleware>();
        }
        public static IServiceCollection ConfigureRedisCache(this IServiceCollection services, Action<RedisCacheSettings> redisCacheSettingspAction)
        {
            var redisCacheSettings = new RedisCacheSettings();

            redisCacheSettingspAction.Invoke(redisCacheSettings);

            services.AddSingleton(factory => redisCacheSettings);
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisCacheSettings.ConnectionString));
            services.AddStackExchangeRedisCache(options => options.Configuration = redisCacheSettings.ConnectionString);

            services.AddSingleton<IRedisCacheService, RedisCacheService>();

            return services;
        }
    }
}
