# E-Commerce Payment Integration API

Balance Management servisi ile entegre bir e-ticaret backend API'si. Urun listeleme, bakiye bloke ederek siparis olusturma, siparis tamamlama ve iptal islemleri sunar.                                  

## Mimari

Proje **Clean Architecture** prensiplerine uygun olarak katmanli bir yapiyla gelistirilmistir:

```
src/
  ECommercePaymentIntegration.API            -> Presentation (Controllers, Middleware)
  ECommercePaymentIntegration.Application    -> Business Logic (CQRS, DTOs, Validators)
  ECommercePaymentIntegration.Domain         -> Entities, Enums, Exceptions, Repository Interfaces
  ECommercePaymentIntegration.Infrastructure -> External Services, Repositories, Caching, Health Checks

tests/
  ECommercePaymentIntegration.UnitTests          -> Unit testler (xUnit, Moq, Shouldly)
  ECommercePaymentIntegration.IntegrationTests   -> Integration testler (WebApplicationFactory)
```

## Teknolojiler

- **.NET 8** / ASP.NET Core
- **Custom CQRS Mediator** (MediatR benzeri, framework bagimsiz)
- **Polly** - Retry & Circuit Breaker (resilience)
- **OpenTelemetry** - Distributed Tracing & Metrics
- **Swagger / OpenAPI** - API dokumantasyonu
- **xUnit, Moq, Shouldly** - Test altyapisi
- **In-Memory Repository** - Veri saklama

## API Endpointleri

| Method | Endpoint                     | Aciklama                                  |
|--------|------------------------------|-------------------------------------------|
| GET    | `/api/products`              | Tum urunleri listeler                     |
| POST   | `/api/orders/create`         | Siparis olusturur ve fon rezerve eder     |
| POST   | `/api/orders/{id}/complete`  | Siparisi tamamlar ve odemeyi kesinlestirir|
| POST   | `/api/orders/{id}/cancel`    | Siparisi iptal eder ve fonu serbest birakir|
| GET    | `/api/orders/{id}`           | Siparis detayini getirir                  |
| GET    | `/api/orders`                | Tum siparisleri listeler                  |
| GET    | `/health`                    | Health check                              |

## Siparis Durumu Akisi

```
Pending -> Reserved -> Completed
                   \-> Cancelled
       \-> Failed
```

- **Pending**: Siparis olusturuldu, henuz fon rezerve edilmedi
- **Reserved**: Balance Management servisinde fon bloke edildi
- **Completed**: Odeme kesinlesti, bloke tutar dusuldu
- **Cancelled**: Siparis iptal edildi, bloke tutar serbest birakildi
- **Failed**: Islem sirasinda hata olustu

## Kurulum ve Calistirma

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Calistirma

```bash
# Projeyi derle
dotnet build

# API'yi calistir
dotnet run --project src/ECommercePaymentIntegration.API

# Testleri calistir
dotnet test
```

API baslatildiktan sonra Swagger UI'a `/swagger` adresinden erisilebilir.

### Konfigrasyon

`appsettings.json` icinde Balance Management servisi ayarlari:

```json
{
  "BalanceManagement": {
    "BaseUrl": "http://localhost:3000",
    "TimeoutSeconds": 30,
    "RetryCount": 3,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  }
}
```

## Onemli Ozellikler

- **Idempotency**: `Idempotency-Key` header'i veya request body ile tekrarlanan isteklerde ayni sonuc doner
- **Correlation ID**: Tum isteklerde `X-Correlation-Id` header'i ile request takibi
- **Rate Limiting**: Dakikada 100 istek limiti (Fixed Window)
- **Response Compression**: HTTPS uzerinde response sikistirma
- **Security Headers**: XSS, Content-Type sniffing vb. koruma header'lari
- **Global Exception Handling**: Merkezi hata yonetimi
- **Caching**: Balance Management servis cagrilari icin memory cache
- **Retry & Circuit Breaker**: Polly ile dayanikli HTTP cagrilari (exponential backoff + jitter)
- **Health Checks**: Balance Management servisi saglik kontrolu
- **Validation Pipeline**: Command/Query validasyonlari CQRS pipeline icinde
- **Logging Pipeline**: Tum handler cagrilari otomatik loglanir

## Lisans

MIT
