// Audio Compression Module
pub mod compression {
    /// Compress audio data using simple RLE (Run-Length Encoding)
    /// For production, would use actual audio codecs like FLAC, Opus, etc.
    pub fn compress_audio(audio_data: &[u8]) -> Vec<u8> {
        if audio_data.is_empty() {
            return Vec::new();
        }

        let mut compressed = Vec::new();
        let mut current = audio_data[0];
        let mut count = 1u8;

        for &byte in &audio_data[1..] {
            if byte == current && count < 255 {
                count += 1;
            } else {
                compressed.push(current);
                compressed.push(count);
                current = byte;
                count = 1;
            }
        }

        compressed.push(current);
        compressed.push(count);
        compressed
    }

    /// Decompress audio data using RLE
    pub fn decompress_audio(compressed_data: &[u8]) -> Vec<u8> {
        if compressed_data.is_empty() {
            return Vec::new();
        }

        let mut decompressed = Vec::new();
        let mut i = 0;

        while i < compressed_data.len() {
            let byte = compressed_data[i];
            let count = compressed_data[i + 1];
            
            for _ in 0..count {
                decompressed.push(byte);
            }
            
            i += 2;
        }

        decompressed
    }

    /// Calculate compression ratio
    pub fn compression_ratio(original_size: usize, compressed_size: usize) -> f64 {
        if original_size == 0 {
            0.0
        } else {
            (original_size as f64 - compressed_size as f64) / original_size as f64 * 100.0
        }
    }
}

// Audio Encoding/Decoding Module
pub mod codec {
    /// Encode audio samples to PCM format
    pub fn encode_pcm(samples: &[i16]) -> Vec<u8> {
        let mut encoded = Vec::with_capacity(samples.len() * 2);
        for &sample in samples {
            encoded.extend_from_slice(&sample.to_le_bytes());
        }
        encoded
    }

    /// Decode audio samples from PCM format
    pub fn decode_pcm(encoded: &[u8]) -> Vec<i16> {
        if encoded.len() % 2 != 0 {
            return Vec::new();
        }

        encoded
            .chunks_exact(2)
            .map(|chunk| i16::from_le_bytes([chunk[0], chunk[1]]))
            .collect()
    }

    /// Normalize audio samples to range [-1.0, 1.0]
    pub fn normalize_samples(samples: &[i16]) -> Vec<f32> {
        samples
            .iter()
            .map(|&s| s as f32 / 32768.0)
            .collect()
    }

    /// Convert normalized samples back to i16
    pub fn denormalize_samples(normalized: &[f32]) -> Vec<i16> {
        normalized
            .iter()
            .map(|&n| (n * 32768.0).clamp(-32768.0, 32767.0) as i16)
            .collect()
    }
}

// Audio Analysis Module
pub mod analysis {
    /// Calculate RMS (Root Mean Square) amplitude
    pub fn calculate_rms(samples: &[i16]) -> f64 {
        if samples.is_empty() {
            return 0.0;
        }

        let sum_sq: f64 = samples
            .iter()
            .map(|&s| (s as f64) * (s as f64))
            .sum();

        (sum_sq / samples.len() as f64).sqrt()
    }

    /// Detect silence in audio samples
    pub fn detect_silence(samples: &[i16], threshold: i16) -> Vec<(usize, usize)> {
        let mut silence_regions = Vec::new();
        let mut in_silence = false;
        let mut start = 0;

        for (i, &sample) in samples.iter().enumerate() {
            if sample.abs() < threshold {
                if !in_silence {
                    in_silence = true;
                    start = i;
                }
            } else {
                if in_silence {
                    silence_regions.push((start, i));
                    in_silence = false;
                }
            }
        }

        if in_silence {
            silence_regions.push((start, samples.len()));
        }

        silence_regions
    }

    /// Calculate audio duration in seconds
    pub fn calculate_duration(sample_count: usize, sample_rate: u32) -> f64 {
        sample_count as f64 / sample_rate as f64
    }
}

// Audio Format Conversion Module
pub mod conversion {
    /// Convert sample rate using simple linear interpolation
    pub fn resample(samples: &[i16], from_rate: u32, to_rate: u32) -> Vec<i16> {
        if from_rate == to_rate {
            return samples.to_vec();
        }

        let ratio = from_rate as f64 / to_rate as f64;
        let new_length = (samples.len() as f64 / ratio).ceil() as usize;
        let mut resampled = Vec::with_capacity(new_length);

        for i in 0..new_length {
            let src_pos = i as f64 * ratio;
            let idx = src_pos as usize;
            let frac = src_pos - idx as f64;

            if idx < samples.len() - 1 {
                let val = samples[idx] as f64 * (1.0 - frac) + samples[idx + 1] as f64 * frac;
                resampled.push(val as i16);
            } else {
                resampled.push(samples[samples.len() - 1]);
            }
        }

        resampled
    }

    /// Convert stereo to mono by averaging channels
    pub fn stereo_to_mono(stereo_samples: &[i16]) -> Vec<i16> {
        stereo_samples
            .chunks_exact(2)
            .map(|chunk| ((chunk[0] as i32 + chunk[1] as i32) / 2) as i16)
            .collect()
    }
}

// Audio Metadata Module
pub mod metadata {
    #[derive(Debug, Clone)]
    pub struct AudioMetadata {
        pub sample_rate: u32,
        pub channels: u8,
        pub bits_per_sample: u8,
        pub duration: f64,
        pub format: String,
    }

    /// Parse audio metadata from header (simplified)
    pub fn parse_metadata(data: &[u8]) -> Option<AudioMetadata> {
        if data.len() < 44 {
            return None;
        }

        // This is a simplified WAV header parser
        // In production, would use proper audio libraries
        let sample_rate = u32::from_le_bytes([data[24], data[25], data[26], data[27]]);
        let channels = data[22];
        let bits_per_sample = data[34];
        let duration = (data.len() - 44) as f64 / (sample_rate as f64 * channels as f64 * (bits_per_sample / 8) as f64);

        Some(AudioMetadata {
            sample_rate,
            channels,
            bits_per_sample,
            duration,
            format: "WAV".to_string(),
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_compression() {
        let data = vec![1u8, 1, 1, 2, 2, 3, 3, 3, 3];
        let compressed = compression::compress_audio(&data);
        let decompressed = compression::decompress_audio(&compressed);
        assert_eq!(data, decompressed);
    }

    #[test]
    fn test_pcm_codec() {
        let samples = vec![100i16, 200, -100, -200];
        let encoded = codec::encode_pcm(&samples);
        let decoded = codec::decode_pcm(&encoded);
        assert_eq!(samples, decoded);
    }

    #[test]
    fn test_rms_calculation() {
        let samples = vec![100i16, 200, 300];
        let rms = analysis::calculate_rms(&samples);
        assert!(rms > 0.0);
    }

    #[test]
    fn test_silence_detection() {
        let samples = vec![0i16, 0, 0, 100, 200, 0, 0];
        let silence = analysis::detect_silence(&samples, 10);
        assert!(!silence.is_empty());
    }
}
