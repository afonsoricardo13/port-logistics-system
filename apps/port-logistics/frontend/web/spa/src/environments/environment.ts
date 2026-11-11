// Production environment configuration (Azure deployment)
export const environment = {
  production: true,
  // Google OAuth2 Configuration (IAM - US 3.2.1)
  oauth: {
    clientId: '426207238285-ft8225v1u0c4rh9ajaedvfsuo727l7h7.apps.googleusercontent.com',
    redirectUri: window.location.origin + '/callback',
    scope: 'openid profile email'
  },
  // Backend API URL for Azure production
  // IMPORTANTE: Atualizar com o URL real da sua API no Azure
  apiUrl: 'https://portlogistics-api-3db01-f4b3b0hgc6dvdtax.swedencentral-01.azurewebsites.net',
  oemApiUrl: 'https://localhost:7001', // OEM API URL - Update with Azure URL when deployed
  schedulingApiUrl: 'http://localhost:5003' // Prolog Server - Update with Azure URL when deployed
};
