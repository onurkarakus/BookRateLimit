//Program.cs Sýnýfýmýz
using System.Threading.RateLimiting;

namespace BookRateLimit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Rate Limit Tanýmlarý
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpcontext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpcontext.Request.Headers.Host.ToString(),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(1),
                        //Kuyruk bilgileri
                        QueueLimit = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }
                ));

                //Eklenen Policy Bilgileri

                //options.AddPolicy("User", httpContext =>
                //RateLimitPartition.GetFixedWindowLimiter(httpContext.Request.Headers.Host.ToString(),
                //partition => new FixedWindowRateLimiterOptions
                //{
                //    AutoReplenishment = true,
                //    PermitLimit = 10,
                //    Window = TimeSpan.FromMinutes(1)                    
                //}));

                //options.AddPolicy("Auth", httpContext =>
                //RateLimitPartition.GetFixedWindowLimiter(httpContext.Request.Headers.Host.ToString(),
                //partition => new FixedWindowRateLimiterOptions
                //{
                //    AutoReplenishment = true,
                //    PermitLimit = 5,
                //    Window = TimeSpan.FromMinutes(1)
                //}));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        await context.HttpContext.Response.WriteAsync(
                            $"Ýstek sýnýr sayýsýna ulaþtýnýz. {retryAfter.TotalMinutes} dakika sonra tekrar deneyiniz. ", cancellationToken: token);
                    }
                    else
                    {
                        await context.HttpContext.Response.WriteAsync(
                            "Ýstek sýnýrýna ulaþtýnýz. Daha sonra tekrar deneyin. ", cancellationToken: token);
                    }
                };
            });

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            //Tanýmladýðýmýz özellikler ile rate limit middleware aktif edilir.
            app.UseRateLimiter();

            app.MapControllers();

            app.Run();
        }
    }
}