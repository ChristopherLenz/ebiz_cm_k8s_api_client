using System.IO;
using System.Threading.Tasks;
using k8s;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ebiz_cm_k8s_api_client.Controllers
{
    [ApiController]
    [Route("logs")]
    public class KubernetesLogsController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly ILogger<KubernetesLogsController> _logger;
        
        public KubernetesLogsController(IKubernetes kubernetesClient, ILogger<KubernetesLogsController> logger)
        {
            _kubernetesClient = kubernetesClient;
            _logger = logger;
        }
        
        [HttpGet]
        [Route("{ns}/{podName}")]
        public async Task<Stream> GetLogs(string ns, string podName)
        {
            _logger.LogInformation($"loading logs for {ns}/{podName}");

            return await _kubernetesClient.ReadNamespacedPodLogAsync(podName, ns).ConfigureAwait(false);
        }
    }
}