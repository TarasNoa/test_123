"use client";

import { CheckIcon, ChevronDownIcon, CpuIcon } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";

export type ModelCapability = {
  name: string;
  supported: boolean;
};

export type ModelInfo = {
  id: string;
  name: string;
  provider: string;
  description: string;
  maxTokens: number;
  costPer1kTokens: number;
  capabilities: ModelCapability[];
  supportsThinking: boolean;
  supportsImages: boolean;
  supportsTools: boolean;
};

export type ModelSelectorProps = {
  models: ModelInfo[];
  selectedModel: string;
  onSelect: (modelId: string) => void;
  disabled?: boolean;
};

export function ModelSelector({
  models,
  selectedModel,
  onSelect,
  disabled = false,
}: ModelSelectorProps) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState("");

  const selectedModelData = models.find((m) => m.id === selectedModel);

  const filteredModels = models.filter((model) =>
    model.name.toLowerCase().includes(search.toLowerCase()) ||
    model.provider.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          disabled={disabled}
          className="w-[300px] justify-between"
        >
          <div className="flex items-center gap-2">
            <CpuIcon className="h-4 w-4" />
            <span className="truncate">
              {selectedModelData?.name || "Select model..."}
            </span>
          </div>
          <ChevronDownIcon className="h-4 w-4 opacity-50" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-[400px] p-0" align="start">
        <div className="p-2 border-b">
          <Input
            placeholder="Search models..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="h-8"
          />
        </div>
        <ScrollArea className="h-[400px]">
          {filteredModels.length === 0 ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              No models found.
            </div>
          ) : (
            filteredModels.map((model) => (
              <ModelItem
                key={model.id}
                model={model}
                isSelected={model.id === selectedModel}
                onSelect={() => {
                  onSelect(model.id);
                  setOpen(false);
                }}
              />
            ))
          )}
        </ScrollArea>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function ModelItem({
  model,
  isSelected,
  onSelect,
}: {
  model: ModelInfo;
  isSelected: boolean;
  onSelect: () => void;
}) {
  return (
    <DropdownMenuItem
      onSelect={onSelect}
      className="flex flex-col items-start p-3 cursor-pointer"
    >
      <div className="flex items-center w-full">
        <div className="flex items-center gap-2 flex-1">
          <div className="flex items-center gap-2">
            {isSelected && <CheckIcon className="h-4 w-4 text-green-500" />}
            <span className="font-medium">{model.name}</span>
          </div>
          <span className="text-xs text-muted-foreground">
            {model.provider}
          </span>
        </div>
        <div className="flex gap-1">
          {model.supportsThinking && (
            <span className="text-xs bg-purple-100 text-purple-700 px-1.5 py-0.5 rounded">
              Thinking
            </span>
          )}
          {model.supportsImages && (
            <span className="text-xs bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded">
              Vision
            </span>
          )}
          {model.supportsTools && (
            <span className="text-xs bg-green-100 text-green-700 px-1.5 py-0.5 rounded">
              Tools
            </span>
          )}
        </div>
      </div>
      <div className="text-xs text-muted-foreground mt-1">
        {model.description}
      </div>
      <div className="flex items-center gap-3 mt-1 text-xs text-muted-foreground">
        <span>Max: {model.maxTokens.toLocaleString()} tokens</span>
        <span>${model.costPer1kTokens}/1K tokens</span>
      </div>
    </DropdownMenuItem>
  );
}
