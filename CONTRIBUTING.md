# Contribuindo para o FinanceFlow

Obrigado pelo interesse em contribuir! Este documento descreve como configurar o ambiente de desenvolvimento e as convenções do projeto.

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET 9 SDK](https://dotnet.microsoft.com/)
- [Node.js 20+](https://nodejs.org/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)

## Setup do ambiente

### 1. Fork e clone

```bash
git clone https://github.com/aurelth/Financeflow.git
cd Financeflow
```

### 2. Configurar variáveis de ambiente

Cria `backend/FinanceFlow.API/appsettings.Development.json` com as tuas credenciais locais (ver [README](README.md#configurar-variáveis-de-ambiente)).

Cria `frontend/.env.development`:
```env
VITE_API_URL=https://localhost:7195
```

### 3. Subir infraestrutura

```bash
cd infra/docker
docker compose up -d
```

### 4. Rodar o projeto

Backend: Visual Studio 2022 → `F5`

Frontend:
```bash
cd frontend
npm install
npm run dev
```

## Fluxo de trabalho

### Branches

| Padrão | Uso |
|---|---|
| `feature/fase{N}-{nome}` | Nova funcionalidade |
| `fix/{nome}` | Correção de bug |
| `hotfix/{nome}` | Correção urgente em produção |
| `docs/{nome}` | Documentação |

### Commits

Seguimos [Conventional Commits](https://www.conventionalcommits.org/) em português brasileiro no particípio passado:

```
feat: funcionalidade de exportação adicionada
fix: erro de validação corrigido no formulário de transações
docs: diagrama de arquitetura adicionado
test: testes de integração para metas adicionados
refactor: repositório de categorias refatorado
chore: dependências atualizadas
```

### Pull Requests

1. Cria uma branch a partir de `develop`
2. Implementa a funcionalidade com testes
3. Garante que todos os testes passam
4. Abre PR para `develop` com título e descrição claros
5. Aguarda revisão

## Testes

Todos os testes devem passar antes de fazer commit:

```bash
# Backend — testes unitários
cd backend
dotnet test FinanceFlow.UnitTests/FinanceFlow.UnitTests.csproj

# Backend — testes de integração
dotnet test FinanceFlow.IntegrationTests/FinanceFlow.IntegrationTests.csproj

# Frontend — testes de componentes
cd frontend
npm run test

# Frontend — build
npm run build
```

## Convenções de código

### Backend (.NET)

- **Arquitetura**: Clean Architecture — respeitar a separação de camadas
- **CQRS**: comandos em `Application/Features/{Feature}/Commands/`, queries em `Application/Features/{Feature}/Queries/`
- **Repositórios**: sempre via interface definida em `Domain/Interfaces/`
- **Validação**: FluentValidation em todos os commands
- **Comentários**: em português brasileiro
- **Modificações**: anotar linhas alteradas com `// Adicionado` ou `// Modificado`
- **EF Core**: nunca queries paralelas com `Task.WhenAll` no mesmo `DbContext`

### Frontend (React/TypeScript)

- **Tailwind**: manter na v3 (v4 incompatível com shadcn/ui)
- **Zod v4**: usar sintaxe `z.enum(['A', 'B'], { error: '...' })`
- **Datas UTC**: sempre adicionar sufixo `Z` antes de parsear — `` `${dateStr}Z` ``
- **Downloads**: usar axios com `responseType: 'blob'` + `window.URL.createObjectURL`
- **SignalR**: usar `HttpTransportType.LongPolling` com URL direta

## Estrutura de um novo módulo

Para adicionar uma nova funcionalidade, segue este padrão:

### Backend
```
Application/Features/{Feature}/
├── Commands/
│   ├── Create{Feature}Command.cs
│   ├── Create{Feature}CommandHandler.cs
│   └── Create{Feature}CommandValidator.cs
├── Queries/
│   ├── Get{Feature}Query.cs
│   └── Get{Feature}QueryHandler.cs
└── DTOs/
    └── {Feature}Dto.cs
```

### Frontend
```
src/features/{feature}/
├── api/
│   └── use{Feature}.ts
├── components/
│   └── {Feature}Card.tsx
├── types/
│   └── {feature}.types.ts
└── pages/
    └── {Feature}Page.tsx
```

## Dúvidas

Abre uma [issue](https://github.com/aurelth/Financeflow/issues) com a etiqueta `question`.
