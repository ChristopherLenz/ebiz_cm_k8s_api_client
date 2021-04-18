using System;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace ebiz_cm_k8s_api_client
{
    internal class MonitorConfigMapsHostedService : Microsoft.Extensions.Hosting.IHostedService, IDisposable
    {
        private readonly string _hostedServiceName;
        private readonly ILogger _logger;
        private readonly IKubernetes _kubernetesClient;

        public MonitorConfigMapsHostedService(ILogger<MonitorConfigMapsHostedService> logger, IKubernetes kubernetesClient)
        {
            _logger = logger;
            _kubernetesClient = kubernetesClient;

            _hostedServiceName = $"Timed Background Service '{nameof(MonitorConfigMapsHostedService)}'";
        }

        public void Dispose()
        {
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_hostedServiceName} is starting.");

            try
            {
                var configMapList = await _kubernetesClient.ListNamespacedConfigMapWithHttpMessagesAsync("ebiz", watch: true);
                configMapList.Watch<V1ConfigMap, V1ConfigMapList>((type, item) =>
                {
                    _logger.LogWarning($"configmap-event trigger: '{item.Metadata.Name}' was '{type}'");
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{_hostedServiceName} error during recurring loader");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{_hostedServiceName} is stopping.");

            return Task.CompletedTask;
        }
    }
}