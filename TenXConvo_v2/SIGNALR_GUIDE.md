# SignalR Chat — Frontend Integration Guide

## Install @microsoft/signalr

```bash
npm install @microsoft/signalr
```

---

## Connect to Hub (after login)

```javascript
import * as signalR from "@microsoft/signalr";

// jwt = accessToken from POST /api/auth/login/step2
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/chat", {
    accessTokenFactory: () => localStorage.getItem("accessToken")
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])  // retry delays
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Start connection
await connection.start();
console.log("Connected to ChatHub ✅");
```

---

## Listen to Events (Frontend receives these)

```javascript
// New message received
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
  // message = {
  //   messageId, conversationId, senderId, senderName,
  //   body, messageType, sentAt, isRead
  // }
  appendMessageToUI(message);
});

// Other user read your messages
connection.on("MessageRead", ({ conversationId, readBy }) => {
  markMessagesAsRead(conversationId);
});

// Consultant online/offline dot indicator on card
connection.on("UserOnline",  (userId) => updateOnlineDot(userId, true));
connection.on("UserOffline", (userId) => updateOnlineDot(userId, false));

// Typing indicator
connection.on("TypingStarted", ({ userId, conversationId }) => showTypingIndicator(conversationId));
connection.on("TypingStopped", ({ userId, conversationId }) => hideTypingIndicator(conversationId));

// Connect ↗ button accepted
connection.on("ConnectionAccepted", (data) => {
  showNotification("Your connection request was accepted!");
  openConversation(data.conversationId);
});

// Consultant gets new connection request
connection.on("NewConnectionRequest", (request) => {
  showNotification(`${request.customerName} wants to connect!`);
  refreshRequestsBadge();
});

// Errors
connection.on("Error", (message) => {
  console.error("Hub error:", message);
});
```

---

## Send Events (Frontend calls these)

```javascript
// 1. Open a chat window → join the room first
await connection.invoke("JoinConversation", conversationId);

// 2. Send a message
await connection.invoke("SendMessage", conversationId, "Hello! How can I help you?");

// 3. Typing indicators
await connection.invoke("StartTyping", conversationId);
await connection.invoke("StopTyping",  conversationId);

// 4. Mark messages as read (when user opens conversation)
await connection.invoke("MarkRead", conversationId);

// 5. Leave room (when closing chat window)
await connection.invoke("LeaveConversation", conversationId);
```

---

## React Hook Example

```javascript
import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

export function useChat(conversationId) {
  const connectionRef = useRef(null);
  const [messages, setMessages] = useState([]);
  const [isTyping, setIsTyping]  = useState(false);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/chat", {
        accessTokenFactory: () => localStorage.getItem("accessToken")
      })
      .withAutomaticReconnect()
      .build();

    conn.on("ReceiveMessage", (msg) => {
      setMessages(prev => [msg, ...prev]);
    });

    conn.on("TypingStarted", () => setIsTyping(true));
    conn.on("TypingStopped", () => setIsTyping(false));

    conn.start()
      .then(() => conn.invoke("JoinConversation", conversationId))
      .then(() => conn.invoke("MarkRead", conversationId));

    connectionRef.current = conn;

    return () => {
      conn.invoke("LeaveConversation", conversationId)
         .then(() => conn.stop());
    };
  }, [conversationId]);

  const sendMessage = (body) =>
    connectionRef.current?.invoke("SendMessage", conversationId, body);

  const startTyping = () =>
    connectionRef.current?.invoke("StartTyping", conversationId);

  const stopTyping = () =>
    connectionRef.current?.invoke("StopTyping", conversationId);

  return { messages, isTyping, sendMessage, startTyping, stopTyping };
}
```

---

## Hub URL Summary

| Environment | URL |
|-------------|-----|
| Local dev   | `ws://localhost:5000/hubs/chat` |
| UAT         | `wss://uat-api.htagsol.com/hubs/chat` |
| Production  | `wss://api.10xdigitalventures.com/hubs/chat` |

---

## Migration Commands (run once to create DB)

```bash
# From TenXConvo.API folder:
cd src/TenXConvo.API

# Apply migrations (creates DB + all tables):
dotnet ef database update --project ../TenXConvo.Infrastructure

# Create a new migration after entity changes:
dotnet ef migrations add AddNewFeature --project ../TenXConvo.Infrastructure --startup-project .

# Rollback to previous migration:
dotnet ef database update PreviousMigrationName --project ../TenXConvo.Infrastructure
```

> In development, `db.Database.EnsureCreated()` in Program.cs handles this automatically on startup.
