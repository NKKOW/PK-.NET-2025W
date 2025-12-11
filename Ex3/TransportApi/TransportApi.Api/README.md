# Transport API – Fleet & Orders Management

Transport API to REST API oparte o .NET 8 Minimal API, umożliwiające zarządzanie flotą pojazdów, kierowcami oraz zleceniami transportowymi.  
Aplikacja wykorzystuje Entity Framework Core z bazą SQLite oraz pełną dokumentacją Swagger/OpenAPI.

---

## Funkcjonalności

### Pojazdy
- Dodawanie pojazdów (truck, van)
- Pobieranie wszystkich pojazdów
- Pobieranie tylko dostępnych pojazdów
- Edycja pojazdu
- Usuwanie pojazdu

### Kierowcy
- Dodawanie kierowców
- Pobieranie kierowców
- Aktualizacja danych
- Usuwanie kierowców

### Zlecenia
- Tworzenie nowych zleceń
- Pobieranie aktywnych zleceń
- Pobieranie wszystkich zleceń
- Zakończenie zlecenia
- Usuwanie zakończonych zleceń

### Dodatkowe funkcje
- Seed przykładowych danych
- Obsługa eventu OnNewOrderCreated

---

## Instalacja i uruchomienie

### 1. Klonowanie repozytorium
```bash
git clone https://github.com/Dziwikk/PK-.NET-2025W.git
cd ex3

2. Wymagania
.NET 8 SDK

SQLite (wbudowany, nie wymaga instalacji)

3. Uruchomienie aplikacji
dotnet run

4. Dostęp do API

http://localhost:5269
https://localhost:7287
Swagger UI:

Baza danych
Aplikacja wykorzystuje plik SQLite tworzony automatycznie:

fleet.db
API – Endpoints
Vehicles
Pobierz wszystkie pojazdy
GET /api/vehicles

Pobierz dostępne pojazdy
GET /api/vehicles/available?minLoadKg=1000

Dodaj pojazd
POST /api/vehicles
Przykład:

{
  "type": "truck",
  "registrationNumber": "WX12345",
  "maxLoadKg": 24000,
  "trailerLength": 13.6
}

Aktualizuj pojazd
PUT /api/vehicles/{id}

Usuń pojazd
DELETE /api/vehicles/{id}

Drivers
Pobierz kierowców
GET /api/drivers

Dodaj kierowcę
POST /api/drivers
Przykład:

{
  "name": "Jan Kowalski",
  "licenseNumber": "C+E/1234",
  "isAvailable": true
}

Aktualizuj kierowcę
PUT /api/drivers/{id}

Usuń kierowcę
DELETE /api/drivers/{id}
Orders

Pobierz aktywne zlecenia
GET /api/orders

Pobierz wszystkie zlecenia
GET /api/orders/all

Utwórz zlecenie
POST /api/orders
Przykład:
{
  "cargoDescription": "Elektronika 1.2t",
  "weight": 1200,
  "vehicleId": 1,
  "driverId": 1
}

PUT /api/orders/{id}/complete

DELETE /api/orders/{id}

Seed danych demo
Endpoint dodający przykładowe dane:
POST /api/seed
Przykładowa odpowiedź:

{ "seeded": true }


Struktura projektu

TransportApi/
│
├── Api/
│   ├── Data/
│   │   └── FleetDbContext.cs
│   ├── Models/
│   │   ├── Vehicle.cs
│   │   ├── Truck.cs
│   │   ├── Van.cs
│   │   ├── Driver.cs
│   │   └── Order.cs
│   ├── Services/
│   │   └── FleetManager.cs
│   ├── Extensions/
│   │   └── VehicleExtensions.cs
│   └── Program.cs
│
└── fleet.db


Technologie
.NET 8 Minimal API

Entity Framework Core

SQLite

Swagger / OpenAPI

LINQ

Dependency Injection