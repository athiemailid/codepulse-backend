# Frontend SignalR Client with JWT Authentication

This guide provides complete examples for connecting to the CodePulse SignalR hub using JWT authentication across different frontend frameworks.

## 🔐 Authentication Requirements

The SignalR hub requires a valid JWT token to establish a connection. The token can be provided in two ways:

1. **Query Parameter**: `?access_token=your-jwt-token`
2. **Authorization Header**: For initial HTTP negotiation

## 📦 Installation

```bash
npm install @microsoft/signalr
```

## 🚀 JavaScript/TypeScript Examples

### Basic JavaScript Connection

```javascript
import * as signalR from "@microsoft/signalr";

class NotificationService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
    }

    async connect() {
        try {
            // Get JWT token from your auth system
            const token = this.getAuthToken();
            
            if (!token) {
                throw new Error("No authentication token available");
            }

            // Create connection with JWT token
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub", {
                    accessTokenFactory: () => token,
                    // Alternative: pass token as query parameter
                    // skipNegotiation: true,
                    // transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // Custom retry logic
                        if (retryContext.previousRetryCount === 0) {
                            return 0;
                        }
                        return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Start connection
            await this.connection.start();
            console.log("SignalR Connected");
            this.isConnected = true;

            // Subscribe to notifications after connection
            await this.subscribeToNotifications();

        } catch (error) {
            console.error("SignalR Connection Error:", error);
            this.isConnected = false;
            
            // Handle specific authentication errors
            if (error.message.includes("401") || error.message.includes("Unauthorized")) {
                console.error("Authentication failed. Please check your JWT token.");
                // Redirect to login or refresh token
                this.handleAuthenticationError();
            }
        }
    }

    setupEventHandlers() {
        // Connection established
        this.connection.on("ConnectionEstablished", (data) => {
            console.log("Connection established:", data);
            this.displayConnectionInfo(data);
        });

        // General notifications
        this.connection.on("ReceiveNotification", (notification) => {
            console.log("Notification received:", notification);
            this.handleNotification(notification);
        });

        // Webhook notifications
        this.connection.on("ReceiveWebhookNotification", (notification) => {
            console.log("Webhook notification:", notification);
            this.handleWebhookNotification(notification);
        });

        // Pull request notifications
        this.connection.on("ReceivePullRequestNotification", (notification) => {
            console.log("Pull request notification:", notification);
            this.handlePullRequestNotification(notification);
        });

        // Commit notifications
        this.connection.on("ReceiveCommitNotification", (notification) => {
            console.log("Commit notification:", notification);
            this.handleCommitNotification(notification);
        });

        // Subscription confirmations
        this.connection.on("SubscriptionConfirmed", (data) => {
            console.log("Subscription confirmed:", data);
        });

        // Error handling
        this.connection.on("Error", (error) => {
            console.error("SignalR Hub Error:", error);
            this.handleHubError(error);
        });

        // Connection events
        this.connection.onclose((error) => {
            console.log("SignalR connection closed:", error);
            this.isConnected = false;
            this.handleConnectionClosed(error);
        });

        this.connection.onreconnecting((error) => {
            console.log("SignalR reconnecting:", error);
            this.isConnected = false;
        });

        this.connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected:", connectionId);
            this.isConnected = true;
            // Re-subscribe to notifications after reconnection
            this.subscribeToNotifications();
        });
    }

    async subscribeToNotifications() {
        try {
            // Subscribe to repository notifications
            const repositoryIds = this.getUserRepositories(); // Get user's repositories
            for (const repoId of repositoryIds) {
                await this.subscribeToRepository(repoId);
            }

            // Subscribe to webhook events
            await this.subscribeToWebhookEvents(["push", "pull_request", "issues"]);

        } catch (error) {
            console.error("Error subscribing to notifications:", error);
        }
    }

    async subscribeToRepository(repositoryId) {
        if (!this.isConnected) {
            console.warn("Cannot subscribe: SignalR not connected");
            return;
        }

        try {
            await this.connection.invoke("SubscribeToRepository", repositoryId);
            console.log(`Subscribed to repository: ${repositoryId}`);
        } catch (error) {
            console.error(`Error subscribing to repository ${repositoryId}:`, error);
        }
    }

    async subscribeToWebhookEvents(eventTypes) {
        if (!this.isConnected) {
            console.warn("Cannot subscribe: SignalR not connected");
            return;
        }

        try {
            await this.connection.invoke("SubscribeToWebhookEvents", eventTypes);
            console.log(`Subscribed to webhook events: ${eventTypes.join(", ")}`);
        } catch (error) {
            console.error("Error subscribing to webhook events:", error);
        }
    }

    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.stop();
                console.log("SignalR disconnected");
            } catch (error) {
                console.error("Error disconnecting SignalR:", error);
            }
            this.isConnected = false;
        }
    }

    getAuthToken() {
        // Get token from localStorage, sessionStorage, or your auth store
        return localStorage.getItem("jwt-token") || 
               sessionStorage.getItem("auth-token") ||
               this.getTokenFromAuthStore();
    }

    getTokenFromAuthStore() {
        // Example for different auth libraries
        // Redux: return store.getState().auth.token;
        // Context: return authContext.token;
        // Custom: return authService.getToken();
        return null;
    }

    getUserRepositories() {
        // Get user's repository IDs from your application state
        return JSON.parse(localStorage.getItem("user-repositories") || "[]");
    }

    handleNotification(notification) {
        // Display notification in your UI
        this.showToast(notification.title, notification.message, notification.severity);
        
        // Update notification badge
        this.updateNotificationBadge();
        
        // Store in notification history
        this.addToNotificationHistory(notification);
    }

    handleWebhookNotification(notification) {
        // Handle webhook-specific notifications
        if (notification.eventType === "push") {
            this.handlePushNotification(notification);
        } else if (notification.eventType === "pull_request") {
            this.handlePullRequestNotification(notification);
        }
    }

    handlePullRequestNotification(notification) {
        // Handle pull request notifications
        this.showToast(
            notification.title,
            notification.message,
            notification.severity,
            notification.actionUrl
        );
    }

    handleCommitNotification(notification) {
        // Handle commit notifications
        this.showToast(
            notification.title,
            `${notification.author} committed to ${notification.branch}`,
            notification.severity,
            notification.actionUrl
        );
    }

    handleAuthenticationError() {
        // Handle authentication errors
        localStorage.removeItem("jwt-token");
        // Redirect to login page or refresh token
        window.location.href = "/login";
    }

    handleConnectionClosed(error) {
        if (error) {
            console.error("Connection closed with error:", error);
            // Show user that connection was lost
            this.showConnectionStatus("disconnected");
        }
    }

    handleHubError(error) {
        console.error("Hub error:", error);
        if (error.message.includes("Authentication")) {
            this.handleAuthenticationError();
        }
    }

    displayConnectionInfo(data) {
        console.log("Connected as:", data.userName, data.userEmail);
        this.showConnectionStatus("connected");
    }

    showToast(title, message, severity, actionUrl = null) {
        // Implementation depends on your UI library
        // Examples: react-hot-toast, vue-toastification, etc.
        console.log(`${severity.toUpperCase()}: ${title} - ${message}`);
    }

    showConnectionStatus(status) {
        // Update UI to show connection status
        const statusElement = document.getElementById("connection-status");
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `status-${status}`;
        }
    }

    updateNotificationBadge() {
        // Update notification badge count
        const badge = document.getElementById("notification-badge");
        if (badge) {
            const count = parseInt(badge.textContent || "0") + 1;
            badge.textContent = count;
            badge.style.display = count > 0 ? "block" : "none";
        }
    }

    addToNotificationHistory(notification) {
        // Store notification in local storage or state management
        const history = JSON.parse(localStorage.getItem("notification-history") || "[]");
        history.unshift({
            ...notification,
            receivedAt: new Date().toISOString()
        });
        
        // Keep only last 100 notifications
        if (history.length > 100) {
            history.splice(100);
        }
        
        localStorage.setItem("notification-history", JSON.stringify(history));
    }
}

// Usage
const notificationService = new NotificationService();

// Connect when user logs in
async function onUserLogin() {
    await notificationService.connect();
}

// Disconnect when user logs out
async function onUserLogout() {
    await notificationService.disconnect();
}

export default NotificationService;
```

### React Hook Implementation

```typescript
import { useState, useEffect, useCallback, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

interface Notification {
    id: string;
    type: string;
    title: string;
    message: string;
    severity: 'Info' | 'Success' | 'Warning' | 'Error';
    timestamp: string;
    repositoryId?: string;
    repositoryName?: string;
    actionUrl?: string;
    data?: any;
}

interface ConnectionInfo {
    userId: string;
    userEmail: string;
    userName: string;
    connectionId: string;
    connectedAt: string;
}

export const useSignalRNotifications = () => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);
    const [notifications, setNotifications] = useState<Notification[]>([]);
    const [connectionInfo, setConnectionInfo] = useState<ConnectionInfo | null>(null);
    const [connectionError, setConnectionError] = useState<string | null>(null);
    
    const reconnectTimeoutRef = useRef<NodeJS.Timeout>();

    const getAuthToken = useCallback(() => {
        // Get token from your auth context/store
        return localStorage.getItem('jwt-token') || '';
    }, []);

    const handleNotification = useCallback((notification: Notification) => {
        setNotifications(prev => [notification, ...prev.slice(0, 99)]); // Keep last 100
        
        // Show browser notification if permission granted
        if (Notification.permission === 'granted') {
            new Notification(notification.title, {
                body: notification.message,
                icon: '/notification-icon.png',
                tag: notification.id
            });
        }
    }, []);

    const setupConnection = useCallback(async () => {
        const token = getAuthToken();
        
        if (!token) {
            setConnectionError('No authentication token available');
            return;
        }

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub', {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Event handlers
        newConnection.on('ConnectionEstablished', (data: ConnectionInfo) => {
            setConnectionInfo(data);
            setConnectionError(null);
        });

        newConnection.on('ReceiveNotification', handleNotification);
        newConnection.on('ReceiveWebhookNotification', handleNotification);
        newConnection.on('ReceivePullRequestNotification', handleNotification);
        newConnection.on('ReceiveCommitNotification', handleNotification);

        newConnection.on('Error', (error: any) => {
            console.error('SignalR Hub Error:', error);
            setConnectionError(error.message || 'Hub error occurred');
        });

        newConnection.onclose((error) => {
            setIsConnected(false);
            if (error) {
                console.error('Connection closed with error:', error);
                setConnectionError(error.message || 'Connection closed unexpectedly');
            }
        });

        newConnection.onreconnecting((error) => {
            setIsConnected(false);
            if (error) {
                console.log('Reconnecting...', error);
            }
        });

        newConnection.onreconnected((connectionId) => {
            setIsConnected(true);
            setConnectionError(null);
            console.log('Reconnected with ID:', connectionId);
        });

        try {
            await newConnection.start();
            setConnection(newConnection);
            setIsConnected(true);
            setConnectionError(null);
        } catch (error: any) {
            console.error('Failed to start SignalR connection:', error);
            setConnectionError(error.message || 'Failed to connect');
            
            if (error.message?.includes('401') || error.message?.includes('Unauthorized')) {
                // Handle authentication error
                localStorage.removeItem('jwt-token');
                // Trigger re-authentication in your app
            }
        }
    }, [getAuthToken, handleNotification]);

    const disconnect = useCallback(async () => {
        if (connection) {
            try {
                await connection.stop();
            } catch (error) {
                console.error('Error stopping connection:', error);
            }
        }
        setConnection(null);
        setIsConnected(false);
        setConnectionInfo(null);
        
        if (reconnectTimeoutRef.current) {
            clearTimeout(reconnectTimeoutRef.current);
        }
    }, [connection]);

    const subscribeToRepository = useCallback(async (repositoryId: string) => {
        if (!connection || !isConnected) {
            console.warn('Cannot subscribe: not connected');
            return;
        }

        try {
            await connection.invoke('SubscribeToRepository', repositoryId);
        } catch (error) {
            console.error('Error subscribing to repository:', error);
        }
    }, [connection, isConnected]);

    const subscribeToWebhookEvents = useCallback(async (eventTypes: string[]) => {
        if (!connection || !isConnected) {
            console.warn('Cannot subscribe: not connected');
            return;
        }

        try {
            await connection.invoke('SubscribeToWebhookEvents', eventTypes);
        } catch (error) {
            console.error('Error subscribing to webhook events:', error);
        }
    }, [connection, isConnected]);

    const clearNotifications = useCallback(() => {
        setNotifications([]);
    }, []);

    const removeNotification = useCallback((id: string) => {
        setNotifications(prev => prev.filter(n => n.id !== id));
    }, []);

    // Auto-connect when token is available
    useEffect(() => {
        const token = getAuthToken();
        if (token && !connection) {
            setupConnection();
        }
    }, [getAuthToken, connection, setupConnection]);

    // Cleanup on unmount
    useEffect(() => {
        return () => {
            disconnect();
        };
    }, [disconnect]);

    return {
        // Connection state
        isConnected,
        connectionInfo,
        connectionError,
        
        // Notifications
        notifications,
        unreadCount: notifications.filter(n => !n.isRead).length,
        
        // Actions
        connect: setupConnection,
        disconnect,
        subscribeToRepository,
        subscribeToWebhookEvents,
        clearNotifications,
        removeNotification,
        
        // Connection object for custom operations
        connection
    };
};

// Usage in React Component
export const NotificationComponent: React.FC = () => {
    const {
        isConnected,
        connectionInfo,
        connectionError,
        notifications,
        unreadCount,
        subscribeToRepository,
        subscribeToWebhookEvents,
        clearNotifications,
        removeNotification
    } = useSignalRNotifications();

    useEffect(() => {
        if (isConnected) {
            // Subscribe to user's repositories
            const userRepos = ['repo1', 'repo2']; // Get from your app state
            userRepos.forEach(repoId => subscribeToRepository(repoId));
            
            // Subscribe to webhook events
            subscribeToWebhookEvents(['push', 'pull_request', 'issues']);
        }
    }, [isConnected, subscribeToRepository, subscribeToWebhookEvents]);

    if (connectionError) {
        return (
            <div className="notification-error">
                <p>Connection Error: {connectionError}</p>
                <button onClick={() => window.location.reload()}>
                    Retry Connection
                </button>
            </div>
        );
    }

    return (
        <div className="notification-container">
            <div className="connection-status">
                <span className={`status-indicator ${isConnected ? 'connected' : 'disconnected'}`}>
                    {isConnected ? '🟢' : '🔴'}
                </span>
                {isConnected ? 'Connected' : 'Disconnected'}
                {connectionInfo && (
                    <span className="user-info">
                        as {connectionInfo.userName}
                    </span>
                )}
            </div>

            <div className="notification-header">
                <h3>Notifications ({unreadCount})</h3>
                {notifications.length > 0 && (
                    <button onClick={clearNotifications}>Clear All</button>
                )}
            </div>

            <div className="notification-list">
                {notifications.map(notification => (
                    <div
                        key={notification.id}
                        className={`notification notification-${notification.severity.toLowerCase()}`}
                    >
                        <div className="notification-header">
                            <strong>{notification.title}</strong>
                            <button onClick={() => removeNotification(notification.id)}>
                                ×
                            </button>
                        </div>
                        <p>{notification.message}</p>
                        <small>{new Date(notification.timestamp).toLocaleString()}</small>
                        {notification.actionUrl && (
                            <a href={notification.actionUrl} className="notification-action">
                                View Details
                            </a>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
};
```

### Vue.js Composition API

```typescript
import { ref, computed, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

export function useSignalRNotifications() {
    const connection = ref<signalR.HubConnection | null>(null);
    const isConnected = ref(false);
    const notifications = ref<any[]>([]);
    const connectionError = ref<string | null>(null);

    const unreadCount = computed(() => 
        notifications.value.filter(n => !n.isRead).length
    );

    const getAuthToken = () => {
        return localStorage.getItem('jwt-token') || '';
    };

    const setupConnection = async () => {
        const token = getAuthToken();
        
        if (!token) {
            connectionError.value = 'No authentication token available';
            return;
        }

        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl('/notificationHub', {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        // Event handlers
        newConnection.on('ReceiveNotification', (notification) => {
            notifications.value.unshift(notification);
            
            // Limit to 100 notifications
            if (notifications.value.length > 100) {
                notifications.value.splice(100);
            }
        });

        newConnection.onclose((error) => {
            isConnected.value = false;
            if (error) {
                connectionError.value = error.message;
            }
        });

        try {
            await newConnection.start();
            connection.value = newConnection;
            isConnected.value = true;
            connectionError.value = null;
        } catch (error: any) {
            connectionError.value = error.message;
        }
    };

    const disconnect = async () => {
        if (connection.value) {
            await connection.value.stop();
            connection.value = null;
            isConnected.value = false;
        }
    };

    onMounted(() => {
        setupConnection();
    });

    onUnmounted(() => {
        disconnect();
    });

    return {
        isConnected: readonly(isConnected),
        notifications: readonly(notifications),
        unreadCount,
        connectionError: readonly(connectionError),
        connect: setupConnection,
        disconnect
    };
}
```

## 🔧 Configuration Examples

### Environment Variables

```env
# .env.local
REACT_APP_API_URL=https://your-api.com
REACT_APP_SIGNALR_URL=https://your-api.com/notificationHub
REACT_APP_JWT_TOKEN_KEY=jwt-token
```

### TypeScript Interfaces

```typescript
// types/signalr.ts
export interface NotificationDto {
    id: string;
    type: string;
    title: string;
    message: string;
    data?: any;
    severity: NotificationSeverity;
    timestamp: string;
    repositoryId?: string;
    repositoryName?: string;
    userId?: string;
    actionUrl?: string;
    isRead: boolean;
    metadata: Record<string, any>;
}

export enum NotificationSeverity {
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

export interface ConnectionInfo {
    userId: string;
    userEmail: string;
    userName: string;
    connectionId: string;
    connectedAt: string;
    isAuthenticated: boolean;
}
```

## 🔍 Troubleshooting

### Common Issues

1. **401 Unauthorized**: 
   - Check JWT token validity
   - Ensure token is not expired
   - Verify token format

2. **Connection Fails**:
   - Check CORS configuration
   - Verify SignalR endpoint URL
   - Check network connectivity

3. **Token Not Sent**:
   - Ensure `accessTokenFactory` returns valid token
   - Check query parameter format
   - Verify authentication middleware

### Debug Mode

```javascript
// Enable detailed SignalR logging
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/notificationHub', {
        accessTokenFactory: () => getAuthToken()
    })
    .configureLogging(signalR.LogLevel.Debug) // Enable debug logging
    .build();
```

This implementation provides a robust, production-ready SignalR client with JWT authentication support for your CodePulse application.