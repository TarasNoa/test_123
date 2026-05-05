'use client';

import React, { useState, useCallback } from 'react';

interface AIBuilderPanelProps {
  workspaceId: string;
  onComponentGenerated?: (code: string, componentName: string) => void;
}

export function AIBuilderPanel({ workspaceId, onComponentGenerated }: AIBuilderPanelProps) {
  const [prompt, setPrompt] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generatedCode, setGeneratedCode] = useState('');

  const handleGenerate = useCallback(async () => {
    if (!prompt.trim()) return;
    
    setIsGenerating(true);
    try {
      // Integrate with existing LlmAppPlannerService
      const response = await fetch('/api/ide/app-generation/generate-ui', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          workspaceId,
          prompt,
          useTambo: true
        })
      });
      
      const result = await response.json();
      
      if (result.success && result.code) {
        setGeneratedCode(result.code);
        onComponentGenerated?.(result.code, result.componentName || 'GeneratedComponent');
      }
    } catch (error) {
      console.error('Failed to generate UI:', error);
    } finally {
      setIsGenerating(false);
    }
  }, [prompt, workspaceId, onComponentGenerated]);

  return (
    <div className="flex flex-col h-full">
      <div className="p-4 border-b">
        <h3 className="text-lg font-semibold mb-2">AI Builder Panel</h3>
        <p className="text-sm text-muted-foreground mb-4">
          Describe UI components in natural language to generate React code
        </p>
        
        <textarea
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          placeholder="e.g., Create a login form with email and password fields, a submit button, and forgot password link..."
          className="w-full h-32 p-3 border rounded-md resize-none text-sm"
          disabled={isGenerating}
        />
        
        <button
          onClick={handleGenerate}
          disabled={isGenerating || !prompt.trim()}
          className="mt-3 px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isGenerating ? 'Generating...' : 'Generate Component'}
        </button>
      </div>
      
      {generatedCode && (
        <div className="flex-1 p-4 overflow-auto">
          <h4 className="text-sm font-semibold mb-2">Generated Code</h4>
          <pre className="p-4 bg-muted rounded-md text-xs overflow-auto">
            {generatedCode}
          </pre>
        </div>
      )}
    </div>
  );
}
