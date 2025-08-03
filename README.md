# **BestPriceOrderBook**

A **.NET 9**-based implementation of a **Meta Crypto Exchange Order Book** that finds the **best execution price** for buying or selling Bitcoin (BTC) across multiple exchanges. The project also includes a **Minimal API** to expose this functionality as a web service, along with **Docker support** for containerized deployment.

---

## **📜 Project Overview**

This solution implements a **meta-exchange** that always finds the **best possible price** for a given BTC order, considering order books from multiple crypto exchanges.

It supports:
- Reading multiple exchange order books.
- Determining the **lowest possible price** for a buy order.
- Determining the **highest possible price** for a sell order.
- Respecting **exchange account balances** (EUR & BTC).
- Providing results via a **REST API**.
- Running in **Docker**.

---

## **⚙ Features**
- **Part 1**: Algorithm to find the **best execution plan** for a BTC order.
- **Part 2**: Minimal API service using **Kestrel**.
- **Bonus**:
  - Unit tests for algorithm & repository.
  - Docker deployment.

---

## **🧮 Example**
Given multiple exchange order books:

Exchange A: 3 BTC @ 3000 EUR, 2 BTC @ 3300 EUR
Exchange B: 5 BTC @ 3100 EUR


If you want to **buy 9 BTC**:
- Buy **4 BTC** from Exchange A (3 BTC x 3k EUR, 1 BTC x 3.3k EUR)
- Buy **5 BTC** from Exchange B (3.1k EUR)
- **Total = 27,800 EUR**

The algorithm picks the **cheapest combination** while respecting available balances.

---

## **📂 Solution Structure**

```
BestPriceOrderBook/
│ OrderBookSolution.sln
│
├── OrderBookAlgorithm/ # Core business logic & algorithm
│ ├── DomainClasses/ # Order, OrderBookRecord, enums
│ ├── OrderBookSources/ # Sample JSON order book data
│ ├── FileSystemAccess/ # Classes to acces local file system, including OrderBookRepository.cs
│ ├── OrderAlgorithm.cs # Best-price calculation logic
│ └── ...
│
├── OrderBookApi/ # Minimal API project to start OrderBookAlgorithm
│ ├── Api/OrdersEndpoints.cs
│ ├── Program.cs # API setup (Swagger, DI)
│ ├── Dockerfile # Docker container setup
│ └── ...
├── OrderBookConsole/ # Console Application to start OrderBookAlgorithm
│ ├── Program.cs # Console App setup (Dependencies)...
│ └── ...
│
├── OrderBookAlgorithm.Tests/ # Unit tests (xUnit + Moq)
│ ├── OrderAlgorithmTests.cs
│ ├── OrderBookRepositoryTests.cs
│ ├── FileOrderBookRepositoryTests.cs

```

---

## **🏗️ Class Dependencies & Architecture Diagram**

### **Interface Implementations**
| Interface | Implementation | Description |
|-----------|----------------|-------------|
| `IOrderAlgorithm` | `OrderAlgorithm` | Core algorithm for finding best prices |
| `IOrderBookRepository` | `FileOrderBookRepository` | Repository for loading order book data |
| `IFileSystem` | `FileSystem` | File system abstraction for testability |

### **Dependency Graph**
```
                                ┌─────────────────────────────────────┐
                                │           OrderBookApi              │
                                │  ┌─────────────────────────────┐    │
                                │  │     OrdersEndpoints         │    │
                                │  │   (REST API Controller)     │    │
                                │  └─────────────┬───────────────┘    │
                                │                │                    │
                                │                ▼                    │
                                │  ┌─────────────────────────────┐    │
                                │  │         OrderManager        │◄───┼─── Orchestrates the process
                                │  │       (Service Layer)       │    │
                                │  └─────────────┬───────────────┘    │
                                └────────────────┼────────────────────┘
                                                 │
                                                 ▼
┌───────────────────────────────────────────────────────────────────────────────────────────────────────┐
│                                           OrderBookAlgorithm                                          │
│                                                                                                       │
│ ┌────────────────────────────┐  │  ┌────────────────────────────┐  │  ┌───────────────────────────┐   │
│ │     IOrderAlgorithm        │  │  │  IOrderBookRepository      │  │  │     Domain Classes        │   │
│ │      (Interface)           │  │  │      (Interface)           │  │  │  ┌─────────────────────┐  │   │
│ └─────────────┬──────────────┘  │  └─────────────┬──────────────┘  │  │  │   OrderBookRecord   │  │   │
│               │                 │                │                 │  │  │   Balance           │  │   │
│               ▼                 │                ▼                 │  │  │   OrderBook         │  │   │
│ ┌────────────────────────────┐  │  ┌────────────────────────────┐  │  │  │   OrderEntry        │  │   │
│ │     OrderAlgorithm         │  │  │  FileOrderBookRepository   │  │  │  │   Order             │  │   │
│ │ (Business Logic/Algorithm) │  │  │    (Data Access Layer)     │  │  │  │   OrderType (enum)  │  │   │
│ └────────────────────────────┘  │  └─────────────┬──────────────┘  │  │  │   OrderKind (enum)  │  │   │
│                                 │                │                 │  │  └─────────────────────┘  │   │
│                                 │                ▼                 │  └───────────────────────────┘   │
│                                 │  ┌────────────────────────────┐  │                                  │
│                                 │  │      IFileSystem           │  │                                  │
│                                 │  │      (Interface)           │  │                                  │
│                                 │  └─────────────┬──────────────┘  │                                  │
│                                 │                │                 │                                  │
│                                 │                ▼                 │                                  │
│                                 │  ┌────────────────────────────┐  │                                  │
│                                 │  │      FileSystem            │  │                                  │
│                                 │  │  (File System Wrapper)     │  │                                  │
│                                 │  └────────────────────────────┘  │                                  │
│                                 │                                  │                                  │
└───────────────────────────────────────────────────────────────────────────────────────────────────────┘

```

### **Data Flow**
```
HTTP Request → OrdersEndpoints → OrderManager → OrderAlgorithm → IOrderBookRepository → FileOrderBookRepository → IFileSystem → FileSystem
                                                      ↑                                                                          │
                                                      └──────────── OrderBookRecord (Domain Objects) ←─────────────────────────
```

### **Key Dependencies**
- **OrderManager** depends on:
  - `IOrderBookRepository` (data access)
  - `IOrderAlgorithm` (business logic)
- **OrderAlgorithm** depends on:
  - Domain classes (`Order`, `OrderBookRecord`, etc.)
- **FileOrderBookRepository** depends on:
  - `IFileSystem` (file operations abstraction)
  - Domain classes for deserialization
- **FileSystem** depends on:
  - System.IO namespace (concrete file operations)

---

## **🚀 Running Locally**

### **Clone the repo**
```bash
git clone https://github.com/Kravchenko-Dmitry/BestPriceOrderBook.git
cd BestPriceOrderBook
```

### **Running Locally as WebApp**

N.B.: Port number may vary

```
dotnet build
dotnet run --project OrderBookApi
http://localhost:5194/swagger
```

### **Running Locally as Console App**

```
dotnet build
dotnet run --project OrderBookConsole
```
---

## **🐳 Running in Docker**
 
### **Build image**    

from root folder (where **OrderBookSolution.sln**) is located
```
docker build -t orderbookapi -f OrderBookApi/Dockerfile .
```
### **Run container**    
```
docker run -it --rm -p 8080:8080 orderbookapi
```
### **Visit Swagger UI:** 

N.B.: In Browser please use **HTTP** protocol (and not HTTPS)
```
http://localhost:8080/swagger
```
---

## **📌 API Endpoints**

### ***POST /orders/bestprice***    
Finds the best execution plan for a given order.

### ***Request Body***
```
{
  "type": "Buy",
  "kind": "Limit",
  "amount": 1.5,
  "price": 30000
}
```

### ***Response***    
```
[
  {
    "exchange": "ExchangeA",
    "price": 29950,
    "amount": 1.0
  },
  {
    "exchange": "ExchangeB",
    "price": 30000,
    "amount": 0.5
  }
]
```
 
