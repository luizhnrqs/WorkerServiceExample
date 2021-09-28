using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ServiceConfigurations _serviceConfigurations;

        public Worker(ILogger<Worker> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceConfigurations = new ServiceConfigurations();
            new ConfigureFromConfigurationOptions<ServiceConfigurations>(
                configuration.GetSection("ServiceConfigurations"))
                    .Configure(_serviceConfigurations);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Worker executando em: {DateTimeOffset.Now}");

                for(int i = 0;i < _serviceConfigurations.Iteracoes; i++)
                {
                    var consulta = new Consulta();
                    consulta.Horario =
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            var response = await client.GetAsync(@"https://www.boredapi.com/api/activity");
                            consulta.Resposta = await response.Content.ReadAsStringAsync();

                            _logger.LogInformation($"Consulta realizada com sucesso.");
                        }
                    }
                    catch (Exception ex)
                    {
                        consulta.Resposta = "Exception";
                        consulta.Exception = ex;
                    }

                    string jsonResultado = JsonConvert.SerializeObject(consulta);
                    if (consulta.Exception == null)
                        _logger.LogInformation(jsonResultado);
                    else
                        _logger.LogError(jsonResultado);
                }                

                await Task.Delay(
                    _serviceConfigurations.Intervalo, stoppingToken);
            }
        }
    }
}
