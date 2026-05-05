'use client';

import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { 
  Sun, 
  Moon, 
  Monitor,
  Palette,
  Check
} from 'lucide-react';

export type ThemeMode = 'light' | 'dark' | 'system';

export type AccentColor = 'blue' | 'green' | 'purple' | 'orange' | 'pink' | 'red';

interface ThemePickerProps {
  currentTheme: ThemeMode;
  currentAccent: AccentColor;
  onThemeChange: (theme: ThemeMode) => void;
  onAccentChange: (accent: AccentColor) => void;
}

interface ThemeConfig {
  value: ThemeMode;
  label: string;
  icon: React.ReactNode;
  description: string;
}

const themeConfigs: ThemeConfig[] = [
  {
    value: 'light',
    label: 'Light',
    icon: <Sun className="w-5 h-5" />,
    description: 'Light mode for bright environments'
  },
  {
    value: 'dark',
    label: 'Dark',
    icon: <Moon className="w-5 h-5" />,
    description: 'Dark mode for low-light environments'
  },
  {
    value: 'system',
    label: 'System',
    icon: <Monitor className="w-5 h-5" />,
    description: 'Follow system preference'
  }
];

interface AccentConfig {
  value: AccentColor;
  label: string;
  color: string;
}

const accentConfigs: AccentConfig[] = [
  { value: 'blue', label: 'Blue', color: 'bg-blue-500' },
  { value: 'green', label: 'Green', color: 'bg-green-500' },
  { value: 'purple', label: 'Purple', color: 'bg-purple-500' },
  { value: 'orange', label: 'Orange', color: 'bg-orange-500' },
  { value: 'pink', label: 'Pink', color: 'bg-pink-500' },
  { value: 'red', label: 'Red', color: 'bg-red-500' }
];

export function ThemePicker({
  currentTheme,
  currentAccent,
  onThemeChange,
  onAccentChange
}: ThemePickerProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Palette className="w-5 h-5" />
          Theme Settings
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-6">
        {/* Theme Mode */}
        <div>
          <h3 className="font-medium mb-3">Theme Mode</h3>
          <div className="grid grid-cols-3 gap-3">
            {themeConfigs.map((config) => (
              <Button
                key={config.value}
                variant={currentTheme === config.value ? 'default' : 'outline'}
                onClick={() => onThemeChange(config.value)}
                className="flex flex-col gap-2 h-auto py-4"
              >
                {config.icon}
                <span className="text-sm">{config.label}</span>
              </Button>
            ))}
          </div>
          <p className="text-xs text-muted-foreground mt-2">
            {themeConfigs.find(c => c.value === currentTheme)?.description}
          </p>
        </div>

        {/* Accent Color */}
        <div>
          <h3 className="font-medium mb-3">Accent Color</h3>
          <div className="flex flex-wrap gap-3">
            {accentConfigs.map((config) => (
              <button
                key={config.value}
                onClick={() => onAccentChange(config.value)}
                className="relative group"
              >
                <div className={`w-10 h-10 rounded-full ${config.color} flex items-center justify-center transition-transform group-hover:scale-110`}>
                  {currentAccent === config.value && (
                    <Check className="w-5 h-5 text-white" />
                  )}
                </div>
                <span className="absolute -bottom-6 left-1/2 -translate-x-1/2 text-xs opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">
                  {config.label}
                </span>
              </button>
            ))}
          </div>
        </div>

        {/* Preview */}
        <div className="pt-4 border-t">
          <h3 className="font-medium mb-3">Preview</h3>
          <div className="space-y-2">
            <Button variant="default" className="w-full">
              Primary Button
            </Button>
            <div className="flex gap-2">
              <Button variant="secondary">Secondary</Button>
              <Button variant="outline">Outline</Button>
              <Button variant="ghost">Ghost</Button>
            </div>
            <div className="flex gap-2">
              <Badge variant="default">Default</Badge>
              <Badge variant="secondary">Secondary</Badge>
              <Badge variant="outline">Outline</Badge>
              <Badge variant="destructive">Destructive</Badge>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

export default ThemePicker;
