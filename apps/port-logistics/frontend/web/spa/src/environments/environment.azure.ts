// Azure Production environment configuration
// Este arquivo deve ser usado ao fazer build para Azure
// Build command: ng build --configuration azure

export const environment = {
  production: true,
  
  // Google OAuth2 Configuration (IAM - US 3.2.1)
  oauth: {
    clientId: '426207238285-ft8225v1u0c4rh9ajaedvfsuo727l7h7.apps.googleusercontent.com',
    // O redirectUri será automaticamente o domínio do Azure Static Web App + /callback
    redirectUri: window.location.origin + '/callback',
    scope: 'openid profile email'
  },
  
  // Backend API URLs for Azure production
  apiUrl: 'https://portlogistics-api-3db01-f4b3b0hgc6dvdtax.swedencentral-01.azurewebsites.net',
  
  // Operations Execution Management API (Sprint C)
  oemApiUrl: 'https://operations-execution-api-3db01.azurewebsites.net',
  
  // Scheduling & Planning API (Prolog - Proxied via backend for HTTPS)
  schedulingApiUrl: 'https://portlogistics-api-3db01-f4b3b0hgc6dvdtax.swedencentral-01.azurewebsites.net'
};
