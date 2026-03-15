
# FinanceFlow — Tecnologias, Ferramentas e Decisões Técnicas

Este documento descreve todas as tecnologias, ferramentas e padrões utilizados no projeto FinanceFlow, com a justificativa de cada escolha.

# Sumário
# Visão Geral da Arquitectura
# Backend
# Frontend
# Infraestrutura Local
# Testes
# CI/CD
# Controlo de Versão
# Ferramentas de Desenvolvimento

1. Visão Geral da Arquitectura {#arquitectura}

Clean Architecture
O backend foi construído seguindo os princípios da Clean Architecture (Robert C. Martin), organizada em camadas com dependências apontando sempre para o centro:

FinanceFlow.API           → Camada de apresentação (Controllers, Middlewares)
FinanceFlow.Application   → Casos de uso (Commands, Queries, DTOs)
FinanceFlow.Domain        → Regras de negócio (Entidades, Interfaces, Enums)
FinanceFlow.Infrastructure → Detalhes técnicos (EF Core, Kafka, Redis)
FinanceFlow.Workers        → Processamento background (Quartz, Kafka Consumers)

Por quê?

Isola as regras de negócio de detalhes técnicos (banco de dados, frameworks)
Facilita testes unitários — o domínio não depende de nada externo
Permite trocar tecnologias (ex: SQL Server → PostgreSQL) sem alterar o domínio
Código mais organizado, previsível e fácil de manter em equipa
CQRS (Command Query Responsibility Segregation)
Separação explícita entre operações de escrita (Commands) e leitura (Queries).

Por quê?

Queries podem ser optimizadas de forma independente (cache, projecções)
Commands têm validação e lógica de negócio isoladas
Facilita auditoria — cada intenção do utilizador é um Command explícito
Escala melhor — leitura e escrita podem evoluir separadamente
Padrão Repository
As interfaces de acesso a dados são definidas no Domain e implementadas na Infrastructure.

Por quê?

Abstrai o acesso a dados — os casos de uso não conhecem o EF Core
Facilita testes — basta criar um mock do repositório
Permite trocar o ORM sem alterar a lógica de negócio
Soft Delete
Registos nunca são eliminados fisicamente — apenas marcados com DeletedAt.

Por quê?

Preserva histórico financeiro (requisito crítico em sistemas financeiros)
Permite recuperação de dados apagados por engano
Auditoria completa de todas as operações

2. Backend {#backend}

Linguagem e Framework
.NET 9 (C#)
A plataforma principal de desenvolvimento do backend.

Por quê?

Alta performance — um dos runtimes mais rápidos para APIs REST
Tipagem forte — reduz erros em tempo de execução
Ecossistema maduro com suporte de longo prazo
Excelente integração com todas as ferramentas escolhidas
Suporte nativo a async/await para operações I/O intensivas
ASP.NET Core Web API
Framework para construção da API REST.

Por quê?

Nativo ao .NET, sem overhead adicional
Suporte nativo a middleware, DI, configuração e logging
Excelente performance em benchmarks independentes
Pacotes NuGet — Camada Application
MediatR (12.x)
Implementação do padrão Mediator — desacopla quem envia um pedido de quem o processa.

Por quê?

Implementa CQRS de forma elegante — cada Command/Query é uma classe isolada
Pipeline behaviors permitem adicionar validação, logging e cache de forma transversal
Reduz acoplamento entre Controllers e lógica de negócio
Facilita testes — cada handler é testado isoladamente
FluentValidation (11.x)
Biblioteca para validação de objectos com sintaxe fluente.

Por quê?

Validações expressivas e legíveis (RuleFor(x => x.Email).NotEmpty().EmailAddress())
Separa as regras de validação da lógica de negócio
Integra com o pipeline do MediatR via Behavior
Mensagens de erro customizáveis e localizáveis
AutoMapper (12.x)
Mapeamento automático entre objectos (Entity → DTO, DTO → Entity).

Por quê?

Elimina código repetitivo de mapeamento manual
Profiles organizam os mapeamentos por funcionalidade
Reduz erros de esquecimento ao adicionar novos campos
Pacotes NuGet — Camada Infrastructure
Entity Framework Core 9 + Microsoft.EntityFrameworkCore.SqlServer
ORM (Object-Relational Mapper) oficial da Microsoft para .NET.

Por quê?

Migrations versionam o schema do banco junto com o código
LINQ para queries type-safe — sem SQL strings vulneráveis a injection
Fluent API permite configuração detalhada do modelo
Soft delete global via Query Filters
Change Tracker automatiza UpdatedAt
Microsoft.EntityFrameworkCore.Tools
Ferramentas CLI para gestão de Migrations do EF Core.

Por quê?

Permite criar e aplicar migrations via Package Manager Console
Necessário para o comando Add-Migration e Update-Database
Microsoft.EntityFrameworkCore.Design
Pacote de design-time para o EF Core.

Por quê?

Necessário para as ferramentas de migration funcionarem no projeto de startup
Requerido pelo GitHub Actions para o build do backend
StackExchange.Redis (2.x)
Cliente Redis de alta performance para .NET.

Por quê?

Cache de queries pesadas do dashboard (TTL de 5 minutos)
Armazenamento de Refresh Tokens JWT
Reduz carga no banco de dados para dados frequentemente acedidos
API simples e bem documentada
Confluent.Kafka (2.x)
Cliente oficial Kafka para .NET da Confluent.

Por quê?

Cliente oficial — melhor suporte e compatibilidade
Processamento assíncrono de eventos (transações, relatórios, alertas)
Desacopla produtores e consumidores de eventos
Garante entrega de mensagens mesmo sob alta carga
Quartz.NET (3.x) + Quartz.Extensions.Hosting
Framework de agendamento de jobs para .NET.

Por quê?

Relatório mensal automático no 1º dia de cada mês
Processamento de transações recorrentes
Dashboard visual para monitorar jobs (Fase 7)
Mais robusto que BackgroundService para jobs complexos
BCrypt.Net-Next (4.x)
Implementação do algoritmo BCrypt para hash de passwords.

Por quê?

BCrypt é o padrão da indústria para hash de passwords
Resistente a ataques de força bruta (custo computacional ajustável)
Salt automático — nunca dois hashes iguais para a mesma password
Simples de usar: BCrypt.HashPassword() e BCrypt.Verify()
Microsoft.AspNetCore.Authentication.JwtBearer (9.x)
Middleware de autenticação JWT para ASP.NET Core.

Por quê?

Autenticação stateless — não requer sessão no servidor
Tokens auto-contidos com claims do utilizador
Padrão da indústria para APIs REST
Integração nativa com ASP.NET Core
Pacotes NuGet — Camada API
Swashbuckle.AspNetCore (6.x)
Geração automática de documentação Swagger/OpenAPI.

Por quê?

Documentação interactiva da API — testa endpoints directamente no browser
Gerado automaticamente a partir dos Controllers e DTOs
Suporte a autenticação JWT no Swagger UI
Facilita integração com o frontend e equipas externas
Serilog.AspNetCore (8.x) + Sinks
Framework de logging estruturado para .NET.

Por quê?

Logs estruturados (JSON) — fáceis de pesquisar e analisar
Múltiplos destinos (Console, Ficheiro, CloudWatch futuramente)
Enriquecimento automático com contexto (MachineN ame, ThreadId)
UseSerilogRequestLogging() loga todos os requests HTTP automaticamente
Rotação automática de ficheiros de log por dia
Serilog.Enrichers.Environment + Serilog.Enrichers.Thread
Enriquecedores que adicionam contexto extra aos logs.

Por quê?

MachineName — identifica o servidor em ambientes com múltiplas instâncias
ThreadId — facilita debug de problemas de concorrência
AspNetCore.HealthChecks.SqlServer + AspNetCore.HealthChecks.Redis
Health checks para SQL Server e Redis.

Por quê?

Endpoint /health indica se a aplicação está saudável
Usado pelo ALB da AWS para routing e auto-recovery
Detecta problemas de conectividade com dependências externas
Facilita monitoramento em produção
QuestPDF (2024.x)
Biblioteca para geração de PDFs em .NET.

Por quê?

API fluente e moderna — muito mais simples que alternativas antigas
Gratuito para uso pessoal e open-source
Suporte a layouts complexos, tabelas e gráficos
Alta performance — geração assíncrona
CsvHelper (33.x)
Biblioteca para leitura e escrita de ficheiros CSV.

Por quê?

A mais popular e robusta para CSV em .NET
Mapeamento automático entre classes C# e colunas CSV
Suporte a culturas (separadores, formatos de data)
Pacotes NuGet — Testes
xUnit (2.x)
Framework de testes unitários para .NET.

Por quê?

O mais moderno e recomendado para .NET
Isolamento por teste — cada teste cria uma nova instância da classe
Suporte nativo a testes assíncronos
Integração perfeita com Visual Studio e dotnet CLI
Moq (4.x)
Biblioteca de mocking para testes .NET.

Por quê?

Cria implementações falsas de interfaces (repositórios, serviços)
Verifica chamadas de métodos e parâmetros
Sintaxe fluente e expressiva
A mais utilizada no ecossistema .NET
FluentAssertions (6.x)
Assertions expressivas para testes .NET.

Por quê?

Mensagens de erro muito mais claras que o Assert padrão
Sintaxe legível: result.Should().Be(expected)
Assertions para colecções, excepções, datas e muito mais
Testcontainers.MsSql (3.x)
Sobe containers Docker para testes de integração.

Por quê?

Testes de integração contra um SQL Server real (não mocks)
Container criado e destruído automaticamente por teste
Garante isolamento total entre testes
Sem dependência de ambiente externo — funciona no CI
Microsoft.AspNetCore.Mvc.Testing (9.x)
Servidor de testes in-memory para ASP.NET Core.

Por quê?

Testa os endpoints HTTP reais sem subir um servidor externo
Reutiliza toda a configuração do Program.cs
Simula requests HTTP completos (autenticação, middlewares, routing)

3. Frontend {#frontend}

Framework e Build
React 19 + TypeScript
Biblioteca principal para construção da interface.

Por quê React?

Ecossistema enorme — mais bibliotecas, mais recursos, mais comunidade
Component-based — reutilização e composição de UI
Virtual DOM — actualizações eficientes da interface
Amplamente adoptado em aplicações financeiras e dashboards
Por quê TypeScript?

Tipagem estática — erros detectados em tempo de compilação
IntelliSense — autocomplete e documentação inline
Refactoring seguro — renomear um tipo actualiza todas as referências
Obrigatório em projectos profissionais de média/grande dimensão
Vite
Build tool e servidor de desenvolvimento.

Por quê?

Hot Module Replacement (HMR) instantâneo — mudanças reflectem em <100ms
Build de produção muito mais rápido que Webpack/CRA
Suporte nativo a TypeScript, JSX e CSS Modules
Configuração simples e moderna
Gestão de Estado
Zustand
Biblioteca de gestão de estado global.

Por quê Zustand e não Redux?

API minimalista — sem boilerplate (actions, reducers, selectors)
Bundle size muito menor (~1kb vs ~15kb do Redux Toolkit)
Fácil de aprender — uma store é um hook simples
Suficiente para a complexidade do FinanceFlow
Integra bem com TanStack Query (server state separado do client state)
Como é usado no FinanceFlow:

Estado de UI (sidebar aberta/fechada, tema dark/light)
Dados do utilizador autenticado (perfil, preferências)
Filtros activos nas páginas de transações
TanStack Query (React Query)
Gestão de estado do servidor — cache, sincronização e actualizações.

Por quê?

Elimina a necessidade de useEffect para fetch de dados
Cache automático — não refaz requests desnecessários
Revalidação automática (focus, reconnect, interval)
Optimistic updates — UI actualiza antes da confirmação do servidor
Gestão de loading, error e success states automática
DevTools para debug visual do cache
Separação de responsabilidades:

TanStack Query → dados do servidor (transações, categorias, dashboard)
Zustand        → estado do cliente (UI, preferências, utilizador)
Roteamento
React Router v6
Roteamento client-side para React.

Por quê?

O padrão da indústria para routing em React
Nested routes — layouts partilhados entre páginas
Protected routes — redireccionamento automático para login
Lazy loading de páginas para melhor performance
Formulários e Validação
React Hook Form
Gestão de formulários com performance optimizada.

Por quê?

Uncontrolled components — mínimos re-renders
API baseada em hooks — sem HOCs ou render props
Integração nativa com Zod para validação
Gestão automática de erros, touched e dirty states
Zod
Schema validation e TypeScript-first.

Por quê?

Validação com inferência automática de tipos TypeScript
Schemas reutilizáveis entre frontend e potencialmente backend
Mensagens de erro customizáveis
Composição de schemas complexos de forma simples
UI e Estilização
shadcn/ui
Colecção de componentes UI baseados em Radix UI e Tailwind.

Por quê?

Componentes acessíveis por padrão (WCAG) via Radix UI
Código copiado para o projecto — total controlo e customização
Sem dependência de versão — os componentes são teus
Design system consistente e profissional
Suporte nativo a dark mode
Preset escolhido: Nova

Design moderno com fonte Geist
Ícones Lucide integrados
Paleta de cores equilibrada para dashboards financeiros
Tailwind CSS v3
Framework CSS utility-first.

Por quê?

Estilização directamente no JSX — sem ficheiros CSS separados
Design system consistente via tokens (cores, espaçamentos, tipografia)
Tree-shaking automático — apenas CSS usado é incluído no bundle
Dark mode com classe dark — simples de implementar
Excelente integração com shadcn/ui
Radix UI
Primitivos de UI acessíveis e sem estilo.

Por quê?

Base do shadcn/ui — fornece comportamento acessível
Gestão de focus, keyboard navigation e ARIA automática
Headless — sem estilos impostos, total liberdade visual
Gráficos
Recharts
Biblioteca de gráficos para React baseada em D3.

Por quê Recharts e não Chart.js?

Componentes React nativos — não manipula o DOM directamente
Responsivo por padrão
API declarativa e simples
Customização via props e CSS
Bundle size razoável (~500kb)
Gráficos utilizados no FinanceFlow:

<LineChart> — evolução do saldo ao longo do mês
<PieChart> — distribuição de despesas por categoria
<BarChart> — receita vs despesa semanal e comparativo histórico
HTTP e Comunicação
Axios
Cliente HTTP para comunicação com a API.

Por quê Axios e não fetch nativo?

Interceptors — adiciona JWT token automaticamente em todos os requests
Interceptor de resposta — renova token expirado automaticamente
Transformação automática de JSON
Melhor tratamento de erros (status codes)
Cancelamento de requests
@microsoft/signalr
Cliente SignalR para comunicação em tempo real.

Por quê?

Comunicação bidirecional com o servidor sem polling
Notificações em tempo real (alertas de orçamento, relatório pronto)
Fallback automático (WebSockets → Server-Sent Events → Long Polling)
SDK oficial da Microsoft
Utilitários
date-fns
Biblioteca de manipulação de datas.

Por quê date-fns e não moment.js?

Modular — importa apenas as funções que usas (tree-shakeable)
Imutável — não altera os objectos Date originais
TypeScript nativo
Mais leve e moderna que moment.js (descontinuado)
Testes Frontend
Vitest
Framework de testes unitários para projetos Vite.

Por quê Vitest e não Jest?

Nativo ao Vite — usa a mesma configuração e transformações
Muito mais rápido que Jest (execução paralela nativa)
API compatível com Jest — curva de aprendizagem mínima
Suporte nativo a TypeScript e ESM
@testing-library/react
Utilitários para testar componentes React.

Por quê?

Testa componentes como o utilizador os usa (não a implementação)
Queries por texto, role e label — sem depender de classes CSS
Integra com Vitest e jsdom
@testing-library/jest-dom
Matchers adicionais para assertions no DOM.

Por quê?

Assertions expressivas: toBeVisible(), toHaveValue(), toBeDisabled()
Complementa o Vitest com verificações específicas do DOM
jsdom
Simulação do ambiente de browser para testes.

Por quê?

Permite testar componentes React sem um browser real
Simula DOM, eventos, localStorage e outras APIs do browser
Configurado via environment: 'jsdom' no Vitest
Playwright (Fase 9 — E2E)
Framework de testes End-to-End.

Por quê Playwright e não Cypress?

Suporta Chrome, Firefox e Safari em paralelo
Mais rápido e estável que Cypress em CI
Auto-wait — não precisa de cy.wait() artificiais
Geração de screenshots e vídeos automática em falhas
Desenvolvido pela Microsoft — excelente suporte a .NET ecosystems

4. Infraestrutura Local {#infraestrutura}

Docker Desktop + Docker Compose
Containerização dos serviços de infraestrutura local.

Por quê?

Ambiente idêntico para todos os developers — "works on my machine" eliminado
Serviços sobem com um único comando (docker compose up -d)
Isolamento — cada serviço corre no seu container
Volumes persistentes — dados sobrevivem a restart dos containers
SQL Server 2022 (Docker)
Base de dados relacional principal.

Por quê SQL Server?

Excelente integração com o ecossistema .NET e EF Core
Azure Data Studio para gestão visual
Funcionalidades avançadas (JSON support, Full-Text Search)
Familiar para developers .NET
Nota: Em produção será migrado para RDS PostgreSQL na AWS (Free Tier).

Apache Kafka + Zookeeper (Docker)
Plataforma de streaming de eventos distribuídos.

Por quê Kafka?

Processamento assíncrono de eventos (transações, relatórios, alertas)
Desacopla produtores e consumidores — a API não espera pelo processamento
Garante entrega de mensagens mesmo se o consumer estiver em baixo
Escalável para alto volume de transações no futuro
Tópicos criados:

finance.transactions.created — nova transação criada
finance.reports.requested — pedido de geração de relatório
finance.budget.alerts — verificação de limites de orçamento
Redis (Docker)
Base de dados em memória para cache e sessões.

Por quê Redis?

Cache de queries pesadas do dashboard (TTL 5 minutos)
Armazenamento de Refresh Tokens JWT (TTL 7 dias)
Estruturas de dados avançadas (sets, hashes, lists)
Sub-milissegundo de latência

5. Testes {#testes}

Estratégia de Testes
A estratégia segue a pirâmide de testes:

         /\
        /E2E\          ← Playwright
       /------\
      /Integração\     ← TestContainers + WebApplicationFactory
     /------------\
    / Unitários    \   ← xUnit + Moq / Vitest + Testing Library
   /--------------/

# Testes Unitários (Backend)

O quê: Commands, Queries, Validações, Regras de Domínio
Ferramentas: xUnit + Moq + FluentAssertions
Quando: A cada fase, junto com o desenvolvimento
Meta de cobertura: 70% nas camadas Application e Domain

# Testes de Integração (Backend)

O quê: Endpoints HTTP completos, Repositórios, Migrations
Ferramentas: TestContainers + WebApplicationFactory
Quando: A cada fase, para fluxos críticos
Nota: Sobem um SQL Server real em Docker — sem mocks de base de dados

# Testes Unitários (Frontend)

O quê: Custom hooks, funções utilitárias, formatadores
Ferramentas: Vitest + Testing Library + jest-dom
Quando: A cada fase, para lógica crítica do frontend

# Testes E2E (Fase 9)

O quê: Fluxos completos do utilizador (login → criar transação → ver dashboard)
Ferramentas: Playwright
Quando: Após o MVP estar funcionalmente completo
Nota: Testam o sistema como um todo — frontend + backend + base de dados

6. CI/CD {#cicd}
GitHub Actions
Motor de automação de CI/CD integrado no GitHub.

Por quê GitHub Actions?

Nativo ao GitHub — sem configuração de servidor externo
Gratuito para repositórios públicos
YAML simples e expressivo
Ecossistema enorme de actions reutilizáveis
Integração directa com Pull Requests

# CI — Backend (ci-backend.yml)
Executado em cada push/PR para develop ou master com alterações em backend/**:

Setup .NET 9
Cache de pacotes NuGet
Restore de dependências
Build Release
Testes Unitários
Testes de Integração
Publicação de resultados

# CI — Frontend (ci-frontend.yml)
Executado em cada push/PR para develop ou master com alterações em frontend/**:

Setup Node.js 20
Cache de pacotes npm
Instalação de dependências
Type check TypeScript
Build de produção
Testes Vitest
Upload do artefacto de build

# CD configurado para deploy automático na AWS:

Build de imagem Docker
Push para ECR (Elastic Container Registry)
Deploy no ECS Fargate

7. Controlo de Versão {#git}
Git + GitHub
Sistema de controlo de versão e plataforma de colaboração.

Estratégia de Branches (Git Flow simplificado)
master     → código de produção (releases)
develop    → integração contínua (features prontas)
feature/*  → desenvolvimento de funcionalidades
Convenção de Commits
Mensagens de commit humanizadas em português:

feat: adição de criação de transações
fix: correção do cálculo do saldo mensal
ci: configuração do pipeline de testes
chore: actualização de dependências
Versionamento Semântico (SemVer)
Tags de release seguindo vMAJOR.MINOR.PATCH:

v0.1.0 → Fase 0 (Setup)
v0.2.0 → Fase 1 (Autenticação)
...
v1.0.0 → MVP completo

8. Ferramentas de Desenvolvimento {#ferramentas}
Visual Studio 2022
IDE principal para desenvolvimento do backend .NET.

Por quê?

Melhor suporte ao ecossistema .NET da Microsoft
IntelliSense avançado para C#
Package Manager Console para EF Core Migrations
Debugger poderoso com Diagnostic Tools
Integração nativa com Git
VS Code
Editor para desenvolvimento do frontend React.

Por quê?

Leve e rápido — ideal para projectos JavaScript/TypeScript
Extensões excelentes (ESLint, Prettier, Tailwind IntelliSense)
Terminal integrado
Suporte nativo a TypeScript
Azure Data Studio
Cliente de base de dados para SQL Server.

Por quê?

Desenvolvido pela Microsoft — integração perfeita com SQL Server
Interface moderna e leve
Suporte a notebooks SQL
Gratuito e cross-platform
Docker Desktop
Interface gráfica para gestão de containers Docker.

Por quê?

Gestão visual de containers, volumes e redes
Dashboard de uso de recursos
Integração com WSL2 no Windows

# Decisões Futuras (Pós-MVP)

Tecnologia	| Substituição	| Motivo

SQL Server local | RDS PostgreSQL (AWS)	| Free Tier managed service

JWT local | AWS Cognito | Segurança enterprise, MFA, gestão de utilizadores

Storage local | AWS S3 | Escalável, durável, CDN

Logs locais	| CloudWatch | Centralizado, alertas, dashboards

Deploy manual | ECS Fargate + CodePipeline	| CD automático

Domínio	Hostinger → aponta para AWS ALB	URL profissional
