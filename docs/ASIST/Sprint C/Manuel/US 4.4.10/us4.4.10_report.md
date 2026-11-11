# US 4.4.10 - SSH Access via Certificate-Based Authentication Only


## 1. Análise da User Story

### 1.1. Descrição

A US 4.4.10 tem como objetivo reforçar a segurança do acesso remoto à máquina virtual, garantindo que o acesso SSH é permitido exclusivamente através de autenticação por chave pública (certificado), eliminando completamente a autenticação baseada em password.

Esta abordagem reduz significativamente o risco de ataques de brute-force, dictionary attacks e uso indevido de credenciais, além de permitir maior controlo e auditoria dos acessos administrativos ao sistema.

### 1.2. Acceptance Criteria

A implementação deve cumprir os seguintes critérios:

- **AC1**: A autenticação por password em SSH deve estar completamente desativada
- **AC2**: O acesso administrativo deve exigir uma chave pública válida previamente autorizada
- **AC3**: As alterações devem ser testadas e documentadas após a configuração

## 2. Abordagem e Justificação

### 2.1. Abordagem Escolhida

A solução adotada baseia-se em:

- Autenticação SSH por par de chaves pública/privada
- Configuração do serviço SSH (sshd) para recusar autenticação por password
- Gestão de acessos através do ficheiro `authorized_keys`

### 2.2. Justificação Técnica

A autenticação baseada em chaves SSH oferece as seguintes vantagens:

- Elimina dependência de passwords fracas ou reutilizadas
- Dificulta ataques automatizados
- Permite revogação simples de acessos (remoção de chaves)
- Garante rastreabilidade de acessos administrativos

Esta solução segue as melhores práticas de segurança para ambientes Linux e é amplamente utilizada em contextos empresariais.

## 3. Implementação

A implementação segue um conjunto de passos claros, que devem ser executados com cuidado para evitar perda de acesso administrativo.

### 3.1. Gerar par de chaves SSH (cliente)

Na máquina cliente (administrador), executar:

```bash
ssh-keygen -t ed25519 -C "admin@vm"
```

Aceitar o caminho por defeito e definir uma passphrase (opcional).

Resultado esperado:

- `~/.ssh/id_ed25519`
- `~/.ssh/id_ed25519.pub`

### 3.2. Copiar chave pública para a VM

Na máquina cliente, executar uma das opções:

```bash
ssh-copy-id user@<IP_da_VM>
```

Ou manualmente:

```bash
cat ~/.ssh/id_ed25519.pub | ssh user@<IP_da_VM> "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

Resultado esperado:

- A chave pública fica registada em `~/.ssh/authorized_keys` no utilizador destino

### 3.3. Testar acesso por chave antes de desativar passwords

Na máquina cliente:

```bash
ssh user@<IP_da_VM>
```

Resultado esperado:

- Login efetuado sem pedido de password do utilizador (apenas passphrase da chave, se configurada)

> Importante: Não prosseguir para desativar password até confirmar que o acesso por chave funciona corretamente.

### 3.4. Desativar autenticação por password (VM)

Na VM, como root ou com sudo, editar `/etc/ssh/sshd_config` e garantir que as seguintes diretivas estão definidas:

```
PasswordAuthentication no
ChallengeResponseAuthentication no
PubkeyAuthentication yes
UsePAM yes
```

Salvar o ficheiro e verificar sintaxe se necessário.

### 3.5. Reiniciar o serviço SSH

```bash
sudo systemctl restart sshd
# ou, em algumas distribuições
sudo systemctl restart ssh
```

## 4. Testes e Validação

Os testes descritos confirmam que a configuração cumpre os critérios de segurança.

### 4.1. Teste de login com chave válida

Na máquina cliente:

```bash
ssh user@<IP_da_VM>
```

Resultado esperado:

- Login efetuado com sucesso via chave pública
- Nenhuma solicitação de password do utilizador

### 4.2. Teste de login com password (esperado falhar)

Forçar tentativa sem chave:

```bash
ssh -o PreferredAuthentications=password user@<IP_da_VM>
```

Resultado esperado:

- `Permission denied (publickey).`

### 4.3. Teste de acesso sem chave autorizada

A partir de outra máquina sem chave registada:

```bash
ssh user@<IP_da_VM>
```

Resultado esperado:

- `Permission denied (publickey).`

### 4.4. Teste de rollback (em caso de erro)

Caso um administrador perca acesso por chave, ter um plano de contingência é crítico. Recomenda-se:

1. Acesso alternativo via console da VM (provider cloud) ou KVM/IPMI
2. Conta de emergência com chave previamente armazenada offline
3. Passos para reverter `sshd_config` e reiniciar o serviço

## 5. Impacto da Solução

### 5.1. Impacto na Segurança

| Aspeto | Antes | Depois |
|-------:|:-----:|:-----:|
| Autenticação por password | Permitida | Desativada |
| Resistência a brute-force | Baixa | Elevada |
| Controlo de acessos | Limitado | Granular |

### 5.2. Impacto Operacional

**Vantagens**

- Maior segurança
- Auditoria simplificada
- Revogação rápida de acessos (remoção de chaves)

**Desvantagens**

- Necessidade de gestão adequada das chaves privadas
- Dependência do cliente SSH configurado

## 6. Conclusão

A implementação da autenticação SSH exclusivamente baseada em certificados cumpre integralmente os requisitos definidos na US 4.4.10.

O sistema passa a aceitar apenas acessos autenticados por chaves públicas válidas, eliminando riscos associados a passwords e garantindo um controlo mais rigoroso e auditável dos acessos administrativos à máquina virtual.

## 7. Conformidade com Acceptance Criteria

| Critério | Estado |
|---------|:------:|
| AC1 – Password SSH desativada | ✓ |
| AC2 – Acesso apenas por chave pública | ✓ |
| AC3 – Testes documentados | ✓ |


---

**Anexos sugeridos:**

- Comandos passo-a-passo para automatizar distribuição de chaves (`ssh-copy-id`, Ansible playbook)
- Exemplo de playbook Ansible para configurar `sshd_config` em múltiplas VMs e gerir `authorized_keys`
- Runbook de emergência para recuperação de acesso via console cloud
