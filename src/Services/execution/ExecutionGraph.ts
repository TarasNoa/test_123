/**
 * Execution Graph Engine
 * 
 * Killer feature: Project execution orchestration visualization.
 * Projects displayed as tree structure:
 * Goal
 *  ├── Planning
 *  ├── Frontend
 *  ├── Backend
 *  ├── Infrastructure
 *  └── Deployment
 * 
 * Service layer - business logic separated from components.
 */

export interface GraphNode {
  id: string;
  label: string;
  type: "goal" | "phase" | "task" | "milestone";
  status: "pending" | "in_progress" | "completed" | "blocked" | "error";
  dependencies: string[];
  metadata: {
    estimatedDuration?: number;
    actualDuration?: number;
    assignedAgent?: string;
    priority: "low" | "medium" | "high" | "critical";
    riskLevel: "low" | "medium" | "high";
  };
}

export interface GraphEdge {
  from: string;
  to: string;
  type: "dependency" | "flow" | "blocker";
}

export interface ExecutionGraph {
  projectId: string;
  projectName: string;
  nodes: GraphNode[];
  edges: GraphEdge[];
  metadata: {
    createdAt: Date;
    updatedAt: Date;
    totalEstimatedDuration: number;
    totalActualDuration: number;
    completion: number;
  };
}

/**
 * Default execution graph template
 */
export function createDefaultExecutionGraph(projectId: string, projectName: string): ExecutionGraph {
  const goalId = `${projectId}:goal`;
  const planningId = `${projectId}:planning`;
  const frontendId = `${projectId}:frontend`;
  const backendId = `${projectId}:backend`;
  const infraId = `${projectId}:infrastructure`;
  const deploymentId = `${projectId}:deployment`;

  const nodes: GraphNode[] = [
    {
      id: goalId,
      label: "Project Goal",
      type: "goal",
      status: "pending",
      dependencies: [],
      metadata: {
        priority: "high",
        riskLevel: "low",
      },
    },
    {
      id: planningId,
      label: "Planning",
      type: "phase",
      status: "pending",
      dependencies: [goalId],
      metadata: {
        estimatedDuration: 7,
        priority: "high",
        riskLevel: "low",
      },
    },
    {
      id: frontendId,
      label: "Frontend",
      type: "phase",
      status: "pending",
      dependencies: [planningId],
      metadata: {
        estimatedDuration: 14,
        priority: "high",
        riskLevel: "medium",
      },
    },
    {
      id: backendId,
      label: "Backend",
      type: "phase",
      status: "pending",
      dependencies: [planningId],
      metadata: {
        estimatedDuration: 14,
        priority: "high",
        riskLevel: "medium",
      },
    },
    {
      id: infraId,
      label: "Infrastructure",
      type: "phase",
      status: "pending",
      dependencies: [planningId],
      metadata: {
        estimatedDuration: 5,
        priority: "medium",
        riskLevel: "high",
      },
    },
    {
      id: deploymentId,
      label: "Deployment",
      type: "phase",
      status: "pending",
      dependencies: [frontendId, backendId, infraId],
      metadata: {
        estimatedDuration: 2,
        priority: "high",
        riskLevel: "high",
      },
    },
  ];

  const edges: GraphEdge[] = [
    { from: goalId, to: planningId, type: "flow" },
    { from: planningId, to: frontendId, type: "flow" },
    { from: planningId, to: backendId, type: "flow" },
    { from: planningId, to: infraId, type: "flow" },
    { from: frontendId, to: deploymentId, type: "dependency" },
    { from: backendId, to: deploymentId, type: "dependency" },
    { from: infraId, to: deploymentId, type: "dependency" },
  ];

  const totalEstimatedDuration = nodes.reduce((sum, node) => sum + (node.metadata.estimatedDuration || 0), 0);

  return {
    projectId,
    projectName,
    nodes,
    edges,
    metadata: {
      createdAt: new Date(),
      updatedAt: new Date(),
      totalEstimatedDuration,
      totalActualDuration: 0,
      completion: 0,
    },
  };
}

/**
 * Add task node to graph
 */
export function addTaskNode(graph: ExecutionGraph, parentId: string, task: Omit<GraphNode, "id" | "dependencies">): ExecutionGraph {
  const taskId = `${graph.projectId}:task:${Date.now()}`;
  const newNode: GraphNode = {
    ...task,
    id: taskId,
    dependencies: [parentId],
  };

  return {
    ...graph,
    nodes: [...graph.nodes, newNode],
    edges: [...graph.edges, { from: parentId, to: taskId, type: "dependency" }],
    metadata: {
      ...graph.metadata,
      updatedAt: new Date(),
      totalEstimatedDuration: graph.metadata.totalEstimatedDuration + (task.metadata.estimatedDuration || 0),
    },
  };
}

/**
 * Update node status
 */
export function updateNodeStatus(graph: ExecutionGraph, nodeId: string, status: GraphNode["status"]): ExecutionGraph {
  return {
    ...graph,
    nodes: graph.nodes.map(node =>
      node.id === nodeId ? { ...node, status } : node
    ),
    metadata: {
      ...graph.metadata,
      updatedAt: new Date(),
      completion: calculateCompletion(graph.nodes, nodeId, status),
    },
  };
}

/**
 * Calculate completion percentage
 */
function calculateCompletion(nodes: GraphNode[], updatedNodeId: string, newStatus: GraphNode["status"]): number {
  const completedNodes = nodes.filter(n => n.status === "completed").length;
  const totalNodes = nodes.length;
  
  if (newStatus === "completed") {
    return ((completedNodes + 1) / totalNodes) * 100;
  }
  return (completedNodes / totalNodes) * 100;
}

/**
 * Get critical path nodes
 */
export function getCriticalPath(graph: ExecutionGraph): GraphNode[] {
  const criticalNodes: GraphNode[] = [];
  const visited = new Set<string>();

  const traverse = (nodeId: string): void => {
    if (visited.has(nodeId)) return;
    visited.add(nodeId);

    const node = graph.nodes.find(n => n.id === nodeId);
    if (!node) return;

    if (node.metadata.priority === "critical" || node.metadata.riskLevel === "high") {
      criticalNodes.push(node);
    }

    const dependencies = graph.edges.filter(e => e.from === nodeId).map(e => e.to);
    dependencies.forEach(dep => traverse(dep));
  };

  const goalNode = graph.nodes.find(n => n.type === "goal");
  if (goalNode) {
    traverse(goalNode.id);
  }

  return criticalNodes;
}

/**
 * Get blocked nodes
 */
export function getBlockedNodes(graph: ExecutionGraph): GraphNode[] {
  return graph.nodes.filter(node => node.status === "blocked");
}

/**
 * Get nodes ready to start
 */
export function getReadyNodes(graph: ExecutionGraph): GraphNode[] {
  const blockedNodeIds = graph.edges
    .filter(e => e.type === "blocker")
    .map(e => e.to);

  return graph.nodes.filter(node =>
    node.status === "pending" &&
    !blockedNodeIds.includes(node.id) &&
    node.dependencies.every(depId => {
      const depNode = graph.nodes.find(n => n.id === depId);
      return depNode?.status === "completed";
    })
  );
}

/**
 * Estimate project completion
 */
export function estimateCompletion(graph: ExecutionGraph): Date {
  const totalEstimated = graph.metadata.totalEstimatedDuration;
  const completedDuration = graph.nodes
    .filter(n => n.status === "completed")
    .reduce((sum, node) => sum + (node.metadata.actualDuration || node.metadata.estimatedDuration || 0), 0);

  const remainingDuration = totalEstimated - completedDuration;
  const completionDate = new Date();
  completionDate.setDate(completionDate.getDate() + remainingDuration);

  return completionDate;
}

/**
 * Detect risks in graph
 */
export function detectRisks(graph: ExecutionGraph): Array<{
  nodeId: string;
  riskType: "blocked" | "overdue" | "high_risk" | "dependency_chain";
  description: string;
}> {
  const risks: Array<{
    nodeId: string;
    riskType: "blocked" | "overdue" | "high_risk" | "dependency_chain";
    description: string;
  }> = [];

  // Blocked nodes
  const blockedNodes = getBlockedNodes(graph);
  blockedNodes.forEach(node => {
    risks.push({
      nodeId: node.id,
      riskType: "blocked",
      description: `Node "${node.label}" is blocked`,
    });
  });

  // High risk nodes
  graph.nodes.forEach(node => {
    if (node.metadata.riskLevel === "high" && node.status !== "completed") {
      risks.push({
        nodeId: node.id,
        riskType: "high_risk",
        description: `Node "${node.label}" has high risk level`,
      });
    }
  });

  // Long dependency chains
  graph.nodes.forEach(node => {
    const chainLength = getDependencyChainLength(graph, node.id);
    if (chainLength > 5) {
      risks.push({
        nodeId: node.id,
        riskType: "dependency_chain",
        description: `Node "${node.label}" has long dependency chain (${chainLength} nodes)`,
      });
    }
  });

  return risks;
}

/**
 * Get dependency chain length
 */
function getDependencyChainLength(graph: ExecutionGraph, nodeId: string): number {
  const visited = new Set<string>();

  const traverse = (currentId: string): number => {
    if (visited.has(currentId)) return 0;
    visited.add(currentId);

    const dependencies = graph.edges.filter(e => e.to === currentId).map(e => e.from);
    if (dependencies.length === 0) return 1;

    return 1 + Math.max(...dependencies.map(dep => traverse(dep)));
  };

  return traverse(nodeId);
}

/**
 * Optimize graph (remove unnecessary dependencies)
 */
export function optimizeGraph(graph: ExecutionGraph): ExecutionGraph {
  const transitiveReductions = new Set<string>();

  graph.edges.forEach(edge => {
    // Check if this edge is redundant (transitive dependency)
    const hasTransitive = graph.edges.some(e =>
      e.from === edge.from &&
      e.to !== edge.to &&
      graph.edges.some(e2 => e2.from === e.to && e2.to === edge.to)
    );

    if (hasTransitive) {
      transitiveReductions.add(`${edge.from}-${edge.to}`);
    }
  });

  const optimizedEdges = graph.edges.filter(
    edge => !transitiveReductions.has(`${edge.from}-${edge.to}`)
  );

  return {
    ...graph,
    edges: optimizedEdges,
    metadata: {
      ...graph.metadata,
      updatedAt: new Date(),
    },
  };
}
