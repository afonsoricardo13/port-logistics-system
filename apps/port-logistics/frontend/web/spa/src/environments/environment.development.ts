// Development environment configuration (localhost)
export const environment = {
  production: false,
  // Google OAuth2 Configuration (IAM - US 3.2.1)
  oauth: {
    clientId: '426207238285-ft8225v1u0c4rh9ajaedvfsuo727l7h7.apps.googleusercontent.com',
    redirectUri: window.location.origin + '/callback',
    scope: 'openid profile email'
  },
  apiUrl: 'https://localhost:7096', // Port Logistics API
  oemApiUrl: 'https://localhost:7001', // OEM API URL
  schedulingApiUrl: 'http://localhost:5003' // Prolog Server
};
