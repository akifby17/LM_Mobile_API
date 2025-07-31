# Loom Monitoring API 🏭

Tekstil fabrikaları için geliştirilmiş gerçek zamanlı dokuma tezgahı izleme sistemi.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--time-blue)](https://docs.microsoft.com/en-us/aspnet/signalr/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-red)](https://www.microsoft.com/en-us/sql-server/)

## 🚀 Özellikler

- ⚡ **Real-time İzleme**: SignalR ile anlık tezgah durumu güncellemeleri
- 🔍 **Gelişmiş Filtreleme**: Hall, Mark, Group, Model, Class, Event bazlı filtreleme
- 🔐 **JWT Authentication**: Güvenli API erişimi
- 📊 **SQL Dependency**: Veritabanı değişikliklerini otomatik dinleme  
- 🧵 **Thread-Safe**: Concurrent operations desteği
- 🏗️ **Clean Architecture**: SOLID prensiplerine uygun mimari

## 📋 Gereksinimler

- .NET 8.0 SDK
- SQL Server 2019+
- IIS 10+ veya Docker (deployment için)

## ⚡ Hızlı Başlangıç

### 1. Repository'yi Clone Edin
```bash
git clone https://github.com/your-repo/loom-monitoring-api.git
cd loom-monitoring-api
```

### 2. Database Setup
```sql
-- SQL Server'da database oluşturun
CREATE DATABASE LoomMonitoring

-- Broker servisini etkinleştirin
ALTER DATABASE LoomMonitoring SET ENABLE_BROKER
```

### 3. Configuration
`appsettings.json` dosyasını düzenleyin:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LoomMonitoring;Trusted_Connection=true;..."
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here-make-it-long-and-secure"
  }
}
```

### 4. Çalıştırın
```bash
dotnet restore
dotnet run
```

API: `http://localhost:5038`  
SignalR Hub: `http://localhost:5038/loomsCurrentlyStatus`  
Swagger: `http://localhost:5038/swagger`

## 🔌 API Kullanımı

### REST Endpoints
```http
GET  /api/looms/monitoring           # Tüm tezgahlar
GET  /api/looms/filters             # Filtre seçenekleri  
POST /api/looms/filtered            # Filtreli tezgahlar
POST /api/looms/changeWeaver        # Dokuyucu değiştir
```

### SignalR Events
```javascript
// Bağlantı
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/loomsCurrentlyStatus")
    .build();

// Event listener - tek event her şey için
connection.on("FilteredLoomsDataChanged", (data) => {
    if (data.looms.length === 1) {
        console.log("🔄 Tezgah değişikliği:", data.looms[0]);
    } else {
        console.log("📊 Tüm veriler:", data.looms);
    }
    // Filtreler her durumda güncellenir
    console.log("🔍 Filtreler:", data.filters);
});

// Filtre uygula
await connection.invoke("SubscribeToFilter", JSON.stringify({
    "HallName": "Hall1",
    "MarkName": "Mark1"
}));
```

## 🔍 Filtre Sistemi

### Filtre Tipleri
- `HallName` - Salon adı
- `MarkName` - Marka adı
- `GroupName` - Grup adı
- `ModelName` - Model adı
- `ClassName` - Sınıf adı
- `EventNameTR` - Olay adı

### Filtre Davranışı
1. **İlk Bağlantı**: Tüm tezgahlar + filtre seçenekleri (`FilteredLoomsDataChanged`)
2. **Filtre Seçimi**: Filtreye uyan TÜM tezgahlar + filtreler (`FilteredLoomsDataChanged`)
3. **Değişiklik**: Sadece değişen tezgah(lar) + güncel filtreler (`FilteredLoomsDataChanged`)

## 📱 Client Örnekleri

### JavaScript
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/loomsCurrentlyStatus", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();

await connection.start();
```

### C# Console
```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5038/loomsCurrentlyStatus")
    .Build();

await connection.StartAsync();
```

### React Hook
```typescript
const useLoomSignalR = (token: string) => {
  const [looms, setLooms] = useState<Loom[]>([]);
  // ... implementation
};
```

**Detaylı örnekler:** [CLIENT_EXAMPLES.md](CLIENT_EXAMPLES.md)

## 🚀 Deployment

### Docker
```bash
docker build -t loom-monitoring-api .
docker run -p 5038:80 loom-monitoring-api
```

### IIS
```powershell
# .NET 8 Hosting Bundle kurulumu gerekli
# WebSocket desteği etkinleştirin
```

### Azure App Service
```bash
az webapp create --name loom-monitoring-api --runtime "DOTNET|8.0"
az webapp config set --web-sockets-enabled true
```

**Detaylı deployment rehberi:** [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)

## 📚 Dokümantasyon

- 📖 [API Documentation](API_DOCUMENTATION.md) - Kapsamlı API rehberi
- 💻 [Client Examples](CLIENT_EXAMPLES.md) - Farklı platformlar için örnekler  
- 🚀 [Deployment Guide](DEPLOYMENT_GUIDE.md) - Production deployment rehberi

## 🏗️ Mimari

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client App    │◄───┤   SignalR Hub   │◄───┤  SQL Dependency │
│                 │    │                 │    │                 │
│ • JavaScript    │    │ • Real-time     │    │ • Auto-refresh  │
│ • React         │    │ • Filtering     │    │ • Change detect │
│ • Flutter       │    │ • Groups        │    │ • Event trigger │
│ • .NET          │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌─────────────────┐              │
         └──────────────►│   REST API      │◄─────────────┘
                        │                 │
                        │ • JWT Auth      │
                        │ • CRUD Ops      │
                        │ • Validation    │
                        │ • Logging       │
                        └─────────────────┘
                                 │
                        ┌─────────────────┐
                        │   SQL Server    │
                        │                 │
                        │ • Loom Data     │
                        │ • Broker        │
                        │ • Triggers      │
                        └─────────────────┘
```

## 🔐 Güvenlik

- **JWT Authentication**: Tüm endpoint'ler korumalı
- **HTTPS**: Production'da zorunlu
- **SQL Injection**: Parametreli sorgular
- **CORS**: Konfigüre edilebilir domain erişimi

## ⚡ Performance

- **Thread-Safe**: ConcurrentDictionary kullanımı
- **SQL Dependency**: Polling yerine event-driven
- **SignalR Groups**: Optimize edilmiş mesaj routing
- **Dapper**: Lightweight ORM

## 🐛 Troubleshooting

### Yaygın Sorunlar

**SignalR bağlantı hatası:**
```javascript
// JWT token kontrolü
accessTokenFactory: () => localStorage.getItem('token')
```

**SQL Dependency çalışmıyor:**
```sql
-- Broker servisini kontrol et
SELECT is_broker_enabled FROM sys.databases WHERE name = 'LoomMonitoring'
```

**CORS hatası:**
```csharp
// Program.cs'te domain'i ekle
.WithOrigins("http://localhost:3000", "https://yourdomain.com")
```

## 📊 Monitoring

### Health Checks
```http
GET /health              # Genel sağlık durumu
GET /health/ready        # Readiness probe
```

### Logs
```bash
# Console çıktısı
📡 Initial data sent to new connection: connection-id
🔍 Filtered data sent to connection-id  
❌ Error messages with details
```

## 🧪 Test Client

Hızlı test için Console Client:
```bash
dotnet run --project TestClient -- http://localhost:5038/loomsCurrentlyStatus
```

Test senaryoları:
1. Bağlantı kur
2. İlk veri yükleme kontrol et
3. Filtre uygula
4. Değişiklik simüle et
5. Real-time güncellemeleri gözlemle

## 📄 Lisans

[MIT License](LICENSE)

## 🤝 Katkıda Bulunma

1. Fork edin
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Push edin (`git push origin feature/AmazingFeature`)
5. Pull Request oluşturun

## 📞 Destek

- 📧 Email: support@yourcompany.com
- 📱 WhatsApp: +90 XXX XXX XX XX
- 💬 Slack: #loom-monitoring

## 🏷️ Versiyonlar

- **v1.0.0** - İlk stable sürüm
  - Real-time monitoring
  - Filtre sistemi
  - JWT authentication
  - Docker support

---

<div align="center">

**Loom Monitoring API** ile dokuma süreçlerinizi gerçek zamanlı izleyin! 🏭⚡

Made with ❤️ by Your Team

</div>