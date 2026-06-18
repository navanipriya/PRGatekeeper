# Deployment Guide - PR Gatekeeper

## Overview

This guide covers deploying the PR Gatekeeper backend prototype in production environments.

## Prerequisites

- .NET 8.0 SDK
- Docker & Docker Compose (optional)
- 2GB RAM minimum
- HTTPS certificate (for production)

## Local Development

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --configuration Release
```

The API will be available at `http://localhost:5000`.

## Docker Deployment

### Build Image

```bash
docker build -t prgatekeeper:latest .
```

### Run Container

```bash
docker run -d \
  --name prgatekeeper \
  -p 5000:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  prgatekeeper:latest
```

### Docker Compose

```bash
docker-compose up -d
```

## Kubernetes Deployment

### Create Namespace

```bash
kubectl create namespace prgatekeeper
```

### Apply Configuration

```bash
kubectl apply -f k8s/deployment.yaml -n prgatekeeper
kubectl apply -f k8s/service.yaml -n prgatekeeper
```

### Scale

```bash
kubectl scale deployment prgatekeeper --replicas=3 -n prgatekeeper
```

## Configuration

### Environment Variables

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
AgentFramework__EnableDetailedLogging=false
Gatekeeper__ReviewApprovalThreshold=80
```

### appsettings.json

Modify `appsettings.json` for:
- Logging levels
- Execution timeouts
- Retry policies
- State management settings

## Monitoring

### Health Check

```bash
curl http://localhost:5000/api/gatekeeper/health
```

### Metrics Endpoint (future)

```
GET /api/gatekeeper/metrics
```

## Performance Tuning

### Single Instance

- Memory: ~100MB
- CPU: Minimal (< 5% idle)
- Throughput: 100+ concurrent requests

### Multi-Instance (Load Balanced)

- Use distributed cache for state management
- Configure sticky sessions or use external state
- Monitor latency and adjust accordingly

## Troubleshooting

### High Memory Usage

Check for state leaks:
```bash
curl http://localhost:5000/api/gatekeeper/state/pr-{number}
```

Clear state if needed:
```bash
curl -X DELETE http://localhost:5000/api/gatekeeper/state/pr-{number}
```

### Slow Execution

Review execution history:
```bash
curl http://localhost:5000/api/gatekeeper/history/pr-{number}
```

### Agent Failures

Check logs:
```bash
docker logs prgatekeeper
```

## Security

- Enable HTTPS in production
- Use authentication/authorization middleware
- Implement rate limiting
- Secure sensitive configuration with Azure Key Vault or similar
- Enable audit logging for compliance

## Backup & Recovery

State is in-memory. For persistence:
1. Migrate to distributed cache (Redis)
2. Implement periodic snapshots
3. Set up backup schedule

## Support

For issues, refer to:
- [Microsoft Agents Documentation](https://github.com/microsoft/agents)
- Project README
- Issue tracker
