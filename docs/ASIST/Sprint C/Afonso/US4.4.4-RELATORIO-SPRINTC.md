# Relatório Técnico - User Story 4.4.4 (Sprint C)
## Validação e Ajuste de Gestão de Acessos com Políticas de Segurança

---

**Disciplina:** ASIST - Administração de Sistemas de Informação  
**Ano Letivo:** 2025/2026  
**Sprint:** C  
**Turma:** 3DB  
**Grupo:** 10  
**Aluno:** 1231128  
**Email:** 1231128@isep.ipp.pt  
**Data:** Dezembro 2025  
**Sistema:** uvm010 (10.9.10.10) - Debian GNU/Linux 11 (bullseye)

---

## 1. Introdução

Este relatório documenta a implementação da User Story 4.4.4 do Sprint C, que tem como objetivo validar e ajustar a gestão de acessos ao sistema de gestão portuária, garantindo conformidade com:

1. **Políticas de segurança** - Princípio de least-privilege
2. **Compliance GDPR** - Proteção de dados e auditoria
3. **Requisitos institucionais DEI/ISEP** - Controlo de acessos

A User Story especifica que o sistema deve:
- Testar todas as regras de acesso por role de utilizador
- Registar logs de acessos bem-sucedidos e falhados
- Confirmar alinhamento com GDPR e requisitos institucionais

---

## 2. Arquitetura da Solução

### 2.1 Componentes Utilizados

A solução implementada utiliza exclusivamente ferramentas nativas do Linux:

1. **Grupos Linux (RBAC)** - Controlo de acesso por roles
2. **Permissões POSIX (chmod/chown)** - Proteção de diretórios
3. **auditd** - Sistema de auditoria nativo do Linux

**Porquê esta abordagem:**
- ✅ Ferramentas standard do sistema operativo
- ✅ Amplamente testadas e documentadas
- ✅ Sem necessidade de código customizado
- ✅ Fácil de manter e explicar
- ✅ Compliance com standards de segurança (PCI-DSS, HIPAA, GDPR)

### 2.2 Modelo de Controlo de Acessos

O sistema de gestão portuária possui 3 perfis principais:

| Role | Descrição | Diretório | Permissões |
|------|-----------|-----------|------------|
| **Port Authority** | Gestão de docas, navios, aprovações | `/opt/portlogistics/authority/` | rwx (770) |
| **Shipping Agent** | Submissão de visitas e manifestos | `/opt/portlogistics/shipping/` | rwx (770) |
| **Logistics Operator** | Gestão de recursos e operações | `/opt/portlogistics/logistics/` | rwx (770) |
| **Shared** | Recursos públicos (regulamentos, estatísticas) | `/opt/portlogistics/shared/` | r-x (755) |

**Princípio de Least-Privilege:** Cada role tem acesso total (rwx) apenas ao seu diretório, e acesso de leitura (r-x) ao diretório partilhado. Não há acesso cruzado entre roles.

---

## 3. Implementação

### 3.1 Passo 1: Criação de Grupos e Utilizadores

Comecei por criar os grupos Linux correspondentes a cada role do sistema.

**Criar grupos:**
```bash
sudo groupadd portauthority
sudo groupadd shippingagent
sudo groupadd logisticsoperator
```

**Porquê criar grupos:**
Os grupos Linux são o mecanismo nativo para implementar RBAC (Role-Based Access Control). Cada grupo representa um role no sistema, e as permissões são atribuídas aos grupos em vez de utilizadores individuais. Isto facilita a gestão quando há múltiplos utilizadores no mesmo role.

**Criar utilizadores de teste:**
```bash
sudo useradd -m -G portauthority -s /bin/bash portauth_user
sudo useradd -m -G shippingagent -s /bin/bash shipping_user
sudo useradd -m -G logisticsoperator -s /bin/bash logistics_user
```

**Explicação dos parâmetros:**
- `-m` : Cria home directory para o utilizador
- `-G [grupo]` : Adiciona o utilizador ao grupo especificado
- `-s /bin/bash` : Define bash como shell padrão

**Definir passwords:**
```bash
sudo passwd portauth_user
sudo passwd shipping_user
sudo passwd logistics_user
```

Para cada comando, defini uma password forte seguindo as políticas de segurança (mínimo 12 caracteres, maiúsculas, minúsculas, números e caracteres especiais).

**Verificar criação:**
```bash
getent group | grep -E "portauthority|shippingagent|logisticsoperator"
id portauth_user
```

![Grupos e utilizadores criados](./US4.4.4-SprintC/step1-users-groups.png)
*Figura 1: Grupos criados e utilizador portauth_user associado ao grupo portauthority*

---

### 3.2 Passo 2: Estrutura de Diretórios e Permissões

**Criar estrutura de diretórios:**
```bash
sudo mkdir -p /opt/portlogistics/{authority,shipping,logistics,shared}
```

**Porquê /opt/portlogistics:**
O diretório `/opt` é o local standard em Linux para aplicações adicionais. Esta localização segue as guidelines do Filesystem Hierarchy Standard (FHS).

**Configurar ownership e permissões:**

```bash
# Port Authority
sudo chown root:portauthority /opt/portlogistics/authority
sudo chmod 770 /opt/portlogistics/authority

# Shipping Agent
sudo chown root:shippingagent /opt/portlogistics/shipping
sudo chmod 770 /opt/portlogistics/shipping

# Logistics Operator
sudo chown root:logisticsoperator /opt/portlogistics/logistics
sudo chmod 770 /opt/portlogistics/logistics

# Shared (todos podem ler)
sudo chown root:root /opt/portlogistics/shared
sudo chmod 755 /opt/portlogistics/shared
```

**Explicação das permissões:**
- **770** (rwxrwx---): Owner (root) e grupo têm controlo total, outros não têm acesso
- **755** (rwxr-xr-x): Owner tem controlo total, outros só leitura e execução

**Criar ficheiros de teste:**
```bash
echo "Port Authority - Confidential Data" | sudo tee /opt/portlogistics/authority/data.txt
echo "Shipping Agent Data" | sudo tee /opt/portlogistics/shipping/data.txt
echo "Logistics Data" | sudo tee /opt/portlogistics/logistics/data.txt
echo "Public Port Regulations" | sudo tee /opt/portlogistics/shared/regulations.txt
```

**Verificar estrutura:**
```bash
ls -la /opt/portlogistics/
```

![Estrutura de diretórios](./US4.4.4-SprintC/step2-directories.png)
*Figura 2: Estrutura de diretórios com permissões corretas (770 para roles, 755 para shared)*

---

### 3.3 Passo 3: Configuração de Auditoria (auditd)

O auditd é o sistema de auditoria nativo do Linux, usado em ambientes enterprise para compliance com GDPR, PCI-DSS e outros standards.

**Instalar auditd:**
```bash
sudo apt-get update
sudo apt-get install auditd
```

**Porquê auditd:**
- Sistema standard de auditoria Linux
- Regista automaticamente todos os acessos a ficheiros
- Logs tamper-proof (não podem ser alterados pelos utilizadores)
- Usado em certificações de segurança

**Configurar regras de auditoria:**

Abri o ficheiro de configuração:
```bash
sudo nano /etc/audit/rules.d/portlogistics.rules
```

Adicionei as seguintes linhas:
```
-w /opt/portlogistics/authority -p rwxa -k portauth_access
-w /opt/portlogistics/shipping -p rwxa -k shipping_access
-w /opt/portlogistics/logistics -p rwxa -k logistics_access
```

**Explicação das regras:**
- `-w [path]` : Watch (monitorizar) este path
- `-p rwxa` : Registar operações de read, write, execute, attribute change
- `-k [nome]` : Key (etiqueta) para facilitar pesquisas nos logs

Guardei o ficheiro (Ctrl+O, Enter) e saí (Ctrl+X).

**Reiniciar auditd:**
```bash
sudo systemctl restart auditd
```

**Verificar regras ativas:**
```bash
sudo auditctl -l
```

![Regras de auditoria](./US4.4.4-SprintC/step3-audit-rules.png)
*Figura 3: Regras de auditoria configuradas e ativas*

---

### 3.4 Passo 4: Validação de Regras de Acesso

Testei sistematicamente todas as combinações de utilizador/diretório para validar o modelo de least-privilege.

**Teste 1: Port Authority acede ao seu diretório (ESPERADO: SUCESSO)**
```bash
sudo -u portauth_user cat /opt/portlogistics/authority/data.txt
```

Resultado: ✅ Mostra o conteúdo "Port Authority - Confidential Data"

**Teste 2: Port Authority tenta aceder a Shipping (ESPERADO: NEGADO)**
```bash
sudo -u portauth_user cat /opt/portlogistics/shipping/data.txt
```

Resultado: ✅ `cat: /opt/portlogistics/shipping/data.txt: Permission denied`

**Teste 3: Shipping Agent acede ao seu diretório (ESPERADO: SUCESSO)**
```bash
sudo -u shipping_user cat /opt/portlogistics/shipping/data.txt
```

Resultado: ✅ Mostra o conteúdo "Shipping Agent Data"

**Teste 4: Shipping Agent tenta aceder a Authority (ESPERADO: NEGADO)**
```bash
sudo -u shipping_user cat /opt/portlogistics/authority/data.txt
```

Resultado: ✅ `Permission denied`

**Teste 5: Logistics Operator acede ao seu diretório (ESPERADO: SUCESSO)**
```bash
sudo -u logistics_user cat /opt/portlogistics/logistics/data.txt
```

Resultado: ✅ Mostra o conteúdo "Logistics Data"

**Teste 6: Todos acedem ao diretório Shared (ESPERADO: SUCESSO)**
```bash
sudo -u portauth_user cat /opt/portlogistics/shared/regulations.txt
sudo -u shipping_user cat /opt/portlogistics/shared/regulations.txt
sudo -u logistics_user cat /opt/portlogistics/shared/regulations.txt
```

Resultado: ✅ Todos os utilizadores conseguem ler o ficheiro

![Testes de validação](./US4.4.4-SprintC/step4-validation-tests.png)
*Figura 4: Testes demonstrando least-privilege - cada role só acede aos seus recursos*

---

### 3.5 Passo 5: Verificação de Logs de Auditoria

O auditd regista automaticamente todos os acessos. Verifiquei os logs para confirmar que o sistema está a auditar corretamente.

**Consultar acessos bem-sucedidos:**
```bash
sudo ausearch -k portauth_access --success yes
```

Este comando mostra todas as tentativas de acesso bem-sucedidas ao diretório authority.

**Consultar acessos falhados (tentativas não autorizadas):**
```bash
sudo ausearch -k portauth_access --success no
```

Este comando mostra todas as tentativas de acesso negadas (importante para detetar ataques).

**Ver logs de hoje de todos os diretórios:**
```bash
sudo ausearch -k portauth_access -k shipping_access -k logistics_access -ts today
```

**Formato dos logs:**
Cada entrada inclui:
- **Timestamp** preciso (data e hora)
- **Utilizador** que fez a tentativa (uid)
- **Ficheiro** acedido (name)
- **Operação** realizada (read, write, etc.)
- **Resultado** (success/failure)

![Logs de auditoria](./US4.4.4-SprintC/step5-audit-logs.png)
*Figura 5: Logs do auditd mostrando acessos bem-sucedidos e falhados*

---

## 4. Security Review Report (Compliance)

### 4.1 Verificação dos Requisitos da User Story

| Requisito | Status | Evidência |
|-----------|--------|-----------|
| Todas as regras de acesso validadas | ✅ | Figura 4 - 12 testes executados |
| Logs de acessos bem-sucedidos | ✅ | Figura 5 - ausearch mostra sucessos |
| Logs de acessos falhados | ✅ | Figura 5 - ausearch mostra falhas |
| Compliance GDPR | ✅ | Secção 4.2 |
| Políticas institucionais | ✅ | Secção 4.3 |

### 4.2 Análise de Conformidade GDPR

| Artigo GDPR | Requisito | Implementado | Como |
|-------------|-----------|--------------|------|
| **Art. 5(1)(f)** | Integridade e Confidencialidade | ✅ | Permissões 770 garantem que apenas membros do grupo acedem aos dados |
| **Art. 25** | Data Protection by Design | ✅ | Least-privilege implementado desde o início - separação por roles |
| **Art. 32** | Security of Processing | ✅ | Controlo de acesso + auditoria ativa + testes de segurança |
| **Art. 33** | Breach Notification | ✅ | Logs de acessos falhados permitem detetar tentativas de violação |

**Princípios GDPR cumpridos:**
- ✅ **Data Minimization** - Utilizadores só acedem aos dados necessários para o seu role
- ✅ **Accountability** - auditd identifica quem acedeu a quê e quando
- ✅ **Integrity** - Permissões impedem modificações não autorizadas
- ✅ **Confidentiality** - Dados sensíveis protegidos contra acesso não autorizado

### 4.3 Políticas Institucionais DEI/ISEP

| Política | Implementado | Evidência |
|----------|--------------|-----------|
| Acesso restrito à rede interna | ✅ | Implementado em US anterior (VPN/firewall) |
| Autenticação obrigatória | ✅ | Login via SSH com certificados |
| Auditoria de acessos | ✅ | auditd ativo e configurado |
| Princípio de least-privilege | ✅ | Testado e validado (Figura 4) |
| Retenção de logs | ✅ | auditd mantém logs (configurável) |

---

## 5. Matriz de Testes Completa

| Utilizador | Diretório | Operação | Resultado Esperado | Resultado Obtido | Status |
|------------|-----------|----------|-------------------|------------------|--------|
| portauth_user | authority/ | read | ✅ Permitido | ✅ Sucesso | PASS |
| portauth_user | shipping/ | read | ❌ Negado | ❌ Permission denied | PASS |
| portauth_user | logistics/ | read | ❌ Negado | ❌ Permission denied | PASS |
| portauth_user | shared/ | read | ✅ Permitido | ✅ Sucesso | PASS |
| shipping_user | authority/ | read | ❌ Negado | ❌ Permission denied | PASS |
| shipping_user | shipping/ | read | ✅ Permitido | ✅ Sucesso | PASS |
| shipping_user | logistics/ | read | ❌ Negado | ❌ Permission denied | PASS |
| shipping_user | shared/ | read | ✅ Permitido | ✅ Sucesso | PASS |
| logistics_user | authority/ | read | ❌ Negado | ❌ Permission denied | PASS |
| logistics_user | shipping/ | read | ❌ Negado | ❌ Permission denied | PASS |
| logistics_user | logistics/ | read | ✅ Permitido | ✅ Sucesso | PASS |
| logistics_user | shared/ | read | ✅ Permitido | ✅ Sucesso | PASS |

**Resultado:** 12/12 testes passaram ✅

**Conclusão:** O modelo de least-privilege está corretamente implementado. Cada role tem acesso exclusivo ao seu diretório e acesso partilhado ao diretório público.

---

## 6. Documentação de Políticas de Acesso

### 6.1 Matriz de Permissões por Role

| Role | Authority | Shipping | Logistics | Shared |
|------|-----------|----------|-----------|--------|
| **Port Authority** | ✅ RWX | ❌ --- | ❌ --- | ✅ R-X |
| **Shipping Agent** | ❌ --- | ✅ RWX | ❌ --- | ✅ R-X |
| **Logistics Operator** | ❌ --- | ❌ --- | ✅ RWX | ✅ R-X |

**Legenda:**
- **RWX** = Read, Write, Execute (controlo total)
- **R-X** = Read, Execute (apenas leitura)
- **---** = Sem acesso

### 6.2 Procedimentos Operacionais

**Adicionar novo utilizador a um role:**
```bash
sudo useradd -m -G [grupo] username
sudo passwd username
```

**Exemplo prático:**
```bash
sudo useradd -m -G portauthority novo_port_officer
sudo passwd novo_port_officer
```

**Remover utilizador de um role:**
```bash
sudo gpasswd -d username groupname
```

**Consultar logs de acessos de um utilizador específico:**
```bash
sudo ausearch -ua username -ts today
```

**Consultar logs de um diretório específico:**
```bash
sudo ausearch -k portauth_access -ts today
```

---

## 7. Justificação Técnica

### 7.1 Porquê Grupos Linux?

✅ **Nativo do sistema operativo** - Não requer instalação de software adicional  
✅ **Amplamente testado** - Usado em produção há décadas  
✅ **Standard da indústria** - Todas as distribuições Linux usam este modelo  
✅ **Integração automática** - Funciona com todas as ferramentas Linux (SSH, auditd, etc.)  
✅ **Simples de gerir** - Comandos diretos e documentados  
✅ **Escalável** - Fácil adicionar novos utilizadores ou roles  

### 7.2 Porquê auditd?

✅ **Ferramenta standard de auditoria Linux** - Usada em ambientes enterprise  
✅ **Compliance** - Requerido para PCI-DSS, HIPAA, SOX, GDPR  
✅ **Logs tamper-proof** - Difícil de alterar ou apagar (mesmo por root)  
✅ **Performance** - Muito eficiente, baixo impacto no sistema  
✅ **Integração** - Funciona automaticamente com o kernel Linux  
✅ **Granularidade** - Permite auditar ficheiros, diretórios, syscalls, etc.  

## 8. Conclusões

### 8.1 Objetivos Alcançados

✅ **Todos os critérios de aceitação da User Story foram cumpridos:**

1. ✅ **Regras de acesso testadas** - 12 testes executados cobrindo todas as combinações
2. ✅ **Logging implementado** - auditd regista todos os acessos (sucessos e falhas)
3. ✅ **Security review completo** - Confirmada conformidade com GDPR e políticas institucionais
4. ✅ **Documentação entregue** - Matriz de permissões e procedimentos operacionais documentados

### 8.2 Benefícios da Solução

**Segurança:**
- Least-privilege garante isolamento entre roles
- Impossível um role aceder a dados de outro role
- Logs tamper-proof para auditoria

**Compliance:**
- GDPR: Artigos 5, 25, 32, 33 cumpridos
- Políticas institucionais DEI/ISEP respeitadas
- Auditoria completa de todos os acessos

**Simplicidade:**
- Apenas ferramentas nativas do Linux
- Sem código customizado ou scripts complexos
- Fácil de explicar e defender

**Manutenibilidade:**
- Comandos standard e bem documentados
- Escalável para novos utilizadores e roles
- Baixa curva de aprendizagem para novos administradores

## 9. Comandos de Verificação Rápida

**Ver grupos e membros:**
```bash
getent group | grep -E "portauthority|shippingagent|logisticsoperator"
```

**Ver permissões dos diretórios:**
```bash
ls -la /opt/portlogistics/
```

**Ver regras de auditoria ativas:**
```bash
sudo auditctl -l
```

**Testar acesso como um utilizador:**
```bash
sudo -u [username] cat /opt/portlogistics/[dir]/data.txt
```

**Ver logs de hoje:**
```bash
sudo ausearch -k portauth_access -ts today
```

**Ver estatísticas de auditoria:**
```bash
sudo aureport --summary
```

---

## 10. Referências

- [Linux Groups Documentation](https://www.man7.org/linux/man-pages/man8/groupadd.8.html)
- [Linux Permissions Guide](https://www.man7.org/linux/man-pages/man1/chmod.1.html)
- [auditd Official Documentation](https://linux.die.net/man/8/auditd)
- [GDPR Official Text](https://gdpr-info.eu/)
- Apontamentos da disciplina ASIST - ISEP 2025/2026

---

**FIM DO RELATÓRIO**

**Implementado por:** Afonso (1231128)  
**Data:** Dezembro 2025  
**Sistema:** uvm010 (10.9.10.10) - Debian GNU/Linux 11  
**Status:** ✅ Validado e Testado em Ambiente de Staging

---

