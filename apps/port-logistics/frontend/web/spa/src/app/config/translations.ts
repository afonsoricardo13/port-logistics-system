export const translations = {
  en: {
    app: { 
      companyName: '3DB Group 01' 
    },
    nav: {
      dashboard: 'Dashboard',
      userManagement: 'User Management',
      privacyPolicyAdmin: 'Privacy Policy',
      privacyPolicy: 'Privacy Policy',
      dataRights: 'Data Rights',
      dataRightsAdmin: 'Data Rights Requests',
      permissions: 'Permissions (RBAC)',
      operatingStaff: 'Operating Staff',
      docks: 'Docks',
      storageAreas: 'Storage Areas',
      vessels: 'Vessels',
      vesselTypes: 'Vessel Types',
      organizations: 'Organizations',
      representatives: 'Representatives',
      qualifications: 'Qualifications',
      physicalResources: 'Physical Resources',
      vesselVisits: 'Vessel Visit Notifications',
      dailyScheduling: 'Daily Scheduling',
      operations: 'Operations & Execution',
      viewer3d: '3D Viewer',
      logout: 'Logout'
    },
    dataRights: {
      title: 'My Data Rights',
      subtitle: 'Exercise your GDPR data rights under Regulation (EU) 2016/679',
      gdprInfo: {
        title: 'Your GDPR Rights',
        description: 'Under the General Data Protection Regulation (GDPR), you have the right to access, rectify, and request deletion of your personal data. All requests are logged and acknowledged as required by law.'
      },
      access: {
        title: 'Access My Data',
        description: 'Download a copy of all your personal data stored in our system.',
        button: 'Export My Data'
      },
      rectification: {
        title: 'Request Correction',
        description: 'Request correction of inaccurate or incomplete personal data.',
        button: 'Request Rectification'
      },
      deletion: {
        title: 'Request Deletion',
        description: 'Request erasure of your personal data (right to be forgotten).',
        button: 'Request Deletion'
      },
      history: {
        title: 'Request History',
        loading: 'Loading your requests...',
        empty: 'You have not made any data rights requests yet.',
        table: {
          id: 'ID',
          type: 'Type',
          status: 'Status',
          createdAt: 'Created',
          processedAt: 'Processed',
          notes: 'Response Notes'
        }
      },
      exportModal: {
        title: 'Export Personal Data',
        description: 'Choose your preferred format for data export. The export will include all personal data we hold about you.',
        preview: 'Data Preview',
        format: 'Export Format',
        download: 'Download Data'
      },
      rectificationModal: {
        title: 'Request Data Rectification',
        description: 'Select the fields you want to correct and provide the new values.',
        selectFields: 'Select Fields to Correct',
        reason: 'Reason for Request (Optional)',
        submit: 'Submit Request'
      },
      deletionModal: {
        title: 'Request Data Deletion',
        warning: 'This action cannot be undone. Once approved, your data will be permanently deleted.',
        description: 'Please note that some data may need to be retained for legal compliance purposes.',
        reason: 'Reason for Request (Optional)',
        submit: 'Submit Deletion Request'
      }
    },
    dataRightsAdmin: {
      title: 'GDPR Data Rights Requests',
      subtitle: 'Manage and process user data rights requests',
      loading: 'Loading requests...',
      empty: 'No requests found with the selected filters.',
      stats: {
        pending: 'Pending',
        inProgress: 'In Progress',
        total: 'Total Requests'
      },
      filters: {
        status: 'Status',
        type: 'Request Type',
        allStatuses: 'All Statuses',
        allTypes: 'All Types',
        refresh: 'Refresh'
      },
      table: {
        id: 'ID',
        user: 'User',
        type: 'Type',
        status: 'Status',
        createdAt: 'Created',
        actions: 'Actions'
      },
      processModal: {
        title: 'Process Request',
        newStatus: 'New Status',
        notes: 'Response Notes (Optional)',
        rejectionReason: 'Rejection Reason',
        submit: 'Process Request'
      },
      detailModal: {
        title: 'Request Details',
        process: 'Process This Request'
      }
    },
    incidentTypes: {
      title: 'Incident Types',
      create: 'Create Incident Type',
      edit: 'Edit Incident Type',
      code: 'Code',
      name: 'Name',
      description: 'Description',
      severity: 'Severity',
      parentType: 'Parent Type',
      noParent: 'None (Root Type)',
      showAll: 'Show All',
      showRootOnly: 'Show Root Only',
      filterBySeverity: 'Filter by Severity',
      filterByParent: 'Filter by Parent Type',
      allSeverities: 'All Severities',
      allTypes: 'All Types',
      noData: 'No incident types found',
      noMatchingTypes: 'No incident types match the selected filters',
      createFirst: 'Create your first incident type',
      confirmDelete: 'Confirm Deletion',
      deleteMessage: 'Are you sure you want to delete this incident type?',
      codeHelp: 'Unique identifier (e.g., T-INC001)',
      parentHelp: 'Select a parent type to create a hierarchical structure'
    },
    operations: {
      title: 'Operations & Execution Management',
      subtitle: 'Manage operation plans, executions, incidents, and tasks',
      incidentTypes: 'Incident Types',
      incidents: 'Incidents',
      operationPlans: 'Operation Plans',
      executions: 'Vessel Visit Executions',
      taskCategories: 'Task Categories',
      tasks: 'Complementary Tasks'
    },
    common: {
      loading: 'Loading...',
      saving: 'Saving...',
      save: 'Save',
      cancel: 'Cancel',
      close: 'Close',
      edit: 'Edit',
      delete: 'Delete',
      create: 'Create'
    },
    user: {
      role: {
        Administrator: 'Administrator',
        LogisticsOperator: 'Logistics Operator',
        PortAuthorityOfficer: 'Port Authority Officer',
        PortOperationsSupervisor: 'Port Operations Supervisor',
        ShippingAgentRepresentative: 'Shipping Agent Representative'
      }
    },
    dashboard: {
      title: 'Dashboard',
      welcome: 'Welcome to the Port Logistics System!',
      manageStaff: 'Manage operational staff members',
      manageDocks: 'View and manage dock information',
      trackVessels: 'Track vessel information',
      manageVisits: 'Manage vessel visit notifications'
    },
    language: {
      english: 'English',
      portuguese: 'Portuguese'
    }
  },
  pt: {
    app: { 
      companyName: '3DB Grupo 01' 
    },
    nav: {
      dashboard: 'Painel',
      userManagement: 'Gestão de Utilizadores',
      privacyPolicyAdmin: 'Política de Privacidade',
      privacyPolicy: 'Política de Privacidade',
      dataRights: 'Direitos de Dados',
      dataRightsAdmin: 'Pedidos de Direitos de Dados',
      permissions: 'Permissões (RBAC)',
      operatingStaff: 'Pessoal Operacional',
      docks: 'Docas',
      storageAreas: 'Áreas de Armazenamento',
      vessels: 'Embarcações',
      vesselTypes: 'Tipos de Embarcação',
      organizations: 'Organizações',
      representatives: 'Representantes',
      qualifications: 'Qualificações',
      physicalResources: 'Recursos Físicos',
      vesselVisits: 'Notificações de Visita',
      dailyScheduling: 'Agendamento Diário',
      operations: 'Operações & Execução',
      viewer3d: 'Visualizador 3D',
      logout: 'Sair'
    },
    dataRights: {
      title: 'Os Meus Direitos de Dados',
      subtitle: 'Exerça os seus direitos RGPD ao abrigo do Regulamento (UE) 2016/679',
      gdprInfo: {
        title: 'Os Seus Direitos RGPD',
        description: 'Ao abrigo do Regulamento Geral de Proteção de Dados (RGPD), tem o direito de aceder, retificar e solicitar a eliminação dos seus dados pessoais. Todos os pedidos são registados e reconhecidos conforme exigido por lei.'
      },
      access: {
        title: 'Aceder aos Meus Dados',
        description: 'Descarregue uma cópia de todos os seus dados pessoais armazenados no nosso sistema.',
        button: 'Exportar Os Meus Dados'
      },
      rectification: {
        title: 'Solicitar Correção',
        description: 'Solicite a correção de dados pessoais inexatos ou incompletos.',
        button: 'Solicitar Retificação'
      },
      deletion: {
        title: 'Solicitar Eliminação',
        description: 'Solicite a eliminação dos seus dados pessoais (direito ao esquecimento).',
        button: 'Solicitar Eliminação'
      },
      history: {
        title: 'Histórico de Pedidos',
        loading: 'A carregar os seus pedidos...',
        empty: 'Ainda não fez nenhum pedido de direitos de dados.',
        table: {
          id: 'ID',
          type: 'Tipo',
          status: 'Estado',
          createdAt: 'Criado',
          processedAt: 'Processado',
          notes: 'Notas de Resposta'
        }
      },
      exportModal: {
        title: 'Exportar Dados Pessoais',
        description: 'Escolha o formato preferido para a exportação de dados. A exportação incluirá todos os dados pessoais que temos sobre si.',
        preview: 'Pré-visualização de Dados',
        format: 'Formato de Exportação',
        download: 'Descarregar Dados'
      },
      rectificationModal: {
        title: 'Solicitar Retificação de Dados',
        description: 'Selecione os campos que pretende corrigir e forneça os novos valores.',
        selectFields: 'Selecionar Campos a Corrigir',
        reason: 'Motivo do Pedido (Opcional)',
        submit: 'Submeter Pedido'
      },
      deletionModal: {
        title: 'Solicitar Eliminação de Dados',
        warning: 'Esta ação não pode ser desfeita. Uma vez aprovado, os seus dados serão permanentemente eliminados.',
        description: 'Note que alguns dados podem precisar de ser retidos para fins de conformidade legal.',
        reason: 'Motivo do Pedido (Opcional)',
        submit: 'Submeter Pedido de Eliminação'
      }
    },
    dataRightsAdmin: {
      title: 'Pedidos de Direitos de Dados RGPD',
      subtitle: 'Gerir e processar pedidos de direitos de dados dos utilizadores',
      loading: 'A carregar pedidos...',
      empty: 'Nenhum pedido encontrado com os filtros selecionados.',
      stats: {
        pending: 'Pendentes',
        inProgress: 'Em Progresso',
        total: 'Total de Pedidos'
      },
      filters: {
        status: 'Estado',
        type: 'Tipo de Pedido',
        allStatuses: 'Todos os Estados',
        allTypes: 'Todos os Tipos',
        refresh: 'Atualizar'
      },
      table: {
        id: 'ID',
        user: 'Utilizador',
        type: 'Tipo',
        status: 'Estado',
        createdAt: 'Criado',
        actions: 'Ações'
      },
      processModal: {
        title: 'Processar Pedido',
        newStatus: 'Novo Estado',
        notes: 'Notas de Resposta (Opcional)',
        rejectionReason: 'Motivo de Rejeição',
        submit: 'Processar Pedido'
      },
      detailModal: {
        title: 'Detalhes do Pedido',
        process: 'Processar Este Pedido'
      }
    },
    incidentTypes: {
      title: 'Tipos de Incidente',
      create: 'Criar Tipo de Incidente',
      edit: 'Editar Tipo de Incidente',
      code: 'Código',
      name: 'Nome',
      description: 'Descrição',
      severity: 'Severidade',
      parentType: 'Tipo Pai',
      noParent: 'Nenhum (Tipo Raiz)',
      showAll: 'Mostrar Todos',
      showRootOnly: 'Mostrar Apenas Raiz',
      filterBySeverity: 'Filtrar por Severidade',
      filterByParent: 'Filtrar por Tipo Pai',
      allSeverities: 'Todas as Severidades',
      allTypes: 'Todos os Tipos',
      noData: 'Nenhum tipo de incidente encontrado',
      noMatchingTypes: 'Nenhum tipo de incidente corresponde aos filtros selecionados',
      createFirst: 'Crie o seu primeiro tipo de incidente',
      confirmDelete: 'Confirmar Eliminação',
      deleteMessage: 'Tem a certeza que deseja eliminar este tipo de incidente?',
      codeHelp: 'Identificador único (ex: T-INC001)',
      parentHelp: 'Selecione um tipo pai para criar uma estrutura hierárquica'
    },
    operations: {
      title: 'Gestão de Operações & Execução',
      subtitle: 'Gerir planos de operação, execuções, incidentes e tarefas',
      incidentTypes: 'Tipos de Incidente',
      incidents: 'Incidentes',
      operationPlans: 'Planos de Operação',
      executions: 'Execuções de Visita',
      taskCategories: 'Categorias de Tarefa',
      tasks: 'Tarefas Complementares'
    },
    common: {
      loading: 'A carregar...',
      saving: 'A guardar...',
      save: 'Guardar',
      cancel: 'Cancelar',
      close: 'Fechar',
      edit: 'Editar',
      delete: 'Eliminar',
      create: 'Criar'
    },
    user: {
      role: {
        Administrator: 'Administrador',
        LogisticsOperator: 'Operador Logístico',
        PortAuthorityOfficer: 'Oficial da Autoridade Portuária',
        PortOperationsSupervisor: 'Supervisor de Operações Portuárias',
        ShippingAgentRepresentative: 'Representante do Agente de Navegação'
      }
    },
    dashboard: {
      title: 'Painel',
      welcome: 'Bem-vindo ao Sistema de Logística Portuária!',
      manageStaff: 'Gerir membros do pessoal operacional',
      manageDocks: 'Ver e gerir informação das docas',
      trackVessels: 'Acompanhar informação das embarcações',
      manageVisits: 'Gerir notificações de visita de embarcações'
    },
    language: {
      english: 'Inglês',
      portuguese: 'Português'
    }
  }
};
