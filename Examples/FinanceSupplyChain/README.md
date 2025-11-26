# Finance & Supply Chain Example

A comprehensive Finance and Supply Chain Management system built with ConduitNet distributed RPC framework.

## Architecture

This example demonstrates a Domain-Driven Design (DDD) architecture with three bounded contexts:

### Finance1 (Core Accounting)
- **GeneralLedger** (Port 5101) - Chart of accounts, journal entries, trial balance
- **AccountsPayable** (Port 5102) - Vendor management, invoices, payments
- **AccountsReceivable** (Port 5103) - Customer management, invoices, collections

### Finance2 (Treasury & Planning)
- **Treasury** (Port 5111) - Bank accounts, cash positions, transfers
- **Forecasting** (Port 5113) - Cash flow projections, scenarios, alerts

### SupplyChain
- **Procurement** (Port 5121) - Purchase orders, suppliers, requisitions
- **Inventory** (Port 5122) - Stock levels, items, valuation

## Prerequisites

- .NET 9.0 SDK
- Node.js 18+
- PowerShell 5.1+

## Quick Start

```powershell
# Navigate to the example directory
cd Examples\FinanceSupplyChain

# Run the demo script
.\run-demo.ps1
```

This will:
1. Start the Directory Service (port 5000)
2. Start all Finance and Supply Chain services
3. Start the frontend application (port 3002)
4. Open your browser to http://localhost:3002

## Manual Start

If you prefer to start services individually:

```powershell
# 1. Start Directory (from ConduitNet root)
cd ConduitNet\ConduitNet.System\ConduitNet.Directory
dotnet run

# 2. Start Finance1 services
cd Examples\FinanceSupplyChain\src\Finance1\GeneralLedger.Node
dotnet run

cd ..\AccountsPayable.Node
dotnet run

cd ..\AccountsReceivable.Node
dotnet run

# 3. Start Finance2 services
cd ..\..\Finance2\Treasury.Node
dotnet run

cd ..\Forecasting.Node
dotnet run

# 4. Start SupplyChain services
cd ..\..\SupplyChain\Procurement.Node
dotnet run

cd ..\Inventory.Node
dotnet run

# 5. Start Frontend
cd ..\..\..\app
npm install
npm run dev
```

## Project Structure

```
Examples/FinanceSupplyChain/
├── FinanceSupplyChain.sln
├── run-demo.ps1
├── ARCHITECTURE.md
├── app/                          # Vite/TypeScript Frontend
│   ├── src/
│   │   ├── components/
│   │   │   ├── pages/           # Page components
│   │   │   ├── app-layout.ts
│   │   │   ├── app-sidebar.ts
│   │   │   └── app-header.ts
│   │   ├── services/
│   │   │   └── ConduitService.ts
│   │   └── style.css
│   └── package.json
└── src/
    ├── Contracts/               # Shared interfaces and DTOs
    │   ├── Finance1/
    │   ├── Finance2/
    │   └── SupplyChain/
    ├── Finance1/                # Finance1 bounded context
    │   ├── GeneralLedger.Node/
    │   ├── AccountsPayable.Node/
    │   └── AccountsReceivable.Node/
    ├── Finance2/                # Finance2 bounded context
    │   ├── Treasury.Node/
    │   └── Forecasting.Node/
    └── SupplyChain/             # SupplyChain bounded context
        ├── Procurement.Node/
        └── Inventory.Node/
```

## Features

### Dashboard
- Cash position overview
- AR/AP aging summaries
- Low stock alerts

### General Ledger
- Chart of accounts management
- Trial balance generation
- Journal entry posting

### Accounts Payable
- Vendor management
- Invoice processing
- Payment tracking
- AP aging analysis

### Accounts Receivable
- Customer management
- Invoice generation
- Payment collection
- AR aging analysis

### Treasury
- Bank account management
- Cash position monitoring
- Inter-account transfers
- Multi-currency support

### Cash Flow Forecast
- 30-day cash projections
- Weekly cash flow summaries
- Forecast alerts
- Scenario analysis

### Inventory
- Stock level monitoring
- Low stock alerts
- Stock valuation (FIFO)
- Item master management

### Procurement
- Supplier management
- Purchase order processing
- Requisition workflow
- Supplier performance tracking

## Technology Stack

- **Backend**: .NET 9.0, ConduitNet RPC Framework
- **Frontend**: TypeScript, Vite, Web Components
- **Communication**: WebSocket, MessagePack serialization
- **Service Discovery**: ConduitNet Directory Service

## Architecture Diagrams

See `ARCHITECTURE.md` for detailed C4 diagrams including:
- System Context
- Container diagrams
- Component diagrams
- Sequence diagrams
