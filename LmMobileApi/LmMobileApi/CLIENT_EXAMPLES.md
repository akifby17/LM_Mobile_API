# Client Implementation Examples

## 🚀 Hızlı Başlangıç

Bu dosya, farklı teknolojilerde Loom Monitoring API'sini nasıl kullanacağınızı gösterir.

---

## 📱 JavaScript/TypeScript Client

### Vanilla JavaScript

```html
<!DOCTYPE html>
<html>
<head>
    <title>Loom Monitor</title>
    <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <div id="status"></div>
    <div id="looms"></div>
    
    <button onclick="applyHallFilter()">Hall1 Filtresi</button>
    <button onclick="clearFilters()">Filtreleri Temizle</button>

    <script>
        let connection;
        
        async function startConnection() {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("http://localhost:5038/loomsCurrentlyStatus", {
                    accessTokenFactory: () => "your-jwt-token-here"
                })
                .build();

            // Event listeners
            connection.on("FilteredLoomsDataChanged", (data) => {
                if (data.looms.length === 1) {
                    console.log("🔄 Tezgah değişikliği:", data);
                    updateSingleLoom(data.looms[0]);
                } else {
                    console.log("📊 Tüm veriler:", data);
                    displayLooms(data.looms);
                }
                // Her durumda filtreler güncellenir
                displayFilters(data.filters);
            });

            connection.on("FilterSubscribed", (groupName) => {
                document.getElementById('status').innerHTML = `✅ Filtre aktif: ${groupName}`;
            });

            connection.on("FilterUnsubscribed", () => {
                document.getElementById('status').innerHTML = `✅ Tüm veriler gösteriliyor`;
            });

            try {
                await connection.start();
                console.log("✅ SignalR bağlantısı kuruldu!");
                document.getElementById('status').innerHTML = "🟢 Bağlandı";
            } catch (err) {
                console.error("❌ Bağlantı hatası:", err);
                document.getElementById('status').innerHTML = "🔴 Bağlantı hatası";
            }
        }

        function displayLooms(looms) {
            const loomsDiv = document.getElementById('looms');
            loomsDiv.innerHTML = '<h3>🏭 Tezgahlar:</h3>';
            
            looms.forEach(loom => {
                loomsDiv.innerHTML += `
                    <div style="border: 1px solid #ccc; margin: 5px; padding: 10px;">
                        <strong>${loom.LoomNo}</strong> - ${loom.HallName} - ${loom.EventNameTR}
                        <br>Verimlilik: ${loom.Efficiency}%
                        <br>Operatör: ${loom.OperatorName}
                    </div>
                `;
            });
        }

        function displayFilters(filters) {
            console.log("Filtreler:", filters);
        }

        function updateSingleLoom(loom) {
            // Tek tezgah güncellemesi - DOM'da ilgili tezgahı bul ve güncelle
            console.log(`Tezgah ${loom.LoomNo} güncellendi`);
        }

        async function applyHallFilter() {
            await connection.invoke("SubscribeToFilter", JSON.stringify({
                "HallName": "Hall1"
            }));
        }

        async function clearFilters() {
            await connection.invoke("UnsubscribeFromFilters");
        }

        // Sayfa yüklendiğinde bağlan
        startConnection();
    </script>
</body>
</html>
```

### React TypeScript Component

```tsx
import React, { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface Loom {
  LoomNo: string;
  Efficiency: number;
  OperationName: string;
  OperatorName: string;
  WeaverName: string;
  EventId: number;
  LoomSpeed: number;
  HallName: string;
  MarkName: string;
  ModelName: string;
  GroupName: string;
  ClassName: string;
  EventNameTR: string;
}

interface FilterOption {
  FilterType: string;
  Options: string[];
}

interface LoomsWithFilters {
  looms: Loom[];
  filters: FilterOption[];
}

interface LoomFilter {
  HallName?: string;
  MarkName?: string;
  GroupName?: string;
  ModelName?: string;
  ClassName?: string;
  EventNameTR?: string;
}

const LoomMonitor: React.FC = () => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [looms, setLooms] = useState<Loom[]>([]);
  const [filters, setFilters] = useState<FilterOption[]>([]);
  const [status, setStatus] = useState<string>('Bağlanıyor...');
  const [activeFilter, setActiveFilter] = useState<LoomFilter | null>(null);

  useEffect(() => {
    const token = localStorage.getItem('jwt-token'); // Token'ı buradan al
    
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5038/loomsCurrentlyStatus', {
        accessTokenFactory: () => token || ''
      })
      .build();

    // Event listeners
    newConnection.on('FilteredLoomsDataChanged', (data: LoomsWithFilters) => {
      if (data.looms.length === 1) {
        console.log('🔄 Tezgah değişikliği:', data);
        // Tek tezgah değişikliği - mevcut listede güncelle
        setLooms(prevLooms => 
          prevLooms.map(prevLoom => 
            prevLoom.LoomNo === data.looms[0].LoomNo ? data.looms[0] : prevLoom
          )
        );
      } else {
        console.log('📊 Tüm veriler:', data);
        // Çoklu veri - listeyi yenile
        setLooms(data.looms);
      }
      // Her durumda filtreler güncellenir
      setFilters(data.filters);
    });

    newConnection.on('FilterSubscribed', (groupName: string) => {
      setStatus(`✅ Filtre aktif: ${groupName}`);
    });

    newConnection.on('FilterUnsubscribed', () => {
      setStatus('✅ Tüm veriler gösteriliyor');
      setActiveFilter(null);
    });

    newConnection.on('FilterSubscriptionError', (error: string) => {
      setStatus(`❌ Filtre hatası: ${error}`);
    });

    // Bağlantı başlat
    newConnection.start()
      .then(() => {
        console.log('✅ SignalR bağlantısı kuruldu!');
        setStatus('🟢 Bağlandı');
      })
      .catch(err => {
        console.error('❌ Bağlantı hatası:', err);
        setStatus('🔴 Bağlantı hatası');
      });

    setConnection(newConnection);

    // Cleanup
    return () => {
      newConnection.stop();
    };
  }, []);

  const applyFilter = useCallback(async (filter: LoomFilter) => {
    if (connection) {
      try {
        await connection.invoke('SubscribeToFilter', JSON.stringify(filter));
        setActiveFilter(filter);
      } catch (err) {
        console.error('Filtre uygulama hatası:', err);
      }
    }
  }, [connection]);

  const clearFilters = useCallback(async () => {
    if (connection) {
      try {
        await connection.invoke('UnsubscribeFromFilters');
        setActiveFilter(null);
      } catch (err) {
        console.error('Filtre temizleme hatası:', err);
      }
    }
  }, [connection]);

  return (
    <div className="loom-monitor">
      <div className="status-bar">
        <h2>Loom Monitor</h2>
        <p>Status: {status}</p>
      </div>

      <div className="filters-section">
        <h3>🔍 Filtreler</h3>
        <button onClick={() => applyFilter({ HallName: 'Hall1' })}>
          Hall1 Filtresi
        </button>
        <button onClick={() => applyFilter({ MarkName: 'Mark1' })}>
          Mark1 Filtresi
        </button>
        <button onClick={() => applyFilter({ HallName: 'Hall1', MarkName: 'Mark1' })}>
          Kombine Filtre
        </button>
        <button onClick={clearFilters}>
          Filtreleri Temizle
        </button>
        
        {activeFilter && (
          <div>
            <strong>Aktif Filtre:</strong> {JSON.stringify(activeFilter)}
          </div>
        )}
      </div>

      <div className="looms-section">
        <h3>🏭 Tezgahlar ({looms.length})</h3>
        <div className="looms-grid">
          {looms.map(loom => (
            <div key={loom.LoomNo} className="loom-card">
              <h4>{loom.LoomNo}</h4>
              <p><strong>Salon:</strong> {loom.HallName}</p>
              <p><strong>Durum:</strong> {loom.EventNameTR}</p>
              <p><strong>Verimlilik:</strong> {loom.Efficiency}%</p>
              <p><strong>Operatör:</strong> {loom.OperatorName}</p>
              <p><strong>Dokuyucu:</strong> {loom.WeaverName}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="filters-info">
        <h3>📋 Mevcut Filtre Seçenekleri</h3>
        {filters.map(filter => (
          <div key={filter.FilterType}>
            <strong>{filter.FilterType}:</strong> {filter.Options.join(', ')}
          </div>
        ))}
      </div>
    </div>
  );
};

export default LoomMonitor;
```

---

## 🖥️ .NET Console Application

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace LoomSignalRClient
{
    public class LoomMonitorClient
    {
        private HubConnection? _connection;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public async Task StartAsync(string hubUrl, string? token = null)
        {
            var builder = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);
                    }
                });

            _connection = builder.Build();

            SetupEventHandlers();

            try
            {
                await _connection.StartAsync();
                Console.WriteLine("✅ SignalR Hub'a bağlandı!");
                
                await RunInteractiveMode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Bağlantı hatası: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            if (_connection == null) return;

            _connection.On<JsonElement>("FilteredLoomsDataChanged", (data) =>
            {
                if (data.TryGetProperty("looms", out var loomsElement))
                {
                    var loomsArray = JsonSerializer.Deserialize<JsonElement[]>(loomsElement);
                    
                    if (loomsArray?.Length == 1)
                    {
                        Console.WriteLine("\n🔄 === LOOM STATUS CHANGED ===");
                        Console.WriteLine("🏭 CHANGED LOOM:");
                    }
                    else
                    {
                        Console.WriteLine("\n📊 === FILTERED LOOMS DATA ===");
                        Console.WriteLine("🏭 LOOMS:");
                    }
                    
                    var loomsJson = JsonSerializer.Serialize(loomsElement, _jsonOptions);
                    Console.WriteLine(loomsJson);
                }

                if (data.TryGetProperty("filters", out var filtersElement))
                {
                    Console.WriteLine("\n🔍 FILTERS:");
                    var filtersJson = JsonSerializer.Serialize(filtersElement, _jsonOptions);
                    Console.WriteLine(filtersJson);
                }
                
                Console.WriteLine("==============================\n");
            });

            _connection.On<string>("FilterSubscribed", (groupName) =>
            {
                Console.WriteLine($"✅ Filtre aboneliği başarılı! Grup: {groupName}\n");
            });

            _connection.On<string>("FilterSubscriptionError", (error) =>
            {
                Console.WriteLine($"❌ Filtre abonelik hatası: {error}\n");
            });

            _connection.On("FilterUnsubscribed", () =>
            {
                Console.WriteLine("✅ Filtre abonelikten çıkıldı! Tüm veriler alınacak.\n");
            });
        }

        private async Task RunInteractiveMode()
        {
            while (true)
            {
                Console.WriteLine("\n🎮 KOMUTLAR:");
                Console.WriteLine("1 - Hall1 filtresi");
                Console.WriteLine("2 - Mark1 filtresi");
                Console.WriteLine("3 - Hall1 + Mark1 kombinasyonu");
                Console.WriteLine("4 - Tüm filtreleri temizle");
                Console.WriteLine("5 - Özel filtre gir");
                Console.WriteLine("q - Çıkış");
                Console.Write("\nKomut seç: ");

                var input = Console.ReadLine();

                try
                {
                    switch (input?.ToLower())
                    {
                        case "1":
                            await ApplyFilter(new { HallName = "Hall1" });
                            break;
                        case "2":
                            await ApplyFilter(new { MarkName = "Mark1" });
                            break;
                        case "3":
                            await ApplyFilter(new { HallName = "Hall1", MarkName = "Mark1" });
                            break;
                        case "4":
                            await ClearFilters();
                            break;
                        case "5":
                            await CustomFilter();
                            break;
                        case "q":
                            return;
                        default:
                            Console.WriteLine("❌ Geçersiz komut!");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Komut çalıştırma hatası: {ex.Message}");
                }
            }
        }

        private async Task ApplyFilter(object filter)
        {
            if (_connection == null) return;

            var filterJson = JsonSerializer.Serialize(filter);
            Console.WriteLine($"🔍 Filtre uygulanıyor: {filterJson}");

            await _connection.InvokeAsync("SubscribeToFilter", filterJson);
        }

        private async Task ClearFilters()
        {
            if (_connection == null) return;

            Console.WriteLine("🧹 Tüm filtreler temizleniyor...");
            await _connection.InvokeAsync("UnsubscribeFromFilters");
        }

        private async Task CustomFilter()
        {
            Console.WriteLine("\n📝 Özel Filtre Girişi");
            Console.WriteLine("Boş bırakmak için Enter'a basın");

            var filter = new Dictionary<string, string>();

            Console.Write("Hall Name: ");
            var hallName = Console.ReadLine();
            if (!string.IsNullOrEmpty(hallName)) filter["HallName"] = hallName;

            Console.Write("Mark Name: ");
            var markName = Console.ReadLine();
            if (!string.IsNullOrEmpty(markName)) filter["MarkName"] = markName;

            Console.Write("Group Name: ");
            var groupName = Console.ReadLine();
            if (!string.IsNullOrEmpty(groupName)) filter["GroupName"] = groupName;

            Console.Write("Model Name: ");
            var modelName = Console.ReadLine();
            if (!string.IsNullOrEmpty(modelName)) filter["ModelName"] = modelName;

            Console.Write("Class Name: ");
            var className = Console.ReadLine();
            if (!string.IsNullOrEmpty(className)) filter["ClassName"] = className;

            Console.Write("Event Name TR: ");
            var eventNameTR = Console.ReadLine();
            if (!string.IsNullOrEmpty(eventNameTR)) filter["EventNameTR"] = eventNameTR;

            if (filter.Any())
            {
                await ApplyFilter(filter);
            }
            else
            {
                Console.WriteLine("❌ Hiç filtre girilmedi!");
            }
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }
        }
    }

    // Program.cs
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔧 Loom SignalR Test Client");
            Console.WriteLine("===========================");

            var client = new LoomMonitorClient();
            
            // JWT token (opsiyonel)
            var token = args.Length > 1 ? args[1] : null;
            
            // Hub URL
            var hubUrl = args.Length > 0 ? args[0] : "http://localhost:5038/loomsCurrentlyStatus";

            try
            {
                await client.StartAsync(hubUrl, token);
            }
            finally
            {
                await client.StopAsync();
            }
        }
    }
}
```

---

## 📱 Flutter/Dart Client

```dart
import 'dart:convert';
import 'package:signalr_netcore/signalr_netcore.dart';

class LoomMonitorService {
  HubConnection? _connection;
  Function(Map<String, dynamic>)? onLoomsChanged;
  Function(Map<String, dynamic>)? onSingleLoomChanged;
  Function(List<dynamic>)? onFiltersChanged;

  Future<void> start(String hubUrl, {String? token}) async {
    _connection = HubConnectionBuilder()
        .withUrl(hubUrl, options: HttpConnectionOptions(
          accessTokenFactory: () => Future.value(token),
        ))
        .build();

    _setupEventHandlers();

    try {
      await _connection!.start();
      print('✅ SignalR connected!');
    } catch (e) {
      print('❌ Connection error: $e');
    }
  }

  void _setupEventHandlers() {
            _connection!.on('FilteredLoomsDataChanged', (args) {
      final data = args![0] as Map<String, dynamic>;
      final looms = data['looms'] as List<dynamic>? ?? [];
      
      if (looms.length == 1) {
        print('🔄 Loom changed: ${looms[0]}');
        onSingleLoomChanged?.call(looms[0] as Map<String, dynamic>);
      } else {
        print('📊 Multiple looms data: $data');
        onLoomsChanged?.call(data);
      }
      
      // Filtreler her durumda güncellenir
      final filters = data['filters'] as List<dynamic>? ?? [];
      onFiltersChanged?.call(filters);
    });

    _connection!.on('FilterSubscribed', (args) {
      final groupName = args![0] as String;
      print('✅ Filter subscribed: $groupName');
    });

    _connection!.on('FilterUnsubscribed', (args) {
      print('✅ Filter unsubscribed');
    });
  }

  Future<void> applyFilter(Map<String, String> filter) async {
    final filterJson = jsonEncode(filter);
    await _connection!.invoke('SubscribeToFilter', args: [filterJson]);
  }

  Future<void> clearFilters() async {
    await _connection!.invoke('UnsubscribeFromFilters');
  }

  Future<void> stop() async {
    await _connection?.stop();
  }
}

// Widget örneği
class LoomMonitorWidget extends StatefulWidget {
  @override
  _LoomMonitorWidgetState createState() => _LoomMonitorWidgetState();
}

class _LoomMonitorWidgetState extends State<LoomMonitorWidget> {
  final LoomMonitorService _service = LoomMonitorService();
  List<Map<String, dynamic>> looms = [];

  @override
  void initState() {
    super.initState();
    _initializeService();
  }

  void _initializeService() {
    _service.onLoomsChanged = (data) {
      setState(() {
        looms = List<Map<String, dynamic>>.from(data['looms'] ?? []);
      });
    };

    _service.start('http://localhost:5038/loomsCurrentlyStatus');
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Loom Monitor')),
      body: Column(
        children: [
          Row(
            children: [
              ElevatedButton(
                onPressed: () => _service.applyFilter({'HallName': 'Hall1'}),
                child: Text('Hall1 Filter'),
              ),
              ElevatedButton(
                onPressed: () => _service.clearFilters(),
                child: Text('Clear Filters'),
              ),
            ],
          ),
          Expanded(
            child: ListView.builder(
              itemCount: looms.length,
              itemBuilder: (context, index) {
                final loom = looms[index];
                return Card(
                  child: ListTile(
                    title: Text(loom['LoomNo'] ?? ''),
                    subtitle: Text('${loom['HallName']} - ${loom['EventNameTR']}'),
                    trailing: Text('${loom['Efficiency']}%'),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  @override
  void dispose() {
    _service.stop();
    super.dispose();
  }
}
```

---

## 🔧 Configuration Örnekleri

### Environment Variables
```bash
# .env dosyası
SIGNALR_HUB_URL=http://localhost:5038/loomsCurrentlyStatus
JWT_TOKEN=your-jwt-token-here
API_BASE_URL=http://localhost:5038/api
```

### Webpack Configuration (React)
```javascript
// webpack.config.js
module.exports = {
  // ... diğer ayarlar
  resolve: {
    fallback: {
      "stream": require.resolve("stream-browserify"),
      "util": require.resolve("util/")
    }
  }
};
```

---

## 🚀 Test Scripti

```bash
#!/bin/bash
# test-signalr.sh

echo "🔧 Loom SignalR Connection Test"
echo "==============================="

# API'nin çalışıp çalışmadığını kontrol et
echo "📡 Testing API connection..."
curl -f http://localhost:5038/api/looms/monitoring || {
    echo "❌ API is not running!"
    exit 1
}

echo "✅ API is running!"

# SignalR endpoint'ini test et
echo "📡 Testing SignalR endpoint..."
curl -f http://localhost:5038/loomsCurrentlyStatus/negotiate || {
    echo "❌ SignalR endpoint is not accessible!"
    exit 1
}

echo "✅ SignalR endpoint is accessible!"
echo "🎉 All tests passed!"
```

Bu örnekler ile farklı platformlarda Loom Monitoring API'sini entegre edebilirsiniz!
Bu örnekler ile farklı platformlarda Loom Monitoring API'sini entegre edebilirsiniz!