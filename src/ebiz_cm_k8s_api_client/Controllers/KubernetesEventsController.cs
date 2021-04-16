using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using k8s;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ebiz_cm_k8s_api_client.Controllers
{
    [ApiController]
    [Route("events")]
    public class KubernetesEventsController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly ILogger<KubernetesEventsController> _logger;
        
        public KubernetesEventsController(IKubernetes kubernetesClient, ILogger<KubernetesEventsController> logger)
        {
            _kubernetesClient = kubernetesClient;
            _logger = logger;
        }
        
        [HttpGet]
        [Route("{ns}")]
        public async Task<IEnumerable<string>> GetEvents(string ns)
        {
            _logger.LogInformation($"loading events for {ns}");

            var events = await _kubernetesClient.ListNamespacedEventAsync(ns).ConfigureAwait(false);
            return events.Items.Select(e => $"{e.LastTimestamp}: [{e.Type}] {e.Message}");
        }
    }
}