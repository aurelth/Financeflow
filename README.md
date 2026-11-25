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

## Lançamento por IA (texto ou foto de comprovante)

Em **Transações → Lançar com IA** o usuário descreve o gasto ou fotografa o comprovante; a IA devolve rascunhos que são conferidos no formulário antes de salvar (nada é gravado sem confirmação). Cada usuário tem um limite diário de usos, renovado à meia-noite no fuso dele.

| Configuração (`Ai:*` / variável `Ai__*`) | Padrão | Descrição |
|---|---|---|
| `Ai:DailyLimit` / `Ai__DailyLimit` | 30 | Usos por usuário por dia |
| `Ai:MaxImageBytes` | 5242880 | Tamanho máximo da foto após a redução no navegador |
| `Ai:MaxTextLength` | 500 | Tamanho máximo do texto |
| `Ai:MaxDraftsPerRequest` | 5 | Lançamentos aceitos por chamada |

As fotos não são armazenadas pelo servidor e nem o texto nem as imagens são registrados em log.

## Compras parceladas

Em **Transações → Nova transação**, marcar **Parcelar compra** e informar de 2 a 60 parcelas lança a compra de uma vez: o valor informado é o **total**, dividido em centavos (a sobra vai para as primeiras parcelas) e uma parcela por mês, sempre ancorada no dia da primeira (31/jan → 28/fev → 31/mar).

- A 1ª parcela segue o status escolhido e leva o comprovante; as demais ficam **Agendadas** e não alteram saldo, dashboard nem relatórios até serem pagas, mas já entram no orçamento do mês em que vencem.
- Editar ou remover uma parcela pergunta se vale só para ela ou também para as seguintes. As parcelas **já pagas nunca são alteradas em massa**.
- O selo `N/M` na lista abre o plano da compra (total, pago, restante) e permite **cancelar a compra**: as parcelas não pagas saem, as pagas permanecem no histórico.
- Transferências e transações recorrentes não podem ser parceladas, e o tipo de uma parcela não pode ser trocado.
- API: `POST /api/transactions` (campo `installmentCount`), `GET|DELETE /api/transactions/installments/{groupId}`.

## Detecção de assinaturas e recorrências

Em **Transações**, um painel recolhido avisa quando o histórico revela lançamentos que se repetem em intervalo regular (semanal, mensal ou anual) e ainda não são uma série: assinaturas de valor fixo, contas de valor variável (luz, água) e receitas fixas (salário). A detecção é recalculada pelo servidor a cada consulta, sem job nem tabela de resultados; só as sugestões dispensadas ficam gravadas.

- Critérios: mesma descrição normalizada e mesmo tipo; no mínimo 4 ocorrências (semanal), 3 (mensal) ou 2 (anual), com ao menos 75% dos intervalos na janela da cadência; valor dentro de 5% da mediana (fixo) ou 40% (variável); séries sem ocorrência recente são tratadas como encerradas. Transferências e parcelas ficam de fora.
- **Confirmar** liga o histórico a um grupo de recorrência e agenda as próximas ocorrências (12 semanais/mensais, 2 anuais) como *Agendadas*; elas passam a gerar os alertas de vencimento e a contar no orçamento do mês. Editar ou remover "esta e as futuras" funciona como em qualquer recorrência.
- **Ignorar** esconde a sugestão para aquela descrição; `DELETE /api/recurring/suggestions/dismissed` volta a oferecê-las.
- API: `GET /api/recurring/suggestions`, `POST /api/recurring/suggestions/confirm`, `POST /api/recurring/suggestions/dismiss`.

## Calendário de contas e lembretes

A página **Calendário** mostra o mês dia a dia: contas a pagar (despesas em aberto), a receber (receitas em aberto) e o que já foi liquidado, com totais do mês. Clicar num dia lista os lançamentos dele, e **Dar baixa** marca como Pago (saldo, dashboard e relatórios passam a contá-lo). Contas vencidas e não pagas dos últimos 90 dias aparecem num painel à parte, de qualquer mês. "Hoje" e "vencida" seguem o fuso configurado no perfil do usuário.

- Lembretes (worker `TransactionDueAlertJob`, só despesas ainda não pagas): vence em 3 dias, vence amanhã, **vence hoje** e **venceu ontem sem pagamento** (avisa uma vez). Cada um tem seu interruptor em Configurações → Notificações. Antes, lançamentos Pendentes eram ignorados pelo job e lançamentos já pagos podiam gerar aviso; agora só o que está em aberto avisa.
- API: `GET /api/calendar?month=&year=`, `POST /api/transactions/{id}/pay` (idempotente; transferências ficam de fora).

## Resumo semanal proativo

Toda **segunda-feira, a partir das 8h no fuso do usuário**, o FinanceFlow envia o balanço da última semana fechada (segunda a domingo): quanto entrou e saiu (só o que foi pago) com a variação sobre a semana anterior, as três categorias que mais pesaram, o que vence nos próximos 7 dias, as despesas vencidas e não pagas e os orçamentos do mês acima de 80%. O resumo chega como notificação no app e por e-mail, e cada canal tem seu interruptor em Configurações → Notificações (ambos ligados por padrão). Semana sem movimento, sem vencimentos e sem alertas não gera envio.

- Worker `WeeklySummaryJob` (roda a cada hora): a API informa quem está na janela de envio naquele momento, então cada fuso recebe na sua manhã de segunda. O Redis (`notif:weekly:{usuário}:{canal}:{semana}`, TTL de 8 dias) garante um envio por usuário, semana e canal, e só marca como enviado quando o envio deu certo — se o SendGrid ou a API falharem, a próxima hora tenta de novo (até o fim da segunda-feira).
- Endpoints internos, exclusivos do JWT de serviço do Worker (policy `RequireService`): `GET /api/weekly-summary/internal/candidates`, `GET /api/weekly-summary/internal/{userId}/notification`, `POST /api/weekly-summary/internal/{userId}/email`.
- O e-mail usa `SendGrid:*` e o link do rodapé usa `Frontend:BaseUrl`. Textos e valores em pt-BR.

## Edição em lote de transações

Na lista de **Transações**, as caixas de seleção (e "selecionar todos da página") ligam uma barra de ações: **Editar** altera categoria, conta e/ou status de todos os selecionados de uma vez, preenchendo só o que se quer mudar, e **Remover** exclui os selecionados. A seleção vale para a página e os filtros atuais e é limpa ao trocar de página ou filtro; o limite é de 200 lançamentos por operação.

- Cada lançamento é validado individualmente: os que não comportam a mudança (categoria de outro tipo, transferência vinculada que trocaria de conta, status de transferência) ficam de fora e a tela mostra quantos e por quê, sem desfazer os demais. Categoria ou conta inexistente/arquivada recusa o lote inteiro.
- O que for aplicado entra num único `SaveChanges` (tudo ou nada) e o cache do dashboard dos meses afetados é invalidado. Ao trocar a categoria a subcategoria é limpa, pois pertence à categoria antiga.
- Remover em lote apaga só os selecionados (nunca o resto de uma recorrência ou de uma compra parcelada) e desfaz o vínculo das transferências selecionadas.
- API: `PATCH /api/transactions/batch` (`ids`, `categoryId?`, `accountId?`, `status?`) e `POST /api/transactions/batch/delete` (`ids`); ambos devolvem `{ requested, succeeded, skipped: [{ id, description, reason }] }`.

## Experiência mobile

O frontend é responsivo de 320 px (celulares pequenos) até desktop, sem rolagem horizontal da página:

- **Layout:** altura dinâmica (`100dvh`), então a barra de endereço do navegador móvel não corta a tela; cabeçalhos de página quebram em linhas (título em cima, botões embaixo) quando faltam espaço.
- **Listas e tabelas:** transações viram cartões no celular (mesmo DOM da tabela, só CSS); a prévia de importação OFX usa grade por áreas; tabelas largas (fluxo de caixa, admin) rolam na horizontal dentro do próprio cartão.
- **Diálogos:** rolam internamente quando o conteúdo é maior que a tela (inclusive em paisagem) e nunca ultrapassam a largura da viewport.
- **Formulários:** campos lado a lado empilham abaixo de 420 px; campos de texto usam 16 px no celular para o iOS Safari não dar zoom ao focar; botões de ação têm área de toque maior.
- **Verificação:** as páginas foram medidas em 320, 360, 390 e 768 px (overflow da página e dos diálogos) com Playwright; testes automatizados desse comportamento entram na rodada final de testes.

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