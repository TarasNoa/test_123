// gRPC клиент отключён: папка generated/ отсутствует (stubs не сгенерированы из .proto).
// Вместо этого используем REST API через gateway.
// TODO: когда появятся .proto файлы — сгенерировать stubs и подключить настоящий gRPC-web клиент.

import { config } from "./config";

export interface ExecutionResult {
  stdout: string;
  stderr: string;
  exitCode: number;
  terminationReason: string;
}

export class GrpcSandboxClient {
  private baseUrl: string;

  constructor(baseUrl: string = config.apiBaseUrl) {
    this.baseUrl = baseUrl;
  }

  async executeCode(request: {
    taskId: string;
    code: string;
    language: string;
    memoryLimitMb: number;
    timeoutSeconds: number;
  }): Promise<ExecutionResult> {
    const token = localStorage.getItem("access_token");
    const response = await fetch(`${this.baseUrl}/api/ide/agent-states/run`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({
        code: request.code,
        language: request.language,
      }),
    });

    if (!response.ok) {
      throw new Error(`Execution failed: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }
}

export const grpcClient = new GrpcSandboxClient();
