using Fiap.FCG.Payment.Functions.Options;
using Fiap.FCG.Payment.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

var host = new HostBuilder()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        cfg.AddEnvironmentVariables();
    })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.Configure<PaymentApiOptions>(context.Configuration.GetSection("PaymentApi"));

        services.AddHttpClient<IPaymentApiClient, PaymentApiClient>((sp, http) =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentApiOptions>>().Value;

            if (string.IsNullOrWhiteSpace(opt.BaseUrl))
                throw new InvalidOperationException("PaymentApi:BaseUrl não configurado.");

            http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    })
    .Build();

host.Run();
