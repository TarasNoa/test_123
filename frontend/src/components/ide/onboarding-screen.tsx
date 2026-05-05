'use client';

import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { 
  ArrowRight, 
  CheckCircle, 
  Zap,
  Rocket,
  Sparkles
} from 'lucide-react';

export type OnboardingStep = 'welcome' | 'setup' | 'configure' | 'complete';

interface OnboardingScreenProps {
  currentStep: OnboardingStep;
  onStepChange?: (step: OnboardingStep) => void;
  onComplete?: () => void;
}

export function OnboardingScreen({ currentStep, onStepChange, onComplete }: OnboardingScreenProps) {
  const [apiKey, setApiKey] = useState('');
  const [workspaceName, setWorkspaceName] = useState('');

  const steps = [
    { id: 'welcome', title: 'Welcome', icon: <Sparkles className="w-5 h-5" /> },
    { id: 'setup', title: 'Setup', icon: <Zap className="w-5 h-5" /> },
    { id: 'configure', title: 'Configure', icon: <Rocket className="w-5 h-5" /> },
    { id: 'complete', title: 'Complete', icon: <CheckCircle className="w-5 h-5" /> }
  ];

  const getCurrentStepIndex = () => {
    return steps.findIndex(s => s.id === currentStep);
  };

  const nextStep = () => {
    const currentIndex = getCurrentStepIndex();
    if (currentIndex < steps.length - 1) {
      onStepChange?.(steps[currentIndex + 1].id as OnboardingStep);
    }
  };

  const prevStep = () => {
    const currentIndex = getCurrentStepIndex();
    if (currentIndex > 0) {
      onStepChange?.(steps[currentIndex - 1].id as OnboardingStep);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <Card className="w-full max-w-2xl">
        <CardHeader>
          <CardTitle>Get Started with Libr4</CardTitle>
          <div className="flex items-center gap-2 mt-4">
            {steps.map((step, index) => (
              <React.Fragment key={step.id}>
                <div className={`flex items-center gap-2 ${
                  index <= getCurrentStepIndex() ? 'text-primary' : 'text-muted-foreground'
                }`}>
                  {step.icon}
                  <span className="text-sm font-medium">{step.title}</span>
                </div>
                {index < steps.length - 1 && (
                  <ArrowRight className={`w-4 h-4 ${
                    index < getCurrentStepIndex() ? 'text-primary' : 'text-muted-foreground'
                  }`} />
                )}
              </React.Fragment>
            ))}
          </div>
        </CardHeader>
        <CardContent>
          {currentStep === 'welcome' && (
            <div className="space-y-6">
              <div className="text-center space-y-4">
                <Sparkles className="w-16 h-16 mx-auto text-primary" />
                <h2 className="text-2xl font-bold">Welcome to Libr4</h2>
                <p className="text-muted-foreground">
                  Your AI-powered development assistant. Build, debug, and ship faster with intelligent code generation and analysis.
                </p>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div className="text-center p-4 border rounded-lg">
                  <Zap className="w-8 h-8 mx-auto mb-2 text-yellow-500" />
                  <h3 className="font-medium mb-1">Fast</h3>
                  <p className="text-xs text-muted-foreground">Lightning-fast code generation</p>
                </div>
                <div className="text-center p-4 border rounded-lg">
                  <Rocket className="w-8 h-8 mx-auto mb-2 text-blue-500" />
                  <h3 className="font-medium mb-1">Smart</h3>
                  <p className="text-xs text-muted-foreground">Context-aware AI assistance</p>
                </div>
                <div className="text-center p-4 border rounded-lg">
                  <Sparkles className="w-8 h-8 mx-auto mb-2 text-purple-500" />
                  <h3 className="font-medium mb-1">Powerful</h3>
                  <p className="text-xs text-muted-foreground">Advanced workflow automation</p>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'setup' && (
            <div className="space-y-4">
              <h2 className="text-xl font-semibold">Setup Your Workspace</h2>
              <p className="text-muted-foreground">
                Enter your API key to get started with Libr4.
              </p>

              <div className="space-y-2">
                <Label htmlFor="apiKey">API Key</Label>
                <Input
                  id="apiKey"
                  type="password"
                  placeholder="Enter your API key"
                  value={apiKey}
                  onChange={(e) => setApiKey(e.target.value)}
                />
                <p className="text-xs text-muted-foreground">
                  Your API key is stored locally and never sent to our servers.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workspaceName">Workspace Name (Optional)</Label>
                <Input
                  id="workspaceName"
                  placeholder="My Workspace"
                  value={workspaceName}
                  onChange={(e) => setWorkspaceName(e.target.value)}
                />
              </div>
            </div>
          )}

          {currentStep === 'configure' && (
            <div className="space-y-4">
              <h2 className="text-xl font-semibold">Configure Preferences</h2>
              <p className="text-muted-foreground">
                Customize your Libr4 experience.
              </p>

              <div className="space-y-4">
                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div>
                    <h3 className="font-medium">Dark Mode</h3>
                    <p className="text-xs text-muted-foreground">Use dark theme</p>
                  </div>
                  <Badge variant="outline">Coming Soon</Badge>
                </div>

                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div>
                    <h3 className="font-medium">Auto-save</h3>
                    <p className="text-xs text-muted-foreground">Automatically save your work</p>
                  </div>
                  <Badge variant="outline">Coming Soon</Badge>
                </div>

                <div className="flex items-center justify-between p-3 border rounded-lg">
                  <div>
                    <h3 className="font-medium">Keyboard Shortcuts</h3>
                    <p className="text-xs text-muted-foreground">Enable keyboard shortcuts</p>
                  </div>
                  <Badge variant="outline">Coming Soon</Badge>
                </div>
              </div>
            </div>
          )}

          {currentStep === 'complete' && (
            <div className="space-y-6 text-center">
              <CheckCircle className="w-16 h-16 mx-auto text-green-500" />
              <div>
                <h2 className="text-2xl font-bold">You're All Set!</h2>
                <p className="text-muted-foreground mt-2">
                  Your workspace is ready. Start building with Libr4 now.
                </p>
              </div>

              <div className="space-y-2">
                <Button onClick={onComplete} className="w-full">
                  <Rocket className="w-4 h-4 mr-2" />
                  Start Building
                </Button>
                <Button variant="outline" className="w-full">
                  View Documentation
                </Button>
              </div>
            </div>
          )}

          {/* Navigation */}
          {currentStep !== 'complete' && (
            <div className="flex justify-between mt-6 pt-4 border-t">
              <Button
                variant="outline"
                onClick={prevStep}
                disabled={getCurrentStepIndex() === 0}
              >
                Back
              </Button>
              <Button onClick={nextStep}>
                {getCurrentStepIndex() === steps.length - 2 ? 'Finish' : 'Next'}
                <ArrowRight className="w-4 h-4 ml-2" />
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export default OnboardingScreen;
