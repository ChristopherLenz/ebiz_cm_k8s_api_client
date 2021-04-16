using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using k8s;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;

namespace ebiz_cm_k8s_api_client.Controllers
{
    [ApiController]
    [Route("scripting")]
    public class KubernetesScriptingController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly ILogger<KubernetesScriptingController> _logger;
        
        public KubernetesScriptingController(IKubernetes kubernetesClient, ILogger<KubernetesScriptingController> logger)
        {
            _kubernetesClient = kubernetesClient;
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<object> ExecuteCode()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            _logger.LogInformation("Starting Request!");
            return Execute(await reader.ReadToEndAsync());
        }
        
        public object Execute(string code)
        {
            var argsWrapper = new ArgsWrapper {Args = new Dictionary<string, object> {{"KubernetesClient", _kubernetesClient}}};
            var scriptOptions = ScriptOptions.Default.AddReferences(typeof(IKubernetes).Assembly).WithImports("System.Linq", "k8s", "k8s.Models");
            return CSharpScript
                .Create("var KubernetesClient = ((IKubernetes)Args[\"KubernetesClient\"]);", scriptOptions,
                    typeof(ArgsWrapper)).ContinueWith(code).RunAsync(argsWrapper).Result.ReturnValue;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public class ArgsWrapper
        {        
            public Dictionary<string, object> Args = new Dictionary<string, object>();
        }
    }
}