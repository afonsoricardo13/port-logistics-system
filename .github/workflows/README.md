# GitHub Actions Workflows - CI/CD

Este diretório contém os workflows automáticos de CI/CD para todos os componentes do projeto.

## 📋 Workflows Disponíveis

### 1. `ci.yml` - Continuous Integration
- **Trigger:** Push ou Pull Request para `main`
- **Ação:** Compila e testa todos os backends (.NET)
- **Não faz deploy** - apenas valida o código

### 2. `deploy-portlogistics-backend.yml` - Port Logistics Backend
- **Trigger:** 
  - Push para `main` com mudanças em `apps/port-logistics/backend/**`
  - Manual via GitHub Actions UI
- **Ação:** Build + Deploy para Azure App Service
- **URL:** `https://portlogistics-api-3db01-f4b3b0hgc6dvdtax.swedencentral-01.azurewebsites.net`

### 3. `deploy-portlogistics-frontend.yml` - Port Logistics Frontend
- **Trigger:** 
  - Push para `main` com mudanças em `apps/port-logistics/frontend/**`
  - Manual via GitHub Actions UI
- **Ação:** Build Angular + Deploy para Azure Storage (Static Website)
- **URL:** Dinâmico (obtido via Azure CLI)

### 4. `deploy-operations-execution.yml` - Operations Execution API
- **Trigger:** 
  - Push para `main` com mudanças em `apps/operations-execution/backend/**`
  - Manual via GitHub Actions UI
- **Ação:** Build + Deploy para Azure App Service
- **URL:** `https://operations-execution-api-3db01.azurewebsites.net`

### 5. `deploy-scheduling-planning.yml` - Scheduling Planning (Prolog)
- **Trigger:** 
  - Push para `main` com mudanças em `apps/scheduling-planning/backend/**`
  - Manual via GitHub Actions UI
- **Ação:** Build Docker image + Deploy para Azure App Service (Linux Container)
- **URL:** `https://scheduling-planning-3db01.azurewebsites.net`

### 6. `vm-deployment.yml` - VM Deployment
- **Trigger:** Manual
- **Ação:** Deploy para máquina virtual (legado)

## 🔑 Secrets Necessários

Para os workflows funcionarem, precisas configurar estes secrets no GitHub:

| Secret | Usado em | Como obter |
|--------|----------|------------|
| `AZURE_CREDENTIALS` | Todos os workflows Azure | `.\setup-github-secrets.ps1` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_PORTLOGISTICS` | Port Logistics Backend | `.\setup-github-secrets.ps1` |
| `AZURE_WEBAPP_PUBLISH_PROFILE_OEM` | Operations Execution | `.\setup-github-secrets.ps1` |

**Para configurar:** Executa `.\setup-github-secrets.ps1` na raiz do projeto.

## 🚀 Como Funciona

### Deploy Automático
1. Fazes alterações no código
2. Commit e push para `main`
3. GitHub deteta mudanças e executa o workflow apropriado
4. O código é automaticamente deployed para Azure
5. Em 2-3 minutos, as alterações estão live!

### Deploy Manual
1. Vai para GitHub → Actions
2. Seleciona o workflow que queres executar
3. Clica em "Run workflow"
4. Seleciona a branch e confirma

## 📊 Monitoring

### Ver Status
1. Vai para o tab **Actions** no GitHub
2. Vês todos os workflows e o seu estado:
   - ✅ Verde = Sucesso
   - ❌ Vermelho = Falhou
   - 🟡 Amarelo = Em execução

### Ver Logs
1. Clica num workflow específico
2. Clica no job "build-and-deploy"
3. Expande os steps para ver detalhes

### Notificações
- GitHub envia email se um workflow falhar
- Podes configurar notificações no Watch → Custom → Workflows

## ⚡ Otimizações

### Cache
- **Node.js:** Cache de `node_modules` ativado
- **.NET:** Cache de NuGet packages comentado (ativar quando tiveres `packages.lock.json`)

### Paths Filter
Cada workflow só executa quando há mudanças no respetivo diretório:
- Backend mudanças → só backend deploy
- Frontend mudanças → só frontend deploy
- Não há mudanças → nenhum workflow executa

### Parallel Jobs
- CI executa testes de todos os projetos em paralelo
- Deploy workflows executam de forma independente

## 🛠️ Troubleshooting

### Workflow não executa
- Verifica se há mudanças nos paths configurados
- Verifica se estás a dar push para `main`
- Verifica se os workflows estão enabled no GitHub

### Deploy falha com "401 Unauthorized"
- Verifica se os secrets estão configurados
- Re-gera os publish profiles: `.\setup-github-secrets.ps1`

### Build falha
- Verifica os logs no GitHub Actions
- Testa localmente: `dotnet build` ou `npm run build`
- Verifica se há dependências em falta

### Frontend mostra versão antiga
- Limpa cache do browser (Ctrl+Shift+R)
- Aguarda 1-2 minutos para propagação
- Verifica se o deploy foi bem-sucedido

## 📚 Documentação Completa

Para setup detalhado e instruções completas, vê:
- **[CICD-SETUP.md](../../CICD-SETUP.md)** - Guia completo de configuração

## 🔄 Manutenção

### Adicionar novo componente
1. Cria novo workflow baseado nos existentes
2. Atualiza os paths para o novo componente
3. Adiciona secrets necessários
4. Testa com deploy manual primeiro

### Atualizar URLs
Se mudares nomes de recursos Azure, atualiza:
- Environment variables nos workflows
- URLs em [environment.azure.ts](../../apps/port-logistics/frontend/web/spa/src/environments/environment.azure.ts)

### Desativar workflow
Adiciona no início do ficheiro `.yml`:
```yaml
on:
  workflow_dispatch:  # Apenas manual
```

---

**Nota:** Estes workflows foram configurados para o projeto LEI-SEM5-PI-2025-26-3DB-01. Ajusta conforme necessário para outros ambientes.
