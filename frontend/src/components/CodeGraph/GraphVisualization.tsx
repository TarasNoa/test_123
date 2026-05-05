'use client';

import { useEffect, useRef } from 'react';

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

interface GraphVisualizationProps {
  nodes: CodeNode[];
  edges: CodeEdge[];
  onNodeClick?: (node: CodeNode) => void;
  width?: number;
  height?: number;
}

export function GraphVisualization({ 
  nodes, 
  edges, 
  onNodeClick,
  width = 800,
  height = 600 
}: GraphVisualizationProps) {
  const svgRef = useRef<SVGSVGElement>(null);
  
  useEffect(() => {
    if (!svgRef.current || nodes.length === 0) return;

    // Clear existing content
    while (svgRef.current.firstChild) {
      svgRef.current.removeChild(svgRef.current.firstChild);
    }

    const svg = svgRef.current;
    const svgRect = svg.getBoundingClientRect();
    const centerX = svgRect.width / 2;
    const centerY = svgRect.height / 2;

    // Create node positions (simple circular layout)
    const nodePositions = new Map<string, { x: number; y: number }>();
    const radius = Math.min(svgRect.width, svgRect.height) / 3;
    
    nodes.forEach((node, index) => {
      const angle = (index / nodes.length) * 2 * Math.PI;
      const x = centerX + radius * Math.cos(angle);
      const y = centerY + radius * Math.sin(angle);
      nodePositions.set(node.id, { x, y });
    });

    // Draw edges
    edges.forEach(edge => {
      const sourcePos = nodePositions.get(edge.source);
      const targetPos = nodePositions.get(edge.target);
      
      if (sourcePos && targetPos) {
        const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
        line.setAttribute('x1', sourcePos.x.toString());
        line.setAttribute('y1', sourcePos.y.toString());
        line.setAttribute('x2', targetPos.x.toString());
        line.setAttribute('y2', targetPos.y.toString());
        line.setAttribute('stroke', '#94a3b8');
        line.setAttribute('stroke-width', '1');
        line.setAttribute('stroke-opacity', '0.6');
        svg.appendChild(line);
      }
    });

    // Draw nodes
    nodes.forEach(node => {
      const pos = nodePositions.get(node.id);
      if (!pos) return;

      const g = document.createElementNS('http://www.w3.org/2000/svg', 'g');
      g.setAttribute('class', 'node');
      g.style.cursor = 'pointer';
      
      // Node circle
      const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
      circle.setAttribute('cx', pos.x.toString());
      circle.setAttribute('cy', pos.y.toString());
      circle.setAttribute('r', '8');
      
      // Color based on node type
      let color = '#6b7280';
      switch (node.nodeType) {
        case 'class':
          color = '#3b82f6';
          break;
        case 'interface':
          color = '#8b5cf6';
          break;
        case 'method':
          color = '#10b981';
          break;
        case 'property':
          color = '#f59e0b';
          break;
        default:
          color = '#6b7280';
      }
      
      circle.setAttribute('fill', color);
      circle.setAttribute('stroke', '#1e293b');
      circle.setAttribute('stroke-width', '2');
      
      // Node label
      const text = document.createElementNS('http://www.w3.org/2000/svg', 'text');
      text.setAttribute('x', pos.x.toString());
      text.setAttribute('y', (pos.y + 20).toString());
      text.setAttribute('font-size', '10');
      text.setAttribute('font-family', 'monospace');
      text.setAttribute('text-anchor', 'middle');
      text.setAttribute('fill', '#1e293b');
      text.textContent = node.label.length > 15 ? node.label.substring(0, 15) + '...' : node.label;
      
      g.appendChild(circle);
      g.appendChild(text);
      
      // Click handler
      g.addEventListener('click', () => {
        if (onNodeClick) onNodeClick(node);
      });
      
      svg.appendChild(g);
    });
  }, [nodes, edges, onNodeClick]);

  return (
    <svg
      ref={svgRef}
      width={width}
      height={height}
      style={{ 
        border: '1px solid #e5e7eb', 
        borderRadius: '8px',
        backgroundColor: '#f8fafc'
      }}
    />
  );
}
