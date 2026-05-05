'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTrigger } from '@/components/ui/sheet';
import { Separator } from '@/components/ui/separator';
import { Badge } from '@/components/ui/badge';
import { 
  PanelLeft,
  PanelRight,
  PanelLeftClose,
  PanelRightClose,
  LayoutGrid,
  Maximize2,
  Minimize2,
  Settings,
  Bell,
  User
} from 'lucide-react';

export type PanelPosition = 'left' | 'right' | 'both' | 'none';

export type WorkspaceView = 'default' | 'focus' | 'split';

export interface WorkspaceLayoutConfig {
  leftPanelOpen: boolean;
  rightPanelOpen: boolean;
  view: WorkspaceView;
  panelWidth?: number;
}

interface WorkspaceLayoutProps {
  config: WorkspaceLayoutConfig;
  onConfigChange?: (config: WorkspaceLayoutConfig) => void;
  leftPanel?: React.ReactNode;
  rightPanel?: React.ReactNode;
  children: React.ReactNode;
  header?: React.ReactNode;
  notificationCount?: number;
}

export function WorkspaceLayout({
  config,
  onConfigChange,
  leftPanel,
  rightPanel,
  children,
  header,
  notificationCount = 0
}: WorkspaceLayoutProps) {
  const [mobileLeftOpen, setMobileLeftOpen] = useState(false);
  const [mobileRightOpen, setMobileRightOpen] = useState(false);

  const toggleLeftPanel = () => {
    onConfigChange?.({
      ...config,
      leftPanelOpen: !config.leftPanelOpen
    });
  };

  const toggleRightPanel = () => {
    onConfigChange?.({
      ...config,
      rightPanelOpen: !config.rightPanelOpen
    });
  };

  const setView = (view: WorkspaceView) => {
    onConfigChange?.({
      ...config,
      view,
      leftPanelOpen: view === 'split' ? true : config.leftPanelOpen,
      rightPanelOpen: view === 'split' ? true : config.rightPanelOpen
    });
  };

  return (
    <div className="h-screen flex flex-col">
      {/* Header */}
      {header && (
        <header className="border-b px-4 py-3 flex items-center justify-between bg-background">
          {header}
        </header>
      )}

      {/* Toolbar */}
      <div className="border-b px-4 py-2 flex items-center justify-between bg-muted/50">
        <div className="flex items-center gap-2">
          {/* Mobile Left Panel Toggle */}
          <Sheet open={mobileLeftOpen} onOpenChange={setMobileLeftOpen}>
            <SheetTrigger asChild>
              <Button size="sm" variant="ghost" className="lg:hidden">
                <PanelLeft className="w-4 h-4" />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-80">
              {leftPanel}
            </SheetContent>
          </Sheet>

          {/* Desktop Left Panel Toggle */}
          <Button
            size="sm"
            variant="ghost"
            onClick={toggleLeftPanel}
            className="hidden lg:flex"
          >
            {config.leftPanelOpen ? (
              <PanelLeftClose className="w-4 h-4" />
            ) : (
              <PanelLeft className="w-4 h-4" />
            )}
          </Button>

          {/* View Mode Selector */}
          <div className="flex gap-1">
            <Button
              size="sm"
              variant={config.view === 'default' ? 'default' : 'ghost'}
              onClick={() => setView('default')}
            >
              <LayoutGrid className="w-4 h-4" />
            </Button>
            <Button
              size="sm"
              variant={config.view === 'focus' ? 'default' : 'ghost'}
              onClick={() => setView('focus')}
            >
              <Maximize2 className="w-4 h-4" />
            </Button>
            <Button
              size="sm"
              variant={config.view === 'split' ? 'default' : 'ghost'}
              onClick={() => setView('split')}
            >
              <Minimize2 className="w-4 h-4" />
            </Button>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button size="sm" variant="ghost">
            <Settings className="w-4 h-4" />
          </Button>
          <Button size="sm" variant="ghost" className="relative">
            <Bell className="w-4 h-4" />
            {notificationCount > 0 && (
              <Badge className="absolute -top-1 -right-1 h-4 w-4 flex items-center justify-center p-0 text-xs">
                {notificationCount}
              </Badge>
            )}
          </Button>
          <Button size="sm" variant="ghost">
            <User className="w-4 h-4" />
          </Button>

          {/* Mobile Right Panel Toggle */}
          <Sheet open={mobileRightOpen} onOpenChange={setMobileRightOpen}>
            <SheetTrigger asChild>
              <Button size="sm" variant="ghost" className="lg:hidden">
                <PanelRight className="w-4 h-4" />
              </Button>
            </SheetTrigger>
            <SheetContent side="right" className="w-80">
              {rightPanel}
            </SheetContent>
          </Sheet>

          {/* Desktop Right Panel Toggle */}
          <Button
            size="sm"
            variant="ghost"
            onClick={toggleRightPanel}
            className="hidden lg:flex"
          >
            {config.rightPanelOpen ? (
              <PanelRightClose className="w-4 h-4" />
            ) : (
              <PanelRight className="w-4 h-4" />
            )}
          </Button>
        </div>
      </div>

      {/* Main Workspace */}
      <div className="flex-1 flex overflow-hidden">
        {/* Left Panel */}
        {config.leftPanelOpen && (
          <>
            <aside 
              className="hidden lg:block w-80 border-r overflow-y-auto"
              style={{ width: config.panelWidth || 320 }}
            >
              {leftPanel}
            </aside>
            <Separator orientation="vertical" />
          </>
        )}

        {/* Main Content */}
        <main className="flex-1 overflow-auto">
          {children}
        </main>

        {/* Right Panel */}
        {config.rightPanelOpen && (
          <>
            <Separator orientation="vertical" />
            <aside 
              className="hidden lg:block w-80 border-l overflow-y-auto"
              style={{ width: config.panelWidth || 320 }}
            >
              {rightPanel}
            </aside>
          </>
        )}
      </div>
    </div>
  );
}

export default WorkspaceLayout;
