using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ebiz_cm_k8s_api_client.Controllers
{
    [ApiController]
    [Route("pods")]
    public class KubernetesPodsController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly ILogger<KubernetesPodsController> _logger;

        public KubernetesPodsController(IKubernetes kubernetesClient, ILogger<KubernetesPodsController> logger)
        {
            _kubernetesClient = kubernetesClient;
            _logger = logger;
        }

        
        [HttpGet]
        public async Task<object> GetAllPods()
        {
            _logger.LogInformation($"loading all default pods");

            var list = await _kubernetesClient.ListNamespacedPodAsync("default").ConfigureAwait(false);
            
            return new { Successful = list.Items.Any(), Pods = list.Items.Select(p => p.Metadata.Name)};
        }
        
        [HttpGet]
        [Route("{ns}")]
        public async Task<object> GetAllPodsByNamespace(string ns)
        {
            _logger.LogInformation($"loading pods for {ns}");

            var list = await _kubernetesClient.ListNamespacedPodAsync(ns).ConfigureAwait(false);
            
            return new { Successful = list.Items.Any(), Pods = list.Items.Select(p => p.Metadata.Name)};
        }
        
        [HttpGet]
        [Route("{ns}/{podName}")]
        public async Task<object> GetPodByName(string ns, string podName)
        {
            _logger.LogInformation($"loading pod for {ns}/{podName}");

            var list = await _kubernetesClient.ListNamespacedPodAsync(ns).ConfigureAwait(false);
            var pod = list.Items.FirstOrDefault(p => p.Metadata.Name == podName);
            return new { Successful = pod != null, Name = pod?.Metadata.Name, Annotations = pod?.Metadata.Annotations, Spec = pod?.Spec, Status = pod?.Status };
        }
        
        [HttpDelete]
        [Route("{ns}/{podName}")]
        public async Task<object> DeletePodByName(string ns, string podName)
        {
            _logger.LogInformation($"deleting pod for {ns}/{podName}");

            var oldState = await GetPodByName(ns, podName);
            var deletedResponse = await _kubernetesClient.DeleteNamespacedPodWithHttpMessagesAsync(podName, ns).ConfigureAwait(false);

            return new { Successful = deletedResponse.Response.IsSuccessStatusCode, Deleted = oldState };
        }
        
        [HttpPut]
        [Route("{ns}/{podName}/{imageName}")]
        public async Task<object> CreatePodByNameAndImage(string ns, string podName, string imageName)
        {
            _logger.LogInformation("Starting Request!");

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var cmd = (await reader.ReadToEndAsync()).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
            var pod = new V1Pod() {
                ApiVersion = "v1",
                Kind = "Pod",
                Metadata = new V1ObjectMeta {
                    Name = podName
                },
                Spec = new V1PodSpec() {
                    Containers = new List<V1Container>() {
                        new V1Container() {
                            Image = imageName,
                            Command = cmd,
                            ImagePullPolicy = "IfNotPresent",
                            Name = imageName
                        }
                    },
                    RestartPolicy = "Always"
                }
            };
            
            var createdResponse = await _kubernetesClient.CreateNamespacedPodWithHttpMessagesAsync(pod, ns).ConfigureAwait(false);
            
            return new { Successful = createdResponse.Response.IsSuccessStatusCode, Created = await GetPodByName(ns, podName) };
        }
    }
}