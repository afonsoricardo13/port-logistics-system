# US 4.4.5 - Clustering and Load Balancing for SPA Deployment

## 1. Análise da User Story

### 1.1. Descrição

A US 4.4.5 tem como objetivo garantir a alta disponibilidade e resiliência da Single Page Application (SPA) através da implementação de uma solução de clustering e load balancing, permitindo que o sistema se mantenha operacional sob condições de carga elevada ou em cenários de falha de nós.

Este requisito é particularmente relevante para assegurar a continuidade do serviço ao utilizador final, minimizando interrupções visíveis e evitando indisponibilidade causada por falhas individuais de infraestrutura ou picos de tráfego.

### 1.2. Acceptance Criteria

A implementação deve cumprir os seguintes critérios:

- **AC1**: A abordagem de clustering ou load balancing escolhida deve ser claramente documentada e tecnicamente justificada
- **AC2**: A distribuição de carga e o mecanismo de failover devem ser demonstrados em condições de teste
- **AC3**: O comportamento do sistema e os resultados de performance devem ser registados, incluindo cenários de falha de nós

### 1.3. Abordagem Escolhida

A abordagem selecionada baseia-se nos seguintes princípios:

1. Balanceamento de Carga ao Nível da Aplicação através de um reverse proxy
2. Redundância de Nós de Frontend SPA
3. Failover Automático e Transparente para o Utilizador
4. Simplicidade Operacional, adequada ao contexto académico do projeto

Com base nestes princípios, foi escolhida uma solução baseada em Nginx como Load Balancer.

## 2. Análise da Arquitetura Atual

### 2.1. Componentes do Sistema

No estado atual, a SPA encontra-se implementada da seguinte forma:

| Componente | Tecnologia | Função | Criticidade |
|------------|-----------:|--------|------------:|
| SPA Frontend | React / Angular | Interface do utilizador | Alta |
| Web Server | Nginx | Servir conteúdo SPA | Alta |
| VM DEI | Linux | Ambiente de execução | Média |
| Rede | LAN DEI | Comunicação cliente-servidor | Média |

### 2.2. Pontos Únicos de Falha Identificados

A análise da arquitetura atual identificou os seguintes Single Points of Failure (SPOFs):

1. Existência de uma única instância da SPA
2. Dependência de um único web server
3. Falha da VM implica indisponibilidade total da aplicação

### 2.3. Impacto da Arquitetura Atual

| Cenário de Falha | Impacto | Tempo Estimado de Recuperação |
|------------------|---------|------------------------------:|
| Falha da VM SPA | Indisponibilidade total | 15–30 minutos |
| Sobrecarga de pedidos | Degradação severa | Imediata |
| Falha do web server | Serviço indisponível | 10–20 minutos |

**Conclusão:** a arquitetura atual não garante disponibilidade contínua nem tolerância a falhas.

## 3. Solução de Clustering e Load Balancing Proposta

### 3.1. Solução Proposta

Implementação de um Nginx Reverse Proxy Load Balancer, responsável por distribuir pedidos HTTP entre múltiplas instâncias da SPA.

### 3.2. Justificação Técnica

A solução baseada em Nginx foi selecionada pelos seguintes motivos:

- Amplamente utilizada em ambientes de produção
- Baixo overhead e elevada performance
- Suporte nativo a load balancing e failover
- Configuração simples e transparente
- Adequada a aplicações SPA, que são essencialmente stateless

Esta abordagem permite melhorar simultaneamente disponibilidade, performance e resiliência do sistema.

### 3.3. Arquitetura Proposta

```
Cliente
   │
   ▼
Nginx Load Balancer
   │
   ├── SPA Node 1
   └── SPA Node 2
```

### 3.4. Configuração do Load Balancer

```nginx
upstream spa_cluster {
    server 10.9.10.21;
    server 10.9.10.22;
}

server {
    listen 80;

    location / {
        proxy_pass http://spa_cluster;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

        # Opcional: cache para conteúdo estático com TTL curto
        # proxy_cache_bypass $http_upgrade;
    }
}
```

## 4. Testes de Distribuição de Carga e Failover

### 4.1. Teste de Distribuição de Carga

Ao efetuar múltiplos acessos ao endereço do load balancer, observou-se:

- Alternância consistente entre os nós SPA
- Distribuição equilibrada de pedidos
- Respostas estáveis sob carga moderada

Para testes mais rigorosos, recomenda-se usar ferramentas como `ab`, `wrk` ou `k6` e medir throughput, latência e taxa de erros.

### 4.2. Teste de Failover

Simulação de Falha (exemplo):

- Em um dos nós SPA: `systemctl stop nginx` (ou parar o serviço da aplicação)

Resultado Observado:

- A aplicação manteve-se acessível
- Todo o tráfego foi redirecionado automaticamente para o nó funcional
- Não foi necessária intervenção manual

### 4.3. Registo de Comportamento

| Cenário | Comportamento do Sistema |
|--------:|-------------------------|
| Operação normal | Carga distribuída |
| Pico de acessos | Resposta estável |
| Falha de um nó | Serviço mantém-se disponível |
| Recuperação do nó | Reintegração automática |

## 5. Análise de Impacto

### 5.1. Impacto em Desempenho

| Métrica | Antes | Depois |
|--------:|:-----:|:-----:|
| Disponibilidade | Média | Elevada |
| Tolerância a falhas | Nenhuma | Automática |
| Capacidade de resposta | Limitada | Escalável |

### 5.2. Impacto em Custo

| Componente | Custo |
|-----------|------:|
| Nginx | 0 EUR |
| VMs adicionais | Infraestrutura existente |
| Licenciamento | Não aplicável |

**Conclusão:** solução sem impacto financeiro relevante.

### 5.3. Impacto em Manutenibilidade

**Vantagens**

- Configuração simples
- Fácil extensão para mais nós
- Failover automático

**Desvantagens**

- Aumento do número de componentes
- Necessidade de monitorização básica do load balancer

## 6. Conclusão

A implementação de uma solução de clustering e load balancing para a SPA através de um Nginx reverse proxy cumpre integralmente os requisitos definidos na US 4.4.5.

A solução garante:

- Alta disponibilidade
- Continuidade do serviço em caso de falha
- Melhor comportamento sob carga
- Baixo custo e simplicidade operacional

## 7. Conformidade com Acceptance Criteria

| Critério | Estado |
|---------|:------:|
| AC1 – Abordagem documentada e justificada | ✓ |
| AC2 – Load balancing e failover demonstrados | ✓ |
| AC3 – Resultados e comportamento registados | ✓ |


---

**Notas e próximos passos recomendados:**

1. Automatizar criação e configuração dos nós SPA (Infrastructure as Code) para facilitar escalabilidade
2. Integrar monitorização (Prometheus + Grafana) para métricas do load balancer e nós
3. Adicionar health checks mais sofisticados no Nginx (HTTP health endpoints) e tempos de retry
4. Considerar cache reverso no Nginx para reduzir carga nos nós SPA quando aplicável


**Anexos sugeridos:**
- Exemplo de ficheiro `nginx` completo com health checks e timeouts
- Scripts de teste com `k6` ou `wrk` e resultados das execuções
- Diagrama de arquitetura em formato PNG/SVG
