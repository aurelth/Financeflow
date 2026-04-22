# FinanceFlow

> **Controle total das suas finanças, potencializado por IA**

FinanceFlow é uma aplicação web de gestão financeira pessoal construída com .NET 9 e React 19. Oferece controle completo de receitas, despesas, orçamentos e metas — com um assistente de IA integrado que responde perguntas sobre as suas finanças em linguagem natural.

[![CI Backend](https://github.com/aurelth/Financeflow/actions/workflows/backend.yml/badge.svg)](https://github.com/aurelth/Financeflow/actions)
[![CI Frontend](https://github.com/aurelth/Financeflow/actions/workflows/frontend.yml/badge.svg)](https://github.com/aurelth/Financeflow/actions)

---

## Screenshots

| Dashboard | Assistente IA |
|---|---|
| ![Dashboard](docs/screenshots/dashboard.png) | ![Assistente IA](docs/screenshots/assistant.png) |

| Saúde Financeira | Metas Financeiras |
|---|---|
| ![Saúde Financeira](docs/screenshots/health-score.png) | ![Metas Financeiras](docs/screenshots/goals.png) |

---

## Funcionalidades

- **Transações** — registo de receitas, despesas e transferências com anexos, tags e recorrência mensal automática
- **Categorias** — categorias e subcategorias personalizadas por tipo de transação
- **Orçamentos** — limites mensais por categoria com acompanhamento em tempo real
- **Metas Financeiras** — objetivos com progresso automático baseado na poupança real e distribuição proporcional
- **Dashboard** — visão geral com gráficos de evolução do saldo, despesas por categoria e comparação semanal
- **Comparativo Histórico** — comparação de receitas e despesas entre períodos
- **Relatórios** — geração de relatórios em PDF e Excel com exportação assíncrona via SignalR
- **Importação OFX** — importação de extratos bancários no formato OFX
- **Score de Saúde Financeira** — pontuação de 0 a 100 baseada em 6 critérios com histórico dos últimos 6 meses
- **Assistente IA** — chat com Claude (Anthropic) com contexto financeiro real do utilizador
- **Notificações** — alertas de vencimento de transações em tempo real via SignalR
- **Perfil e Preferências** — idioma (PT, EN, ES, FR), tema, alteração de senha e reset por e-mail
- **Administração** — gestão de utilizadores e categorias padrão

---

## Stack Técnica

### Backend
- **.NET 9** — Clean Architecture (Domain, Application, Infrastructure, API, Workers)
- **CQRS** com MediatR
- **Entity Framework Core** — SQL Server 2022
- **Apache Kafka** — eventos assíncronos (alertas de orçamento)
- **Redis** — cache e deduplicação de notificações
- **SignalR** — notificações e progresso de relatórios em tempo real
- **Quartz.NET** — jobs agendados (verificação de vencimentos)
- **JWT** — autenticação e autorização
- **SendGrid** — envio de e-mails (reset de senha)
- **Anthropic API** — integração com Claude para o assistente IA

### Frontend
- **React 19** + **TypeScript** + **Vite**
- **TanStack Query v5** — gestão de estado servidor
- **Zustand** — estado global (autenticação)
- **React Hook Form v7** + **Zod v4** — formulários e validação
- **Tailwind CSS v3** + **shadcn/ui** — componentes e estilos
- **Recharts** — gráficos e visualizações
- **i18next** — internacionalização (PT, EN, ES, FR)

### Infraestrutura Local
- **Docker Compose** — SQL Server 2022, Apache Kafka, Zookeeper, Redis

### Testes
- **xUnit** — testes unitários e de integração (backend)
- **TestContainers** — SQL Server e Redis em containers para testes de integração
- **Vitest** + **Testing Library** — testes de componentes (frontend)
- **Playwright** — 31 testes E2E (Chromium)

---

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [Azure Data Studio](https://azure.microsoft.com/products/data-studio) *(opcional)*

---

## Setup Local

### 1. Clonar o repositório

```bash
git clone https://github.com/aurelth/Financeflow.git
cd Financeflow
```

### 2. Configurar variáveis de ambiente

Cria o ficheiro `backend/FinanceFlow.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FinanceFlow;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-chars",
    "Issuer": "FinanceFlow",
    "Audience": "FinanceFlow"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "Anthropic": {
    "ApiKey": "your-anthropic-api-key"
  },
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "FinanceFlow"
  },
  "WorkerServiceToken": "your-worker-service-token"
}
```

Cria o ficheiro `frontend/.env.development`:

```env
VITE_API_URL=https://localhost:7195
```

### 3. Subir a infraestrutura

```bash
cd infra/docker
docker compose up -d
```

Aguarda os containers ficarem prontos (SQL Server, Kafka, Redis).

### 4. Rodar o backend

Abre `backend/FinanceFlow.sln` no **Visual Studio 2022** e pressiona `F5`.

As migrations são aplicadas automaticamente na primeira execução.

### 5. Rodar o frontend

```bash
cd frontend
npm install
npm run dev
```

Acede a [http://localhost:3000](http://localhost:3000).

---

## Testes

### Backend

```bash
# Testes unitários
cd backend
dotnet test FinanceFlow.UnitTests/FinanceFlow.UnitTests.csproj

# Testes de integração (requer Docker)
dotnet test FinanceFlow.IntegrationTests/FinanceFlow.IntegrationTests.csproj
```

### Frontend

```bash
cd frontend

# Testes de componentes (Vitest)
npm run test

# Testes E2E (Playwright) — requer backend e frontend a correr
npx playwright test --project=chromium
```

---

## Estrutura do Projeto

```
financeflow/
├── backend/
│   ├── FinanceFlow.Domain/           → Entidades e interfaces
│   ├── FinanceFlow.Application/      → CQRS, handlers, services
│   ├── FinanceFlow.Infrastructure/   → EF Core, repositórios, integrações
│   ├── FinanceFlow.API/              → Controllers, middleware, startup
│   ├── FinanceFlow.Workers/          → Jobs Quartz.NET e consumers Kafka
│   ├── FinanceFlow.UnitTests/        → Testes unitários
│   └── FinanceFlow.IntegrationTests/ → Testes de integração
├── frontend/
│   ├── src/
│   │   ├── features/                 → Módulos por funcionalidade
│   │   ├── components/               → Componentes partilhados
│   │   ├── store/                    → Estado global (Zustand)
│   │   ├── lib/                      → Utilitários e configurações
│   │   └── tests/                    → Testes Vitest
│   └── e2e/                          → Testes Playwright
├── infra/
│   └── docker/                       → Docker Compose
└── docs/
    └── screenshots/                  → Screenshots da aplicação
```

---

## Arquitetura

O backend segue **Clean Architecture** com separação clara de camadas:

```
API / Workers
    ↓
Application (CQRS — Commands, Queries, Handlers)
    ↓
Domain (Entidades, Interfaces, Value Objects)
    ↑
Infrastructure (EF Core, Repositórios, Kafka, Redis, SignalR)
```

O Worker comunica-se com a API via HTTP usando um service token — nunca acede à base de dados diretamente.

---

## Git Flow

| Branch | Descrição |
|---|---|
| `master` | Produção — código estável |
| `develop` | Desenvolvimento — integração contínua |
| `feature/fase{N}-{nome}` | Funcionalidades em desenvolvimento |
| `hotfix/{nome}` | Correções urgentes em produção |

### Criar uma feature

```bash
git checkout develop
git pull origin develop
git checkout -b feature/fase27-deploy
```

### Fazer merge para develop

```bash
git checkout develop
git merge feature/fase27-deploy
git push origin develop
```

### Criar um hotfix

```bash
git checkout master
git pull origin master
git checkout -b hotfix/correcao-urgente

# ... corrigir o bug ...

git checkout master
git merge hotfix/correcao-urgente
git push origin master

git checkout develop
git merge hotfix/correcao-urgente
git push origin develop
```

---


## Documentação

- [Diagrama de Arquitetura](docs/architecture.svg)
- [Diagrama de Base de Dados](docs/database.svg)
- [Guia de Contribuição](CONTRIBUTING.md)


---


## Roadmap

| Versão | Fase | Status |
|---|---|---|
| v0.1.0 — v0.11.0 | Setup, Auth, Categorias, Transações, Orçamentos, Dashboard, Relatórios, Exportações, Notificações, Reset de Senha, Perfil | ✅ Concluído |
| v0.12.0 | Transações Recorrentes | ✅ Concluído |
| v0.22.0 | Assistente Financeiro com IA | ✅ Concluído |
| v0.23.0 | Score de Saúde Financeira | ✅ Concluído |
| v0.24.0 | Metas Financeiras | ✅ Concluído |
| v0.25.0 | Testes E2E com Playwright | ✅ Concluído |
| v0.26.0 | Documentação | ✅ Concluído |
| v0.27.0 | Deploy VPS Hostinger | ✅ Concluído |
| v0.28.0 | Observabilidade (OpenTelemetry, Grafana, Prometheus) | ✅ Concluído |
| v0.29.0 | CI/CD E2E | ✅ Concluído |
| v0.30.0 | Mobile (React Native) | ✅ Concluído |

---

## Licença

Este projeto está licenciado sob a [MIT License](LICENSE).

---

Desenvolvido por **Aurel Lossou**
