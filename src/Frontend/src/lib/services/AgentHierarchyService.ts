import { apiClient } from '@libr4/shared';

export interface AgentInfo {
  id: string;
  name: string;
  type: string;
  capabilities: {
    supportedTasks: string[];
    supportedLanguages: string[];
    maxConcurrentTasks: number;
    successRate: number;
  };
  childAgents?: AgentInfo[];
}

export interface AgentExecutionTrace {
  agentId: string;
  agentName: string;
  level: number; // 0 = orchestrator, 1 = direct children, etc.
  taskName: string;
  status: 'pending' | 'running' | 'success' | 'failed';
  duration: number;
  result?: string;
  error?: string;
  subExecutions: AgentExecutionTrace[];
}

export class AgentHierarchyService {
  static readonly BASE_URL = '/api/ai/agents';

  static async executeWithHierarchy(
    task: string,
    context: string,
    parameters?: Record<string, any>
  ): Promise<AgentExecutionTrace> {
    return apiClient.post(`${this.BASE_URL}/execute`, {
      task,
      context,
      parameters: parameters || {}
    });
  }

  static async getAgentHierarchy(): Promise<AgentInfo> {
    return apiClient.get(`${this.BASE_URL}/hierarchy`);
  }

  static async getAgentStats(): Promise<Record<string, any>> {
    return apiClient.get(`${this.BASE_URL}/stats`);
  }

  static async getExecutionTrace(executionId: string): Promise<AgentExecutionTrace> {
    return apiClient.get(`${this.BASE_URL}/trace/${executionId}`);
  }
}