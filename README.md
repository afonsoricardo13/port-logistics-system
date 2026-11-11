# 🚢 Port Logistics Management System

**Integrative Project - 5th Semester LEI-ISEP 2025-2026**

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-orange.svg)](https://www.mysql.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud-blue.svg)](https://azure.microsoft.com/)
[![License](https://img.shields.io/badge/License-Educational-green.svg)]()

---

## 📖 Sobre o Projeto

Sistema de gestão portuária de nova geração para suporte a operações portuárias sustentáveis e inteligentes, incluindo:

- 🚢 Gestão de visitas de navios
- 🏗️ Atribuição de docas
- 📦 Operações de carga/descarga
- 👷 Gestão de recursos e pessoal operacional
- 🏢 Coordenação entre Autoridade Portuária, Agentes de Navegação e Operadores Logísticos

---

## 🏗️ Arquitetura

O projeto segue **Clean Architecture** e princípios **DDD (Domain Driven Design)**, agora organizado em módulos por aplicação (monorepo):

```
apps/
  port-logistics/
    backend/  (.NET: Domain, Application, DataModel, WebApi, Tests, SLN)
    frontend/ (Angular SPA)
  scheduling-planning/
    backend/  (placeholder)
    frontend/ (placeholder)
```

```
┌─────────────────────────────────────┐
│         WebApi (REST API)           │
│   Controllers, Endpoints, CORS      │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│      Application (Services)         │
│    Business Logic, DTOs, Use Cases  │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│       Domain (Entities)             │
│   Aggregates, Value Objects, Rules  │
└─────────────────┬───────────────────┘
                  │
┌─────────────────▼───────────────────┐
│    DataModel (Persistence)          │
│   EF Core, Repositories, MySQL      │
└─────────────────────────────────────┘
```

### Camadas:

- **WebApi**: Endpoints REST, Controllers, CORS
- **Application**: Serviços de aplicação, DTOs, lógica de orquestração
- **Domain**: Entidades de domínio, regras de negócio, factories
- **DataModel**: Acesso a dados, repositórios, mapeamento EF Core

---

## 🛠️ Tecnologias

- **.NET 9.0** - Framework principal
- **ASP.NET Core 9.0** - Web API
- **Entity Framework Core 9.0** - ORM
- **MySQL 8.0** - Base de dados (**Azure Database for MySQL**)
- **Pomelo.EntityFrameworkCore.MySql** - MySQL provider
- **Azure App Service** - Hosting da API
- **Swagger/OpenAPI** - Documentação da API
- **xUnit** - Testes unitários e integração

---

## 📋 Pré-requisitos

- ✅ **.NET SDK 9.0** ou superior
- ✅ **MySQL 8.0** (local) ou XAMPP
- ✅ **Azure CLI** (para deploy)
- ✅ Editor: Visual Studio, VS Code, ou JetBrains Rider
- ✅ Git

---

## 🚀 Quick Start

### 1️⃣ Instalar MySQL Localmente

Descarregar e instalar MySQL 8.0+:
- **MySQL Community**: https://dev.mysql.com/downloads/mysql/
- **XAMPP** (alternativa): https://www.apachefriends.org/

### 2️⃣ Criar Base de Dados Local

```sql
CREATE DATABASE AbsanteeDB;
```

### 3️⃣ Configurar Connection String

Editar `WebApi/appsettings.json` com a tua password MySQL:

```json
{
  "ConnectionStrings": {
    "AbsanteeDatabase": "Server=localhost;Port=3306;Database=AbsanteeDB;User=root;Password=TUA_PASSWORD;SslMode=None;"
  }
}
```

### 4️⃣ Aplicar Migrations

```powershell
# Restaurar dependências
dotnet restore

# Aplicar migrations na BD local
dotnet ef database update --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi
```

### 5️⃣ Executar Aplicação

```powershell
# Desenvolvimento local
dotnet run --project .\apps\port-logistics\backend\WebApi\WebApi.csproj

# Ou com auto-reload
dotnet watch run --project .\apps\port-logistics\backend\WebApi\WebApi.csproj
```

**Aceder à API:** http://localhost:5005/swagger

---

## ☁️ Base de Dados Azure (Produção)

O projeto está configurado para usar **Azure Database for MySQL** em produção:

- **Servidor**: `portlogistics-mysql-4325.mysql.database.azure.com`
- **Base de Dados**: `absanteedb`
- **Região**: Sweden Central
- **Versão**: MySQL 8.0.21

### Aplicar Migrations no Azure

```powershell
# Definir ambiente de produção
$env:ASPNETCORE_ENVIRONMENT="Production"

# Aplicar migrations no Azure MySQL
dotnet ef database update --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi
```

### Deploy Manual

```powershell
# 1. Build em modo Release (solution do backend)
dotnet build .\apps\port-logistics\backend\PortLogistics.sln -c Release

# 2. Publicar aplicação
dotnet publish -c Release -o ./publish

# 3. Executar com perfil Production
cd publish
.\WebApi.exe
```

> **Nota:** A aplicação em produção conecta automaticamente ao Azure MySQL através de `appsettings.Production.json`

---

## 📚 Documentação

- **docs/** - Documentação técnica e Sprint artifacts

---

## 🧪 Testes

### Executar Todos os Testes

```powershell
dotnet test .\apps\port-logistics\backend\PortLogistics.sln
```

### Testes Unitários

```powershell
dotnet test .\apps\port-logistics\backend\Domain.Tests
```

### Testes de Integração

```powershell
dotnet test .\apps\port-logistics\backend\WebApi.IntegrationTests
```

---

## 📂 Estrutura do Projeto

```
LEI-SEM5-PI-2025-26-3DB-01/
├── apps/
│   ├── port-logistics/
│   │   ├── backend/
│   │   │   ├── Application/
│   │   │   ├── DataModel/
│   │   │   ├── Domain/
│   │   │   ├── WebApi/
│   │   │   ├── Application.Tests/
│   │   │   ├── Domain.Tests/
│   │   │   ├── WebApi.IntegrationTests/
│   │   │   └── PortLogistics.sln
│   │   └── frontend/
│   │       └── web/          # Angular SPA
│   └── scheduling-planning/
│       ├── backend/          # placeholder para futura app
│       └── frontend/         # placeholder para futura app
├── docs/
│   └── c4/                   # Diagramas e documentação C4 por app
├── ARCHITECTURE.md           # Guia de arquitetura e C4
└── README.md
```

---

## 🎯 Funcionalidades Principais (Sprint A)

### 👮 Oficial da Autoridade Portuária
- Registar e gerir tipos de navios
- Registar e atualizar navios
- Registar e atualizar docas
- Registar e atualizar áreas de armazenamento
- Gerir organizações de agentes de navegação
- Rever e aprovar/rejeitar notificações de visita

### 🚢 Representante de Agente de Navegação
- Submeter notificações de visita de navios
- Incluir manifestos de carga
- Atualizar notificações em progresso
- Ver estado de aprovações

### 🏗️ Operador Logístico
- Registar e gerir pessoal operacional
- Registar e gerir recursos físicos
- Gerir qualificações
- Planear e agendar operações

---

## 🔧 Comandos Úteis

### Desenvolvimento

```powershell
# Restaurar dependências
dotnet restore

# Compilar (solution do backend)
dotnet build .\apps\port-logistics\backend\PortLogistics.sln

# Executar (desenvolvimento)
dotnet run --project .\apps\port-logistics\backend\WebApi\WebApi.csproj

# Executar com auto-reload
dotnet watch run --project .\apps\port-logistics\backend\WebApi\WebApi.csproj

# Limpar build
dotnet clean
```

### Entity Framework (Migrations)

```powershell
# Criar nova migration
dotnet ef migrations add NomeDaMigration --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi

# Aplicar migrations (local)
dotnet ef database update --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi

# Aplicar migrations (Azure)
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet ef database update --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi

# Remover última migration
dotnet ef migrations remove --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi

# Ver lista de migrations
dotnet ef migrations list --project .\apps\port-logistics\backend\DataModel --startup-project .\apps\port-logistics\backend\WebApi
```

### Testes

```powershell
# Executar todos os testes
dotnet test

# Testes unitários
dotnet test Domain.Tests

# Testes de aplicação
dotnet test Application.Tests

# Testes de integração
dotnet test WebApi.IntegrationTests
```

### Deploy

```powershell
# Build de produção
dotnet build -c Release

# Publicar aplicação
dotnet publish -c Release -o ./publish

# Ver logs Azure MySQL
az mysql flexible-server show --resource-group rg-portlogistics --name portlogistics-mysql-4325
```

### 🚀 CI/CD Automático (Recomendado)

```powershell
# Setup completo de CI/CD
.\setup-cicd-complete.ps1

# Ou apenas testar configuração
.\test-cicd-setup.ps1

# Ver URLs dos serviços Azure
.\get-azure-urls.ps1
```

**📚 Documentação completa:** [CICD-SETUP.md](CICD-SETUP.md)

---

## 🌐 Endpoints da API

**Base URL (Local):** `http://localhost:5005`

### Principais Endpoints:

- **Docas**: `/api/dock`
- **Navios**: `/api/vessel`
- **Visitas**: `/api/vesselvisitnotification`
- **Organizações**: `/api/organization`
- **Recursos**: `/api/physicalresource`
- **Pessoal Operacional**: `/api/operatingstaff`

**Documentação interativa:** http://localhost:5005/swagger

---

## 🎓 Unidades Curriculares Integradas

- **ASIST** - Administração de Sistemas e Infraestruturas
- **ARQSI** - Arquitetura de Sistemas de Informação
- **GESTA** - Gestão
- **IART** - Inteligência Artificial
- **LAPR5** - Laboratório e Projeto V
- **SGRAI** - Sistemas Gráficos e Interação

---

## 👥 Equipa

**Team 3DB-01**

Projeto desenvolvido no âmbito do Projeto Integrador do 5º Semestre de LEI-ISEP.

---

## 📄 Licença

Projeto educacional para uso académico.

**Instituto Superior de Engenharia do Porto (ISEP)**  
**Ano Letivo 2025-2026**

---

## 🆘 Suporte

- 📖 Consultar documentação em `docs/`
- 🌐 Documentação .NET: https://docs.microsoft.com/dotnet/
- 🌐 Documentação EF Core: https://docs.microsoft.com/ef/core/
- 🌐 Documentação MySQL: https://dev.mysql.com/doc/

---

## ✅ Arquitetura Atual

- **Desenvolvimento**: MySQL local (`localhost:3306`)
- **Produção**: Azure Database for MySQL (`portlogistics-mysql-4325.mysql.database.azure.com`)
- **API**: ASP.NET Core 9.0 com Swagger
- **ORM**: Entity Framework Core 9.0 com Pomelo.EntityFrameworkCore.MySql

> ✅ **Base de dados em produção não usa localhost** (Azure MySQL Flexible Server)
