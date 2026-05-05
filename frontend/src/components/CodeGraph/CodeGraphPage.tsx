'use client';

import { useState, useEffect } from 'react';
import { GraphVisualization } from './GraphVisualization';

interface CodeNode {
  id: string;
  label: string;
  nodeType: string;
  filePath: string;
  lineNumber: number;
  metadata?: Record<string, unknown>;
}

interface CodeEdge {
  source: string;
  target: string;
  relation: string;
  filePath: string;
  lineNumber: number;
}

export function CodeGraphPage() {
  const [nodes, setNodes] = useState<CodeNode[]>([]);
  const [edges, setEdges] = useState<CodeEdge[]>([]);
  const [selectedNode, setSelectedNode] = useState<CodeNode | null>(null);
  const [loading, setLoading] = useState(false);
  const [filterType, setFilterType] = useState<string>('all');

  useEffect(() => {
    loadGraphData();
  }, []);

  const loadGraphData = async () => {
    setLoading(true);
    try {
      // Fetch graph data from API
      const response = await fetch('/api/code-graph');
      if (response.ok) {
        const data = await response.json();
        setNodes(data.nodes || []);
        setEdges(data.edges || []);
      }
    } catch (error) {
      console.error('Failed to load graph data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleNodeClick = (node: CodeNode) => {
    setSelectedNode(node);
  };

  const filteredNodes = filterType === 'all' 
    ? nodes 
    : nodes.filter(n => n.nodeType === filterType);

  const nodeTypeCounts = {
    all: nodes.length,
    class: nodes.filter(n => n.nodeType === 'class').length,
    interface: nodes.filter(n => n.nodeType === 'interface').length,
    method: nodes.filter(n => n.nodeType === 'method').length,
    property: nodes.filter(n => n.nodeType === 'property').length,
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Codebase Knowledge Graph</h1>
        <p className="text-gray-600">Visualize code structure and relationships</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Controls Panel */}
        <div className="lg:col-span-1 space-y-4">
          <div className="bg-white rounded-lg shadow p-4">
            <h2 className="font-semibold mb-3">Filter by Type</h2>
            <select
              value={filterType}
              onChange={(e) => setFilterType(e.target.value)}
              className="w-full p-2 border rounded-md"
            >
              <option value="all">All ({nodeTypeCounts.all})</option>
              <option value="class">Classes ({nodeTypeCounts.class})</option>
              <option value="interface">Interfaces ({nodeTypeCounts.interface})</option>
              <option value="method">Methods ({nodeTypeCounts.method})</option>
              <option value="property">Properties ({nodeTypeCounts.property})</option>
            </select>
          </div>

          <div className="bg-white rounded-lg shadow p-4">
            <h2 className="font-semibold mb-3">Statistics</h2>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span>Total Nodes:</span>
                <span className="font-medium">{nodes.length}</span>
              </div>
              <div className="flex justify-between">
                <span>Total Edges:</span>
                <span className="font-medium">{edges.length}</span>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-4">
            <h2 className="font-semibold mb-3">Legend</h2>
            <div className="space-y-2 text-sm">
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded-full bg-blue-500"></div>
                <span>Class</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded-full bg-purple-500"></div>
                <span>Interface</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded-full bg-green-500"></div>
                <span>Method</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-4 rounded-full bg-yellow-500"></div>
                <span>Property</span>
              </div>
            </div>
          </div>

          <button
            onClick={loadGraphData}
            disabled={loading}
            className="w-full bg-blue-500 text-white py-2 rounded-md hover:bg-blue-600 disabled:opacity-50"
          >
            {loading ? 'Loading...' : 'Refresh Graph'}
          </button>
        </div>

        {/* Graph Visualization */}
        <div className="lg:col-span-3">
          <div className="bg-white rounded-lg shadow p-4">
            {loading ? (
              <div className="flex items-center justify-center h-[600px]">
                <div className="text-gray-500">Loading graph...</div>
              </div>
            ) : filteredNodes.length === 0 ? (
              <div className="flex items-center justify-center h-[600px]">
                <div className="text-gray-500">No nodes to display</div>
              </div>
            ) : (
              <GraphVisualization
                nodes={filteredNodes}
                edges={edges}
                onNodeClick={handleNodeClick}
              />
            )}
          </div>
        </div>
      </div>

      {/* Selected Node Details */}
      {selectedNode && (
        <div className="mt-6 bg-white rounded-lg shadow p-4">
          <h2 className="font-semibold mb-3">Selected Node</h2>
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-500">ID:</span>
              <span className="ml-2">{selectedNode.id}</span>
            </div>
            <div>
              <span className="text-gray-500">Label:</span>
              <span className="ml-2">{selectedNode.label}</span>
            </div>
            <div>
              <span className="text-gray-500">Type:</span>
              <span className="ml-2">{selectedNode.nodeType}</span>
            </div>
            <div>
              <span className="text-gray-500">File:</span>
              <span className="ml-2">{selectedNode.filePath}</span>
            </div>
            <div>
              <span className="text-gray-500">Line:</span>
              <span className="ml-2">{selectedNode.lineNumber}</span>
            </div>
          </div>
          {selectedNode.metadata && Object.keys(selectedNode.metadata).length > 0 && (
            <div className="mt-3">
              <span className="text-gray-500 text-sm">Metadata:</span>
              <pre className="mt-2 bg-gray-50 p-3 rounded text-xs overflow-auto">
                {JSON.stringify(selectedNode.metadata, null, 2)}
              </pre>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
