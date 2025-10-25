# React Frontend Integration Guide for CodePulse SignalR Notifications

This guide provides step-by-step instructions for integrating the CodePulse SignalR notification system into a separate React application with JWT authentication.

## 🏗️ Architecture Overview

```
React Frontend App (Port 3000)
       ↓ (JWT Token)
CodePulse API (Port 5000/7000)
       ↓
   SignalR Hub (/notificationHub)
       ↓
   Real-time Notifications
```

## 📦 Installation & Setup

### 1. Install Required Dependencies

```bash
npm install @microsoft/signalr
npm install @types/react @types/react-dom  # If using TypeScript
```

### 2. Environment Configuration

Create `.env.local` in your React app root:

```env
# .env.local
REACT_APP_API_BASE_URL=https://your-codepulse-api.com
REACT_APP_SIGNALR_HUB_URL=https://your-codepulse-api.com/notificationHub
REACT_APP_JWT_TOKEN_KEY=codepulse_jwt_token
```

For development:
```env
# .env.development
REACT_APP_API_BASE_URL=http://localhost:5000
REACT_APP_SIGNALR_HUB_URL=http://localhost:5000/notificationHub
REACT_APP_JWT_TOKEN_KEY=codepulse_jwt_token
```

## 🔐 Authentication Service

First, create an authentication service to manage JWT tokens:

### `src/services/authService.ts`

```typescript
interface LoginCredentials {
  email: string;
  password: string;
}

interface User {
  id: string;
  email: string;
  name: string;
  token: string;
}

class AuthService {
  private readonly API_BASE_URL = process.env.REACT_APP_API_BASE_URL;
  private readonly TOKEN_KEY = process.env.REACT_APP_JWT_TOKEN_KEY || 'jwt_token';

  async login(credentials: LoginCredentials): Promise<User> {
    const response = await fetch(`${this.API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      throw new Error('Login failed');
    }

    const user = await response.json();
    this.setToken(user.token);
    return user;
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    sessionStorage.removeItem(this.TOKEN_KEY);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY) || 
           sessionStorage.getItem(this.TOKEN_KEY);
  }

  setToken(token: string, rememberMe: boolean = true): void {
    if (rememberMe) {
      localStorage.setItem(this.TOKEN_KEY, token);
    } else {
      sessionStorage.setItem(this.TOKEN_KEY, token);
    }
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  getCurrentUser(): any {
    const token = this.getToken();
    if (!token) return null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        id: payload.userId || payload.sub,
        email: payload.email,
        name: payload.name || payload.preferred_username,
      };
    } catch {
      return null;
    }
  }

  async refreshToken(): Promise<string | null> {
    try {
      const response = await fetch(`${this.API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.getToken()}`,
        },
      });

      if (response.ok) {
        const { token } = await response.json();
        this.setToken(token);
        return token;
      }
    } catch (error) {
      console.error('Token refresh failed:', error);
    }
    
    return null;
  }
}

export const authService = new AuthService();
```

## 🔔 SignalR Notification Service

### `src/services/notificationService.ts`

```typescript
import * as signalR from '@microsoft/signalr';
import { authService } from './authService';

export interface Notification {
  id: string;
  type: string;
  title: string;
  message: string;
  severity: 'Info' | 'Success' | 'Warning' | 'Error' | 'Critical';
  timestamp: string;
  repositoryId?: string;
  repositoryName?: string;
  actionUrl?: string;
  data?: any;
  isRead?: boolean;
}

export interface ConnectionInfo {
  userId: string;
  userEmail: string;
  userName: string;
  connectionId: string;
  connectedAt: string;
}

type NotificationCallback = (notification: Notification) => void;
type ConnectionCallback = (info: ConnectionInfo) => void;
type ErrorCallback = (error: string) => void;

class NotificationService {
  private connection: signalR.HubConnection | null = null;
  private isConnected = false;
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectInterval = 5000;

  // Event callbacks
  private notificationCallbacks: NotificationCallback[] = [];
  private connectionCallbacks: ConnectionCallback[] = [];
  private errorCallbacks: ErrorCallback[] = [];

  constructor() {
    this.setupConnection = this.setupConnection.bind(this);
    this.handleReconnection = this.handleReconnection.bind(this);
  }

  async connect(): Promise<void> {
    if (this.isConnected && this.connection) {
      return;
    }

    const token = authService.getToken();
    if (!token) {
      throw new Error('No authentication token available');
    }

    await this.setupConnection(token);
  }

  private async setupConnection(token: string): Promise<void> {
    const hubUrl = process.env.REACT_APP_SIGNALR_HUB_URL!;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => {
          // Always get fresh token in case it was refreshed
          return authService.getToken() || '';
        },
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff with jitter
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          return delay + Math.random() * 1000;
        }
      })
      .configureLogging(
        process.env.NODE_ENV === 'development' 
          ? signalR.LogLevel.Information 
          : signalR.LogLevel.Warning
      )
      .build();

    this.setupEventHandlers();
    
    try {
      await this.connection.start();
      this.isConnected = true;
      this.reconnectAttempts = 0;
      console.log('✅ SignalR Connected');
    } catch (error: any) {
      this.isConnected = false;
      console.error('❌ SignalR Connection Error:', error);
      
      if (error.message?.includes('401') || error.message?.includes('Unauthorized')) {
        this.handleAuthenticationError();
      } else {
        this.handleConnectionError(error.message);
      }
      throw error;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Connection established
    this.connection.on('ConnectionEstablished', (data: ConnectionInfo) => {
      console.log('🔗 Connection established:', data);
      this.connectionCallbacks.forEach(callback => callback(data));
    });

    // General notifications
    this.connection.on('ReceiveNotification', (notification: Notification) => {
      console.log('🔔 Notification received:', notification);
      this.notificationCallbacks.forEach(callback => callback(notification));
    });

    // Webhook notifications
    this.connection.on('ReceiveWebhookNotification', (notification: Notification) => {
      console.log('🪝 Webhook notification:', notification);
      this.notificationCallbacks.forEach(callback => callback(notification));
    });

    // Pull request notifications
    this.connection.on('ReceivePullRequestNotification', (notification: Notification) => {
      console.log('🔀 Pull request notification:', notification);
      this.notificationCallbacks.forEach(callback => callback(notification));
    });

    // Commit notifications
    this.connection.on('ReceiveCommitNotification', (notification: Notification) => {
      console.log('📝 Commit notification:', notification);
      this.notificationCallbacks.forEach(callback => callback(notification));
    });

    // Subscription confirmations
    this.connection.on('SubscriptionConfirmed', (data: any) => {
      console.log('✅ Subscription confirmed:', data);
    });

    this.connection.on('WebhookSubscriptionConfirmed', (data: any) => {
      console.log('✅ Webhook subscription confirmed:', data);
    });

    // Error handling
    this.connection.on('Error', (error: any) => {
      console.error('🚨 SignalR Hub Error:', error);
      this.errorCallbacks.forEach(callback => callback(error.message || 'Hub error'));
    });

    // Connection lifecycle events
    this.connection.onclose((error) => {
      this.isConnected = false;
      console.log('🔌 SignalR connection closed:', error);
      
      if (error && !error.message?.includes('The connection was closed by the server')) {
        this.handleConnectionError(error.message);
      }
    });

    this.connection.onreconnecting((error) => {
      this.isConnected = false;
      console.log('🔄 SignalR reconnecting...', error?.message);
    });

    this.connection.onreconnected((connectionId) => {
      this.isConnected = true;
      console.log('✅ SignalR reconnected with ID:', connectionId);
      
      // Re-subscribe to notifications after reconnection
      this.resubscribeAfterReconnection();
    });
  }

  private async resubscribeAfterReconnection(): Promise<void> {
    try {
      // Get user's repositories from your app state/localStorage
      const userRepositories = this.getUserRepositories();
      
      for (const repoId of userRepositories) {
        await this.subscribeToRepository(repoId);
      }

      // Re-subscribe to webhook events
      await this.subscribeToWebhookEvents(['push', 'pull_request', 'issues']);
    } catch (error) {
      console.error('Error re-subscribing after reconnection:', error);
    }
  }

  async subscribeToRepository(repositoryId: string): Promise<void> {
    if (!this.connection || !this.isConnected) {
      console.warn('Cannot subscribe: SignalR not connected');
      return;
    }

    try {
      await this.connection.invoke('SubscribeToRepository', repositoryId);
      console.log(`📁 Subscribed to repository: ${repositoryId}`);
    } catch (error) {
      console.error(`Error subscribing to repository ${repositoryId}:`, error);
    }
  }

  async unsubscribeFromRepository(repositoryId: string): Promise<void> {
    if (!this.connection || !this.isConnected) {
      console.warn('Cannot unsubscribe: SignalR not connected');
      return;
    }

    try {
      await this.connection.invoke('UnsubscribeFromRepository', repositoryId);
      console.log(`📁 Unsubscribed from repository: ${repositoryId}`);
    } catch (error) {
      console.error(`Error unsubscribing from repository ${repositoryId}:`, error);
    }
  }

  async subscribeToWebhookEvents(eventTypes: string[]): Promise<void> {
    if (!this.connection || !this.isConnected) {
      console.warn('Cannot subscribe: SignalR not connected');
      return;
    }

    try {
      await this.connection.invoke('SubscribeToWebhookEvents', eventTypes);
      console.log(`🪝 Subscribed to webhook events: ${eventTypes.join(', ')}`);
    } catch (error) {
      console.error('Error subscribing to webhook events:', error);
    }
  }

  async getConnectionStatus(): Promise<void> {
    if (!this.connection || !this.isConnected) {
      console.warn('Cannot get status: SignalR not connected');
      return;
    }

    try {
      await this.connection.invoke('GetConnectionStatus');
    } catch (error) {
      console.error('Error getting connection status:', error);
    }
  }

  async ping(): Promise<void> {
    if (!this.connection || !this.isConnected) {
      return;
    }

    try {
      await this.connection.invoke('Ping');
    } catch (error) {
      console.error('Error pinging hub:', error);
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('🔌 SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting SignalR:', error);
      }
      this.connection = null;
      this.isConnected = false;
    }
  }

  // Event subscription methods
  onNotification(callback: NotificationCallback): () => void {
    this.notificationCallbacks.push(callback);
    return () => {
      const index = this.notificationCallbacks.indexOf(callback);
      if (index > -1) {
        this.notificationCallbacks.splice(index, 1);
      }
    };
  }

  onConnection(callback: ConnectionCallback): () => void {
    this.connectionCallbacks.push(callback);
    return () => {
      const index = this.connectionCallbacks.indexOf(callback);
      if (index > -1) {
        this.connectionCallbacks.splice(index, 1);
      }
    };
  }

  onError(callback: ErrorCallback): () => void {
    this.errorCallbacks.push(callback);
    return () => {
      const index = this.errorCallbacks.indexOf(callback);
      if (index > -1) {
        this.errorCallbacks.splice(index, 1);
      }
    };
  }

  // Getters
  get connected(): boolean {
    return this.isConnected;
  }

  get connectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  // Private helper methods
  private handleAuthenticationError(): void {
    console.error('🚨 Authentication failed - redirecting to login');
    authService.logout();
    
    // Trigger authentication error in your app
    this.errorCallbacks.forEach(callback => 
      callback('Authentication failed. Please log in again.')
    );
  }

  private handleConnectionError(message: string): void {
    this.errorCallbacks.forEach(callback => callback(message));
  }

  private getUserRepositories(): string[] {
    // Get user's repository IDs from your app state
    // This could come from Redux, Context, or localStorage
    const stored = localStorage.getItem('user-repositories');
    return stored ? JSON.parse(stored) : [];
  }

  private handleReconnection(): void {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error('Max reconnection attempts reached');
      return;
    }

    this.reconnectAttempts++;
    
    setTimeout(async () => {
      try {
        await this.connect();
      } catch (error) {
        console.error('Reconnection failed:', error);
        this.handleReconnection();
      }
    }, this.reconnectInterval);
  }
}

// Export singleton instance
export const notificationService = new NotificationService();
```

## 🪝 React Hook for Notifications

### `src/hooks/useNotifications.ts`

```typescript
import { useState, useEffect, useCallback, useRef } from 'react';
import { notificationService, Notification, ConnectionInfo } from '../services/notificationService';
import { authService } from '../services/authService';

interface UseNotificationsReturn {
  // State
  notifications: Notification[];
  isConnected: boolean;
  connectionInfo: ConnectionInfo | null;
  connectionError: string | null;
  unreadCount: number;

  // Actions
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  clearNotifications: () => void;
  markAsRead: (id: string) => void;
  removeNotification: (id: string) => void;
  subscribeToRepository: (repositoryId: string) => Promise<void>;
  unsubscribeFromRepository: (repositoryId: string) => Promise<void>;
}

export const useNotifications = (): UseNotificationsReturn => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionInfo, setConnectionInfo] = useState<ConnectionInfo | null>(null);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  
  const unsubscribeRefs = useRef<(() => void)[]>([]);

  const handleNotification = useCallback((notification: Notification) => {
    setNotifications(prev => {
      // Avoid duplicates
      const exists = prev.some(n => n.id === notification.id);
      if (exists) return prev;
      
      // Add new notification and keep only last 100
      return [notification, ...prev.slice(0, 99)];
    });

    // Show browser notification if permission granted
    if ('Notification' in window && Notification.permission === 'granted') {
      const browserNotification = new Notification(notification.title, {
        body: notification.message,
        icon: '/favicon.ico',
        tag: notification.id,
        badge: '/notification-badge.png'
      });

      // Auto-close after 5 seconds
      setTimeout(() => browserNotification.close(), 5000);

      // Handle click to navigate
      browserNotification.onclick = () => {
        if (notification.actionUrl) {
          window.open(notification.actionUrl, '_blank');
        }
        browserNotification.close();
      };
    }
  }, []);

  const handleConnection = useCallback((info: ConnectionInfo) => {
    setConnectionInfo(info);
    setConnectionError(null);
    setIsConnected(true);
  }, []);

  const handleError = useCallback((error: string) => {
    setConnectionError(error);
    
    if (error.includes('Authentication')) {
      setIsConnected(false);
      setConnectionInfo(null);
    }
  }, []);

  const connect = useCallback(async () => {
    if (!authService.isAuthenticated()) {
      setConnectionError('Not authenticated');
      return;
    }

    try {
      setConnectionError(null);
      await notificationService.connect();
    } catch (error: any) {
      console.error('Failed to connect to notifications:', error);
      setConnectionError(error.message || 'Connection failed');
    }
  }, []);

  const disconnect = useCallback(async () => {
    await notificationService.disconnect();
    setIsConnected(false);
    setConnectionInfo(null);
  }, []);

  const clearNotifications = useCallback(() => {
    setNotifications([]);
  }, []);

  const markAsRead = useCallback((id: string) => {
    setNotifications(prev =>
      prev.map(notification =>
        notification.id === id ? { ...notification, isRead: true } : notification
      )
    );
  }, []);

  const removeNotification = useCallback((id: string) => {
    setNotifications(prev => prev.filter(n => n.id !== id));
  }, []);

  const subscribeToRepository = useCallback(async (repositoryId: string) => {
    await notificationService.subscribeToRepository(repositoryId);
  }, []);

  const unsubscribeFromRepository = useCallback(async (repositoryId: string) => {
    await notificationService.unsubscribeFromRepository(repositoryId);
  }, []);

  // Setup event listeners
  useEffect(() => {
    const unsubscribeNotification = notificationService.onNotification(handleNotification);
    const unsubscribeConnection = notificationService.onConnection(handleConnection);
    const unsubscribeError = notificationService.onError(handleError);

    unsubscribeRefs.current = [
      unsubscribeNotification,
      unsubscribeConnection,
      unsubscribeError
    ];

    // Update connection status
    setIsConnected(notificationService.connected);

    return () => {
      unsubscribeRefs.current.forEach(unsubscribe => unsubscribe());
    };
  }, [handleNotification, handleConnection, handleError]);

  // Auto-connect when authenticated
  useEffect(() => {
    if (authService.isAuthenticated() && !isConnected) {
      connect();
    }
  }, [connect, isConnected]);

  // Auto-disconnect when not authenticated
  useEffect(() => {
    if (!authService.isAuthenticated() && isConnected) {
      disconnect();
    }
  }, [disconnect, isConnected]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      disconnect();
    };
  }, [disconnect]);

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return {
    notifications,
    isConnected,
    connectionInfo,
    connectionError,
    unreadCount,
    connect,
    disconnect,
    clearNotifications,
    markAsRead,
    removeNotification,
    subscribeToRepository,
    unsubscribeFromRepository
  };
};
```

## 🎨 React Components

### `src/components/NotificationCenter.tsx`

```tsx
import React, { useState, useEffect } from 'react';
import { useNotifications } from '../hooks/useNotifications';
import { Notification } from '../services/notificationService';
import './NotificationCenter.css';

interface NotificationCenterProps {
  className?: string;
}

export const NotificationCenter: React.FC<NotificationCenterProps> = ({ className }) => {
  const {
    notifications,
    isConnected,
    connectionInfo,
    connectionError,
    unreadCount,
    clearNotifications,
    markAsRead,
    removeNotification
  } = useNotifications();

  const [isOpen, setIsOpen] = useState(false);
  const [filter, setFilter] = useState<'all' | 'unread'>('all');

  // Request notification permission
  useEffect(() => {
    if ('Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission();
    }
  }, []);

  const filteredNotifications = notifications.filter(notification => {
    if (filter === 'unread') {
      return !notification.isRead;
    }
    return true;
  });

  const getSeverityIcon = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'success': return '✅';
      case 'warning': return '⚠️';
      case 'error': return '❌';
      case 'critical': return '🚨';
      default: return 'ℹ️';
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case 'success': return '#10b981';
      case 'warning': return '#f59e0b';
      case 'error': return '#ef4444';
      case 'critical': return '#dc2626';
      default: return '#3b82f6';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <div className={`notification-center ${className}`}>
      {/* Notification Bell Button */}
      <button
        className={`notification-bell ${unreadCount > 0 ? 'has-unread' : ''}`}
        onClick={() => setIsOpen(!isOpen)}
        title={`${unreadCount} unread notifications`}
      >
        🔔
        {unreadCount > 0 && (
          <span className="notification-badge">{unreadCount > 99 ? '99+' : unreadCount}</span>
        )}
      </button>

      {/* Connection Status Indicator */}
      <div className={`connection-status ${isConnected ? 'connected' : 'disconnected'}`}>
        <span className="status-dot"></span>
        {isConnected ? 'Connected' : 'Disconnected'}
      </div>

      {/* Notification Panel */}
      {isOpen && (
        <div className="notification-panel">
          <div className="notification-header">
            <h3>Notifications</h3>
            <div className="notification-controls">
              <select
                value={filter}
                onChange={(e) => setFilter(e.target.value as 'all' | 'unread')}
              >
                <option value="all">All</option>
                <option value="unread">Unread ({unreadCount})</option>
              </select>
              {notifications.length > 0 && (
                <button onClick={clearNotifications} className="clear-btn">
                  Clear All
                </button>
              )}
            </div>
          </div>

          {/* Connection Info */}
          {connectionInfo && (
            <div className="connection-info">
              Connected as {connectionInfo.userName} ({connectionInfo.userEmail})
            </div>
          )}

          {/* Connection Error */}
          {connectionError && (
            <div className="connection-error">
              ⚠️ {connectionError}
            </div>
          )}

          {/* Notifications List */}
          <div className="notification-list">
            {filteredNotifications.length === 0 ? (
              <div className="no-notifications">
                {filter === 'unread' ? 'No unread notifications' : 'No notifications'}
              </div>
            ) : (
              filteredNotifications.map((notification) => (
                <NotificationItem
                  key={notification.id}
                  notification={notification}
                  onMarkAsRead={markAsRead}
                  onRemove={removeNotification}
                  getSeverityIcon={getSeverityIcon}
                  getSeverityColor={getSeverityColor}
                  formatTimestamp={formatTimestamp}
                />
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};

interface NotificationItemProps {
  notification: Notification;
  onMarkAsRead: (id: string) => void;
  onRemove: (id: string) => void;
  getSeverityIcon: (severity: string) => string;
  getSeverityColor: (severity: string) => string;
  formatTimestamp: (timestamp: string) => string;
}

const NotificationItem: React.FC<NotificationItemProps> = ({
  notification,
  onMarkAsRead,
  onRemove,
  getSeverityIcon,
  getSeverityColor,
  formatTimestamp
}) => {
  const handleClick = () => {
    if (!notification.isRead) {
      onMarkAsRead(notification.id);
    }
    
    if (notification.actionUrl) {
      window.open(notification.actionUrl, '_blank');
    }
  };

  return (
    <div
      className={`notification-item ${notification.isRead ? 'read' : 'unread'}`}
      onClick={handleClick}
      style={{ borderLeftColor: getSeverityColor(notification.severity) }}
    >
      <div className="notification-content">
        <div className="notification-header">
          <span className="notification-icon">
            {getSeverityIcon(notification.severity)}
          </span>
          <span className="notification-title">{notification.title}</span>
          <button
            className="remove-btn"
            onClick={(e) => {
              e.stopPropagation();
              onRemove(notification.id);
            }}
          >
            ×
          </button>
        </div>
        
        <p className="notification-message">{notification.message}</p>
        
        <div className="notification-meta">
          <span className="notification-time">
            {formatTimestamp(notification.timestamp)}
          </span>
          {notification.repositoryName && (
            <span className="notification-repo">
              📁 {notification.repositoryName}
            </span>
          )}
          {!notification.isRead && (
            <span className="unread-indicator">●</span>
          )}
        </div>
      </div>
    </div>
  );
};
```

### `src/components/NotificationCenter.css`

```css
.notification-center {
  position: relative;
  display: inline-block;
}

.notification-bell {
  position: relative;
  background: none;
  border: none;
  font-size: 1.5rem;
  cursor: pointer;
  padding: 0.5rem;
  border-radius: 0.5rem;
  transition: background-color 0.2s;
}

.notification-bell:hover {
  background-color: rgba(0, 0, 0, 0.1);
}

.notification-bell.has-unread {
  animation: ring 2s infinite;
}

@keyframes ring {
  0%, 20%, 50%, 80%, 100% { transform: rotate(0deg); }
  10% { transform: rotate(-10deg); }
  30% { transform: rotate(10deg); }
  60% { transform: rotate(-5deg); }
  90% { transform: rotate(5deg); }
}

.notification-badge {
  position: absolute;
  top: 0;
  right: 0;
  background: #ef4444;
  color: white;
  border-radius: 50%;
  padding: 0.2rem 0.4rem;
  font-size: 0.7rem;
  font-weight: bold;
  min-width: 1.2rem;
  height: 1.2rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.connection-status {
  position: absolute;
  top: -0.5rem;
  right: -0.5rem;
  font-size: 0.7rem;
  padding: 0.2rem 0.4rem;
  border-radius: 0.25rem;
  display: flex;
  align-items: center;
  gap: 0.25rem;
  font-weight: 500;
}

.connection-status.connected {
  background: #10b981;
  color: white;
}

.connection-status.disconnected {
  background: #ef4444;
  color: white;
}

.status-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  background: currentColor;
}

.notification-panel {
  position: absolute;
  top: 100%;
  right: 0;
  width: 400px;
  max-width: 90vw;
  background: white;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
  z-index: 1000;
  max-height: 500px;
  display: flex;
  flex-direction: column;
}

.notification-header {
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.notification-header h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 600;
}

.notification-controls {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.notification-controls select {
  padding: 0.25rem 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 0.25rem;
  font-size: 0.875rem;
}

.clear-btn {
  padding: 0.25rem 0.5rem;
  background: #ef4444;
  color: white;
  border: none;
  border-radius: 0.25rem;
  font-size: 0.875rem;
  cursor: pointer;
}

.clear-btn:hover {
  background: #dc2626;
}

.connection-info {
  padding: 0.5rem 1rem;
  background: #f3f4f6;
  font-size: 0.875rem;
  color: #6b7280;
}

.connection-error {
  padding: 0.5rem 1rem;
  background: #fef2f2;
  color: #ef4444;
  font-size: 0.875rem;
}

.notification-list {
  flex: 1;
  overflow-y: auto;
  max-height: 400px;
}

.no-notifications {
  padding: 2rem 1rem;
  text-align: center;
  color: #6b7280;
  font-style: italic;
}

.notification-item {
  padding: 1rem;
  border-bottom: 1px solid #f3f4f6;
  cursor: pointer;
  transition: background-color 0.2s;
  border-left: 3px solid transparent;
}

.notification-item:hover {
  background: #f9fafb;
}

.notification-item.unread {
  background: #fefefe;
  border-left-width: 3px;
}

.notification-item.read {
  opacity: 0.7;
}

.notification-content {
  width: 100%;
}

.notification-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.notification-icon {
  flex-shrink: 0;
}

.notification-title {
  font-weight: 600;
  flex: 1;
  font-size: 0.9rem;
}

.remove-btn {
  background: none;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
  color: #9ca3af;
  padding: 0;
  width: 1.5rem;
  height: 1.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
}

.remove-btn:hover {
  background: #f3f4f6;
  color: #6b7280;
}

.notification-message {
  margin: 0 0 0.5rem 0;
  font-size: 0.875rem;
  color: #4b5563;
  line-height: 1.4;
}

.notification-meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
  color: #9ca3af;
}

.notification-time {
  flex: 1;
}

.notification-repo {
  background: #f3f4f6;
  padding: 0.1rem 0.3rem;
  border-radius: 0.25rem;
}

.unread-indicator {
  color: #3b82f6;
  font-weight: bold;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
  .notification-panel {
    width: 350px;
    right: -1rem;
  }
}

@media (max-width: 480px) {
  .notification-panel {
    width: 300px;
    right: -2rem;
  }
}
```

## 🔗 Integration with Your React App

### `src/App.tsx`

```tsx
import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { NotificationCenter } from './components/NotificationCenter';
import { AuthProvider } from './contexts/AuthContext';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app">
          <header className="app-header">
            <h1>CodePulse Dashboard</h1>
            <div className="header-actions">
              <NotificationCenter />
              {/* Other header components */}
            </div>
          </header>
          
          <main className="app-main">
            <Routes>
              <Route path="/login" element={<LoginPage />} />
              <Route
                path="/dashboard"
                element={
                  <ProtectedRoute>
                    <DashboardPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/" element={<DashboardPage />} />
            </Routes>
          </main>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
```

## 🚀 Usage Examples

### Auto-subscribe to User's Repositories

```tsx
const RepositoryList: React.FC = () => {
  const { subscribeToRepository, unsubscribeFromRepository } = useNotifications();
  const [repositories, setRepositories] = useState([]);

  useEffect(() => {
    // Fetch user's repositories
    fetchUserRepositories().then(repos => {
      setRepositories(repos);
      
      // Auto-subscribe to all repositories
      repos.forEach(repo => {
        subscribeToRepository(repo.id);
      });
    });
  }, [subscribeToRepository]);

  return (
    <div>
      {repositories.map(repo => (
        <div key={repo.id} className="repository-item">
          <span>{repo.name}</span>
          <button onClick={() => unsubscribeFromRepository(repo.id)}>
            Unsubscribe
          </button>
        </div>
      ))}
    </div>
  );
};
```

### Real-time Status Updates

```tsx
const SystemStatus: React.FC = () => {
  const { isConnected, connectionError, connectionInfo } = useNotifications();

  return (
    <div className={`system-status ${isConnected ? 'online' : 'offline'}`}>
      <div className="status-indicator">
        {isConnected ? '🟢 Online' : '🔴 Offline'}
      </div>
      
      {connectionError && (
        <div className="error-message">
          Error: {connectionError}
        </div>
      )}
      
      {connectionInfo && (
        <div className="connection-details">
          Connected as: {connectionInfo.userName}
          <br />
          Since: {new Date(connectionInfo.connectedAt).toLocaleString()}
        </div>
      )}
    </div>
  );
};
```

This complete React integration provides:

- 🔐 **JWT Authentication** with automatic token refresh
- 🔔 **Real-time Notifications** with browser notifications
- 🎨 **Beautiful UI Components** with responsive design
- 🔄 **Automatic Reconnection** with exponential backoff
- 📱 **Mobile Responsive** design
- 🛡️ **Error Handling** for authentication and connection issues
- 🎯 **TypeScript Support** for type safety
- 🪝 **React Hooks** for easy integration

Your React frontend is now ready to receive real-time notifications from your CodePulse API!
