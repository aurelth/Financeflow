# FinanceFlow

Sistema de gestão financeira pessoal — controle de receitas, despesas, orçamentos e relatórios.

## Stack

- **Backend**: .NET 10, EF Core, MediatR, Kafka, SignalR, Quartz.NET
- **Frontend**: React 19, Vite, TypeScript, shadcn/ui, Recharts
- **Infra**: SQL Server 2022, Apache Kafka, Redis (Docker)

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [Azure Data Studio](https://azure.microsoft.com/products/data-studio)

## Setup local

### 1. Subir infraestrutura
```bash
cd infra/docker
docker compose up -d
```

### 2. Rodar o backend
Abra `backend/FinanceFlow.sln` no Visual Studio 2022 e pressione `F5`.

### 3. Rodar o frontend
```bash
cd frontend
npm install
npm run dev
```

## Estrutura do projeto

```
financeflow/
├── backend/     → Solução .NET 10 (Clean Architecture)
├── frontend/    → React 19 + Vite
├── infra/
│   └── docker/  → Docker Compose (SQL Server, Kafka, Redis)
└── docs/        → Diagramas e documentação
```

## Branches

| Branch | Descrição |
|--------|-----------|
| `master` | Produção |
| `develop` | Desenvolvimento / staging |
| `feature/*` | Novas funcionalidades |