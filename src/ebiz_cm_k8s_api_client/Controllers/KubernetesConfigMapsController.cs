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
    [Route("configMaps")]
    public class KubernetesConfigMapsController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly ILogger<KubernetesConfigMapsController> _logger;

        public KubernetesConfigMapsController(IKubernetes kubernetesClient, ILogger<KubernetesConfigMapsController> logger)
        {
            _kubernetesClient = kubernetesClient;
            _logger = logger;
        }

        
        [HttpGet]
        public async Task<object> GetAllConfigMaps()
        {
            _logger.LogInformation($"loading all config-maps");

            var list = await _kubernetesClient.ListConfigMapForAllNamespacesAsync().ConfigureAwait(false);
            
            return new { Successful = list.Items.Any(), ConfigMaps = list.Items.Select(p => p.Metadata.Name)};
        }
        
        [HttpGet]
        [Route("{ns}")]
        public async Task<object> GetAllConfigMapsByNamespace(string ns)
        {
            _logger.LogInformation($"loading configMaps for {ns}");

            var list = await _kubernetesClient.ListNamespacedConfigMapAsync(ns).ConfigureAwait(false);
            
            return new { Successful = list.Items.Any(), ConfigMaps = list.Items.Select(p => p.Metadata.Name)};
        }
        
        [HttpGet]
        [Route("{ns}/{configMapName}")]
        public async Task<object> GetConfigMapByName(string ns, string configMapName)
        {
            _logger.LogInformation($"loading configMap for {ns}/{configMapName}");

            var configMap = await _kubernetesClient.ReadNamespacedConfigMapAsync(configMapName, ns).ConfigureAwait(false);
            return new { Successful = configMap != null, Name = configMap?.Metadata.Name, Annotations = configMap?.Metadata.Annotations, Data = configMap?.Data };
        }
        
        [HttpDelete]
        [Route("{ns}/{configMapName}")]
        public async Task<object> DeleteConfigMapByName(string ns, string configMapName)
        {
            _logger.LogInformation($"deleting configMap for {ns}/{configMapName}");

            var oldState = await GetConfigMapByName(ns, configMapName);
            var deletedResponse = await _kubernetesClient.DeleteNamespacedConfigMapWithHttpMessagesAsync(configMapName, ns).ConfigureAwait(false);

            return new { Successful = deletedResponse.Response.IsSuccessStatusCode, Deleted = oldState };
        }
        
        [HttpPut]
        [Route("{ns}/{configMapName}/{value}")]
        public async Task<object> CreateConfigMapByNameAndImage(string ns, string configMapName, string value)
        {
            _logger.LogInformation($"creating configMap for {ns}/{configMapName}");

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var configMap = new V1ConfigMap() {
                ApiVersion = "v1",
                Kind = "ConfigMap",
                Metadata = new V1ObjectMeta {
                    Name = configMapName
                },
                Data = new Dictionary<string, string>(){{"key", value}}
            };
            
            var createdResponse = await _kubernetesClient.CreateNamespacedConfigMapWithHttpMessagesAsync(configMap, ns).ConfigureAwait(false);
            
            return new { Successful = createdResponse.Response.IsSuccessStatusCode, Created = await GetConfigMapByName(ns, configMapName) };
        }
    }
}