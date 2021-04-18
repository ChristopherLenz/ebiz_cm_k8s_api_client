variable "do_token" {
  type = string
}

variable "github_token" {
  type = string
}

terraform {
    required_version = ">= 0.14.0"

    required_providers {
        digitalocean = {
            source = "registry.terraform.io/digitalocean/digitalocean"
            version = "2.7.0"
        }

        github = {
            source = "registry.terraform.io/integrations/github"
            version = "4.8.0"
        }

        kubernetes = {
            source = "registry.terraform.io/hashicorp/kubernetes"
            version = "2.0.3"
        }
    }
}

# Configure the DigitalOcean Provider
provider "digitalocean" {
  token   = var.do_token
}

provider "github" {
  token = var.github_token
}

provider "kubernetes" {
  host             = digitalocean_kubernetes_cluster.kubernetes_cluster.endpoint
  token            = digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].token
  cluster_ca_certificate = base64decode(
    digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].cluster_ca_certificate
  )
}

# Deploy the actual Kubernetes cluster
resource "digitalocean_kubernetes_cluster" "kubernetes_cluster" {
  name    = "terraform-do-cluster"
  region  = "fra1"
  # https://slugs.do-api.dev/
  version = "1.20.2-do.0"

  tags = ["my-tag"]

  # This default node pool is mandatory
  node_pool {
    name       = "default-pool"
    size       = "s-1vcpu-2gb" # minimum size, list available options with `doctl compute size list`
    auto_scale = false
    node_count = 1
    tags       = ["node-pool-tag"]
    labels = {
      "chris" = "cross"
    }
  }
}

resource "digitalocean_container_registry_docker_credentials" "lenzalot" {
  registry_name = "lenz-a-lot"
  write = true
}

resource "kubernetes_secret" "dockerconfigjson" {
  metadata {
    name = "docker-cfg"
  }

  data = {
    ".dockerconfigjson" = digitalocean_container_registry_docker_credentials.lenzalot.docker_credentials
  }

  type = "kubernetes.io/dockerconfigjson"
}

resource "github_actions_secret" "docker_registry_credentials" {
  repository       = "ebiz_cm_k8s_api_client"
  secret_name      = "docker_registry_credentials"
  plaintext_value  = digitalocean_container_registry_docker_credentials.lenzalot.docker_credentials
}

resource "local_file" "local_docker_registry_credentials" {
    content     = digitalocean_container_registry_docker_credentials.lenzalot.docker_credentials
    filename = "${path.module}/local_docker_registry_credentials.json"
}

resource "github_actions_secret" "kubernetes_token" {
  repository       = "ebiz_cm_k8s_api_client"
  secret_name      = "kubernetes_token"
  plaintext_value  = digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].token
}

resource "github_actions_secret" "kubernetes_config" {
  repository       = "ebiz_cm_k8s_api_client"
  secret_name      = "KUBE_CONFIG"
  plaintext_value  = digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].raw_config
}

resource "local_file" "local_kubernetes_config" {
    content     = digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].raw_config
    filename = "${path.module}/local_kubernetes_config.json"
}

resource "local_file" "local_kubernetes_token" {
    content     = digitalocean_kubernetes_cluster.kubernetes_cluster.kube_config[0].token
    filename = "${path.module}/local_kubernetes_token.json"
}

resource "kubernetes_namespace" "ns_ebiz" {
  metadata {
    annotations = {
      name = "ebiz"
    }

    name = "ebiz"
  }
}

resource "kubernetes_secret" "ebiz_dockerconfigjson" {
  metadata {
    name = "docker-cfg"
    namespace = kubernetes_namespace.ns_ebiz.metadata[0].name
  }

  data = {
    ".dockerconfigjson" = digitalocean_container_registry_docker_credentials.lenzalot.docker_credentials
  }

  type = "kubernetes.io/dockerconfigjson"
}

# ServiceAccount, Role & Binding are needed for Kubernetes API Client
# see: https://github.com/kubernetes-client/csharp/issues/273#issuecomment-492114211
resource "kubernetes_service_account" "serviceAccount_api" {
  metadata {
    name = "open-api-account"
    namespace = kubernetes_namespace.ns_ebiz.metadata[0].name
  }
}

resource "kubernetes_cluster_role" "clusterRole_api" {
  metadata {
    name = "open-api-service-reader"
  }

  rule {
    api_groups = [""]
    resources  = ["namespaces", "pods", "pods/log", "events", "configmaps"]
    verbs      = ["get", "list", "watch"]
  }  
  
  rule {
    api_groups = [""]
    resources  = ["pods", "configmaps"]
    verbs      = ["create", "delete"]
  }
}

resource "kubernetes_cluster_role_binding" "clusterRoleBinding_api" {
  metadata {
    name = "open-api-service-reader"
  }

  role_ref {
    api_group = "rbac.authorization.k8s.io"
    kind      = "ClusterRole"
    name      = kubernetes_cluster_role.clusterRole_api.metadata[0].name
  }
  subject {
    kind      = "ServiceAccount"
    name      = kubernetes_service_account.serviceAccount_api.metadata[0].name
    namespace = kubernetes_namespace.ns_ebiz.metadata[0].name
  }
}