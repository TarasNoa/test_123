/**
 * Product Onboarding Layer
 * 
 * Implements guided onboarding, AI walkthrough, and progressive disclosure:
 * - First-run experience (less than 1 minute)
 * - Guided tour of AI orchestration workflow
 * - Progressive disclosure of features
 * - Smart hints based on context
 * - AI walkthrough assistance
 * 
 * Execution graph is a new concept - users need onboarding
 */

export interface OnboardingStep {
  id: string;
  title: string;
  description: string;
  target?: string; // CSS selector for target element
  action?: () => void;
  skipable: boolean;
}

export interface OnboardingTour {
  id: string;
  name: string;
  description: string;
  steps: OnboardingStep[];
  required: boolean;
}

export class OnboardingManager {
  private tours: Map<string, OnboardingTour> = new Map();
  private completedTours: Set<string> = new Set();
  private currentTour: OnboardingTour | null = null;
  private currentStepIndex = 0;
  private enabled = true;

  constructor() {
    this.loadCompletedTours();
    this.registerDefaultTours();
  }

  /**
   * Load completed tours from localStorage
   */
  private loadCompletedTours(): void {
    try {
      const stored = localStorage.getItem("onboarding_completed");
      if (stored) {
        this.completedTours = new Set(JSON.parse(stored));
      }
    } catch (error) {
      console.error("Failed to load completed tours:", error);
    }
  }

  /**
   * Save completed tours to localStorage
   */
  private saveCompletedTours(): void {
    try {
      localStorage.setItem(
        "onboarding_completed",
        JSON.stringify(Array.from(this.completedTours))
      );
    } catch (error) {
      console.error("Failed to save completed tours:", error);
    }
  }

  /**
   * Register default onboarding tours
   */
  private registerDefaultTours(): void {
    // First-run tour
    this.register({
      id: "first-run",
      name: "Welcome to Libr4",
      description: "Quick introduction to AI orchestration workspace",
      required: true,
      steps: [
        {
          id: "welcome",
          title: "Welcome to Libr4",
          description: "AI-native workspace for orchestration, collaboration, and execution. Let's show you around in under a minute.",
          skipable: false,
        },
        {
          id: "execution-graph",
          title: "Execution Graph",
          description: "This is your execution graph - it shows how AI agents plan and execute work. Nodes are tasks, edges show dependencies.",
          target: "[data-tour='execution-graph']",
          skipable: false,
        },
        {
          id: "ai-panel",
          title: "AI Intelligence Panel",
          description: "The AI panel shows reasoning, recommendations, risks, and opportunities. It's not a chat - it's intelligence.",
          target: "[data-tour='ai-panel']",
          skipable: false,
        },
        {
          id: "timeline",
          title: "Workspace Timeline",
          description: "Real-time timeline of all workspace events - builds, deployments, agent activities, and more.",
          target: "[data-tour='timeline']",
          skipable: false,
        },
        {
          id: "multi-agent",
          title: "Multi-Agent Visualization",
          description: "See how AI agents collaborate - handoffs, coordination, and execution flow.",
          target: "[data-tour='multi-agent']",
          skipable: false,
        },
        {
          id: "focus-modes",
          title: "Focus Modes",
          description: "Switch between Planning, Coding, Collaboration, and Intelligence modes to reduce cognitive load.",
          target: "[data-tour='focus-modes']",
          skipable: false,
        },
        {
          id: "complete",
          title: "You're Ready!",
          description: "Create a project to start. AI will analyze scope, generate execution graph, and assist with orchestration.",
          skipable: false,
        },
      ],
    });

    // Execution graph tour
    this.register({
      id: "execution-graph",
      name: "Understanding Execution Graph",
      description: "Learn how the execution graph works",
      required: false,
      steps: [
        {
          id: "graph-basics",
          title: "Graph Basics",
          description: "Nodes represent tasks or milestones. Edges show dependencies and flow. Colors indicate status (green=complete, yellow=in-progress, red=failed).",
          target: "[data-tour='execution-graph']",
          skipable: true,
        },
        {
          id: "graph-interaction",
          title: "Interacting with Graph",
          description: "Click nodes to see details. Drag to rearrange. The graph updates in real-time as work progresses.",
          target: "[data-tour='execution-graph']",
          skipable: true,
        },
        {
          id: "graph-ai",
          title: "AI and Graph",
          description: "AI automatically generates and updates the execution graph based on project scope and progress.",
          target: "[data-tour='execution-graph']",
          skipable: true,
        },
      ],
    });

    // Keyboard shortcuts tour
    this.register({
      id: "keyboard-shortcuts",
      name: "Keyboard Shortcuts",
      description: "Learn essential keyboard shortcuts",
      required: false,
      steps: [
        {
          id: "cmd-k",
          title: "Command Palette",
          description: "Press Cmd+K to open the command palette for quick access to all commands.",
          skipable: true,
        },
        {
          id: "cmd-p",
          title: "Quick Navigation",
          description: "Press Cmd+P for quick file and workspace navigation.",
          skipable: true,
        },
        {
          id: "focus-modes",
          title: "Focus Mode Shortcuts",
          description: "Cmd+Shift+P for Planning, Cmd+Shift+C for Coding, Cmd+Shift+O for Collaboration, Cmd+Shift+I for Intelligence.",
          skipable: true,
        },
      ],
    });
  }

  /**
   * Register onboarding tour
   */
  register(tour: OnboardingTour): void {
    this.tours.set(tour.id, tour);
  }

  /**
   * Start onboarding tour
   */
  startTour(tourId: string): void {
    const tour = this.tours.get(tourId);
    if (!tour) return;

    this.currentTour = tour;
    this.currentStepIndex = 0;
    this.showCurrentStep();
  }

  /**
   * Show current onboarding step
   */
  private showCurrentStep(): void {
    if (!this.currentTour) return;

    const step = this.currentTour.steps[this.currentStepIndex];
    
    // Emit event for UI to show tooltip/highlight
    const event = new CustomEvent("onboarding:step", {
      detail: {
        tour: this.currentTour,
        step,
        stepIndex: this.currentStepIndex,
        totalSteps: this.currentTour.steps.length,
      },
    });
    window.dispatchEvent(event);
  }

  /**
   * Next onboarding step
   */
  nextStep(): void {
    if (!this.currentTour) return;

    this.currentStepIndex++;
    if (this.currentStepIndex >= this.currentTour.steps.length) {
      this.completeTour(this.currentTour.id);
    } else {
      this.showCurrentStep();
    }
  }

  /**
   * Previous onboarding step
   */
  previousStep(): void {
    if (!this.currentTour) return;

    this.currentStepIndex = Math.max(0, this.currentStepIndex - 1);
    this.showCurrentStep();
  }

  /**
   * Skip onboarding tour
   */
  skipTour(): void {
    if (!this.currentTour) return;

    if (this.currentTour.steps[this.currentStepIndex].skipable) {
      this.completeTour(this.currentTour.id);
    }
  }

  /**
   * Complete onboarding tour
   */
  completeTour(tourId: string): void {
    this.completedTours.add(tourId);
    this.saveCompletedTours();
    this.currentTour = null;
    this.currentStepIndex = 0;

    // Emit completion event
    const event = new CustomEvent("onboarding:complete", {
      detail: { tourId },
    });
    window.dispatchEvent(event);
  }

  /**
   * Check if tour is completed
   */
  isTourCompleted(tourId: string): boolean {
    return this.completedTours.has(tourId);
  }

  /**
   * Get pending required tours
   */
  getPendingRequiredTours(): OnboardingTour[] {
    return Array.from(this.tours.values()).filter(
      tour => tour.required && !this.completedTours.has(tour.id)
    );
  }

  /**
   * Get all tours
   */
  getAllTours(): OnboardingTour[] {
    return Array.from(this.tours.values());
  }

  /**
   * Reset all onboarding progress
   */
  resetProgress(): void {
    this.completedTours.clear();
    this.saveCompletedTours();
  }

  /**
   * Enable onboarding
   */
  enable(): void {
    this.enabled = true;
  }

  /**
   * Disable onboarding
   */
  disable(): void {
    this.enabled = false;
  }

  /**
   * Check if onboarding is enabled
   */
  isEnabled(): boolean {
    return this.enabled;
  }

  /**
   * Get current tour
   */
  getCurrentTour(): OnboardingTour | null {
    return this.currentTour;
  }

  /**
   * Get current step
   */
  getCurrentStep(): OnboardingStep | null {
    if (!this.currentTour) return null;
    return this.currentTour.steps[this.currentStepIndex];
  }
}

// Global onboarding manager instance
export const onboardingManager = new OnboardingManager();

/**
 * Onboarding manager helpers
 */
export function startOnboardingTour(tourId: string): void {
  onboardingManager.startTour(tourId);
}

export function nextOnboardingStep(): void {
  onboardingManager.nextStep();
}

export function previousOnboardingStep(): void {
  onboardingManager.previousStep();
}

export function skipOnboardingTour(): void {
  onboardingManager.skipTour();
}

export function isOnboardingCompleted(tourId: string): boolean {
  return onboardingManager.isTourCompleted(tourId);
}

export function getPendingRequiredTours(): OnboardingTour[] {
  return onboardingManager.getPendingRequiredTours();
}

export function resetOnboardingProgress(): void {
  onboardingManager.resetProgress();
}

export function enableOnboarding(): void {
  onboardingManager.enable();
}

export function disableOnboarding(): void {
  onboardingManager.disable();
}
