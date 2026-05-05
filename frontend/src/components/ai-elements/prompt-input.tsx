"use client";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import {
  ArrowUpIcon,
  ImageIcon,
  Loader2Icon,
  PaperclipIcon,
  PlusIcon,
  SquareIcon,
  XIcon,
} from "lucide-react";
import { useState, useRef, type ChangeEvent, type ClipboardEvent, type KeyboardEvent } from "react";

export type PromptInputFile = {
  id: string;
  name: string;
  size: number;
  type: string;
};

export type PromptInputProps = {
  value: string;
  onChange: (value: string) => void;
  onSubmit: () => void;
  disabled?: boolean;
  isLoading?: boolean;
  onStop?: () => void;
  placeholder?: string;
  maxFiles?: number;
};

export function PromptInput({
  value,
  onChange,
  onSubmit,
  disabled = false,
  isLoading = false,
  onStop,
  placeholder = "Type your message...",
  maxFiles = 5,
}: PromptInputProps) {
  const [files, setFiles] = useState<PromptInputFile[]>([]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (value.trim() || files.length > 0) {
        onSubmit();
      }
    }
  };

  const handlePaste = (e: ClipboardEvent<HTMLTextAreaElement>) => {
    const items = e.clipboardData?.items;
    if (!items) return;

    const newFiles: PromptInputFile[] = [];
    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.kind === "file" && item.type.startsWith("image/")) {
        const file = item.getAsFile();
        if (file && files.length + newFiles.length < maxFiles) {
          newFiles.push({
            id: Math.random().toString(36).substring(7),
            name: file.name,
            size: file.size,
            type: file.type,
          });
        }
      }
    }

    if (newFiles.length > 0) {
      e.preventDefault();
      setFiles([...files, ...newFiles]);
    }
  };

  const handleFileSelect = (e: ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files || []);
    const newFiles: PromptInputFile[] = selectedFiles
      .slice(0, maxFiles - files.length)
      .map((file) => ({
        id: Math.random().toString(36).substring(7),
        name: file.name,
        size: file.size,
        type: file.type,
      }));

    setFiles([...files, ...newFiles]);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const removeFile = (id: string) => {
    setFiles(files.filter((f) => f.id !== id));
  };

  const openFileDialog = () => {
    fileInputRef.current?.click();
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return "0 B";
    const k = 1024;
    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  };

  return (
    <div className="flex flex-col gap-2">
      {/* Files display */}
      {files.length > 0 && (
        <ScrollArea className="h-20 border rounded-md p-2">
          <div className="flex flex-wrap gap-2">
            {files.map((file) => (
              <Badge
                key={file.id}
                variant="secondary"
                className="flex items-center gap-1 px-2 py-1"
              >
                <ImageIcon className="h-3 w-3" />
                <span className="text-xs max-w-[150px] truncate">{file.name}</span>
                <span className="text-xs text-muted-foreground">
                  ({formatFileSize(file.size)})
                </span>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-4 w-4 p-0 ml-1"
                  onClick={() => removeFile(file.id)}
                >
                  <XIcon className="h-3 w-3" />
                </Button>
              </Badge>
            ))}
          </div>
        </ScrollArea>
      )}

      {/* Input area */}
      <div className="relative">
        <Textarea
          value={value}
          onChange={(e) => onChange(e.target.value)}
          onKeyDown={handleKeyDown}
          onPaste={handlePaste}
          placeholder={placeholder}
          disabled={disabled}
          className="min-h-[80px] max-h-[300px] pr-24 resize-none"
          rows={3}
        />

        {/* Action buttons */}
        <div className="absolute bottom-2 right-2 flex items-center gap-1">
          {!isLoading && (
            <>
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      onClick={openFileDialog}
                      disabled={disabled || files.length >= maxFiles}
                    >
                      <PaperclipIcon className="h-4 w-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Attach file (max {maxFiles})</TooltipContent>
                </Tooltip>
              </TooltipProvider>

              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-8 w-8"
                      disabled={disabled}
                    >
                      <ImageIcon className="h-4 w-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Add image</TooltipContent>
                </Tooltip>
              </TooltipProvider>
            </>
          )}

          {isLoading && onStop ? (
            <Button
              variant="destructive"
              size="icon"
              className="h-8 w-8"
              onClick={onStop}
            >
              <SquareIcon className="h-4 w-4" />
            </Button>
          ) : (
            <Button
              variant="default"
              size="icon"
              className="h-8 w-8"
              onClick={onSubmit}
              disabled={disabled || (!value.trim() && files.length === 0)}
            >
              {isLoading ? (
                <Loader2Icon className="h-4 w-4 animate-spin" />
              ) : (
                <ArrowUpIcon className="h-4 w-4" />
              )}
            </Button>
          )}
        </div>
      </div>

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        multiple
        className="hidden"
        onChange={handleFileSelect}
        accept="image/*,.pdf,.txt,.md"
      />
    </div>
  );
}
