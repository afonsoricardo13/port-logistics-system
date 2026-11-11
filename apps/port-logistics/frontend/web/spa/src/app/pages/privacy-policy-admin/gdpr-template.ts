/**
 * GDPR-Compliant Privacy Policy Template
 * Based on RGPD Articles 13 and 14
 * 
 * This template ensures full compliance with GDPR requirements for:
 * - Transparency (Art. 5)
 * - Information to data subjects (Art. 13-14)
 * - Data subject rights (Art. 15-22)
 * - Special provisions for indirect data collection (vessel crew, etc.)
 */

export const GDPR_PRIVACY_POLICY_TEMPLATE = `
<div class="privacy-policy-document">

<h1>Política de Privacidade e Proteção de Dados</h1>

<p class="last-updated"><em>A presente Política de Privacidade aplica-se ao tratamento de dados pessoais no âmbito do Sistema de Logística Portuária, em cumprimento do Regulamento Geral sobre a Proteção de Dados (RGPD - Regulamento UE 2016/679) e da legislação nacional aplicável.</em></p>

<section>
<h2>1. Responsável pelo Tratamento de Dados</h2>
<p><strong>Entidade:</strong> Porto de [Nome do Porto], S.A.</p>
<p><strong>Morada:</strong> [Endereço completo]</p>
<p><strong>Email de Contacto:</strong> <a href="mailto:privacy@portlogistics.com">privacy@portlogistics.com</a></p>
<p><strong>Encarregado de Proteção de Dados (DPO):</strong> [Nome do DPO]</p>
<p><strong>Email do DPO:</strong> <a href="mailto:dpo@portlogistics.com">dpo@portlogistics.com</a></p>
</section>

<section>
<h2>2. Categorias de Dados Pessoais Tratados</h2>

<h3>2.1. Utilizadores Registados do Sistema (System Users)</h3>
<p>Recolhemos e tratamos os seguintes dados pessoais:</p>
<ul>
  <li><strong>Dados de Identificação:</strong> Nome completo, endereço de email, número de telefone</li>
  <li><strong>Dados de Autenticação:</strong> Credenciais de login, tokens de sessão</li>
  <li><strong>Dados de Atividade:</strong> Logs de acesso, operações realizadas, timestamps de ações</li>
  <li><strong>Dados Profissionais:</strong> Empresa/organização, função/cargo, permissões de sistema</li>
</ul>

<h3>2.2. Dados de Terceiros Não-Utilizadores (Indirect Data Subjects)</h3>
<p><em>Em conformidade com o Art. 14.º do RGPD, informamos sobre o tratamento de dados de pessoas que não interagem diretamente com o sistema:</em></p>

<h4>Tripulantes de Embarcações</h4>
<ul>
  <li><strong>Dados Recolhidos:</strong> Nome, nacionalidade, função a bordo, número de documento de identificação/passaporte</li>
  <li><strong>Fonte dos Dados:</strong> Manifesto da embarcação (FAL Convention), fornecidos pelo armador ou agente marítimo</li>
  <li><strong>Finalidade:</strong> Cumprimento de obrigações legais de segurança marítima, controlo fronteiriço e alfandegário</li>
  <li><strong>Base Legal:</strong> Cumprimento de obrigação legal (RGPD Art. 6.º, n.º 1, al. c) - Decreto-Lei n.º 166/2019 (segurança marítima e portuária)</li>
  <li><strong>Conservação:</strong> 3 anos após a escala da embarcação</li>
  <li><strong>Destinatários:</strong> Autoridades Marítimas, SEF/AIMA, Autoridade Tributária e Aduaneira</li>
</ul>

<h4>Passageiros de Embarcações</h4>
<ul>
  <li><strong>Dados Recolhidos:</strong> Nome, nacionalidade, número de documento</li>
  <li><strong>Fonte:</strong> Lista de passageiros fornecida pelo transportador marítimo</li>
  <li><strong>Finalidade:</strong> Segurança marítima e controlo fronteiriço</li>
  <li><strong>Base Legal:</strong> Obrigação legal (RGPD Art. 6.º, n.º 1, al. c)</li>
  <li><strong>Conservação:</strong> 2 anos</li>
</ul>

<h4>Motoristas e Operadores Externos</h4>
<ul>
  <li><strong>Dados Recolhidos:</strong> Nome, matrícula do veículo, empresa, número de contacto</li>
  <li><strong>Fonte:</strong> Registo de entrada/saída de zona portuária</li>
  <li><strong>Finalidade:</strong> Controlo de acessos, segurança das instalações portuárias</li>
  <li><strong>Base Legal:</strong> Obrigação legal (Código ISPS) e interesse legítimo do responsável</li>
  <li><strong>Conservação:</strong> 1 ano</li>
</ul>
</section>

<section>
<h2>3. Finalidades do Tratamento de Dados</h2>
<p>Os dados pessoais são tratados exclusivamente para as seguintes finalidades:</p>
<table class="data-purposes-table">
  <thead>
    <tr>
      <th>Finalidade</th>
      <th>Base Legal (RGPD Art. 6.º)</th>
      <th>Categorias de Dados</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Gestão de contas de utilizador e autenticação</td>
      <td>Execução de contrato (al. b)</td>
      <td>Dados de identificação e autenticação</td>
    </tr>
    <tr>
      <td>Planeamento e execução de operações portuárias</td>
      <td>Execução de contrato (al. b) / Interesse legítimo (al. f)</td>
      <td>Dados profissionais, logs de atividade</td>
    </tr>
    <tr>
      <td>Segurança marítima e portuária (ISPS Code)</td>
      <td>Cumprimento de obrigação legal (al. c)</td>
      <td>Dados de tripulantes, registos de acessos</td>
    </tr>
    <tr>
      <td>Cumprimento de obrigações fiscais e aduaneiras</td>
      <td>Cumprimento de obrigação legal (al. c)</td>
      <td>Dados de carga e tripulação</td>
    </tr>
    <tr>
      <td>Auditoria e rastreabilidade de operações</td>
      <td>Interesse legítimo (al. f)</td>
      <td>Logs de sistema, registos de atividade</td>
    </tr>
  </tbody>
</table>
</section>

<section>
<h2>4. Destinatários dos Dados</h2>
<p>Os dados pessoais podem ser partilhados com:</p>
<ul>
  <li><strong>Autoridades Competentes:</strong> Autoridade Marítima, Capitania do Porto, SEF/AIMA, Autoridade Tributária e Aduaneira, PSP/GNR</li>
  <li><strong>Prestadores de Serviços:</strong> Fornecedores de serviços cloud (armazenamento de dados), prestadores de suporte técnico</li>
  <li><strong>Operadores Portuários:</strong> Agentes marítimos, operadores de terminal, empresas de estiva</li>
  <li><strong>Organizações Internacionais:</strong> International Maritime Organization (IMO), no âmbito de obrigações de reporte</li>
</ul>
<p><strong>Transferências Internacionais:</strong> Alguns dados podem ser transferidos para países fora da UE/EEE apenas quando existam garantias adequadas nos termos do RGPD (Art. 44.º-46.º).</p>
</section>

<section>
<h2>5. Prazos de Conservação</h2>
<p>Os dados pessoais são conservados apenas pelo tempo estritamente necessário:</p>
<ul>
  <li><strong>Contas de Utilizador:</strong> Até 6 meses após cessação de relação contratual</li>
  <li><strong>Logs de Sistema:</strong> 2 anos (auditoria e segurança)</li>
  <li><strong>Dados de Tripulantes:</strong> 3 anos (obrigações legais marítimas)</li>
  <li><strong>Dados de Operações Portuárias:</strong> 5 anos (obrigações fiscais e comerciais)</li>
  <li><strong>Dados de Videovigilância (se aplicável):</strong> 30 dias (máximo legal)</li>
</ul>
<p>Após os prazos indicados, os dados são eliminados de forma segura e irreversível.</p>
</section>

<section>
<h2>6. Direitos dos Titulares dos Dados</h2>
<p>Nos termos dos Arts. 15.º a 22.º do RGPD, os titulares dos dados têm os seguintes direitos:</p>

<div class="rights-grid">
  <div class="right-card">
    <h4>🔍 Direito de Acesso (Art. 15.º)</h4>
    <p>Obter confirmação sobre se os seus dados estão a ser tratados e aceder aos mesmos.</p>
  </div>

  <div class="right-card">
    <h4>✏️ Direito de Retificação (Art. 16.º)</h4>
    <p>Corrigir dados inexatos ou completar dados incompletos.</p>
  </div>

  <div class="right-card">
    <h4>🗑️ Direito ao Apagamento (Art. 17.º)</h4>
    <p>Solicitar eliminação dos dados ("direito a ser esquecido"), exceto quando existam obrigações legais de conservação.</p>
  </div>

  <div class="right-card">
    <h4>⏸️ Direito à Limitação (Art. 18.º)</h4>
    <p>Restringir o tratamento em determinadas circunstâncias.</p>
  </div>

  <div class="right-card">
    <h4>📤 Direito à Portabilidade (Art. 20.º)</h4>
    <p>Receber os seus dados em formato estruturado e de uso corrente.</p>
  </div>

  <div class="right-card">
    <h4>🚫 Direito de Oposição (Art. 21.º)</h4>
    <p>Opor-se ao tratamento baseado em interesse legítimo.</p>
  </div>

  <div class="right-card">
    <h4>👤 Decisões Automatizadas (Art. 22.º)</h4>
    <p>Não ficar sujeito a decisões baseadas unicamente em tratamento automatizado.</p>
  </div>

  <div class="right-card">
    <h4>⚖️ Direito de Reclamação</h4>
    <p>Apresentar reclamação junto da Comissão Nacional de Proteção de Dados (CNPD) - <a href="https://www.cnpd.pt" target="_blank">www.cnpd.pt</a></p>
  </div>
</div>

<p><strong>Como Exercer os Seus Direitos:</strong></p>
<p>Para exercer qualquer dos direitos acima, envie pedido por escrito para <a href="mailto:privacy@portlogistics.com">privacy@portlogistics.com</a> ou <a href="mailto:dpo@portlogistics.com">dpo@portlogistics.com</a>. Responderemos no prazo de 1 mês (prorrogável por mais 2 meses em casos complexos).</p>
</section>

<section>
<h2>7. Segurança dos Dados</h2>
<p>Implementamos medidas técnicas e organizativas adequadas para proteger os dados pessoais contra acessos não autorizados, perda, destruição ou alteração:</p>
<ul>
  <li>Encriptação de dados em trânsito (TLS/SSL) e em repouso</li>
  <li>Autenticação multi-fator para utilizadores</li>
  <li>Controlo de acessos baseado em funções (RBAC)</li>
  <li>Logs de auditoria e monitorização de segurança</li>
  <li>Backups regulares e plano de continuidade de negócio</li>
  <li>Formação em proteção de dados para colaboradores</li>
</ul>
</section>

<section>
<h2>8. Cookies e Tecnologias Similares</h2>
<p>O sistema utiliza cookies essenciais para o funcionamento da plataforma:</p>
<ul>
  <li><strong>Cookies de Sessão:</strong> Mantêm a autenticação do utilizador (eliminados ao fechar o browser)</li>
  <li><strong>Cookies de Preferências:</strong> Armazenam configurações do utilizador (idioma, tema)</li>
</ul>
<p>Não utilizamos cookies de marketing ou tracking de terceiros.</p>
</section>

<section>
<h2>9. Menores de Idade</h2>
<p>O sistema não é destinado a menores de 16 anos. Não recolhemos intencionalmente dados de crianças. Se tomar conhecimento de que um menor forneceu dados pessoais, contacte-nos imediatamente.</p>
</section>

<section>
<h2>10. Alterações a Esta Política</h2>
<p>Podemos atualizar esta Política de Privacidade periodicamente para refletir alterações nas nossas práticas ou na legislação aplicável. Alterações significativas serão notificadas através de:</p>
<ul>
  <li>Notificação no sistema aquando do próximo login (em cumprimento do Art. 13.º, n.º 3 do RGPD)</li>
  <li>Email para os utilizadores registados</li>
  <li>Publicação na página principal do sistema</li>
</ul>
<p>O versionamento desta política garante rastreabilidade completa das alterações.</p>
</section>

<section>
<h2>11. Base Legal e Enquadramento Normativo</h2>
<p>O tratamento de dados pessoais é efetuado em conformidade com:</p>
<ul>
  <li><strong>Regulamento (UE) 2016/679</strong> - Regulamento Geral sobre a Proteção de Dados (RGPD)</li>
  <li><strong>Lei n.º 58/2019, de 8 de agosto</strong> - Execução do RGPD em Portugal</li>
  <li><strong>Decreto-Lei n.º 166/2019</strong> - Segurança das instalações marítimas e portuárias</li>
  <li><strong>Convenção FAL (IMO)</strong> - Facilitação do tráfego marítimo internacional</li>
  <li><strong>Código ISPS</strong> - International Ship and Port Facility Security Code</li>
</ul>
</section>

<section>
<h2>12. Contactos e Informações Adicionais</h2>
<p><strong>Para questões sobre esta Política de Privacidade:</strong></p>
<p>Email: <a href="mailto:privacy@portlogistics.com">privacy@portlogistics.com</a></p>
<p>Telefone: [+XXX XXX XXX XXX]</p>

<p><strong>Encarregado de Proteção de Dados (DPO):</strong></p>
<p>Email: <a href="mailto:dpo@portlogistics.com">dpo@portlogistics.com</a></p>

<p><strong>Comissão Nacional de Proteção de Dados (CNPD):</strong></p>
<p>Website: <a href="https://www.cnpd.pt" target="_blank">www.cnpd.pt</a></p>
<p>Email: geral@cnpd.pt</p>
</section>

<div class="policy-footer">
  <p><em>Esta política de privacidade foi elaborada em cumprimento do disposto nos artigos 13.º e 14.º do Regulamento (UE) 2016/679 (RGPD) e reflete o nosso compromisso com a transparência e a proteção dos direitos dos titulares dos dados.</em></p>
</div>

</div>
`;

/**
 * Gets the current date in ISO format for effective date default
 */
export function getDefaultEffectiveDate(): string {
  const today = new Date();
  return today.toISOString().split('T')[0];
}

/**
 * Validates if content meets minimum GDPR requirements
 */
export function validateGDPRContent(content: string): { valid: boolean; errors: string[] } {
  const errors: string[] = [];
  
  // Check minimum length
  if (content.length < 500) {
    errors.push('Content too short - must provide comprehensive information');
  }
  
  // Check for essential GDPR sections (Articles 13-14 requirements)
  const requiredKeywords = [
    { keyword: /responsável|controller|tratamento/i, error: 'Must identify data controller' },
    { keyword: /finalidade|purpose/i, error: 'Must specify purposes of processing' },
    { keyword: /dados pessoais|personal data/i, error: 'Must describe personal data categories' },
    { keyword: /conservação|retention|prazo/i, error: 'Must specify retention periods' },
    { keyword: /direitos|rights/i, error: 'Must inform about data subject rights' },
    { keyword: /rgpd|gdpr/i, error: 'Must reference GDPR/RGPD' },
  ];
  
  requiredKeywords.forEach(({ keyword, error }) => {
    if (!keyword.test(content)) {
      errors.push(error);
    }
  });
  
  return {
    valid: errors.length === 0,
    errors
  };
}
