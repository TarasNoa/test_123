import { createSignal, Show, For, onMount, onCleanup, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { apiClient, type VerificationStatusDto } from '../../lib/api-client';
import { config } from '../../lib/config';

type VerificationStep = 'upload' | 'processing' | 'result' | 'retry';
type DocumentType = 'passport' | 'selfie' | 'additional';

const VerificationPage: Component = () => {
  const navigate = useNavigate();
  
  // State
  const [step, setStep] = createSignal<VerificationStep>('upload');
  const [cvFile, setCvFile] = createSignal<File | null>(null);
  const [linkedInUrl, setLinkedInUrl] = createSignal('');
  const [documents, setDocuments] = createSignal<Record<DocumentType, File | null>>({
    passport: null,
    selfie: null,
    additional: null
  });
  const [uploadProgress, setUploadProgress] = createSignal(0);
  const [verificationStatus, setVerificationStatus] = createSignal<VerificationStatusDto | null>(null);
  const [error, setError] = createSignal('');
  const [processingStage, setProcessingStage] = createSignal(0);
  const [analysisLogs, setAnalysisLogs] = createSignal<string[]>([]);
  
  // Processing stages for animation
  const stages = [
    { name: 'Uploading documents...', icon: 'cloud-upload' },
    { name: 'Analyzing CV...', icon: 'document-text' },
    { name: 'Verifying LinkedIn profile...', icon: 'link' },
    { name: 'Scanning passport...', icon: 'id-card' },
    { name: 'Face recognition...', icon: 'user' },
    { name: 'Liveness detection...', icon: 'eye' },
    { name: 'Cross-referencing data...', icon: 'search' },
    { name: 'AI consensus building...', icon: 'cpu' },
    { name: 'Final verification...', icon: 'check-circle' }
  ];
  
  let processingInterval: number;
  let logInterval: number;
  
  const analysisMessages = [
    "Extracting text from CV using OCR...",
    "Parsing LinkedIn profile data...",
    "Analyzing professional experience...",
    "Verifying skill certifications...",
    "Checking document authenticity markers...",
    "Performing facial geometry analysis...",
    "Comparing selfie with passport photo...",
    "Detecting digital manipulation...",
    "Running sanctions list check...",
    "Validating document security features...",
    "Cross-referencing with public records...",
    "Calculating trust score...",
    "AI agents voting on verification...",
    "Finalizing verification report..."
  ];
  
  onMount(async () => {
    // Check if already verified
    try {
      const status = await apiClient.getVerificationStatus();
      setVerificationStatus(status);
      
      if (status.isVerified) {
        navigate('/dashboard');
        return;
      }
      
      if (status.status === 'UnderReview' || status.status === 'Pending') {
        setStep('processing');
        startProcessingAnimation();
        pollStatus();
      }
    } catch {
      // Not started, stay on upload step
    }
  });
  
  onCleanup(() => {
    clearInterval(processingInterval);
    clearInterval(logInterval);
  });
  
  const startProcessingAnimation = () => {
    let stage = 0;
    processingInterval = window.setInterval(() => {
      stage = (stage + 1) % stages.length;
      setProcessingStage(stage);
    }, 2000);
    
    let msgIndex = 0;
    logInterval = window.setInterval(() => {
      const msg = analysisMessages[msgIndex % analysisMessages.length];
      setAnalysisLogs(prev => [...prev.slice(-4), `[${new Date().toLocaleTimeString()}] ${msg}`]);
      msgIndex++;
    }, 1500);
  };
  
  const pollStatus = async () => {
    const check = async () => {
      try {
        const status = await apiClient.getVerificationStatus();
        setVerificationStatus(status);
        
        if (status.status === 'Approved') {
          clearInterval(processingInterval);
          clearInterval(logInterval);
          setStep('result');
          setTimeout(() => navigate('/dashboard'), 3000);
        } else if (status.status === 'Rejected' || status.status === 'AdditionalInfoRequired') {
          clearInterval(processingInterval);
          clearInterval(logInterval);
          setStep('retry');
          setError(status.rejectionReason || 'Verification failed. Please upload clearer documents.');
        }
      } catch {
        // Continue polling
      }
    };
    
    // Poll every 3 seconds
    const pollId = setInterval(check, 3000);
    
    // Stop polling after 5 minutes
    setTimeout(() => clearInterval(pollId), 5 * 60 * 1000);
  };
  
  const handleFileSelect = (type: DocumentType, file: File) => {
    setDocuments(prev => ({ ...prev, [type]: file }));
  };
  
  const handleUpload = async () => {
    setError('');
    
    // Validation
    if (!cvFile()) {
      setError('Please upload your CV');
      return;
    }
    if (!linkedInUrl()) {
      setError('Please provide your LinkedIn URL');
      return;
    }
    if (!documents().passport || !documents().selfie) {
      setError('Please upload passport and selfie photos');
      return;
    }
    
    try {
      setStep('processing');
      startProcessingAnimation();
      
      // Upload CV
      setUploadProgress(10);
      await apiClient.uploadCvWithLinkedIn(cvFile()!, linkedInUrl());
      
      // Upload documents
      setUploadProgress(30);
      await apiClient.uploadVerificationDocuments(
        documents().passport!,
        documents().selfie!,
        documents().additional
      );
      
      setUploadProgress(50);
      
      // Trigger verification
      await apiClient.triggerVerification();
      
      setUploadProgress(100);
      
      // Start polling for result
      pollStatus();
      
    } catch (err: any) {
      setError(err.message || 'Upload failed. Please try again.');
      setStep('upload');
    }
  };
  
  const handleRetry = () => {
    setStep('upload');
    setError('');
    setDocuments({ passport: null, selfie: null, additional: null });
    setCvFile(null);
    setLinkedInUrl('');
    setAnalysisLogs([]);
  };
  
  // UI Components
  const UploadBox: Component<{ 
    type: DocumentType; 
    label: string; 
    accept?: string;
    icon: string;
  }> = (props) => {
    const file = () => documents()[props.type];
    
    return (
      <div class={[
        "border-2 border-dashed rounded-xl p-6 text-center transition-all cursor-pointer",
        file() ? "border-[#35E0D0] bg-[#35E0D0]/5" : "border-white/20 hover:border-white/40"
      ].join(" ")}>
        <input
          type="file"
          accept={props.accept || "image/*,.pdf"}
          class="hidden"
          id={`file-${props.type}`}
          onChange={e => {
            const f = e.currentTarget.files?.[0];
            if (f) handleFileSelect(props.type, f);
          }}
        />
        <label for={`file-${props.type}`} class="cursor-pointer block">
          <div class="text-4xl mb-2">{props.icon}</div>
          <p class="text-sm font-medium">{file()?.name || props.label}</p>
          <p class="text-xs text-muted-foreground mt-1">
            {file() ? 'Click to change' : 'Click or drag to upload'}
          </p>
        </label>
      </div>
    );
  };
  
  return (
    <div class="min-h-screen bg-[#05050a] text-foreground flex items-center justify-center p-4">
      <div class="w-full max-w-2xl">
        {/* Header */}
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold mb-2">Identity Verification</h1>
          <p class="text-muted-foreground">
            Complete verification to access the platform
          </p>
        </div>
        
        {/* Error */}
        <Show when={error()}>
          <div class="bg-red-500/10 border border-red-500/30 text-red-400 px-4 py-3 rounded-xl mb-6">
            {error()}
          </div>
        </Show>
        
        {/* Upload Step */}
        <Show when={step() === 'upload'}>
          <div class="space-y-6">
            {/* CV Upload */}
            <div class="bg-surface-1/50 border border-white/10 rounded-2xl p-6">
              <h3 class="font-semibold mb-4 flex items-center gap-2">
                <span class="text-xl">📄</span> CV & LinkedIn
              </h3>
              <div class="space-y-4">
                <div class="border-2 border-dashed border-white/20 rounded-xl p-6 text-center hover:border-white/40 transition-all">
                  <input
                    type="file"
                    accept=".pdf,.doc,.docx"
                    class="hidden"
                    id="cv-file"
                    onChange={e => setCvFile(e.currentTarget.files?.[0] || null)}
                  />
                  <label for="cv-file" class="cursor-pointer block">
                    <div class="text-4xl mb-2">📄</div>
                    <p class="text-sm font-medium">{cvFile()?.name || 'Upload your CV (PDF, DOC, DOCX)'}</p>
                  </label>
                </div>
                <input
                  type="url"
                  placeholder="LinkedIn Profile URL (https://linkedin.com/in/...)"
                  value={linkedInUrl()}
                  onInput={e => setLinkedInUrl(e.currentTarget.value)}
                  class="w-full px-4 py-3 bg-surface-2/60 border border-surface-3/60 rounded-xl text-sm"
                />
              </div>
            </div>
            
            {/* Identity Documents */}
            <div class="bg-surface-1/50 border border-white/10 rounded-2xl p-6">
              <h3 class="font-semibold mb-4 flex items-center gap-2">
                <span class="text-xl">🆔</span> Identity Documents
              </h3>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <UploadBox type="passport" label="Passport (Photo page)" icon="🛂" />
                <UploadBox type="selfie" label="Selfie with Passport" icon="🤳" />
              </div>
              <div class="mt-4">
                <UploadBox 
                  type="additional" 
                  label="Additional Document (Optional)" 
                  icon="📎"
                />
              </div>
              <p class="text-xs text-muted-foreground mt-4">
                Photos must be clear, well-lit, and show all document details. 
                For selfie, hold passport next to your face.
              </p>
            </div>
            
            {/* Submit */}
            <button
              onClick={handleUpload}
              class="w-full py-4 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 transition-all"
            >
              Start Verification
            </button>
          </div>
        </Show>
        
        {/* Processing Step */}
        <Show when={step() === 'processing'}>
          <div class="bg-surface-1/50 border border-white/10 rounded-2xl p-8 text-center">
            {/* Animated rings */}
            <div class="relative w-48 h-48 mx-auto mb-8">
              {/* Outer ring */}
              <div class="absolute inset-0 rounded-full border-4 border-[#35E0D0]/20 animate-ping" style="animation-duration: 2s" />
              {/* Middle ring */}
              <div class="absolute inset-4 rounded-full border-4 border-[#35E0D0]/40 animate-pulse" />
              {/* Inner ring */}
              <div class="absolute inset-8 rounded-full border-4 border-t-[#35E0D0] border-r-transparent border-b-[#35E0D0]/50 border-l-transparent animate-spin" />
              {/* Center icon */}
              <div class="absolute inset-0 flex items-center justify-center">
                <span class="text-4xl">🔐</span>
              </div>
            </div>
            
            {/* Stage indicator */}
            <div class="mb-6">
              <p class="text-lg font-medium text-[#35E0D0] mb-2">
                {stages[processingStage()].name}
              </p>
              <div class="w-full bg-white/10 rounded-full h-2 overflow-hidden">
                <div 
                  class="h-full bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] transition-all duration-500"
                  style={`width: ${((processingStage() + 1) / stages.length) * 100}%`}
                />
              </div>
            </div>
            
            {/* AI Analysis Logs */}
            <div class="bg-black/30 rounded-xl p-4 text-left font-mono text-xs space-y-1 max-h-40 overflow-hidden">
              <For each={analysisLogs()}>
                {(log) => <div class="text-[#35E0D0]/70">{log}</div>}
              </For>
              <div class="animate-pulse text-[#35E0D0]">▋</div>
            </div>
            
            <p class="text-sm text-muted-foreground mt-6">
              AI agents are analyzing your documents. This usually takes 1-2 minutes.
            </p>
          </div>
        </Show>
        
        {/* Result Step */}
        <Show when={step() === 'result'}>
          <div class="bg-surface-1/50 border border-[#35E0D0]/30 rounded-2xl p-8 text-center">
            <div class="w-24 h-24 mx-auto mb-6 rounded-full bg-[#35E0D0]/20 flex items-center justify-center">
              <span class="text-5xl">✅</span>
            </div>
            <h2 class="text-2xl font-bold mb-2">Verification Complete!</h2>
            <p class="text-muted-foreground mb-6">
              Your identity has been verified. Redirecting to dashboard...
            </p>
            <div class="w-full bg-white/10 rounded-full h-2 overflow-hidden">
              <div class="h-full bg-[#35E0D0] animate-pulse" style="width: 100%" />
            </div>
          </div>
        </Show>
        
        {/* Retry Step */}
        <Show when={step() === 'retry'}>
          <div class="bg-surface-1/50 border border-red-500/30 rounded-2xl p-8 text-center">
            <div class="w-24 h-24 mx-auto mb-6 rounded-full bg-red-500/20 flex items-center justify-center">
              <span class="text-5xl">⚠️</span>
            </div>
            <h2 class="text-2xl font-bold mb-2">Verification Failed</h2>
            <p class="text-muted-foreground mb-6">
              {error() || 'We could not verify your identity with the provided documents.'}
            </p>
            <div class="space-y-3">
              <button
                onClick={handleRetry}
                class="w-full py-4 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl hover:opacity-90 transition-all"
              >
                Try Again
              </button>
              <button
                onClick={() => navigate('/auth')}
                class="w-full py-3 border border-white/20 rounded-xl hover:bg-white/5 transition-all"
              >
                Back to Login
              </button>
            </div>
          </div>
        </Show>
      </div>
    </div>
  );
};

export default VerificationPage;
