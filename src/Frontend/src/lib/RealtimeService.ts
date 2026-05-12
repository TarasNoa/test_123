import * as signalR from "@microsoft/signalr";
import { config } from "./config";
import { agentApi } from "./api-client";

export class RealtimeService {
  private connection: signalR.HubConnection;
  private static instance: RealtimeService;

  private constructor() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${config.wsBaseUrl.replace("ws://", "http://").replace("wss://", "https://")}/hubs/agents`, {
        // accessTokenFactory передаёт JWT в QueryString (?access_token=...)
        // Backend уже настроен читать токен из QueryString для WebSocket
        accessTokenFactory: () => localStorage.getItem("accessToken") ?? "",
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  public static getInstance(): RealtimeService {
    if (!RealtimeService.instance) {
      RealtimeService.instance = new RealtimeService();
    }
    return RealtimeService.instance;
  }

  public async start(agentId: string, onUpdate: (data: unknown) => void): Promise<void> {
    if (!agentId) {
      console.warn("[SignalR] agentId is empty, skipping subscription");
      return;
    }

    try {
      if (this.connection.state === signalR.HubConnectionState.Disconnected) {
        await this.connection.start();
      }

      await this.connection.invoke("SubscribeToAgent", agentId);

      // Снимаем старый обработчик ПЕРЕД добавлением нового
      // Иначе при каждом вызове start() события будут дублироваться
      this.connection.off("OnAgentStateUpdated");
      this.connection.on("OnAgentStateUpdated", (data: { State?: string; Timestamp?: string }) => {
        onUpdate(data);
        // Пушим событие в глобальный лог для AgentEventList
        agentApi.pushEvent({
          status: data.State ?? "UNKNOWN",
          timestamp: data.Timestamp ?? new Date().toISOString(),
          data: JSON.stringify(data),
        });
      });

      console.log(`[SignalR] Subscribed to agent: ${agentId}`);
    } catch (err) {
      console.error("[SignalR] Connection failed:", err);
      throw err;
    }
  }

  public async stop(agentId: string): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke("UnsubscribeFromAgent", agentId);
      } catch {
        // игнорируем ошибку при отписке
      }
      this.connection.off("OnAgentStateUpdated");
    }
  }

  public getState(): signalR.HubConnectionState {
    return this.connection.state;
  }
}
