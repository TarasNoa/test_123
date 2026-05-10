import { createSignal, onMount } from 'solid-js';

export function Whiteboard(props) {
  let canvasRef;
  const [isDrawing, setIsDrawing] = createSignal(false);
  const [tool, setTool] = createSignal('pen');
  const [color, setColor] = createSignal('#000000');
  const [strokeWidth, setStrokeWidth] = createSignal('2');

  let context;
  let lastX = 0;
  let lastY = 0;

  onMount(() => {
    if (canvasRef) {
      context = canvasRef.getContext('2d');
      canvasRef.width = canvasRef.offsetWidth;
      canvasRef.height = canvasRef.offsetHeight;
    }
  });

  const startDrawing = (e) => {
    setIsDrawing(true);
    const rect = canvasRef.getBoundingClientRect();
    lastX = e.clientX - rect.left;
    lastY = e.clientY - rect.top;
  };

  const draw = (e) => {
    if (!isDrawing()) return;

    const rect = canvasRef.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    context.strokeStyle = color();
    context.lineWidth = strokeWidth();
    context.lineJoin = 'round';
    context.lineCap = 'round';

    if (tool() === 'pen') {
      context.beginPath();
      context.moveTo(lastX, lastY);
      context.lineTo(x, y);
      context.stroke();
    } else if (tool() === 'eraser') {
      context.clearRect(x - strokeWidth() / 2, y - strokeWidth() / 2, strokeWidth(), strokeWidth());
    }

    props.onDrawing({
      type: tool(),
      x,
      y,
      color: color(),
      strokeWidth: strokeWidth()
    });

    lastX = x;
    lastY = y;
  };

  const endDrawing = () => {
    setIsDrawing(false);
  };

  return (
    <div class="whiteboard">
      <h3>{props.whiteboard.name}</h3>
      <div class="whiteboard-tools">
        <button onClick={() => setTool('pen')} class={tool() === 'pen' ? 'active' : ''}>✏️ Pen</button>
        <button onClick={() => setTool('eraser')} class={tool() === 'eraser' ? 'active' : ''}>🧹 Eraser</button>
        <input 
          type="color" 
          value={color()} 
          onInput={(e) => setColor(e.currentTarget.value)}
        />
        <input 
          type="range" 
          min="1" 
          max="20" 
          value={strokeWidth()} 
          onInput={(e) => setStrokeWidth(e.currentTarget.value)}
        />
        <button onClick={() => context.clearRect(0, 0, canvasRef.width, canvasRef.height)}>🗑️ Clear</button>
      </div>
      <canvas
        ref={canvasRef}
        onMouseDown={startDrawing}
        onMouseMove={draw}
        onMouseUp={endDrawing}
        onMouseLeave={endDrawing}
        class="whiteboard-canvas"
      />
    </div>
  );
}