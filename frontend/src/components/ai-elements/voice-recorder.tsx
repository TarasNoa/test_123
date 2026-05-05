'use client';

import React, { useState, useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { 
  Mic, 
  MicOff, 
  Square,
  Play,
  Trash2
} from 'lucide-react';

export type RecordingStatus = 'idle' | 'recording' | 'paused' | 'processing';

interface VoiceRecorderProps {
  onTranscribe?: (audioBlob: Blob) => Promise<string>;
  onTextChange?: (text: string) => void;
}

export function VoiceRecorder({ onTranscribe, onTextChange }: VoiceRecorderProps) {
  const [status, setStatus] = useState<RecordingStatus>('idle');
  const [duration, setDuration] = useState(0);
  const [transcribedText, setTranscribedText] = useState('');
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);

  const formatDuration = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream);
      
      mediaRecorder.ondataavailable = (e) => {
        chunksRef.current.push(e.data);
      };

      mediaRecorder.onstop = async () => {
        const audioBlob = new Blob(chunksRef.current, { type: 'audio/webm' });
        chunksRef.current = [];
        
        setStatus('processing');
        
        try {
          const text = await onTranscribe?.(audioBlob);
          if (text) {
            setTranscribedText(text);
            onTextChange?.(text);
          }
          setStatus('idle');
        } catch (error) {
          console.error('Transcription failed:', error);
          setStatus('idle');
        }
      };

      mediaRecorderRef.current = mediaRecorder;
      mediaRecorder.start();
      setStatus('recording');
      
      timerRef.current = setInterval(() => {
        setDuration(prev => prev + 1);
      }, 1000);
    } catch (error) {
      console.error('Failed to access microphone:', error);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
      mediaRecorderRef.current.stop();
      mediaRecorderRef.current.stream.getTracks().forEach(track => track.stop());
    }
    
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
  };

  const pauseRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
      mediaRecorderRef.current.pause();
      setStatus('paused');
      if (timerRef.current) {
        clearInterval(timerRef.current);
      }
    }
  };

  const resumeRecording = () => {
    if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'paused') {
      mediaRecorderRef.current.resume();
      setStatus('recording');
      timerRef.current = setInterval(() => {
        setDuration(prev => prev + 1);
      }, 1000);
    }
  };

  const resetRecording = () => {
    stopRecording();
    setDuration(0);
    setTranscribedText('');
  };

  const getStatusBadge = () => {
    switch (status) {
      case 'recording': return <Badge className="bg-red-500">Recording</Badge>;
      case 'paused': return <Badge variant="secondary">Paused</Badge>;
      case 'processing': return <Badge className="bg-yellow-500">Processing</Badge>;
      default: return <Badge variant="outline">Idle</Badge>;
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Mic className="w-5 h-5" />
          Voice Input
          {getStatusBadge()}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {/* Duration Display */}
          {status !== 'idle' && (
            <div className="text-center">
              <span className="text-3xl font-mono">{formatDuration(duration)}</span>
            </div>
          )}

          {/* Controls */}
          <div className="flex justify-center gap-2">
            {status === 'idle' && (
              <Button onClick={startRecording} size="lg">
                <Mic className="w-5 h-5 mr-2" />
                Start Recording
              </Button>
            )}
            
            {status === 'recording' && (
              <>
                <Button onClick={pauseRecording} variant="outline" size="lg">
                  <Square className="w-5 h-5 mr-2" />
                  Pause
                </Button>
                <Button onClick={stopRecording} variant="destructive" size="lg">
                  <MicOff className="w-5 h-5 mr-2" />
                  Stop
                </Button>
              </>
            )}
            
            {status === 'paused' && (
              <>
                <Button onClick={resumeRecording} size="lg">
                  <Play className="w-5 h-5 mr-2" />
                  Resume
                </Button>
                <Button onClick={stopRecording} variant="destructive" size="lg">
                  <MicOff className="w-5 h-5 mr-2" />
                  Stop
                </Button>
              </>
            )}
            
            {status === 'processing' && (
              <Button disabled size="lg">
                Processing...
              </Button>
            )}
          </div>

          {/* Reset Button */}
          {(status !== 'idle' || transcribedText) && (
            <div className="flex justify-center">
              <Button variant="ghost" onClick={resetRecording}>
                <Trash2 className="w-4 h-4 mr-2" />
                Reset
              </Button>
            </div>
          )}

          {/* Transcribed Text */}
          {transcribedText && (
            <div className="p-3 bg-muted rounded-lg">
              <p className="text-sm">{transcribedText}</p>
            </div>
          )}

          {/* Info */}
          <p className="text-xs text-muted-foreground text-center">
            Voice input uses your browser's speech recognition. Ensure microphone access is enabled.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}

export default VoiceRecorder;
