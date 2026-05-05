'use client';

import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { 
  Settings, 
  Palette, 
  Database, 
  Brain, 
  Wrench,
  User,
  Bell,
  Shield
} from 'lucide-react';

export type SettingsTab = 'appearance' | 'memory' | 'skill' | 'tool' | 'account' | 'notifications' | 'security';

interface SettingsDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  defaultTab?: SettingsTab;
}

export function SettingsDialog({
  open,
  onOpenChange,
  defaultTab = 'appearance'
}: SettingsDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Settings className="w-5 h-5" />
            Settings
          </DialogTitle>
        </DialogHeader>

        <Tabs defaultValue={defaultTab} className="w-full">
          <TabsList className="grid w-full grid-cols-4">
            <TabsTrigger value="appearance">
              <Palette className="w-4 h-4 mr-2" />
              Appearance
            </TabsTrigger>
            <TabsTrigger value="memory">
              <Database className="w-4 h-4 mr-2" />
              Memory
            </TabsTrigger>
            <TabsTrigger value="skill">
              <Brain className="w-4 h-4 mr-2" />
              Skills
            </TabsTrigger>
            <TabsTrigger value="tool">
              <Wrench className="w-4 h-4 mr-2" />
              Tools
            </TabsTrigger>
          </TabsList>

          <TabsContent value="appearance" className="space-y-4">
            <div className="space-y-4">
              <h3 className="font-medium">Theme</h3>
              <div className="space-y-2">
                <Label>Theme Mode</Label>
                <div className="flex gap-2">
                  <Button variant="outline">Light</Button>
                  <Button variant="outline">Dark</Button>
                  <Button variant="outline">System</Button>
                </div>
              </div>
              
              <div className="space-y-2">
                <Label>Accent Color</Label>
                <div className="flex gap-2">
                  <div className="w-8 h-8 rounded-full bg-blue-500 cursor-pointer" />
                  <div className="w-8 h-8 rounded-full bg-green-500 cursor-pointer" />
                  <div className="w-8 h-8 rounded-full bg-purple-500 cursor-pointer" />
                  <div className="w-8 h-8 rounded-full bg-orange-500 cursor-pointer" />
                  <div className="w-8 h-8 rounded-full bg-pink-500 cursor-pointer" />
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Compact Mode</Label>
                  <p className="text-sm text-muted-foreground">Reduce spacing in UI</p>
                </div>
                <Switch />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Show Animations</Label>
                  <p className="text-sm text-muted-foreground">Enable UI animations</p>
                </div>
                <Switch defaultChecked />
              </div>
            </div>
          </TabsContent>

          <TabsContent value="memory" className="space-y-4">
            <div className="space-y-4">
              <h3 className="font-medium">Memory Settings</h3>
              
              <div className="space-y-2">
                <Label>Max Context Window</Label>
                <Input type="number" defaultValue={200000} />
                <p className="text-sm text-muted-foreground">Maximum tokens in context</p>
              </div>

              <div className="space-y-2">
                <Label>Memory Retention Days</Label>
                <Input type="number" defaultValue={30} />
                <p className="text-sm text-muted-foreground">How long to keep memory</p>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Enable Long-term Memory</Label>
                  <p className="text-sm text-muted-foreground">Store conversations for future reference</p>
                </div>
                <Switch defaultChecked />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Enable Skill Memory</Label>
                  <p className="text-sm text-muted-foreground">Remember learned skills</p>
                </div>
                <Switch defaultChecked />
              </div>

              <Button variant="outline" className="w-full">
                Clear All Memory
              </Button>
            </div>
          </TabsContent>

          <TabsContent value="skill" className="space-y-4">
            <div className="space-y-4">
              <h3 className="font-medium">Skill Configuration</h3>
              
              <div className="space-y-2">
                <Label>Default Skill Set</Label>
                <select className="w-full p-2 border rounded-md">
                  <option>General Coding</option>
                  <option>Data Science</option>
                  <option>Web Development</option>
                  <option>System Programming</option>
                </select>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Auto-learn Skills</Label>
                  <p className="text-sm text-muted-foreground">Automatically learn new skills from interactions</p>
                </div>
                <Switch defaultChecked />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Skill Recommendations</Label>
                  <p className="text-sm text-muted-foreground">Suggest relevant skills based on context</p>
                </div>
                <Switch defaultChecked />
              </div>

              <div className="space-y-2">
                <Label>Custom Skills</Label>
                <div className="border rounded-lg p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <span>Python Expert</span>
                    <Switch defaultChecked />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>React Specialist</span>
                    <Switch />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Database Architect</span>
                    <Switch />
                  </div>
                </div>
                <Button variant="outline" className="w-full">
                  Add Custom Skill
                </Button>
              </div>
            </div>
          </TabsContent>

          <TabsContent value="tool" className="space-y-4">
            <div className="space-y-4">
              <h3 className="font-medium">Tool Configuration</h3>
              
              <div className="space-y-2">
                <Label>Available Tools</Label>
                <div className="border rounded-lg p-4 space-y-2">
                  <div className="flex items-center justify-between">
                    <span>File Operations</span>
                    <Switch defaultChecked />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Terminal Access</span>
                    <Switch defaultChecked />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Web Search</span>
                    <Switch defaultChecked />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Browser Automation</span>
                    <Switch />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Database Access</span>
                    <Switch />
                  </div>
                  <div className="flex items-center justify-between">
                    <span>Git Operations</span>
                    <Switch defaultChecked />
                  </div>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Tool Auto-detection</Label>
                  <p className="text-sm text-muted-foreground">Automatically select appropriate tools</p>
                </div>
                <Switch defaultChecked />
              </div>

              <div className="flex items-center justify-between">
                <div>
                  <Label>Show Tool Usage</Label>
                  <p className="text-sm text-muted-foreground">Display which tools are being used</p>
                </div>
                <Switch defaultChecked />
              </div>
            </div>
          </TabsContent>
        </Tabs>

        {/* Additional tabs for account, notifications, security */}
        <div className="mt-4 pt-4 border-t">
          <Tabs defaultValue="account" className="w-full">
            <TabsList className="grid w-full grid-cols-3">
              <TabsTrigger value="account">
                <User className="w-4 h-4 mr-2" />
                Account
              </TabsTrigger>
              <TabsTrigger value="notifications">
                <Bell className="w-4 h-4 mr-2" />
                Notifications
              </TabsTrigger>
              <TabsTrigger value="security">
                <Shield className="w-4 h-4 mr-2" />
                Security
              </TabsTrigger>
            </TabsList>

            <TabsContent value="account" className="space-y-4">
              <div className="space-y-4">
                <h3 className="font-medium">Account Settings</h3>
                <div className="space-y-2">
                  <Label>Display Name</Label>
                  <Input defaultValue="User" />
                </div>
                <div className="space-y-2">
                  <Label>Email</Label>
                  <Input type="email" defaultValue="user@example.com" />
                </div>
              </div>
            </TabsContent>

            <TabsContent value="notifications" className="space-y-4">
              <div className="space-y-4">
                <h3 className="font-medium">Notification Preferences</h3>
                <div className="flex items-center justify-between">
                  <div>
                    <Label>Email Notifications</Label>
                    <p className="text-sm text-muted-foreground">Receive email updates</p>
                  </div>
                  <Switch />
                </div>
                <div className="flex items-center justify-between">
                  <div>
                    <Label>Push Notifications</Label>
                    <p className="text-sm text-muted-foreground">Browser notifications</p>
                  </div>
                  <Switch defaultChecked />
                </div>
                <div className="flex items-center justify-between">
                  <div>
                    <Label>Sound Alerts</Label>
                    <p className="text-sm text-muted-foreground">Play sound on new messages</p>
                  </div>
                  <Switch />
                </div>
              </div>
            </TabsContent>

            <TabsContent value="security" className="space-y-4">
              <div className="space-y-4">
                <h3 className="font-medium">Security Settings</h3>
                <div className="flex items-center justify-between">
                  <div>
                    <Label>Two-Factor Authentication</Label>
                    <p className="text-sm text-muted-foreground">Add extra security to your account</p>
                  </div>
                  <Switch />
                </div>
                <div className="flex items-center justify-between">
                  <div>
                    <Label>Session Timeout</Label>
                    <p className="text-sm text-muted-foreground">Auto-logout after inactivity</p>
                  </div>
                  <Switch defaultChecked />
                </div>
                <Button variant="destructive" className="w-full">
                  Change Password
                </Button>
              </div>
            </TabsContent>
          </Tabs>
        </div>
      </DialogContent>
    </Dialog>
  );
}

export default SettingsDialog;
