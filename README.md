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


## Criar a branch develop

Estando na master, crie e mude para a develop:
git checkout master
git pull origin master  # Garante que você está atualizado
git checkout -b develop
git push -u origin develop


## Criação de uma funcionalidade (Feature)

1. Garante que está na develop e atualizado
    git checkout develop
    git pull origin develop

2. Cria a branch específica da funcionalidade
    git checkout -b feature/login-social

3. Após finalizar o trabalho, envia para o servidor
    git add .
    git commit -m "feat: implementado login com Google"
    git push -u origin feature/login-social


## Mesclando a Feature na Develop

Após o código ser revisado (via Pull Request ou comando):
git checkout develop
git merge feature/login-social
git push origin develop

#Opcional: deletar a branch de feature que já foi mesclada
git branch -d feature/login-social


## Criando um Release (da Develop para Master)

Quando a develop estiver pronta para ir ao ar:
git checkout master
git merge develop
git push origin master


## Criar a branch de Hotfix a partir da Master

1. Criar a branch de Hotfix a partir da Master
    git checkout master
    git pull origin master
    git checkout -b hotfix/correcao-urgente-login

2. Corrigir o erro e commitar
    git add .
    git commit -m "fix: corrigido erro crítico no login de usuários"

3. Levar a correção para a Produção (Master) => para corrigir o erro em produção agora
    git checkout master
    git merge hotfix/correcao-urgente-login
    git push origin master

4. Levar a correção para a Develop => Fundamental para garantir que o erro não volte na próxima release
    git checkout develop
    git merge hotfix/correcao-urgente-login
    git push origin develop

5. Deletar a branch de hotfix
    git branch -d hotfix/correcao-urgente-login



# Por que não criar o Hotfix a partir da Develop?
Se você criar a partir da develop, você corre o risco de levar funcionalidades que ainda estão "em testes" ou incompletas para a master junto com a sua correção. O Hotfix deve conter apenas o código necessário para resolver o problema urgente.