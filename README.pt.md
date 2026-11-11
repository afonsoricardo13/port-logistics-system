# Port Logistics System

[English](README.md) | **Português**

Plataforma de gestão de operações portuárias desenvolvida no âmbito do Projeto Integrador do 5.º semestre da Licenciatura em Engenharia Informática (LEI) do ISEP, Politécnico do Porto, no ano letivo 2025/26.

## Visão geral

O sistema apoia o trabalho diário de um porto comercial e cobre todo o ciclo de vida de uma visita de navio: o agente de navegação notifica a chegada, a autoridade portuária aprova-a e atribui uma doca, é gerado um plano diário de operações e a execução real é registada e comparada com o plano.

Os principais utilizadores são:

- **Oficiais da Autoridade Portuária**: gerem navios, tipos de navio, docas, áreas de armazenamento e organizações de agentes de navegação, e analisam as notificações de visita.
- **Representantes de Agentes de Navegação**: submetem e acompanham notificações de visita, incluindo os manifestos de carga.
- **Operadores Logísticos**: gerem pessoal, qualificações e recursos físicos, geram planos de operações e registam o que acontece em cada visita (execuções, incidentes, tarefas complementares).
- **Administradores**: gerem utilizadores, perfis e a política de privacidade.

## Arquitetura

A solução é um monorepo com três serviços independentes e uma aplicação de página única. Os serviços comunicam apenas através de APIs REST; nenhum serviço acede à base de dados de outro.

| Componente | Caminho | Responsabilidade |
|---|---|---|
| API Port Logistics | `apps/port-logistics/backend` | Dados mestre, notificações de visita, utilizadores e perfis |
| API Operations & Execution (OEM) | `apps/operations-execution/backend` | Planos de operações, execuções de visitas, incidentes, tarefas complementares |
| Serviço de escalonamento | `apps/scheduling-planning/backend` | Algoritmos de escalonamento diário, incluindo um algoritmo genético |
| Cliente web | `apps/port-logistics/frontend/web/spa` | SPA em Angular com uma vista 3D do porto |

Os dois serviços .NET seguem uma arquitetura em camadas com Domain-Driven Design:

- **Domain**: agregados, value objects e regras de negócio
- **Application**: serviços, DTOs e casos de uso
- **DataModel**: persistência com Entity Framework Core e repositórios
- **WebApi**: controladores REST, autenticação e autorização

A autenticação usa Google OAuth 2.0. A autorização é baseada em perfis, com uma verificação adicional de permissões por recurso.

## Tecnologias

- **Back end**: .NET 9, ASP.NET Core, Entity Framework Core, MySQL 8 (provider Pomelo), Swagger / OpenAPI
- **Escalonamento**: SWI-Prolog (servidor HTTP, algoritmos heurísticos e genético), Docker
- **Front end**: Angular 20, TypeScript, Three.js, tween.js, ngx-translate
- **Testes**: xUnit, Moq, EF Core InMemory, Karma/Jasmine, Cypress
- **Infraestrutura**: Azure App Service, Azure Database for MySQL, Azure Storage, VMs Linux, GitHub Actions

## Como executar

### Pré-requisitos

- .NET SDK 9.0
- Node.js 20 ou superior
- MySQL 8.0
- SWI-Prolog 9 (ou Docker)

### 1. Bases de dados

Criar duas bases de dados MySQL, uma por cada serviço .NET:

```sql
CREATE DATABASE absanteedb;
CREATE DATABASE oemdb;
```

Editar as connection strings em `apps/port-logistics/backend/WebApi/appsettings.Development.json` e `apps/operations-execution/backend/WebApi/appsettings.Development.json`, substituindo `YOUR_DB_PASSWORD` pela password do MySQL. As migrações pendentes da API Port Logistics são aplicadas automaticamente no arranque.

### 2. API Port Logistics

```bash
dotnet run --project apps/port-logistics/backend/WebApi --launch-profile https
```

Disponível em `https://localhost:7096` (Swagger em `/swagger`).

### 3. API Operations & Execution

```bash
dotnet ef database update --project apps/operations-execution/backend/DataModel --startup-project apps/operations-execution/backend/WebApi
dotnet run --project apps/operations-execution/backend/WebApi --launch-profile https
```

Disponível em `https://localhost:7001`.

### 4. Serviço de escalonamento

```bash
cd apps/scheduling-planning/backend
swipl -s scheduling_server.pl -g start_scheduling_server
```

Disponível em `http://localhost:5003`. Em alternativa, pode ser usado o `Dockerfile` incluído, que serve na porta 8080.

### 5. Cliente web

```bash
cd apps/port-logistics/frontend/web/spa
npm install
npm start
```

Disponível em `http://localhost:4200`.

Em desenvolvimento, as APIs aceitam o cabeçalho `X-User-Email` em vez do login Google, o que permite testar cada perfil a partir do Swagger.

### Testes

```bash
dotnet test apps/port-logistics/backend/PortLogistics.sln
dotnet test apps/operations-execution/backend/OEM.sln
cd apps/port-logistics/frontend/web/spa && npm test
```

## Estrutura do repositório

```
apps/
  port-logistics/
    backend/                API Port Logistics (solução .NET e testes)
    frontend/web/spa/       SPA em Angular
  operations-execution/
    backend/                API OEM (solução .NET e testes)
  scheduling-planning/
    backend/                Serviço de escalonamento em Prolog
docs/
  sprintA, sprintB, sprintC Requisitos, modelo de domínio e vistas C4 / 4+1 (PlantUML)
  ASIST                     Relatórios de administração de sistemas e continuidade de negócio
  IARTI                     Relatórios dos algoritmos de escalonamento
  SGRAI                     Relatório da visualização 3D
  GESTA                     Relatório de gestão
  LAPR5                     Documentação RGPD
.github/workflows/          Pipelines de CI e deploy
```

## Contexto académico

Trabalho de grupo desenvolvido ao longo de três sprints por uma equipa de cinco estudantes. Integra as seguintes unidades curriculares:

- **LAPR5**: Laboratório/Projeto V
- **ARQSI**: Arquitetura de Sistemas de Informação
- **SGRAI**: Sistemas Gráficos e Interação
- **IARTI**: Inteligência Artificial
- **ASIST**: Administração de Sistemas e Infraestruturas
- **GESTA**: Gestão

O repositório original pertence à organização da unidade curricular e é privado. Esta cópia mantém o código e a documentação finais, sem o histórico de commits, e com credenciais e dados pessoais substituídos por placeholders. Os modelos 3D em `src/assets/models` são de terceiros e pertencem aos respetivos autores.

## Equipa

| Membro | GitHub |
|---|---|
| Afonso Ricardo | [@afonsoricardo13](https://github.com/afonsoricardo13) |
| Pedro Carriço Pereira | [@pedrocarrico6](https://github.com/pedrocarrico6) |
| _Nome_ | [@JoaoSousa0655](https://github.com/JoaoSousa0655) |
| _Nome_ | [@RFaria05](https://github.com/RFaria05) |
| _Nome_ | [@manuzinho7](https://github.com/manuzinho7) |

## A minha contribuição

User stories pelas quais fui responsável. As marcadas com *(partilhada)* foram desenvolvidas com outros membros da equipa.

**Back end: API Port Logistics (Sprint A)**
- US 2.2.10: consulta, pelos agentes de navegação, do estado das suas notificações de visita
- US 2.2.11: registo e gestão do pessoal operacional (design, implementação e testes)
- US 2.2.13: registo e gestão de qualificações *(partilhada)*
- US 2.2.4: registo e atualização de áreas de armazenamento *(partilhada)*
- Modelo de domínio, glossário e diagrama de casos de uso *(partilhada)*

**Front end, segurança e arquitetura (Sprint B)**
- US 3.1.1: configuração inicial da SPA em Angular
- US 3.2.4: restrição de ações e funcionalidades com base no perfil
- US 3.2.5: atribuição e atualização de perfis de utilizador pelo administrador
- US 3.4.1: módulo de escalonamento dedicado, que expõe os algoritmos através de uma API REST
- US 3.3.4: texturas e estilo visual dos elementos 3D do porto

**Operations & Execution (Sprint C)**
- US 4.1.1: módulo OEM como serviço de back end independente *(partilhada)*
- US 4.1.5: deteção de notificações de visita sem plano de operações, com regeneração dos planos de um dia
- US 4.1.10: pesquisa de execuções de visitas por período, navio ou estado, com métricas de execução
- US 4.1.15: registo e gestão de tarefas complementares *(partilhada)*

**Algoritmos e visualização 3D (Sprint C)**
- US 4.3.1: algoritmo genético para planos diários de operações, com parâmetros configuráveis e soluções com uma ou várias gruas
- US 4.2.6: transições animadas da câmara e do foco de luz entre elementos selecionados

**Administração de sistemas (Sprints B e C)**
- US 3.5.4: pasta pública partilhada para utilizadores registados
- US 3.5.9: registo dos logins remotos por SSH
- US 4.4.4: validação da gestão de acessos face a políticas de menor privilégio, com auditoria de acessos
- US 4.4.9: eliminação automática de backups da base de dados segundo regras de retenção
- US 4.4.11: diretório de rede partilhado (SMB/CIFS ou NFS)

**RGPD e gestão**
- US 4.5.1 a 4.5.4: política de privacidade, direitos dos titulares dos dados e área de direitos na SPA *(partilhada)*
- US 2.3.7, 3.7.3 e 4.6.5: estratégia organizacional, sistemas de informação de gestão e análise da cadeia de abastecimento
