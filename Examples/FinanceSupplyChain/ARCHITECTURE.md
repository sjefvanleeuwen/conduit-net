# Finance & Supply Chain Management Architecture

## Executive Summary

This document outlines a Domain-Driven Design (DDD) architecture for a distributed Finance and Supply Chain Management system built on ConduitNet. The system comprises two financial bounded contexts (Finance1: Core Accounting, Finance2: Treasury & Risk) and a Supply Chain Management context, all communicating via WebSocket-based RPC through the Conduit mesh.

---

## 1. Strategic Domain Design

### 1.1 Domain Overview

```mermaid
mindmap
  root((Enterprise Domain))
    Finance1
      General Ledger
      Accounts Payable
      Accounts Receivable
      Fixed Assets
    Finance2
      Treasury Management
      Cash Flow
      Risk Management
      Financial Planning
    Supply Chain
      Procurement
      Inventory
      Warehouse
      Logistics
```

### 1.2 Bounded Contexts & Context Map

```mermaid
graph TB
    subgraph "Core Domain"
        F1[Finance1<br/>Core Accounting]
        F2[Finance2<br/>Treasury & Risk]
    end
    
    subgraph "Supporting Domain"
        SCM[Supply Chain<br/>Management]
    end
    
    subgraph "Generic Subdomain"
        DIR[Directory Service]
        TEL[Telemetry]
        REG[Registry]
    end
    
    F1 -->|Conformist| F2
    SCM -->|Customer-Supplier| F1
    SCM -->|Published Language| F2
    
    F1 -.->|Discovers| DIR
    F2 -.->|Discovers| DIR
    SCM -.->|Discovers| DIR
    
    F1 -.->|Traces| TEL
    F2 -.->|Traces| TEL
    SCM -.->|Traces| TEL

    style F1 fill:#e1f5fe
    style F2 fill:#e1f5fe
    style SCM fill:#fff3e0
    style DIR fill:#f5f5f5
    style TEL fill:#f5f5f5
    style REG fill:#f5f5f5
```

### 1.3 Context Relationships

| Upstream | Downstream | Relationship | Description |
|----------|------------|--------------|-------------|
| Finance1 | Finance2 | Conformist | Treasury conforms to GL account structures |
| Finance1 | Supply Chain | Customer-Supplier | SCM raises purchase orders, F1 processes invoices |
| Finance2 | Supply Chain | Published Language | SCM consumes cash flow forecasts via shared contracts |

---

## 2. C4 Model Architecture

### 2.1 Level 1: System Context Diagram

```mermaid
C4Context
    title System Context Diagram - Finance & Supply Chain Platform

    Person(user, "Business User", "Finance analyst, procurement officer, warehouse manager")
    Person(admin, "System Administrator", "Manages nodes and monitors health")
    
    System(platform, "Finance & SCM Platform", "Distributed microservices platform for financial and supply chain operations")
    
    System_Ext(bank, "Banking Systems", "External banking APIs for payments and statements")
    System_Ext(erp, "Legacy ERP", "Existing enterprise resource planning system")
    System_Ext(suppliers, "Supplier Portal", "External supplier integration")
    
    Rel(user, platform, "Uses", "WebSocket/HTTP")
    Rel(admin, platform, "Monitors & Configures", "Admin UI")
    Rel(platform, bank, "Integrates", "API")
    Rel(platform, erp, "Syncs data", "Events")
    Rel(platform, suppliers, "Orders & Invoices", "EDI/API")
```

### 2.2 Level 2: Container Diagram

```mermaid
C4Container
    title Container Diagram - Finance & Supply Chain Platform

    Person(user, "Business User")
    
    Container_Boundary(conduit, "Conduit Mesh") {
        Container(dir, "Directory Node", "ConduitNet", "Service discovery and routing")
        Container(tel, "Telemetry Node", "ConduitNet", "Distributed tracing")
        Container(reg, "Registry Node", "ConduitNet", "Schema & contract registry")
    }
    
    Container_Boundary(f1, "Finance1 Context") {
        Container(gl, "General Ledger Node", "ConduitNet", "Chart of accounts, journal entries")
        Container(ap, "Accounts Payable Node", "ConduitNet", "Vendor invoices, payments")
        Container(ar, "Accounts Receivable Node", "ConduitNet", "Customer invoices, collections")
    }
    
    Container_Boundary(f2, "Finance2 Context") {
        Container(treasury, "Treasury Node", "ConduitNet", "Cash management, bank accounts")
        Container(risk, "Risk Management Node", "ConduitNet", "Financial risk analysis")
        Container(forecast, "Forecasting Node", "ConduitNet", "Cash flow projections")
    }
    
    Container_Boundary(scm, "Supply Chain Context") {
        Container(proc, "Procurement Node", "ConduitNet", "Purchase orders, vendor management")
        Container(inv, "Inventory Node", "ConduitNet", "Stock levels, reorder points")
        Container(wh, "Warehouse Node", "ConduitNet", "Locations, movements")
        Container(log, "Logistics Node", "ConduitNet", "Shipments, tracking")
    }
    
    Container(admin_ui, "Admin Dashboard", "TypeScript/Vite", "System monitoring and management")
    Container(portal, "Business Portal", "TypeScript/Vite", "User-facing application")
    
    Rel(user, portal, "Uses")
    Rel(portal, dir, "Discovers services", "WebSocket")
    Rel(admin_ui, tel, "Views traces", "WebSocket")
    
    Rel(proc, ap, "Creates invoices", "RPC")
    Rel(ap, gl, "Posts entries", "RPC")
    Rel(treasury, gl, "Reads balances", "RPC")
```

### 2.3 Level 3: Component Diagram (Finance1 - General Ledger Node)

```mermaid
C4Component
    title Component Diagram - General Ledger Node

    Container_Boundary(gl_node, "General Ledger Node") {
        Component(gl_svc, "IGeneralLedgerService", "Conduit Service", "Core GL operations")
        Component(coa_svc, "IChartOfAccountsService", "Conduit Service", "Account management")
        Component(journal_svc, "IJournalEntryService", "Conduit Service", "Journal posting")
        Component(period_svc, "IPeriodService", "Conduit Service", "Fiscal period management")
        
        Component(gl_agg, "GeneralLedgerAggregate", "Domain", "GL aggregate root")
        Component(account_entity, "Account", "Entity", "GL account entity")
        Component(journal_entity, "JournalEntry", "Entity", "Journal entry entity")
        
        Component(gl_repo, "IGeneralLedgerRepository", "Repository", "Persistence abstraction")
        Component(event_pub, "IDomainEventPublisher", "Infrastructure", "Domain event publishing")
    }
    
    Rel(gl_svc, gl_agg, "Uses")
    Rel(coa_svc, account_entity, "Manages")
    Rel(journal_svc, journal_entity, "Creates")
    Rel(gl_agg, gl_repo, "Persists via")
    Rel(gl_agg, event_pub, "Publishes events")
```

---

## 3. Domain Model

### 3.1 Finance1: Core Accounting Domain

```mermaid
classDiagram
    class GeneralLedger {
        +Guid Id
        +string CompanyCode
        +FiscalYear CurrentYear
        +PostJournalEntry(entry)
        +CloseMonth(period)
        +GetTrialBalance(asOfDate)
    }
    
    class Account {
        +Guid Id
        +string AccountNumber
        +string Name
        +AccountType Type
        +bool IsActive
        +decimal Balance
        +Debit(amount)
        +Credit(amount)
    }
    
    class JournalEntry {
        +Guid Id
        +DateTime PostingDate
        +string Description
        +JournalStatus Status
        +List~JournalLine~ Lines
        +Validate()
        +Post()
        +Reverse()
    }
    
    class JournalLine {
        +Guid AccountId
        +decimal DebitAmount
        +decimal CreditAmount
        +string CostCenter
    }
    
    class Vendor {
        +Guid Id
        +string VendorCode
        +string Name
        +PaymentTerms Terms
        +Guid PayableAccountId
    }
    
    class Invoice {
        +Guid Id
        +Guid VendorId
        +string InvoiceNumber
        +decimal Amount
        +DateTime DueDate
        +InvoiceStatus Status
        +Approve()
        +SchedulePayment()
    }
    
    GeneralLedger "1" --> "*" Account
    GeneralLedger "1" --> "*" JournalEntry
    JournalEntry "1" --> "*" JournalLine
    JournalLine --> Account
    Invoice --> Vendor
    Invoice ..> JournalEntry : creates
```

### 3.2 Finance2: Treasury & Risk Domain

```mermaid
classDiagram
    class Treasury {
        +Guid Id
        +List~BankAccount~ Accounts
        +GetCashPosition()
        +ForecastCashFlow(horizon)
        +OptimizeLiquidity()
    }
    
    class BankAccount {
        +Guid Id
        +string AccountNumber
        +string BankCode
        +Currency Currency
        +decimal CurrentBalance
        +decimal AvailableBalance
        +Reconcile(statement)
    }
    
    class CashForecast {
        +Guid Id
        +DateTime ForecastDate
        +int HorizonDays
        +List~CashFlowItem~ Inflows
        +List~CashFlowItem~ Outflows
        +Calculate()
    }
    
    class RiskAssessment {
        +Guid Id
        +RiskType Type
        +decimal Exposure
        +decimal Probability
        +RiskLevel Level
        +List~Mitigation~ Mitigations
        +Evaluate()
    }
    
    class Payment {
        +Guid Id
        +Guid BankAccountId
        +Guid VendorId
        +decimal Amount
        +PaymentMethod Method
        +PaymentStatus Status
        +Execute()
        +Confirm()
    }
    
    Treasury "1" --> "*" BankAccount
    Treasury "1" --> "*" CashForecast
    Treasury "1" --> "*" RiskAssessment
    BankAccount "1" --> "*" Payment
```

### 3.3 Supply Chain Management Domain

```mermaid
classDiagram
    class PurchaseOrder {
        +Guid Id
        +string PONumber
        +Guid SupplierId
        +DateTime OrderDate
        +POStatus Status
        +List~POLine~ Lines
        +Submit()
        +Approve()
        +Receive(receipt)
    }
    
    class POLine {
        +Guid ProductId
        +int Quantity
        +decimal UnitPrice
        +DateTime ExpectedDate
    }
    
    class Supplier {
        +Guid Id
        +string SupplierCode
        +string Name
        +SupplierRating Rating
        +List~Product~ CatalogProducts
    }
    
    class InventoryItem {
        +Guid Id
        +Guid ProductId
        +Guid WarehouseId
        +int QuantityOnHand
        +int QuantityReserved
        +int ReorderPoint
        +Reserve(qty)
        +Release(qty)
        +Adjust(qty, reason)
    }
    
    class Warehouse {
        +Guid Id
        +string Code
        +string Name
        +List~Location~ Locations
        +ReceiveGoods(receipt)
        +ShipGoods(shipment)
        +Transfer(from, to, qty)
    }
    
    class Shipment {
        +Guid Id
        +string TrackingNumber
        +Guid OriginWarehouseId
        +ShipmentStatus Status
        +DateTime EstimatedArrival
        +Track()
        +Deliver()
    }
    
    PurchaseOrder "1" --> "*" POLine
    PurchaseOrder --> Supplier
    Supplier "1" --> "*" PurchaseOrder
    InventoryItem --> Warehouse
    Warehouse "1" --> "*" InventoryItem
    Warehouse "1" --> "*" Shipment
```

---

## 4. Conduit Node Architecture

### 4.1 Node Deployment Topology

```mermaid
graph TB
    subgraph "Infrastructure Layer"
        DIR[Directory Node<br/>:5000]
        TEL[Telemetry Node<br/>:5001]
        REG[Registry Node<br/>:5004]
    end
    
    subgraph "Finance1 Nodes"
        GL[GeneralLedger Node<br/>:5010]
        AP[AccountsPayable Node<br/>:5011]
        AR[AccountsReceivable Node<br/>:5012]
        FA[FixedAssets Node<br/>:5013]
    end
    
    subgraph "Finance2 Nodes"
        TREAS[Treasury Node<br/>:5020]
        RISK[RiskMgmt Node<br/>:5021]
        FORE[Forecasting Node<br/>:5022]
    end
    
    subgraph "Supply Chain Nodes"
        PROC[Procurement Node<br/>:5030]
        INV[Inventory Node<br/>:5031]
        WH[Warehouse Node<br/>:5032]
        LOG[Logistics Node<br/>:5033]
    end
    
    GL & AP & AR & FA --> DIR
    TREAS & RISK & FORE --> DIR
    PROC & INV & WH & LOG --> DIR
    
    GL & AP & AR & FA -.-> TEL
    TREAS & RISK & FORE -.-> TEL
    PROC & INV & WH & LOG -.-> TEL
```

### 4.2 Service Interface Contracts

```
ConduitNet.Examples.FinanceSupplyChain/
├── Contracts/
│   ├── Finance1/
│   │   ├── IGeneralLedgerService.cs
│   │   ├── IAccountsPayableService.cs
│   │   ├── IAccountsReceivableService.cs
│   │   └── DTOs/
│   │       ├── AccountDto.cs
│   │       ├── JournalEntryDto.cs
│   │       └── InvoiceDto.cs
│   ├── Finance2/
│   │   ├── ITreasuryService.cs
│   │   ├── IRiskManagementService.cs
│   │   ├── IForecastingService.cs
│   │   └── DTOs/
│   │       ├── CashPositionDto.cs
│   │       ├── RiskAssessmentDto.cs
│   │       └── ForecastDto.cs
│   └── SupplyChain/
│       ├── IProcurementService.cs
│       ├── IInventoryService.cs
│       ├── IWarehouseService.cs
│       ├── ILogisticsService.cs
│       └── DTOs/
│           ├── PurchaseOrderDto.cs
│           ├── InventoryItemDto.cs
│           └── ShipmentDto.cs
```

---

## 5. Integration Patterns

### 5.1 Purchase-to-Pay Flow

```mermaid
sequenceDiagram
    participant User
    participant Procurement as Procurement Node
    participant Inventory as Inventory Node
    participant AP as Accounts Payable Node
    participant GL as General Ledger Node
    participant Treasury as Treasury Node
    
    User->>Procurement: Create Purchase Order
    activate Procurement
    Procurement->>Inventory: Check Stock Levels
    Inventory-->>Procurement: Stock Below Reorder Point
    Procurement->>Procurement: Submit PO
    Procurement-->>User: PO Created (PO-2024-001)
    deactivate Procurement
    
    Note over Procurement,Inventory: Goods Received
    
    Procurement->>Inventory: Record Receipt
    activate Inventory
    Inventory->>Inventory: Update Stock
    Inventory-->>Procurement: Receipt Confirmed
    deactivate Inventory
    
    Procurement->>AP: Create Invoice
    activate AP
    AP->>AP: Three-Way Match (PO, Receipt, Invoice)
    AP->>GL: Post Accrual Entry
    GL-->>AP: Entry Posted
    AP-->>Procurement: Invoice Approved
    deactivate AP
    
    Note over AP,Treasury: Payment Due Date
    
    AP->>Treasury: Request Payment
    activate Treasury
    Treasury->>Treasury: Check Cash Position
    Treasury->>GL: Post Payment Entry
    GL-->>Treasury: Entry Posted
    Treasury-->>AP: Payment Executed
    deactivate Treasury
```

### 5.2 Month-End Close Flow

```mermaid
sequenceDiagram
    participant Controller
    participant GL as General Ledger Node
    participant AP as Accounts Payable Node
    participant AR as Accounts Receivable Node
    participant Treasury as Treasury Node
    participant Risk as Risk Management Node
    
    Controller->>GL: Initiate Month-End Close
    activate GL
    
    par Parallel Reconciliation
        GL->>AP: Request AP Reconciliation
        AP-->>GL: AP Subledger Balanced
    and
        GL->>AR: Request AR Reconciliation
        AR-->>GL: AR Subledger Balanced
    and
        GL->>Treasury: Request Bank Reconciliation
        Treasury-->>GL: Bank Accounts Reconciled
    end
    
    GL->>GL: Run Trial Balance
    GL->>GL: Post Adjusting Entries
    GL->>GL: Close Period
    
    GL->>Treasury: Publish Final Balances
    Treasury->>Risk: Update Risk Exposure
    Risk->>Risk: Calculate Month-End Risk Metrics
    
    GL-->>Controller: Month-End Complete
    deactivate GL
```

### 5.3 Cash Flow Forecasting

```mermaid
sequenceDiagram
    participant Forecast as Forecasting Node
    participant AP as Accounts Payable Node
    participant AR as Accounts Receivable Node
    participant Procurement as Procurement Node
    participant Treasury as Treasury Node
    
    Forecast->>Forecast: Initialize 90-Day Forecast
    activate Forecast
    
    par Gather Cash Flow Data
        Forecast->>AP: Get Scheduled Payments
        AP-->>Forecast: Payment Schedule (Outflows)
    and
        Forecast->>AR: Get Expected Collections
        AR-->>Forecast: Collection Schedule (Inflows)
    and
        Forecast->>Procurement: Get Committed POs
        Procurement-->>Forecast: Future Commitments
    and
        Forecast->>Treasury: Get Current Cash Position
        Treasury-->>Forecast: Bank Balances
    end
    
    Forecast->>Forecast: Calculate Daily Cash Position
    Forecast->>Forecast: Identify Cash Gaps
    Forecast->>Treasury: Publish Forecast
    Treasury->>Treasury: Plan Liquidity Actions
    
    Forecast-->>Forecast: Forecast Complete
    deactivate Forecast
```

---

## 6. Application Architecture

### 6.1 Node Internal Architecture (Hexagonal/Clean Architecture)

```
┌─────────────────────────────────────────────────────────────────┐
│                        Conduit Node                              │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                 Application Layer                         │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │   │
│  │  │  Commands   │  │   Queries   │  │   Events    │      │   │
│  │  │  Handlers   │  │  Handlers   │  │  Handlers   │      │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                   Domain Layer                            │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │   │
│  │  │ Aggregates  │  │  Entities   │  │Value Objects│      │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘      │   │
│  │  ┌─────────────┐  ┌─────────────┐                        │   │
│  │  │Domain Events│  │Domain Svc   │                        │   │
│  │  └─────────────┘  └─────────────┘                        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │               Infrastructure Layer                        │   │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌──────────┐ │   │
│  │  │Conduit Svc│ │Repository │ │ Event Bus │ │ External │ │   │
│  │  │ Adapters  │ │   Impl    │ │   Impl    │ │ Adapters │ │   │
│  │  └───────────┘ └───────────┘ └───────────┘ └──────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────┤
│                    ConduitNet Framework                          │
│  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐       │
│  │ WebSocket │ │ Discovery │ │ Telemetry │ │Serializer │       │
│  │   RPC     │ │  Client   │ │  Export   │ │ MessagePk │       │
│  └───────────┘ └───────────┘ └───────────┘ └───────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Project Structure

```
Examples/
└── FinanceSupplyChain/
    ├── ARCHITECTURE.md                    # This document
    │
    ├── src/
    │   ├── Contracts/                     # Shared contracts (Published Language)
    │   │   ├── FinanceSupplyChain.Contracts.csproj
    │   │   ├── Finance1/
    │   │   │   ├── IGeneralLedgerService.cs
    │   │   │   ├── IAccountsPayableService.cs
    │   │   │   ├── IAccountsReceivableService.cs
    │   │   │   └── DTOs/
    │   │   ├── Finance2/
    │   │   │   ├── ITreasuryService.cs
    │   │   │   ├── IRiskManagementService.cs
    │   │   │   └── DTOs/
    │   │   └── SupplyChain/
    │   │       ├── IProcurementService.cs
    │   │       ├── IInventoryService.cs
    │   │       └── DTOs/
    │   │
    │   ├── Finance1/                      # Finance1 Bounded Context
    │   │   ├── GeneralLedger/
    │   │   │   ├── GeneralLedger.Node.csproj
    │   │   │   ├── Program.cs
    │   │   │   ├── Domain/
    │   │   │   │   ├── Aggregates/
    │   │   │   │   ├── Entities/
    │   │   │   │   ├── ValueObjects/
    │   │   │   │   └── Events/
    │   │   │   ├── Application/
    │   │   │   │   ├── Commands/
    │   │   │   │   ├── Queries/
    │   │   │   │   └── Services/
    │   │   │   └── Infrastructure/
    │   │   │       ├── Repositories/
    │   │   │       └── ConduitServices/
    │   │   │           └── GeneralLedgerService.cs
    │   │   │
    │   │   ├── AccountsPayable/
    │   │   │   └── ... (same structure)
    │   │   │
    │   │   └── AccountsReceivable/
    │   │       └── ... (same structure)
    │   │
    │   ├── Finance2/                      # Finance2 Bounded Context
    │   │   ├── Treasury/
    │   │   ├── RiskManagement/
    │   │   └── Forecasting/
    │   │
    │   └── SupplyChain/                   # Supply Chain Bounded Context
    │       ├── Procurement/
    │       ├── Inventory/
    │       ├── Warehouse/
    │       └── Logistics/
    │
    ├── tests/
    │   ├── Finance1.Tests/
    │   ├── Finance2.Tests/
    │   ├── SupplyChain.Tests/
    │   └── Integration.Tests/
    │
    └── scripts/
        ├── run-finance1.ps1
        ├── run-finance2.ps1
        ├── run-supplychain.ps1
        └── run-all.ps1
```

---

## 7. Data Consistency Patterns

### 7.1 Saga Pattern for Cross-Context Transactions

```mermaid
stateDiagram-v2
    [*] --> POCreated: Create Purchase Order
    POCreated --> StockReserved: Reserve Inventory
    StockReserved --> InvoiceCreated: Create Invoice
    InvoiceCreated --> PaymentScheduled: Schedule Payment
    PaymentScheduled --> [*]: Complete
    
    StockReserved --> StockReleased: Compensate
    StockReleased --> POCancelled: Compensate
    POCancelled --> [*]: Rollback Complete
    
    InvoiceCreated --> InvoiceVoided: Compensate
    InvoiceVoided --> StockReleased
    
    PaymentScheduled --> PaymentCancelled: Compensate
    PaymentCancelled --> InvoiceVoided
```

### 7.2 Event-Driven Integration

```mermaid
flowchart LR
    subgraph "Procurement Node"
        PO[PurchaseOrder]
        POE[PurchaseOrderApproved Event]
    end
    
    subgraph "Event Bus"
        EB((Domain Events))
    end
    
    subgraph "Inventory Node"
        INV_H[Event Handler]
        INV[InventoryItem]
    end
    
    subgraph "AP Node"
        AP_H[Event Handler]
        AP[Invoice]
    end
    
    PO -->|Publishes| POE
    POE --> EB
    EB -->|Subscribes| INV_H
    EB -->|Subscribes| AP_H
    INV_H -->|Updates| INV
    AP_H -->|Creates| AP
```

---

## 8. Security & Access Control

### 8.1 Service-Level Authorization

```mermaid
flowchart TB
    subgraph "Client"
        REQ[RPC Request]
    end
    
    subgraph "Conduit Node"
        AUTH[Authorization Filter]
        SVC[Service Handler]
    end
    
    subgraph "ACL Service"
        ACL[Access Control]
    end
    
    REQ --> AUTH
    AUTH -->|Check Permission| ACL
    ACL -->|Allowed/Denied| AUTH
    AUTH -->|If Allowed| SVC
    AUTH -->|If Denied| REQ
```

### 8.2 Role-Based Access Matrix

| Role | Finance1 | Finance2 | Supply Chain |
|------|----------|----------|--------------|
| Finance Manager | Full | Read | Read |
| Treasury Analyst | Read | Full | None |
| Procurement Officer | Invoice View | Forecast View | Full |
| Warehouse Staff | None | None | Inventory/Warehouse |
| Auditor | Read | Read | Read |

---

## 9. Deployment Configuration

### 9.1 Development Environment

```powershell
# run-all.ps1
# Start all Finance & Supply Chain nodes for development

# Infrastructure
Start-ConduitService -Project "ConduitNet.Directory" -Port 5000 -Title "Directory"
Start-Sleep -Seconds 2
Start-ConduitService -Project "ConduitNet.Telemetry" -Port 5001 -Title "Telemetry"

# Finance1 Context
Start-ConduitService -Project "GeneralLedger.Node" -Port 5010 -Title "General Ledger"
Start-ConduitService -Project "AccountsPayable.Node" -Port 5011 -Title "Accounts Payable"
Start-ConduitService -Project "AccountsReceivable.Node" -Port 5012 -Title "Accounts Receivable"

# Finance2 Context
Start-ConduitService -Project "Treasury.Node" -Port 5020 -Title "Treasury"
Start-ConduitService -Project "RiskManagement.Node" -Port 5021 -Title "Risk Management"
Start-ConduitService -Project "Forecasting.Node" -Port 5022 -Title "Forecasting"

# Supply Chain Context
Start-ConduitService -Project "Procurement.Node" -Port 5030 -Title "Procurement"
Start-ConduitService -Project "Inventory.Node" -Port 5031 -Title "Inventory"
Start-ConduitService -Project "Warehouse.Node" -Port 5032 -Title "Warehouse"
Start-ConduitService -Project "Logistics.Node" -Port 5033 -Title "Logistics"
```

### 9.2 Port Allocation

| Context | Node | Port |
|---------|------|------|
| Infrastructure | Directory | 5000 |
| Infrastructure | Telemetry | 5001 |
| Infrastructure | Registry | 5004 |
| Finance1 | General Ledger | 5010 |
| Finance1 | Accounts Payable | 5011 |
| Finance1 | Accounts Receivable | 5012 |
| Finance1 | Fixed Assets | 5013 |
| Finance2 | Treasury | 5020 |
| Finance2 | Risk Management | 5021 |
| Finance2 | Forecasting | 5022 |
| Supply Chain | Procurement | 5030 |
| Supply Chain | Inventory | 5031 |
| Supply Chain | Warehouse | 5032 |
| Supply Chain | Logistics | 5033 |

---

## 10. Next Steps

1. **Phase 1: Core Infrastructure**
   - [ ] Create shared Contracts project
   - [ ] Define service interfaces for each bounded context
   - [ ] Create DTOs with MessagePack serialization

2. **Phase 2: Finance1 Implementation**
   - [ ] Implement General Ledger Node
   - [ ] Implement Accounts Payable Node
   - [ ] Implement Accounts Receivable Node
   - [ ] Integration tests

3. **Phase 3: Finance2 Implementation**
   - [ ] Implement Treasury Node
   - [ ] Implement Risk Management Node
   - [ ] Implement Forecasting Node
   - [ ] Cross-context integration

4. **Phase 4: Supply Chain Implementation**
   - [ ] Implement Procurement Node
   - [ ] Implement Inventory Node
   - [ ] Implement Warehouse Node
   - [ ] Implement Logistics Node

5. **Phase 5: Integration & Testing**
   - [ ] End-to-end Purchase-to-Pay flow
   - [ ] Month-end close process
   - [ ] Performance testing
   - [ ] Security audit

---

## Appendix A: Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 9.0 |
| RPC Framework | ConduitNet (WebSocket + MessagePack) |
| Service Discovery | ConduitNet Directory |
| Observability | OpenTelemetry + ConduitNet Telemetry |
| Serialization | MessagePack |
| Frontend | TypeScript + Vite |
| Database | TBD (PostgreSQL recommended) |
| Event Store | TBD (EventStoreDB recommended) |

## Appendix B: References

- [Domain-Driven Design by Eric Evans](https://www.domainlanguage.com/ddd/)
- [C4 Model by Simon Brown](https://c4model.com/)
- [ConduitNet Documentation](../README.md)
- [Implementing Domain-Driven Design by Vaughn Vernon](https://www.informit.com/store/implementing-domain-driven-design-9780321834577)
