# SignalR Real-Time Notification System - Azure Setup Guide

## Overview

This guide provides comprehensive instructions for setting up Azure SignalR Service for the CodePulse real-time notification system. The implementation includes both local SignalR support and Azure SignalR Service for production scalability.

## 🏗️ Architecture Overview

```
Frontend Application
       ↓
   SignalR Client
       ↓
Azure SignalR Service / Local SignalR
       ↓
   CodePulse API
       ↓
  Notification Service
       ↓
   Webhook Events
```

## 🚀 Features Implemented

### SignalR Hub (`NotificationHub`)
- **Authentication**: JWT-based authentication required
- **Groups**: User-specific and repository-specific notification groups
- **Events**: Webhook events, pull requests, commits, reviews, system alerts
- **Methods**: Subscribe/unsubscribe to repositories and webhook events

### Notification Service (`NotificationService`)
- **Real-time broadcasting**: To users, groups, and all clients
- **Notification types**: Webhook, Pull Request, Commit, Review, System notifications
- **Persistence**: Notification storage (ready for database implementation)
- **Filtering**: By repository, user, event type, and severity

### Supported Notification Types
- **Webhook Received**: When GitHub/Azure DevOps webhooks are processed
- **Push Events**: New commits pushed to repositories
- **Pull Request Events**: Created, merged, closed pull requests
- **Code Reviews**: AI and human code review notifications
- **System Alerts**: Errors, warnings, and system status updates

## 🔧 Azure SignalR Service Setup

### Step 1: Create Azure SignalR Service

1. **Azure Portal Setup**:
   ```bash
   # Using Azure CLI
   az signalr create \
     --name "codepulse-signalr" \
     --resource-group "codepulse-rg" \
     --location "East US" \
     --sku "Standard_S1" \
     --service-mode "Default"
   ```

2. **Service Mode Configuration**:
   - **Default Mode**: Recommended for this implementation
   - **Serverless Mode**: For Azure Functions integration (future enhancement)
   - **Classic Mode**: Legacy support

### Step 2: Get Connection String

1. Navigate to Azure Portal → SignalR Service → Keys
2. Copy the **Connection String** (Primary or Secondary)
3. Format: `Endpoint=https://your-signalr.service.signalr.net;AccessKey=your-access-key;Version=1.0;`

### Step 3: Configure Application Settings

Add to `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-sql-connection-string",
    "AzureSignalRConnectionString": "Endpoint=https://codepulse-signalr.service.signalr.net;AccessKey=your-access-key;Version=1.0;"
  },
  "Frontend": {
    "Url": "https://your-frontend-domain.com"
  },
  "SignalR": {
    "EnableDetailedErrors": false,
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30",
    "MaximumReceiveMessageSize": 32768
  }
}
```

### Step 4: Production Configuration

For production environments, add to `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "AzureSignalRConnectionString": "{{AZURE_SIGNALR_CONNECTION_STRING}}"
  },
  "SignalR": {
    "EnableDetailedErrors": false,
    "KeepAliveInterval": "00:00:15",
    "ClientTimeoutInterval": "00:00:30"
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Warning",
      "Microsoft.AspNetCore.Http.Connections": "Warning"
    }
  }
}
```

## 🔑 Authentication & Security

### JWT Token Configuration

The SignalR hub requires authentication. Configure JWT in your frontend:

```javascript
// Frontend SignalR connection with JWT
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub", {
        accessTokenFactory: () => {
            return localStorage.getItem("jwt-token");
        }
    })
    .build();
```

### CORS Configuration

Ensure CORS is properly configured for your frontend domain:
```csharp
// In Program.cs - already configured
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://your-frontend-domain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});
```

## 📱 Frontend Integration

### JavaScript/TypeScript Client Setup

1. **Install SignalR Client**:
   ```bash
   npm install @microsoft/signalr
   ```

2. **Basic Connection Setup**:
   ```typescript
   import * as signalR from "@microsoft/signalr";

   class NotificationService {
     private connection: signalR.HubConnection;

     constructor() {
       this.connection = new signalR.HubConnectionBuilder()
         .withUrl("/notificationHub", {
           accessTokenFactory: () => this.getAuthToken()
         })
         .withAutomaticReconnect()
         .build();

       this.setupEventHandlers();
     }

     private setupEventHandlers() {
       // General notifications
       this.connection.on("ReceiveNotification", (notification) => {
         this.handleNotification(notification);
       });

       // Webhook notifications
       this.connection.on("ReceiveWebhookNotification", (notification) => {
         this.handleWebhookNotification(notification);
       });

       // Pull request notifications
       this.connection.on("ReceivePullRequestNotification", (notification) => {
         this.handlePullRequestNotification(notification);
       });

       // Commit notifications
       this.connection.on("ReceiveCommitNotification", (notification) => {
         this.handleCommitNotification(notification);
       });
     }

     async connect() {
       try {
         await this.connection.start();
         console.log("SignalR Connected");
         
         // Subscribe to repository notifications
         await this.subscribeToRepository("your-repo-id");
         
         // Subscribe to webhook events
         await this.subscribeToWebhookEvents(["push", "pull_request"]);
       } catch (err) {
         console.error("SignalR Connection Error: ", err);
       }
     }

     async subscribeToRepository(repositoryId: string) {
       await this.connection.invoke("SubscribeToRepository", repositoryId);
     }

     async subscribeToWebhookEvents(eventTypes: string[]) {
       await this.connection.invoke("SubscribeToWebhookEvents", eventTypes);
     }

     private getAuthToken(): string {
       return localStorage.getItem("jwt-token") || "";
     }

     private handleNotification(notification: any) {
       // Display notification in UI
       console.log("Notification received:", notification);
     }

     private handleWebhookNotification(notification: any) {
       // Handle webhook-specific notifications
       console.log("Webhook notification:", notification);
     }

     private handlePullRequestNotification(notification: any) {
       // Handle pull request notifications
       console.log("PR notification:", notification);
     }

     private handleCommitNotification(notification: any) {
       // Handle commit notifications
       console.log("Commit notification:", notification);
     }
   }
   ```

3. **React Hook Example**:
   ```typescript
   import { useEffect, useState } from 'react';
   import * as signalR from '@microsoft/signalr';

   export const useNotifications = () => {
     const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
     const [notifications, setNotifications] = useState<any[]>([]);

     useEffect(() => {
       const newConnection = new signalR.HubConnectionBuilder()
         .withUrl('/notificationHub', {
           accessTokenFactory: () => localStorage.getItem('jwt-token') || ''
         })
         .withAutomaticReconnect()
         .build();

       setConnection(newConnection);

       return () => {
         newConnection.stop();
       };
     }, []);

     useEffect(() => {
       if (connection) {
         connection.start()
           .then(() => {
             console.log('Connected to SignalR');
             
             connection.on('ReceiveNotification', (notification) => {
               setNotifications(prev => [notification, ...prev]);
             });
           })
           .catch(error => console.error('SignalR Connection Error:', error));
       }
     }, [connection]);

     return { connection, notifications };
   };
   ```

## 🔧 Configuration Options

### Azure SignalR Service Tiers

| Tier | Concurrent Connections | Message Rate | Price Range |
|------|----------------------|--------------|-------------|
| Free | 20 | 20K messages/day | Free |
| Standard | 1,000 | 1M messages/day | ~$50/month |
| Premium | 50,000 | 50M messages/day | ~$500/month |

### Performance Tuning

1. **Connection Limits**:
   ```csharp
   builder.Services.AddSignalR(options =>
   {
       options.EnableDetailedErrors = false; // Disable in production
       options.KeepAliveInterval = TimeSpan.FromSeconds(15);
       options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
       options.StreamBufferCapacity = 10;
   });
   ```

2. **Message Size Limits**:
   ```csharp
   builder.Services.Configure<HubOptions>(options =>
   {
       options.MaximumReceiveMessageSize = 64 * 1024; // 64KB
   });
   ```

## 📊 Monitoring & Diagnostics

### Azure Monitor Integration

1. **Enable Application Insights**:
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

2. **SignalR Metrics**:
   - Connection count
   - Message count
   - Connection duration
   - Error rate

### Logging Configuration

```csharp
builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddApplicationInsights();
});
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddSignalRHub("/notificationHub");

app.MapHealthChecks("/health");
```

## 🚨 Troubleshooting

### Common Issues

1. **CORS Errors**:
   - Ensure `AllowCredentials()` is set
   - Verify frontend domain in CORS policy
   - Check protocol (HTTP vs HTTPS)

2. **Authentication Failures**:
   - Verify JWT token is valid
   - Check token expiration
   - Ensure `accessTokenFactory` returns valid token

3. **Connection Timeouts**:
   - Adjust `KeepAliveInterval` and `ClientTimeoutInterval`
   - Check network connectivity
   - Verify Azure SignalR service status

4. **Message Delivery Issues**:
   - Check user group subscriptions
   - Verify notification targeting logic
   - Monitor Azure SignalR metrics

### Debug Mode

Enable detailed errors for development:
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
```

## 💰 Cost Optimization

### Development Environment
- Use local SignalR (comment out `.AddAzureSignalR()`)
- Free tier for testing with limited connections

### Production Environment
- Monitor connection count and message volume
- Implement connection pooling
- Use message batching for high-frequency notifications
- Consider serverless mode for sporadic usage

## 🔐 Security Best Practices

1. **Use HTTPS**: Always use HTTPS in production
2. **JWT Validation**: Implement proper JWT validation
3. **Rate Limiting**: Implement rate limiting for webhook endpoints
4. **Access Keys**: Rotate Azure SignalR access keys regularly
5. **Network Security**: Use VNet integration for enhanced security

## 📈 Scaling Considerations

### Horizontal Scaling
- Azure SignalR automatically handles scaling
- Use sticky sessions if using local SignalR
- Consider multiple SignalR instances with load balancer

### Message Broadcasting Optimization
- Use specific groups instead of broadcasting to all
- Implement message filtering on client side
- Cache notification preferences

## 🔄 Future Enhancements

1. **Notification Persistence**: Add database storage for notifications
2. **Email Integration**: Send email notifications for critical events
3. **Mobile Push Notifications**: Integrate with mobile push services
4. **Notification Templates**: Customizable notification templates
5. **Analytics**: Track notification engagement and effectiveness

---

## Quick Start Checklist

- [ ] Create Azure SignalR Service
- [ ] Configure connection string in appsettings
- [ ] Update CORS policy with frontend domain
- [ ] Test local SignalR functionality
- [ ] Deploy to Azure with SignalR service
- [ ] Implement frontend SignalR client
- [ ] Configure webhook endpoints
- [ ] Test end-to-end notification flow
- [ ] Set up monitoring and alerts
- [ ] Document notification types for frontend team

## Support

For issues related to:
- **Azure SignalR Service**: Check Azure Service Health and support documentation
- **Implementation Issues**: Review logs and enable detailed errors in development
- **Performance**: Monitor Azure SignalR metrics and adjust configuration as needed