const TABS = [
  { id: 'code', label: 'Code' },
  { id: 'agents', label: 'Agents' },
  { id: 'execution', label: 'Execution' },
  { id: 'multi-agent', label: 'Multi-Agent' },
];

interface IDETabsProps {
  activeTab: string;
  onTabChange: (tab: string) => void;
}

export function IDETabs(props: IDETabsProps) {
  return (
    <div class="ide-tabs">
      {TABS.map((tab) => (
        <button
          class={`ide-tab ${props.activeTab === tab.id ? 'active' : ''}`}
          onClick={() => props.onTabChange(tab.id)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
