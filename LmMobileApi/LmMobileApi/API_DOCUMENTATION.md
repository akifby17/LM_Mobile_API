# Loom Monitoring API Documentation

## 📋 Genel Bakış

Bu API, tekstil fabrikalarında dokuma tezgahlarının (Looms) gerçek zamanlı durumunu izlemek için geliştirilmiş bir ASP.NET Core Web API servisidir. SignalR teknolojisi ile anlık veri güncellemeleri sağlar.

## 🏗️ Mimari

- **.NET 8.0** - Modern framework
- **SignalR** - Real-time iletişim
- **SQL Server** - Veri tabanı
- **Dapper** - ORM
- **JWT Authentication** - Güvenlik
- **Clean Architecture** - Yapısal düzen

---

## 🔐 Authentication

### JWT Token Kullanımı
API, JWT token tabanlı authentication kullanır. 

**Header Format:**
```
Authorization: Bearer <your-jwt-token>
```

**SignalR Bağlantısı için:**
```
/loomsCurrentlyStatus?access_token=<your-jwt-token>
```

---

## 📡 SignalR Hub

### Hub URL
```
/loomsCurrentlyStatus
```

### 🎯 Client Events (Sunucudan Gelen)

#### 1. `FilteredLoomsDataChanged`
Tezgah verileri + filtre seçenekleri (hem ilk yükleme hem de değişiklikler için)

**İlk bağlantı/filtre seçimi:** Tüm tezgahlar gelir
**Değişiklik olduğunda:** Sadece değişen tezgah(lar) gelir

```json
{
  "looms": [
    {
      "LoomNo": "L001",
      "Efficiency": 85.5,
      "OperationName": "Weaving",
      "OperatorName": "John Doe",
      "WeaverName": "Jane Smith",
      "EventId": 1,
      "LoomSpeed": 150,
      "HallName": "Hall1",
      "MarkName": "Mark1",
      "ModelName": "Model1",
      "GroupName": "Group1",
      "ClassName": "Class1",
      "WarpName": "Warp1",
      "VariantNo": "V001",
      "StyleName": "Style1",
      "WeaverEff": 82.0,
      "EventDuration": "00:15:30",
      "ProductedLength": 125.5,
      "TotalLength": 1000.0,
      "EventNameTR": "Çalışıyor",
      "OpDuration": "02:30:00"
    }
  ],
  "filters": [
    {
      "FilterType": "HallName",
      "Options": ["Hall1", "Hall2", "Hall3"]
    },
    {
      "FilterType": "MarkName",
      "Options": ["Mark1", "Mark2"]
    }
  ]
}
```

**Client'ta nasıl ayırt edilir:**
```javascript
connection.on("FilteredLoomsDataChanged", (data) => {
    if (data.looms.length === 1) {
        // Tek tezgah değişikliği
        console.log("Değişen tezgah:", data.looms[0]);
    } else {
        // Çoklu veri (ilk yükleme veya filtre değişimi)
        console.log("Tüm veriler:", data.looms);
    }
    // Filtreler her durumda güncellenir
    updateFilters(data.filters);
});
```

#### 2. `FilterSubscribed`
Filtre abonelik onayı
```json
"hall:Hall1|mark:Mark1"
```

#### 3. `FilterUnsubscribed`
Filtre abonelikten çıkış onayı

#### 4. `FilterSubscriptionError`
Filtre abonelik hatası
```json
"Invalid filter format"
```

### 🎮 Server Methods (Client'tan Çağrılan)

#### 1. `SubscribeToFilter`
Belirli bir filtreye abone ol
```javascript
await connection.invoke("SubscribeToFilter", JSON.stringify({
    "HallName": "Hall1",
    "MarkName": "Mark1"
}));
```

#### 2. `UnsubscribeFromFilters`
Tüm filtrelerden çık, genel feed'e dön
```javascript
await connection.invoke("UnsubscribeFromFilters");
```

---

## 🌐 REST API Endpoints

### Base URL
```
https://your-domain.com/api
```

### 🏭 Looms Endpoints

#### 1. Get All Looms
```http
GET /api/looms/monitoring
```
**Response:** Tüm tezgah listesi

#### 2. Get Filter Options
```http
GET /api/looms/filters
```
**Response:** Mevcut filtre seçenekleri

#### 3. Get Filtered Looms
```http
POST /api/looms/filtered
Content-Type: application/json

{
  "HallName": "Hall1",
  "MarkName": "Mark1"
}
```

#### 4. Get Looms With Filters
```http
GET /api/looms/with-filters
POST /api/looms/with-filters

{
  "HallName": "Hall1"
}
```

### 🔧 Operations Endpoints

#### 1. Change Weaver
```http
POST /api/looms/changeWeaver
Content-Type: application/json

{
  // ChangeWeaver contract data
}
```

#### 2. Operation Start/Stop
```http
POST /api/looms/operationStartStop
Content-Type: application/json

{
  // OperationStartStop contract data
}
```

#### 3. Piece Cutting
```http
POST /api/looms/pieceCutting
Content-Type: application/json

{
  // PieceCutting contract data
}
```

---

## 🔍 Filtre Sistemi

### Filtre Tipleri
- **HallName** - Salon adı
- **MarkName** - Marka adı  
- **GroupName** - Grup adı
- **ModelName** - Model adı
- **ClassName** - Sınıf adı
- **EventNameTR** - Olay adı (Türkçe)

### Filtre Davranışı

#### 🟢 Filtresiz Kullanım
- **İlk bağlantı:** `FilteredLoomsDataChanged` → Tüm tezgahlar + filtreler
- **Değişiklik:** `FilteredLoomsDataChanged` → Sadece değişen tezgah(lar) + güncel filtreler

#### 🔍 Filtreli Kullanım  
- **Filtre seçimi:** `FilteredLoomsDataChanged` → Filtreye uyan TÜM tezgahlar + filtreler
- **Değişiklik:** `FilteredLoomsDataChanged` → Sadece filtreye uyan + değişen tezgah(lar) + güncel filtreler

#### 📡 Event Yapısı
Artık sadece **tek event** kullanılıyor: `FilteredLoomsDataChanged`
- `looms` array'i: İlk seferde çoklu, değişiklikte genellikle tekli
- `filters` array'i: Her zaman güncel filtre seçenekleri

---

## 💻 Client Kullanım Örnekleri

### JavaScript/TypeScript

```javascript
import * as signalR from "@microsoft/signalr";

// Bağlantı oluştur
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/loomsCurrentlyStatus", {
        accessTokenFactory: () => "your-jwt-token"
    })
    .build();

// Event listener'lar
connection.on("FilteredLoomsDataChanged", (data) => {
    console.log("Tüm veriler:", data);
    updateLoomsDisplay(data.looms);
    updateFiltersDisplay(data.filters);
});

connection.on("LoomCurrentlyStatusChanged", (loom) => {
    console.log("Tezgah değişikliği:", loom);
    updateSingleLoom(loom);
});

connection.on("FilterOptionsChanged", (filters) => {
    console.log("Filtre güncelleme:", filters);
    updateFiltersDisplay(filters);
});

// Bağlan
await connection.start();

// Filtre uygula
await connection.invoke("SubscribeToFilter", JSON.stringify({
    "HallName": "Hall1",
    "MarkName": "Mark1"
}));

// Filtre temizle
await connection.invoke("UnsubscribeFromFilters");
```

### C# Console Client

```csharp
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/loomsCurrentlyStatus")
    .Build();

// Event listeners
connection.On<object>("FilteredLoomsDataChanged", (data) =>
{
    Console.WriteLine($"Tüm veriler: {JsonSerializer.Serialize(data)}");
});

connection.On<object>("LoomCurrentlyStatusChanged", (loom) =>
{
    Console.WriteLine($"Tezgah değişikliği: {JsonSerializer.Serialize(loom)}");
});

// Bağlan
await connection.StartAsync();

// Filtre uygula
await connection.InvokeAsync("SubscribeToFilter", 
    JsonSerializer.Serialize(new { HallName = "Hall1" }));
```

### React Hook Örneği

```typescript
import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

interface Loom {
  LoomNo: string;
  Efficiency: number;
  HallName: string;
  // ... diğer özellikler
}

export const useLoomSignalR = (token: string) => {
  const [looms, setLooms] = useState<Loom[]>([]);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("/loomsCurrentlyStatus", {
        accessTokenFactory: () => token
      })
      .build();

    newConnection.on("FilteredLoomsDataChanged", (data) => {
      setLooms(data.looms);
    });

    newConnection.on("LoomCurrentlyStatusChanged", (changedLoom) => {
      setLooms(prev => 
        prev.map(loom => 
          loom.LoomNo === changedLoom.LoomNo ? changedLoom : loom
        )
      );
    });

    newConnection.start();
    setConnection(newConnection);

    return () => {
      newConnection.stop();
    };
  }, [token]);

  const applyFilter = async (filter: any) => {
    if (connection) {
      await connection.invoke("SubscribeToFilter", JSON.stringify(filter));
    }
  };

  return { looms, applyFilter };
};
```

---

## ⚙️ Kurulum ve Konfigürasyon

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=YourDatabase;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here-make-it-long-and-secure",
    "Issuer": "LmMobileApi",
    "Audience": "LmMobileApiUsers",
    "ExpiryInMinutes": 60
  },
  "DataManApiOptions": {
    "BaseUrl": "http://your-dataman-api-url/"
  }
}
```

### CORS Ayarları
Frontend domain'lerini CORS'a ekleyin:
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("http://localhost:3000", "https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

---

## 🚀 Deployment

### IIS Deployment
1. Publish profile kullanarak build alın
2. IIS'e deploy edin
3. SignalR için WebSocket desteğini etkinleştirin

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "LmMobileApi.dll"]
```

---

## 🐛 Troubleshooting

### SignalR Bağlantı Sorunları
- JWT token'ın geçerli olduğundan emin olun
- CORS ayarlarını kontrol edin
- WebSocket desteğinin aktif olduğunu doğrulayın

### Filtre Çalışmıyor
- Filtre JSON formatının doğru olduğunu kontrol edin
- Console'da debug mesajlarını takip edin
- SQL Dependency'nin çalıştığını doğrulayın

### Performance Sorunları
- Bağlantı sayısını izleyin
- SQL Server broker servisinin aktif olduğunu kontrol edin

---

## 📊 Monitoring ve Logs

### Console Logs
API şu formatda log üretir:
```
📡 Initial data sent to new connection: connection-id
🔍 SendInitialFilteredData başladı. ConnectionId: connection-id
✅ Filtered data sent to connection-id
❌ OnConnectedAsync error: error-message
```

### Health Checks
API endpoint'lerini health check için kullanabilirsiniz:
```http
GET /api/looms/monitoring
```

---

## 🔒 Güvenlik

- JWT token authentication zorunlu
- HTTPS kullanımı önerilir
- SQL Injection koruması (Dapper parametreli sorgular)
- CORS konfigürasyonu yapılmalı

---

## 📞 Destek

API ile ilgili sorular için:
- Console debug mesajlarını kontrol edin
- Network trafiğini izleyin (F12 Developer Tools)
- JWT token'ın geçerli olduğunu doğrulayın

---

## 📝 Sürüm Notları

### v1.0.0
- ✅ Real-time loom monitoring
- ✅ Filtre sistemi
- ✅ JWT authentication  
- ✅ Thread-safe operations
- ✅ SQL Dependency integration
- ✅ Clean Architecture