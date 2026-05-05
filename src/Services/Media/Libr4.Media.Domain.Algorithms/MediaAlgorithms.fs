namespace Libr4.Media.Domain.Algorithms

open System
open System.Text.Json
open Libr4.AI.Infrastructure.AI

/// Audio analysis algorithms with AI integration
module AudioAnalyzer =
    
    type AudioQuality = {
        OverallScore: float
        Clarity: float
        VolumeLevel: float
        NoiseLevel: float
        FrequencyBalance: float
        HasIssues: bool
        Issues: string list
    }
    
    type NoiseDetectionResult = {
        HasNoise: bool
        NoiseType: string option
        NoiseLevel: float
        AffectedSegments: int list
        RecommendedAction: string
    }
    
    type AudioOptimizationResult = {
        RecommendedBitrate: int
        RecommendedSampleRate: int
        RecommendedChannels: int
        CompressionLevel: string
        EstimatedSizeReduction: float
        QualityImpact: string
    }
    
    type FormatRecommendation = {
        CurrentFormat: string
        RecommendedFormat: string
        Reason: string
        Benefits: string list
        ConversionComplexity: string
    }
    
    /// Analyze audio quality with AI
    let analyzeAudioQualityWithAI (aiService: IAIService) (audioData: byte[]) (metadata: Map<string, string>) =
        async {
            let prompt = "Analyze the audio quality based on the provided metadata and characteristics.\nReturn a JSON response with the following structure:\n{\"overallScore\": number (0-100), \"clarity\": number (0-100), \"volumeLevel\": number (0-100), \"noiseLevel\": number (0-100), \"frequencyBalance\": number (0-100), \"hasIssues\": boolean, \"issues\": string[]}\nConsider factors like:\n- Bitrate and sample rate from metadata\n- Dynamic range\n- Frequency response\n- Distortion or clipping\n- Background noise"
            
            let context = JsonSerializer.Serialize(metadata)
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, context)
                let root = JsonDocument.Parse(response).RootElement
                
                let overallScore = try root.GetProperty("overallScore").GetDouble() with _ -> 75.0
                let clarity = try root.GetProperty("clarity").GetDouble() with _ -> 75.0
                let volumeLevel = try root.GetProperty("volumeLevel").GetDouble() with _ -> 75.0
                let noiseLevel = try root.GetProperty("noiseLevel").GetDouble() with _ -> 25.0
                let frequencyBalance = try root.GetProperty("frequencyBalance").GetDouble() with _ -> 75.0
                let hasIssues = try root.GetProperty("hasIssues").GetBoolean() with _ -> false
                let issues = try root.GetProperty("issues").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                return {
                    OverallScore = overallScore
                    Clarity = clarity
                    VolumeLevel = volumeLevel
                    NoiseLevel = noiseLevel
                    FrequencyBalance = frequencyBalance
                    HasIssues = hasIssues
                    Issues = issues
                }
            with ex ->
                let bitrate = metadata |> Map.tryFind "bitrate" |> Option.map (fun b -> match Int32.TryParse b with true, v -> v | _ -> 128000) |> Option.defaultValue 128000
                let sampleRate = metadata |> Map.tryFind "sampleRate" |> Option.map (fun s -> match Int32.TryParse s with true, v -> v | _ -> 44100) |> Option.defaultValue 44100
                
                let overallScore = min 100.0 (float bitrate / 1000.0 * 0.5 + float sampleRate / 1000.0 * 0.5)
                let hasIssues = bitrate < 64000 || sampleRate < 22050
                
                return {
                    OverallScore = overallScore
                    Clarity = overallScore * 0.9
                    VolumeLevel = 75.0
                    NoiseLevel = if hasIssues then 40.0 else 20.0
                    FrequencyBalance = overallScore * 0.85
                    HasIssues = hasIssues
                    Issues = if hasIssues then ["Low bitrate"; "Low sample rate"] else []
                }
        }
    
    /// Detect noise in audio with AI
    let detectNoiseWithAI (aiService: IAIService) (audioData: byte[]) (segments: float[][]) =
        async {
            let prompt = "Analyze the audio segments for noise detection.\nReturn a JSON response with the following structure:\n{\"hasNoise\": boolean, \"noiseType\": string or null, \"noiseLevel\": number (0-100), \"affectedSegments\": number[], \"recommendedAction\": string}\nConsider types of noise:\n- White noise\n- Hiss\n- Hum\n- Clicks and pops\n- Background sounds"
            
            let segmentInfo = segments |> Array.mapi (fun i s -> sprintf "Segment %d: %.2f" i (s |> Array.average))
            let context = String.Join("\n", segmentInfo)
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, context)
                let root = JsonDocument.Parse(response).RootElement
                
                let hasNoise = try root.GetProperty("hasNoise").GetBoolean() with _ -> false
                let noiseType = try Some (root.GetProperty("noiseType").GetString()) with _ -> None
                let noiseLevel = try root.GetProperty("noiseLevel").GetDouble() with _ -> 0.0
                let affectedSegments = try root.GetProperty("affectedSegments").EnumerateArray() |> Seq.map (fun i -> i.GetInt32()) |> List.ofSeq with _ -> []
                let recommendedAction = try root.GetProperty("recommendedAction").GetString() with _ -> "No action needed"
                
                return {
                    HasNoise = hasNoise
                    NoiseType = noiseType
                    NoiseLevel = noiseLevel
                    AffectedSegments = affectedSegments
                    RecommendedAction = recommendedAction
                }
            with ex ->
                let avgAmplitude = segments |> Array.map (Array.average) |> Array.average
                let hasNoise = avgAmplitude > 0.3
                let affectedSegments = segments |> Array.mapi (fun i s -> if Array.average s > 0.3 then i else -1) |> Array.filter ((<) 0) |> Array.toList
                
                return {
                    HasNoise = hasNoise
                    NoiseType = if hasNoise then Some "Background noise" else None
                    NoiseLevel = if hasNoise then 45.0 else 0.0
                    AffectedSegments = affectedSegments
                    RecommendedAction = if hasNoise then "Apply noise reduction filter" else "No action needed"
                }
        }
    
    /// Optimize audio settings with AI
    let optimizeAudioWithAI (aiService: IAIService) (currentBitrate: int) (currentSampleRate: int) (currentChannels: int) (targetUse: string) =
        async {
            let prompt = sprintf "Recommend optimal audio settings for: %s\n\nCurrent settings:\n- Bitrate: %d kbps\n- Sample rate: %d Hz\n- Channels: %d\n\nReturn a JSON response with the following structure:\n{\"recommendedBitrate\": number, \"recommendedSampleRate\": number, \"recommendedChannels\": number, \"compressionLevel\": string, \"estimatedSizeReduction\": number (percentage), \"qualityImpact\": string}\n\nConsider target use cases:\n- Music streaming\n- Podcast\n- Voice recording\n- Sound effects\n- Background music" targetUse (currentBitrate / 1000) currentSampleRate currentChannels
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let recommendedBitrate = try root.GetProperty("recommendedBitrate").GetInt32() with _ -> currentBitrate
                let recommendedSampleRate = try root.GetProperty("recommendedSampleRate").GetInt32() with _ -> currentSampleRate
                let recommendedChannels = try root.GetProperty("recommendedChannels").GetInt32() with _ -> currentChannels
                let compressionLevel = try root.GetProperty("compressionLevel").GetString() with _ -> "medium"
                let estimatedSizeReduction = try root.GetProperty("estimatedSizeReduction").GetDouble() with _ -> 0.0
                let qualityImpact = try root.GetProperty("qualityImpact").GetString() with _ -> "minimal"
                
                return {
                    RecommendedBitrate = recommendedBitrate
                    RecommendedSampleRate = recommendedSampleRate
                    RecommendedChannels = recommendedChannels
                    CompressionLevel = compressionLevel
                    EstimatedSizeReduction = estimatedSizeReduction
                    QualityImpact = qualityImpact
                }
            with ex ->
                let recommendedBitrate = 
                    match targetUse.ToLower() with
                    | s when s.Contains("music") -> 320000
                    | s when s.Contains("podcast") -> 128000
                    | s when s.Contains("voice") -> 64000
                    | _ -> 192000
                
                let estimatedSizeReduction = float (currentBitrate - recommendedBitrate) / float currentBitrate * 100.0
                
                return {
                    RecommendedBitrate = recommendedBitrate
                    RecommendedSampleRate = 44100
                    RecommendedChannels = 2
                    CompressionLevel = "medium"
                    EstimatedSizeReduction = estimatedSizeReduction
                    QualityImpact = if estimatedSizeReduction > 30.0 then "moderate" else "minimal"
                }
        }
    
    /// Recommend audio format with AI
    let recommendFormatWithAI (aiService: IAIService) (currentFormat: string) (targetPlatform: string) (content: string) =
        async {
            let prompt = sprintf "Recommend the best audio format for: %s\n\nCurrent format: %s\nContent type: %s\n\nReturn a JSON response with the following structure:\n{\"recommendedFormat\": string, \"reason\": string, \"benefits\": string[], \"conversionComplexity\": string}\n\nConsider formats:\n- MP3: Universal, good compression\n- AAC: Better quality at same bitrate\n- FLAC: Lossless, larger files\n- OGG: Open source, good compression\n- WAV: Uncompressed, largest files" targetPlatform currentFormat content
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let recommendedFormat = try root.GetProperty("recommendedFormat").GetString() with _ -> currentFormat
                let reason = try root.GetProperty("reason").GetString() with _ -> "Format is suitable"
                let benefits = try root.GetProperty("benefits").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                let conversionComplexity = try root.GetProperty("conversionComplexity").GetString() with _ -> "low"
                
                return {
                    CurrentFormat = currentFormat
                    RecommendedFormat = recommendedFormat
                    Reason = reason
                    Benefits = benefits
                    ConversionComplexity = conversionComplexity
                }
            with ex ->
                let recommendedFormat = 
                    match targetPlatform.ToLower() with
                    | s when s.Contains("web") || s.Contains("stream") -> "AAC"
                    | s when s.Contains("lossless") -> "FLAC"
                    | _ -> "MP3"
                
                return {
                    CurrentFormat = currentFormat
                    RecommendedFormat = recommendedFormat
                    Reason = "Optimized for target platform"
                    Benefits = ["Better compression"; "Wider compatibility"]
                    ConversionComplexity = "low"
                }
        }

/// 3D media analysis algorithms with AI integration
module Media3DAnalyzer =
    
    type ModelQuality = {
        OverallScore: float
        PolygonCount: int
        VertexCount: int
        TextureResolution: string
        HasUVs: bool
        HasNormals: bool
        OptimizationLevel: string
        Issues: string list
    }
    
    type OptimizationDetection = {
        NeedsOptimization: bool
        OptimizationType: string list
        PotentialSavings: float
        RecommendedActions: string list
    }
    
    type MeshOptimization = {
        TargetPolygonCount: int
        DecimationLevel: string
        LODLevels: int
        TextureCompression: string
        EstimatedSizeReduction: float
        QualityImpact: string
    }
    
    type ExportRecommendation = {
        RecommendedFormat: string
        ExportSettings: Map<string, string>
        Reason: string
        PlatformSupport: string list
    }
    
    /// Analyze 3D model quality with AI
    let analyzeModelQualityWithAI (aiService: IAIService) (polygonCount: int) (vertexCount: int) (textureResolution: string) (hasUVs: bool) (hasNormals: bool) =
        async {
            let prompt = sprintf "Analyze the 3D model quality based on the provided metrics.\n\nModel metrics:\n- Polygon count: %d\n- Vertex count: %d\n- Texture resolution: %s\n- Has UVs: %b\n- Has normals: %b\n\nReturn a JSON response with the following structure:\n{\"overallScore\": number (0-100), \"optimizationLevel\": string, \"issues\": string[]}\n\nConsider factors like:\n- Polygon count for target use (game, web, render)\n- Texture resolution quality\n- UV mapping completeness\n- Normal map quality" polygonCount vertexCount textureResolution hasUVs hasNormals
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let overallScore = try root.GetProperty("overallScore").GetDouble() with _ -> 75.0
                let optimizationLevel = try root.GetProperty("optimizationLevel").GetString() with _ -> "medium"
                let issues = try root.GetProperty("issues").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                return {
                    OverallScore = overallScore
                    PolygonCount = polygonCount
                    VertexCount = vertexCount
                    TextureResolution = textureResolution
                    HasUVs = hasUVs
                    HasNormals = hasNormals
                    OptimizationLevel = optimizationLevel
                    Issues = issues
                }
            with ex ->
                let overallScore = min 100.0 (float polygonCount / 10000.0 * 10.0)
                let optimizationLevel = 
                    if polygonCount > 100000 then "high"
                    elif polygonCount > 50000 then "medium"
                    else "low"
                
                let issues = 
                    [ if not hasUVs then "Missing UV mapping" else ""
                      if not hasNormals then "Missing normals" else ""
                      if polygonCount > 100000 then "High polygon count" else "" ]
                    |> List.filter (String.IsNullOrEmpty >> not)
                
                return {
                    OverallScore = overallScore
                    PolygonCount = polygonCount
                    VertexCount = vertexCount
                    TextureResolution = textureResolution
                    HasUVs = hasUVs
                    HasNormals = hasNormals
                    OptimizationLevel = optimizationLevel
                    Issues = issues
                }
        }
    
    /// Detect optimization opportunities with AI
    let detectOptimizationWithAI (aiService: IAIService) (currentPolygons: int) (targetUse: string) (platformConstraints: Map<string, string>) =
        async {
            let constraintsStr = String.Join(", ", platformConstraints |> Map.toList |> List.map (fun (k, v) -> sprintf "%s: %s" k v))
            let prompt = sprintf "Detect optimization opportunities for 3D model.\n\nCurrent state:\n- Polygon count: %d\n- Target use: %s\n- Platform constraints: %s\n\nReturn a JSON response with the following structure:\n{\"needsOptimization\": boolean, \"optimizationType\": string[], \"potentialSavings\": number (percentage), \"recommendedActions\": string[]}\n\nConsider optimization types:\n- Polygon reduction\n- Texture compression\n- LOD generation\n- Mesh simplification" currentPolygons targetUse constraintsStr
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let needsOptimization = try root.GetProperty("needsOptimization").GetBoolean() with _ -> false
                let optimizationType = try root.GetProperty("optimizationType").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                let potentialSavings = try root.GetProperty("potentialSavings").GetDouble() with _ -> 0.0
                let recommendedActions = try root.GetProperty("recommendedActions").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                return {
                    NeedsOptimization = needsOptimization
                    OptimizationType = optimizationType
                    PotentialSavings = potentialSavings
                    RecommendedActions = recommendedActions
                }
            with ex ->
                let needsOptimization = currentPolygons > 50000
                let optimizationType = if needsOptimization then ["Polygon reduction"; "LOD generation"] else []
                let potentialSavings = if needsOptimization then min 70.0 (float (currentPolygons - 20000) / float currentPolygons * 100.0) else 0.0
                let recommendedActions = if needsOptimization then ["Apply polygon reduction"; "Generate LOD levels"] else []
                
                return {
                    NeedsOptimization = needsOptimization
                    OptimizationType = optimizationType
                    PotentialSavings = potentialSavings
                    RecommendedActions = recommendedActions
                }
        }
    
    /// Optimize mesh with AI
    let optimizeMeshWithAI (aiService: IAIService) (currentPolygons: int) (targetPolygons: int) (preserveDetails: bool) =
        async {
            let prompt = sprintf "Recommend mesh optimization settings.\n\nCurrent polygon count: %d\nTarget polygon count: %d\nPreserve details: %b\n\nReturn a JSON response with the following structure:\n{\"decimationLevel\": string, \"lodLevels\": number, \"textureCompression\": string, \"qualityImpact\": string}" currentPolygons targetPolygons preserveDetails
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let decimationLevel = try root.GetProperty("decimationLevel").GetString() with _ -> "medium"
                let lodLevels = try root.GetProperty("lodLevels").GetInt32() with _ -> 3
                let textureCompression = try root.GetProperty("textureCompression").GetString() with _ -> "medium"
                let qualityImpact = try root.GetProperty("qualityImpact").GetString() with _ -> "minimal"
                
                return {
                    TargetPolygonCount = targetPolygons
                    DecimationLevel = decimationLevel
                    LODLevels = lodLevels
                    TextureCompression = textureCompression
                    EstimatedSizeReduction = float (currentPolygons - targetPolygons) / float currentPolygons * 100.0
                    QualityImpact = qualityImpact
                }
            with ex ->
                let decimationLevel = 
                    if float targetPolygons / float currentPolygons < 0.5 then "high"
                    elif float targetPolygons / float currentPolygons < 0.8 then "medium"
                    else "low"
                
                return {
                    TargetPolygonCount = targetPolygons
                    DecimationLevel = decimationLevel
                    LODLevels = 3
                    TextureCompression = "medium"
                    EstimatedSizeReduction = float (currentPolygons - targetPolygons) / float currentPolygons * 100.0
                    QualityImpact = if decimationLevel = "high" then "moderate" else "minimal"
                }
        }
    
    /// Recommend export format with AI
    let recommendExportWithAI (aiService: IAIService) (modelType: string) (targetPlatform: string) (requirements: string list) =
        async {
            let requirementsStr = String.Join(", ", requirements)
            let prompt = sprintf "Recommend the best 3D export format.\n\nModel type: %s\nTarget platform: %s\nRequirements: %s\n\nReturn a JSON response with the following structure:\n{\"recommendedFormat\": string, \"exportSettings\": object, \"reason\": string, \"platformSupport\": string[]}\n\nConsider formats:\n- FBX: Universal, good for most platforms\n- GLTF/GLB: Web-optimized, modern\n- OBJ: Simple, widely supported\n- USD: Pixar format, modern pipeline" modelType targetPlatform requirementsStr
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let recommendedFormat = try root.GetProperty("recommendedFormat").GetString() with _ -> "GLB"
                let reason = try root.GetProperty("reason").GetString() with _ -> "Best for target platform"
                let platformSupport = try root.GetProperty("platformSupport").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                let exportSettings = 
                    try
                        root.GetProperty("exportSettings").EnumerateObject()
                        |> Seq.map (fun p -> (p.Name, p.Value.ToString()))
                        |> Map.ofSeq
                    with _ -> Map.empty
                
                return {
                    RecommendedFormat = recommendedFormat
                    ExportSettings = exportSettings
                    Reason = reason
                    PlatformSupport = platformSupport
                }
            with ex ->
                let recommendedFormat = 
                    match targetPlatform.ToLower() with
                    | s when s.Contains("web") -> "GLB"
                    | s when s.Contains("unity") || s.Contains("unreal") -> "FBX"
                    | _ -> "GLB"
                
                return {
                    RecommendedFormat = recommendedFormat
                    ExportSettings = Map ["embedTextures", "true"; "compression", "draco"]
                    Reason = "Optimized for target platform"
                    PlatformSupport = ["Web"; "Mobile"; "Desktop"]
                }
        }

/// Media quality assessment algorithms with AI integration
module MediaQualityAssessor =
    
    type ImageQuality = {
        OverallScore: float
        Resolution: string
        Sharpness: float
        ColorAccuracy: float
        CompressionArtifacts: float
        HasIssues: bool
        Issues: string list
    }
    
    type VideoQuality = {
        OverallScore: float
        Resolution: string
        FrameRate: int
        Bitrate: int
        CompressionQuality: float
        HasIssues: bool
        Issues: string list
    }
    
    type EnhancementSuggestion = {
        EnhancementType: string
        Description: string
        ExpectedImprovement: string
        Complexity: string
    }
    
    /// Assess image quality with AI
    let assessImageQualityWithAI (aiService: IAIService) (width: int) (height: int) (format: string) (fileSize: int) =
        async {
            let prompt = sprintf "Assess the image quality based on the provided metrics.\n\nImage metrics:\n- Resolution: %dx%d\n- Format: %s\n- File size: %d bytes\n\nReturn a JSON response with the following structure:\n{\"overallScore\": number (0-100), \"sharpness\": number (0-100), \"colorAccuracy\": number (0-100), \"compressionArtifacts\": number (0-100), \"hasIssues\": boolean, \"issues\": string[]}\n\nConsider factors like:\n- Resolution for intended use\n- Format compression characteristics\n- File size vs quality ratio" width height format fileSize
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let overallScore = try root.GetProperty("overallScore").GetDouble() with _ -> 75.0
                let sharpness = try root.GetProperty("sharpness").GetDouble() with _ -> 75.0
                let colorAccuracy = try root.GetProperty("colorAccuracy").GetDouble() with _ -> 75.0
                let compressionArtifacts = try root.GetProperty("compressionArtifacts").GetDouble() with _ -> 25.0
                let hasIssues = try root.GetProperty("hasIssues").GetBoolean() with _ -> false
                let issues = try root.GetProperty("issues").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                return {
                    OverallScore = overallScore
                    Resolution = sprintf "%dx%d" width height
                    Sharpness = sharpness
                    ColorAccuracy = colorAccuracy
                    CompressionArtifacts = compressionArtifacts
                    HasIssues = hasIssues
                    Issues = issues
                }
            with ex ->
                let totalPixels = width * height
                let overallScore = min 100.0 (float totalPixels / 1000000.0 * 50.0 + 50.0)
                let hasIssues = totalPixels < 500000 || format = "JPEG"
                
                let issues = 
                    [ if totalPixels < 500000 then "Low resolution" else ""
                      if format = "JPEG" then "Lossy compression" else "" ]
                    |> List.filter (String.IsNullOrEmpty >> not)
                
                return {
                    OverallScore = overallScore
                    Resolution = sprintf "%dx%d" width height
                    Sharpness = overallScore * 0.9
                    ColorAccuracy = overallScore * 0.85
                    CompressionArtifacts = if format = "JPEG" then 30.0 else 10.0
                    HasIssues = hasIssues
                    Issues = issues
                }
        }
    
    /// Assess video quality with AI
    let assessVideoQualityWithAI (aiService: IAIService) (width: int) (height: int) (frameRate: int) (bitrate: int) (codec: string) =
        async {
            let prompt = sprintf "Assess the video quality based on the provided metrics.\n\nVideo metrics:\n- Resolution: %dx%d\n- Frame rate: %d fps\n- Bitrate: %d kbps\n- Codec: %s\n\nReturn a JSON response with the following structure:\n{\"overallScore\": number (0-100), \"compressionQuality\": number (0-100), \"hasIssues\": boolean, \"issues\": string[]}\n\nConsider factors like:\n- Resolution for target platform\n- Frame rate smoothness\n- Bitrate vs resolution ratio\n- Codec efficiency" width height frameRate (bitrate / 1000) codec
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let overallScore = try root.GetProperty("overallScore").GetDouble() with _ -> 75.0
                let compressionQuality = try root.GetProperty("compressionQuality").GetDouble() with _ -> 75.0
                let hasIssues = try root.GetProperty("hasIssues").GetBoolean() with _ -> false
                let issues = try root.GetProperty("issues").EnumerateArray() |> Seq.map (fun i -> i.GetString()) |> List.ofSeq with _ -> []
                
                return {
                    OverallScore = overallScore
                    Resolution = sprintf "%dx%d" width height
                    FrameRate = frameRate
                    Bitrate = bitrate
                    CompressionQuality = compressionQuality
                    HasIssues = hasIssues
                    Issues = issues
                }
            with ex ->
                let totalPixels = width * height
                let overallScore = min 100.0 (float totalPixels / 2000000.0 * 40.0 + float frameRate * 0.5 + 30.0)
                let hasIssues = totalPixels < 921600 || frameRate < 24 || bitrate < 2000000
                
                let issues = 
                    [ if totalPixels < 921600 then "Low resolution" else ""
                      if frameRate < 24 then "Low frame rate" else ""
                      if bitrate < 2000000 then "Low bitrate" else "" ]
                    |> List.filter (String.IsNullOrEmpty >> not)
                
                return {
                    OverallScore = overallScore
                    Resolution = sprintf "%dx%d" width height
                    FrameRate = frameRate
                    Bitrate = bitrate
                    CompressionQuality = overallScore * 0.8
                    HasIssues = hasIssues
                    Issues = issues
                }
        }
    
    /// Suggest enhancements with AI
    let suggestEnhancementsWithAI (aiService: IAIService) (mediaType: string) (currentQuality: float) (targetQuality: float) =
        async {
            let prompt = sprintf "Suggest enhancements to improve media quality.\n\nMedia type: %s\nCurrent quality: %.1f/100\nTarget quality: %.1f/100\n\nReturn a JSON response with the following structure:\n{\"enhancements\": [{\"enhancementType\": string, \"description\": string, \"expectedImprovement\": string, \"complexity\": string}]}\n\nConsider enhancement types:\n- Upscaling\n- Denoising\n- Sharpening\n- Color correction\n- Stabilization\n- Compression optimization" mediaType currentQuality targetQuality
            
            try
                let! response = aiService.AnalyzeTextAsync(prompt, "")
                let root = JsonDocument.Parse(response).RootElement
                
                let enhancements = 
                    try
                        root.GetProperty("enhancements").EnumerateArray()
                        |> Seq.map (fun e ->
                            {
                                EnhancementType = e.GetProperty("enhancementType").GetString()
                                Description = e.GetProperty("description").GetString()
                                ExpectedImprovement = e.GetProperty("expectedImprovement").GetString()
                                Complexity = e.GetProperty("complexity").GetString()
                            })
                        |> List.ofSeq
                    with _ -> []
                
                return enhancements
            with ex ->
                let gap = targetQuality - currentQuality
                let enhancements = 
                    if gap > 30.0 then
                        [ { EnhancementType = "Upscaling"; Description = "Increase resolution using AI upscaling"; ExpectedImprovement = "High"; Complexity = "Medium" }
                          { EnhancementType = "Denoising"; Description = "Remove noise and artifacts"; ExpectedImprovement = "Medium"; Complexity = "Low" } ]
                    elif gap > 15.0 then
                        [ { EnhancementType = "Sharpening"; Description = "Enhance edge details"; ExpectedImprovement = "Medium"; Complexity = "Low" }
                          { EnhancementType = "Color correction"; Description = "Improve color accuracy"; ExpectedImprovement = "Medium"; Complexity = "Low" } ]
                    else
                        [ { EnhancementType = "Compression optimization"; Description = "Optimize compression settings"; ExpectedImprovement = "Low"; Complexity = "Low" } ]
                
                return enhancements
        }
