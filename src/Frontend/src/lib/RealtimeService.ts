import * as signalR from "@microsoft/signalr";

export class RealtimeService {
    private connection: signalR.HubConnection;
    private static instance: RealtimeService;

    private constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5000/hubs/agents", {
                skipNegotiation: true,
                transport: signalR.HttpTransportType.WebSockets
            })
            .withAutomaticReconnect()
            .build();
    }

    public static getInstance(): RealtimeService {
        if (!RealtimeService.instance) {
            RealtimeService.instance = new RealtimeService();
        }
        return RealtimeService.instance;
    }

    public async start(agentId: string, onUpdate: (data: any) => void) {
        try {
            if (this.connection.state === signalR.HubConnectionState.Disconnected) {
                await this.connection.start();
            }
            
            // Подписываемся на события конкретного агента
            await this.connection.invoke("SubscribeToAgent", agentId);
            
            // Слушаем обновления
            this.connection.on("OnAgentStateUpdated", (data) => {
                onUpdate(data);
            });

            console.log(`[SignalR] Subscribed to agent: ${agentId}`);
        } catch (err) {
            console.error("[SignalR] Connection failed: ", err);
        }
    }

    public async stop(agentId: string) {
        if (this.connection.state === signalR.HubConnectionState.Connected) {
            await this.connection.invoke("UnsubscribeFromAgent", agentId);
            this.connection.off("OnAgentStateUpdated");
        }
    }
}
