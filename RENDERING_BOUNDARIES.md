# Rendering Boundaries

## Strict Rule: UI Must Stay Stupid

Components should:
- **Render only**
- **No orchestration**
- **No business intelligence**
- **Minimal state**

## What Components CAN Do

- Receive data via props
- Render UI based on props
- Emit events (callbacks)
- Manage local UI state (hover, selection, expansion)
- Handle user interactions

## What Components CANNOT Do

- Directly call services
- Perform business logic
- Orchestrate workflows
- Manage global state
- Make network requests
- Execute AI operations
- Perform data transformations

## Data Flow

```
Services/Orchestration Layer
    ↓ (data)
Components (render only)
    ↓ (events)
Services/Orchestration Layer
```

## Example: Bad Pattern

```tsx
// ❌ Component with orchestration
const ExecutionGraph = () => {
  const [graph, setGraph] = useState(null);
  
  useEffect(() => {
    // Direct service call - BAD
    fetchGraph().then(data => setGraph(data));
  }, []);
  
  // Business logic - BAD
  const handleNodeClick = (nodeId) => {
    updateNodeStatus(nodeId, 'in_progress');
    assignAgent(nodeId);
  };
  
  return <svg>...</svg>;
};
```

## Example: Good Pattern

```tsx
// ✅ Component render-only
interface ExecutionGraphProps {
  graph: ExecutionGraph | null;
  onNodeClick: (nodeId: string) => void;
  loading: boolean;
}

const ExecutionGraph: Component<ExecutionGraphProps> = (props) => {
  const { graph, onNodeClick, loading } = props;
  
  // Only local UI state
  const [hoveredNode, setHoveredNode] = useState<string | null>(null);
  
  // Only render logic
  return (
    <svg>
      {graph?.nodes.map(node => (
        <g onClick={() => onNodeClick(node.id)}>
          {/* render node */}
        </g>
      ))}
    </svg>
  );
};
```

## Orchestration Layer

```tsx
// ✅ Orchestration happens in parent/container
const ExecutionGraphContainer = () => {
  const graph = useExecutionGraph(); // hook handles orchestration
  const { updateNodeStatus, assignAgent } = useGraphActions();
  
  const handleNodeClick = (nodeId: string) => {
    // Business logic here
    updateNodeStatus(nodeId, 'in_progress');
    assignAgent(nodeId);
  };
  
  return (
    <ExecutionGraph
      graph={graph.data}
      loading={graph.loading}
      onNodeClick={handleNodeClick}
    />
  );
};
```

## Component Size Limits

- **Dumb components**: < 150 lines
- **Smart components**: < 250 lines
- **Page components**: < 300 lines

If larger, split into smaller components.

## Component Pruning Priority

1. **Graph components** - Split node rendering, edge rendering, layout
2. **Intelligence panels** - Split sections (reasoning, recommendations, risks)
3. **Timeline** - Split event rendering, filtering, grouping
4. **Orchestration UI** - Split actions, status, progress

## Performance Rules

- Memoize heavy computations
- Isolate streaming areas
- Minimize reactive scope
- Throttle event updates
- Virtualize long lists

## Anti-Patterns to Avoid

- Giant mega-components
- Over-composition
- Prop hell (use context for deep nesting)
- Business logic in render
- Direct service calls in components
- State management in components

## Good Patterns to Follow

- Single responsibility components
- Clear data flow (props down, events up)
- Composition over inheritance
- Hooks for side effects
- Container/presenter pattern
- Render props for flexibility
