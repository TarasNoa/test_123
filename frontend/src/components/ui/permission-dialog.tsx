'use client';

import React from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ScrollArea } from '@/components/ui/scroll-area';
import { 
  Shield, 
  FileCode, 
  Terminal, 
  Globe, 
  Database,
  Check,
  X,
  AlertTriangle
} from 'lucide-react';

export type PermissionType = 
  | 'file_read'
  | 'file_write'
  | 'file_delete'
  | 'terminal_execute'
  | 'network_request'
  | 'database_access'
  | 'mcp_server';

export interface PermissionRequest {
  id: string;
  type: PermissionType;
  resource: string;
  description: string;
  agent?: string;
  riskLevel: 'low' | 'medium' | 'high';
}

interface PermissionDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  requests: PermissionRequest[];
  onApprove: (requestId: string) => void;
  onDeny: (requestId: string) => void;
  onApproveAll?: () => void;
  onDenyAll?: () => void;
}

export function PermissionDialog({
  open,
  onOpenChange,
  requests,
  onApprove,
  onDeny,
  onApproveAll,
  onDenyAll
}: PermissionDialogProps) {
  const getPermissionIcon = (type: PermissionType) => {
    switch (type) {
      case 'file_read':
      case 'file_write':
      case 'file_delete':
        return <FileCode className="w-5 h-5" />;
      case 'terminal_execute':
        return <Terminal className="w-5 h-5" />;
      case 'network_request':
        return <Globe className="w-5 h-5" />;
      case 'database_access':
        return <Database className="w-5 h-5" />;
      case 'mcp_server':
        return <Shield className="w-5 h-5" />;
      default:
        return <Shield className="w-5 h-5" />;
    }
  };

  const getRiskColor = (riskLevel: string) => {
    switch (riskLevel) {
      case 'high': return 'bg-red-500';
      case 'medium': return 'bg-yellow-500';
      case 'low': return 'bg-green-500';
      default: return 'bg-gray-400';
    }
  };

  const getRiskBadgeVariant = (riskLevel: string) => {
    switch (riskLevel) {
      case 'high': return 'destructive';
      case 'medium': return 'default';
      case 'low': return 'secondary';
      default: return 'outline';
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Permission Requests
            <Badge variant="secondary">{requests.length}</Badge>
          </DialogTitle>
          <DialogDescription>
            The following actions require your approval. Review each request carefully.
          </DialogDescription>
        </DialogHeader>

        <ScrollArea className="max-h-[400px] pr-4">
          <div className="space-y-3">
            {requests.map((request) => (
              <div
                key={request.id}
                className="p-4 border rounded-lg hover:bg-muted/50 transition-colors"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 flex-1">
                    <div className={`p-2 rounded ${getRiskColor(request.riskLevel)} text-white`}>
                      {getPermissionIcon(request.type)}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-1">
                        <h4 className="font-medium">{request.resource}</h4>
                        <Badge variant={getRiskBadgeVariant(request.riskLevel)}>
                          {request.riskLevel} risk
                        </Badge>
                      </div>
                      <p className="text-sm text-muted-foreground">
                        {request.description}
                      </p>
                      {request.agent && (
                        <p className="text-xs text-muted-foreground mt-2">
                          Agent: {request.agent}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => onDeny(request.id)}
                      className="text-red-600 hover:text-red-700 hover:bg-red-50"
                    >
                      <X className="w-4 h-4 mr-1" />
                      Deny
                    </Button>
                    <Button
                      size="sm"
                      onClick={() => onApprove(request.id)}
                    >
                      <Check className="w-4 h-4 mr-1" />
                      Approve
                    </Button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>

        {requests.length > 1 && (
          <DialogFooter className="flex gap-2">
            {onDenyAll && (
              <Button
                variant="outline"
                onClick={onDenyAll}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <X className="w-4 h-4 mr-2" />
                Deny All
              </Button>
            )}
            {onApproveAll && (
              <Button onClick={onApproveAll}>
                <Check className="w-4 h-4 mr-2" />
                Approve All
              </Button>
            )}
          </DialogFooter>
        )}

        {requests.some(r => r.riskLevel === 'high') && (
          <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg flex items-start gap-2">
            <AlertTriangle className="w-5 h-5 text-yellow-600 flex-shrink-0 mt-0.5" />
            <div className="text-sm text-yellow-800">
              <p className="font-medium">High Risk Actions Detected</p>
              <p className="mt-1">Some requests have high risk. Please review carefully before approving.</p>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

export default PermissionDialog;
