import { apiClient } from '@libr4/shared';

export enum ExecutionStatus {
  Pending = 'Pending',
  Running = 'Running',
  Success = 'Success',
  Failed = 'Failed',
  FixRequired = 'FixRequired',
  Fixed = 'Fixed'
}

export interface ExecutionResult {
  status: ExecutionStatus;
  output: string;
  errorMessage?: string;
  executionTime: string;
  attemptNumber: number;
  createdAt: string;
}

export interface CodeGeneration {
  language: string;
  code: string;
  description?: string;
  generatedAt: string;
}

export interface ExecutionContext {
  id: string;
  currentStatus: ExecutionStatus;
  task: string;
  currentAttempt: number;
  maxRetryAttempts: number;
  startedAt: string;
  completedAt?: string;
  codeGenerations: CodeGeneration[];
  executionResults: ExecutionResult[];
}

export class AgentExecutionService {
  static readonly BASE_URL = '/api/v1/agent/execution';

  static async executeCode(
    code: string,
    language: string,
    task: string,
    agentId?: string,
    workspaceId?: string
  ): Promise<{ id: string; currentStatus: ExecutionStatus; lastError?: string }> {
    return apiClient.post(`${this.BASE_URL}/execute`, {
      code,
      language,
      task,
      agentId,
      workspaceId
    });
  }

  static async getExecutionContext(contextId: string): Promise<ExecutionContext> {
    return apiClient.get(`${this.BASE_URL}/context/${contextId}`);
  }

  static async getExecutionResults(
    contextId: string,
    skip: number = 0,
    take: number = 10
  ): Promise<ExecutionResult[]> {
    return apiClient.get(`${this.BASE_URL}/results/${contextId}?skip=${skip}&take=${take}`);
  }

  static async pollExecutionStatus(
    contextId: string,
    maxAttempts: number = 30,
    delayMs: number = 1000
  ): Promise<ExecutionContext> {
    for (let i = 0; i < maxAttempts; i++) {
      const context = await this.getExecutionContext(contextId);

      if (
        context.currentStatus === ExecutionStatus.Success ||
        context.currentStatus === ExecutionStatus.Failed
      ) {
        return context;
      }

      await new Promise(resolve => setTimeout(resolve, delayMs));
    }

    throw new Error('Execution polling timeout');
  }
}