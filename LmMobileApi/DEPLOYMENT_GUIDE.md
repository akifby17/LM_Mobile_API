# Deployment Guide

## 🚀 Deployment Seçenekleri

Bu rehber, Loom Monitoring API'sini farklı ortamlarda nasıl deploy edeceğinizi gösterir.

---

## 🐳 Docker Deployment

### Dockerfile
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["LmMobileApi.csproj", "./"]
RUN dotnet restore "LmMobileApi.csproj"

# Copy source code
COPY . .
RUN dotnet build "LmMobileApi.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "LmMobileApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install SQL Server tools (optional)
RUN apt-get update && apt-get install -y curl

# Copy published app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 80
EXPOSE 443

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/api/looms/monitoring || exit 1

ENTRYPOINT ["dotnet", "LmMobileApi.dll"]
```

### docker-compose.yml
```yaml
version: '3.8'

services:
  loom-api:
    build: .
    ports:
      - "5038:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=LoomMonitoring;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;MultipleActiveResultSets=true
      - JwtSettings__SecretKey=your-super-secret-key-here-make-it-very-long-and-secure
      - JwtSettings__Issuer=LmMobileApi
      - JwtSettings__Audience=LmMobileApiUsers
      - JwtSettings__ExpiryInMinutes=60
    depends_on:
      - sql-server
    networks:
      - loom-network

  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
      - MSSQL_PID=Express
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    networks:
      - loom-network

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - loom-api
    networks:
      - loom-network

volumes:
  sqlserver_data:

networks:
  loom-network:
    driver: bridge
```

### nginx.conf
```nginx
events {
    worker_connections 1024;
}

http {
    upstream loom-api {
        server loom-api:80;
    }

    server {
        listen 80;
        server_name localhost;

        # Redirect HTTP to HTTPS
        return 301 https://$server_name$request_uri;
    }

    server {
        listen 443 ssl http2;
        server_name localhost;

        # SSL configuration
        ssl_certificate /etc/nginx/ssl/cert.pem;
        ssl_certificate_key /etc/nginx/ssl/key.pem;
        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers HIGH:!aNULL:!MD5;

        # SignalR support
        location /loomsCurrentlyStatus {
            proxy_pass http://loom-api;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection $connection_upgrade;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
            
            # SignalR timeout settings
            proxy_read_timeout 86400;
            proxy_send_timeout 86400;
        }

        # API endpoints
        location /api {
            proxy_pass http://loom-api;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Swagger (sadece development)
        location /swagger {
            proxy_pass http://loom-api;
            proxy_set_header Host $host;
        }
    }

    # WebSocket connection upgrade
    map $http_upgrade $connection_upgrade {
        default Upgrade;
        '' close;
    }
}
```

### Docker Build & Run
```bash
# Build image
docker build -t loom-monitoring-api .

# Run with docker-compose
docker-compose up -d

# Check logs
docker-compose logs -f loom-api

# Stop services
docker-compose down
```

---

## 🏢 IIS Deployment

### web.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\LmMobileApi.dll" 
                  stdoutLogEnabled="false" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
      
      <!-- SignalR için WebSocket desteği -->
      <webSocket enabled="true" />
      
      <!-- CORS headers -->
      <httpProtocol>
        <customHeaders>
          <add name="Access-Control-Allow-Origin" value="*" />
          <add name="Access-Control-Allow-Methods" value="GET, POST, PUT, DELETE, OPTIONS" />
          <add name="Access-Control-Allow-Headers" value="Content-Type, Authorization" />
        </customHeaders>
      </httpProtocol>
      
      <!-- SignalR için URL Rewrite -->
      <rewrite>
        <rules>
          <rule name="SignalR" stopProcessing="true">
            <match url="^loomsCurrentlyStatus/?(.*)" />
            <action type="Rewrite" url="loomsCurrentlyStatus/{R:1}" />
          </rule>
        </rules>
      </rewrite>
    </system.webServer>
  </location>
</configuration>
```

### IIS Kurulum Adımları
```powershell
# 1. IIS özelliklerini etkinleştir
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-NetFxExtensibility45, IIS-ISAPIExtensions, IIS-ISAPIFilter, IIS-DefaultDocument, IIS-DirectoryBrowsing, IIS-StaticContent, IIS-HttpCompressionStatic, IIS-HttpCompressionDynamic, IIS-WebSockets

# 2. .NET 8 Hosting Bundle'ı indir ve kur
# https://dotnet.microsoft.com/download/dotnet/8.0

# 3. Uygulama klasörünü oluştur
New-Item -Path "C:\inetpub\wwwroot\LoomMonitoringApi" -ItemType Directory

# 4. IIS'te site oluştur
Import-Module WebAdministration
New-WebSite -Name "LoomMonitoringApi" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\LoomMonitoringApi"

# 5. Application Pool ayarları
Set-ItemProperty -Path "IIS:\AppPools\LoomMonitoringApi" -Name processModel.identityType -Value ApplicationPoolIdentity
Set-ItemProperty -Path "IIS:\AppPools\LoomMonitoringApi" -Name recycling.periodicRestart.time -Value "00:00:00"
```

### Production appsettings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=LoomMonitoring;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;"
  },
  "JwtSettings": {
    "SecretKey": "your-production-secret-key-must-be-very-long-and-secure-at-least-32-characters",
    "Issuer": "LmMobileApi",
    "Audience": "LmMobileApiUsers", 
    "ExpiryInMinutes": 60
  },
  "DataManApiOptions": {
    "BaseUrl": "http://your-production-dataman-api/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "LmMobileApi.SqlDependencies": "Information",
      "LmMobileApi.Hubs": "Information"
    },
    "EventLog": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*"
}
```

---

## ☁️ Azure Deployment

### Azure App Service
```yaml
# azure-pipelines.yml
trigger:
- main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'
    projects: '**/*.csproj'

- task: DotNetCoreCLI@2
  displayName: 'Build project'
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration)'

- task: DotNetCoreCLI@2
  displayName: 'Publish project'
  inputs:
    command: 'publish'
    projects: '**/*.csproj'
    arguments: '--configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)'
    zipAfterPublish: true

- task: AzureWebApp@1
  displayName: 'Deploy to Azure Web App'
  inputs:
    azureSubscription: 'your-azure-subscription'
    appType: 'webApp'
    appName: 'loom-monitoring-api'
    package: '$(Build.ArtifactStagingDirectory)/**/*.zip'
```

### Azure Configuration
```bash
# Azure CLI ile App Service oluştur
az webapp create \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --name loom-monitoring-api \
  --runtime "DOTNET|8.0"

# WebSocket'leri etkinleştir
az webapp config set \
  --name loom-monitoring-api \
  --resource-group myResourceGroup \
  --web-sockets-enabled true

# Connection string ayarla
az webapp config connection-string set \
  --name loom-monitoring-api \
  --resource-group myResourceGroup \
  --connection-string-type SQLServer \
  --settings DefaultConnection="Server=your-server.database.windows.net;Database=LoomMonitoring;User Id=your-user;Password=your-password;"
```

---

## 🔧 Environment Configuration

### Production Environment Variables
```bash
# Linux/Docker
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=LoomMonitoring;..."
export JwtSettings__SecretKey="your-production-secret-key"
export JwtSettings__Issuer="LmMobileApi"
export JwtSettings__Audience="LmMobileApiUsers"
export JwtSettings__ExpiryInMinutes="60"

# Windows
set ASPNETCORE_ENVIRONMENT=Production
set ConnectionStrings__DefaultConnection=Server=prod-server;Database=LoomMonitoring;...
set JwtSettings__SecretKey=your-production-secret-key
```

### Development vs Production
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "LmMobileApi.SqlDependencies": "Debug",
      "LmMobileApi.Hubs": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LoomMonitoring_Dev;..."
  }
}

// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 📊 Monitoring & Health Checks

### Health Check Endpoint
```csharp
// Program.cs'e ekle
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddSignalR();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Prometheus Metrics (Opsiyonel)
```csharp
// NuGet: prometheus-net.AspNetCore
builder.Services.AddSingleton<IMetricsLogger, MetricsLogger>();

app.UseMetricServer(); // /metrics endpoint
app.UseHttpMetrics();
```

### Application Insights (Azure)
```csharp
// NuGet: Microsoft.ApplicationInsights.AspNetCore
builder.Services.AddApplicationInsightsTelemetry();
```

---

## 🔒 Security Checklist

### Production Security
- [ ] HTTPS zorla etkinleştir
- [ ] Strong JWT secret key kullan (32+ karakter)
- [ ] CORS ayarlarını production domain'lere göre konfigüre et
- [ ] SQL Server authentication güvenli
- [ ] Firewall kuralları ayarla
- [ ] API rate limiting ekle
- [ ] Request size limits ayarla

### Security Headers
```csharp
// Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    await next();
});
```

---

## 🚨 Troubleshooting

### Common Issues

#### SignalR Connection Failed
```bash
# IIS'te WebSocket desteğini kontrol et
Get-WindowsFeature -Name IIS-WebSockets

# Docker'da port mapping kontrolü
docker port container-name
```

#### SQL Dependency Not Working
```sql
-- SQL Server'da Broker servisini kontrol et
SELECT name, is_broker_enabled FROM sys.databases WHERE name = 'LoomMonitoring'

-- Broker'ı etkinleştir
ALTER DATABASE LoomMonitoring SET ENABLE_BROKER
```

#### High Memory Usage
```bash
# .NET GC ayarları
export DOTNET_gcServer=true
export DOTNET_GCRetainVM=1
```

### Performance Tuning
```csharp
// Program.cs - SignalR performance ayarları
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = false; // Production'da false
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
```

---

## 📋 Deployment Checklist

### Pre-Deployment
- [ ] Database migration script'leri hazır
- [ ] Connection string'ler güncellendi
- [ ] JWT secret key production için değiştirildi
- [ ] CORS ayarları production domain'leri içeriyor
- [ ] SSL sertifikası hazır
- [ ] Backup strategy planlandı

### Post-Deployment
- [ ] Health check endpoint'i çalışıyor
- [ ] SignalR bağlantısı test edildi
- [ ] API endpoint'leri test edildi
- [ ] SQL Dependency çalışıyor
- [ ] Logging çalışıyor
- [ ] Performance monitoring aktif

Bu rehber ile API'nizi güvenli ve performanslı şekilde production'a deploy edebilirsiniz!