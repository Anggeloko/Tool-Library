﻿namespace Axl.Base.Http.Models
{
    public class HttpConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        
        // Credentials
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        
        // Dynamic Mapping for Auth Request
        public string UserField { get; set; } = "username";
        public string PasswordField { get; set; } = "password";

        // Dynamic Mapping for Auth Response
        public string TokenKey { get; set; } = "access_token";
        public string ExpirationKey { get; set; } = "expires_in";

        // Essential Endpoints
        public string AuthEndpoint { get; set; } = "/api/auth/login";

        // Defaults
        public int TimeoutSeconds { get; set; } = 300;
        public bool UseAuth { get; set; } = true;
        public string UserAgent { get; set; } = string.Empty;
        public bool IsDeveloperMode { get; set; } = false;
    }
}

