using Libr4.AI.Domain.Agents;
using Microsoft.Extensions.Logging;

namespace Libr4.AI.Application.Agents;

public class SubagentService
{
    private readonly ILogger<SubagentService> _logger;
    private readonly Dictionary<string, SubagentDefinition> _subagents;
    private readonly List<SubagentInstance> _instances;

    public SubagentService(ILogger<SubagentService> logger)
    {
        _logger = logger;
        _subagents = LoadSubagentDefinitions();
        _instances = new List<SubagentInstance>();
    }

    private Dictionary<string, SubagentDefinition> LoadSubagentDefinitions()
    {
        return new Dictionary<string, SubagentDefinition>
        {
            // Language Specialists (8)
            ["csharp-specialist"] = new SubagentDefinition
            {
                Id = "csharp-specialist",
                Name = "C# Specialist",
                Description = "Specializes in C# development, debugging, and optimization",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_csharp" },
                SystemPrompt = "You are a C# specialist. Focus on clean code, performance, and best practices.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "csharp", "fsharp" },
                    ["expertise"] = new[] { "debugging", "optimization", "architecture" }
                }
            },
            ["frontend-specialist"] = new SubagentDefinition
            {
                Id = "frontend-specialist",
                Name = "Frontend Specialist",
                Description = "Specializes in React, TypeScript, and UI development",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_javascript" },
                SystemPrompt = "You are a frontend specialist. Focus on UX, performance, and accessibility.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "typescript", "javascript", "tsx", "jsx" },
                    ["expertise"] = new[] { "react", "ui", "accessibility" }
                }
            },
            ["rust-specialist"] = new SubagentDefinition
            {
                Id = "rust-specialist",
                Name = "Rust Specialist",
                Description = "Specializes in Rust development and systems programming",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_rust" },
                SystemPrompt = "You are a Rust specialist. Focus on memory safety, performance, and idiomatic patterns.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "rust" },
                    ["expertise"] = new[] { "systems", "ffi", "async" }
                }
            },
            ["python-specialist"] = new SubagentDefinition
            {
                Id = "python-specialist",
                Name = "Python Specialist",
                Description = "Specializes in Python development, data science, and scripting",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_python" },
                SystemPrompt = "You are a Python specialist. Focus on clean code, testing, and PEP 8 compliance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "python" },
                    ["expertise"] = new[] { "data-science", "scripting", "automation" }
                }
            },
            ["go-specialist"] = new SubagentDefinition
            {
                Id = "go-specialist",
                Name = "Go Specialist",
                Description = "Specializes in Go development and microservices",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_go" },
                SystemPrompt = "You are a Go specialist. Focus on concurrency, performance, and simplicity.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "go" },
                    ["expertise"] = new[] { "microservices", "concurrency", "networking" }
                }
            },
            ["java-specialist"] = new SubagentDefinition
            {
                Id = "java-specialist",
                Name = "Java Specialist",
                Description = "Specializes in Java enterprise development and Spring",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_java" },
                SystemPrompt = "You are a Java specialist. Focus on enterprise patterns, Spring, and JVM optimization.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "java", "kotlin" },
                    ["expertise"] = new[] { "spring", "enterprise", "jvm" }
                }
            },
            ["javascript-specialist"] = new SubagentDefinition
            {
                Id = "javascript-specialist",
                Name = "JavaScript Specialist",
                Description = "Specializes in vanilla JavaScript and Node.js development",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_javascript" },
                SystemPrompt = "You are a JavaScript specialist. Focus on Node.js, async patterns, and performance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "javascript", "nodejs" },
                    ["expertise"] = new[] { "nodejs", "async", "performance" }
                }
            },
            ["cpp-specialist"] = new SubagentDefinition
            {
                Id = "cpp-specialist",
                Name = "C++ Specialist",
                Description = "Specializes in C++ development and high-performance systems",
                Category = SubagentCategory.LanguageSpecialist,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_cpp" },
                SystemPrompt = "You are a C++ specialist. Focus on modern C++, memory management, and performance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "cpp", "c" },
                    ["expertise"] = new[] { "performance", "memory", "systems" }
                }
            },

            // Infrastructure & DevOps (6)
            ["database-specialist"] = new SubagentDefinition
            {
                Id = "database-specialist",
                Name = "Database Specialist",
                Description = "Specializes in SQL, database design, and optimization",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_sql" },
                SystemPrompt = "You are a database specialist. Focus on performance, normalization, and data integrity.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "sql", "postgresql", "sqlite", "mysql" },
                    ["expertise"] = new[] { "optimization", "migration", "design" }
                }
            },
            ["devops-specialist"] = new SubagentDefinition
            {
                Id = "devops-specialist",
                Name = "DevOps Specialist",
                Description = "Specializes in CI/CD, Docker, and Kubernetes",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_shell", "docker" },
                SystemPrompt = "You are a DevOps specialist. Focus on automation, deployment, and infrastructure as code.",
                Capabilities = new Dictionary<string, object>
                {
                    ["tools"] = new[] { "docker", "kubernetes", "terraform", "ansible" },
                    ["expertise"] = new[] { "cicd", "deployment", "monitoring" }
                }
            },
            ["cloud-specialist"] = new SubagentDefinition
            {
                Id = "cloud-specialist",
                Name = "Cloud Specialist",
                Description = "Specializes in AWS, Azure, and GCP cloud services",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "cloud_api" },
                SystemPrompt = "You are a cloud specialist. Focus on cost optimization, scalability, and security.",
                Capabilities = new Dictionary<string, object>
                {
                    ["providers"] = new[] { "aws", "azure", "gcp" },
                    ["expertise"] = new[] { "serverless", "storage", "networking" }
                }
            },
            ["security-specialist"] = new SubagentDefinition
            {
                Id = "security-specialist",
                Name = "Security Specialist",
                Description = "Specializes in application security and vulnerability assessment",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "scan_vulnerabilities", "audit_logs" },
                SystemPrompt = "You are a security specialist. Focus on OWASP, encryption, and secure coding practices.",
                Capabilities = new Dictionary<string, object>
                {
                    ["domains"] = new[] { "appsec", "network", "crypto" },
                    ["expertise"] = new[] { "penetration-testing", "audit", "compliance" }
                }
            },
            ["api-specialist"] = new SubagentDefinition
            {
                Id = "api-specialist",
                Name = "API Specialist",
                Description = "Specializes in REST, GraphQL, and gRPC API design",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "test_api", "generate_docs" },
                SystemPrompt = "You are an API specialist. Focus on RESTful design, documentation, and versioning.",
                Capabilities = new Dictionary<string, object>
                {
                    ["protocols"] = new[] { "rest", "graphql", "grpc" },
                    ["expertise"] = new[] { "design", "testing", "documentation" }
                }
            },
            ["testing-specialist"] = new SubagentDefinition
            {
                Id = "testing-specialist",
                Name = "Testing Specialist",
                Description = "Specializes in unit, integration, and E2E testing",
                Category = SubagentCategory.Infrastructure,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "run_tests", "coverage" },
                SystemPrompt = "You are a testing specialist. Focus on test coverage, automation, and quality assurance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["frameworks"] = new[] { "jest", "pytest", "xunit", "selenium" },
                    ["expertise"] = new[] { "unit", "integration", "e2e" }
                }
            },

            // Data & AI (6)
            ["ai-specialist"] = new SubagentDefinition
            {
                Id = "ai-specialist",
                Name = "AI Specialist",
                Description = "Specializes in AI/ML implementation and integration",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "search_code", "execute_python" },
                SystemPrompt = "You are an AI/ML specialist. Focus on model integration, data preprocessing, and evaluation.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "python", "typescript" },
                    ["expertise"] = new[] { "ml", "nlp", "llm", "rag" }
                }
            },
            ["data-engineer"] = new SubagentDefinition
            {
                Id = "data-engineer",
                Name = "Data Engineer",
                Description = "Specializes in ETL pipelines and data warehousing",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_sql", "execute_python" },
                SystemPrompt = "You are a data engineer. Focus on pipeline efficiency, data quality, and scalability.",
                Capabilities = new Dictionary<string, object>
                {
                    ["tools"] = new[] { "spark", "airflow", "dbt", "kafka" },
                    ["expertise"] = new[] { "etl", "warehousing", "streaming" }
                }
            },
            ["ml-engineer"] = new SubagentDefinition
            {
                Id = "ml-engineer",
                Name = "ML Engineer",
                Description = "Specializes in machine learning model deployment and MLOps",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_python", "deploy_model" },
                SystemPrompt = "You are an ML engineer. Focus on model serving, monitoring, and performance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["frameworks"] = new[] { "tensorflow", "pytorch", "onnx", "mlflow" },
                    ["expertise"] = new[] { "deployment", "monitoring", "optimization" }
                }
            },
            ["data-scientist"] = new SubagentDefinition
            {
                Id = "data-scientist",
                Name = "Data Scientist",
                Description = "Specializes in statistical analysis and data visualization",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_python", "visualize" },
                SystemPrompt = "You are a data scientist. Focus on statistical rigor, visualization, and insights.",
                Capabilities = new Dictionary<string, object>
                {
                    ["libraries"] = new[] { "pandas", "numpy", "scikit-learn", "matplotlib" },
                    ["expertise"] = new[] { "statistics", "visualization", "analysis" }
                }
            },
            ["nlp-specialist"] = new SubagentDefinition
            {
                Id = "nlp-specialist",
                Name = "NLP Specialist",
                Description = "Specializes in natural language processing and text analysis",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_python", "process_text" },
                SystemPrompt = "You are an NLP specialist. Focus on text processing, sentiment analysis, and NER.",
                Capabilities = new Dictionary<string, object>
                {
                    ["libraries"] = new[] { "spacy", "nltk", "transformers", "huggingface" },
                    ["expertise"] = new[] { "tokenization", "embedding", "classification" }
                }
            },
            ["computer-vision-specialist"] = new SubagentDefinition
            {
                Id = "computer-vision-specialist",
                Name = "Computer Vision Specialist",
                Description = "Specializes in image processing and object detection",
                Category = SubagentCategory.DataAndAI,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_python", "process_image" },
                SystemPrompt = "You are a computer vision specialist. Focus on image classification, detection, and segmentation.",
                Capabilities = new Dictionary<string, object>
                {
                    ["libraries"] = new[] { "opencv", "pillow", "yolo", "detectron2" },
                    ["expertise"] = new[] { "classification", "detection", "segmentation" }
                }
            },

            // Architecture & Design (4)
            ["architect-specialist"] = new SubagentDefinition
            {
                Id = "architect-specialist",
                Name = "Software Architect",
                Description = "Specializes in system architecture and design patterns",
                Category = SubagentCategory.Architecture,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "generate_diagram", "review_design" },
                SystemPrompt = "You are a software architect. Focus on scalability, maintainability, and design patterns.",
                Capabilities = new Dictionary<string, object>
                {
                    ["patterns"] = new[] { "microservices", "event-driven", "hexagonal" },
                    ["expertise"] = new[] { "scalability", "reliability", "security" }
                }
            },
            ["microservices-specialist"] = new SubagentDefinition
            {
                Id = "microservices-specialist",
                Name = "Microservices Specialist",
                Description = "Specializes in microservices architecture and distributed systems",
                Category = SubagentCategory.Architecture,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "deploy_service", "monitor_system" },
                SystemPrompt = "You are a microservices specialist. Focus on service boundaries, communication, and resilience.",
                Capabilities = new Dictionary<string, object>
                {
                    ["patterns"] = new[] { "saga", "circuit-breaker", "bulkhead" },
                    ["expertise"] = new[] { "distributed", "event-sourcing", "cqrs" }
                }
            },
            ["performance-specialist"] = new SubagentDefinition
            {
                Id = "performance-specialist",
                Name = "Performance Specialist",
                Description = "Specializes in application performance optimization",
                Category = SubagentCategory.Architecture,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "profile", "benchmark" },
                SystemPrompt = "You are a performance specialist. Focus on profiling, optimization, and scalability.",
                Capabilities = new Dictionary<string, object>
                {
                    ["techniques"] = new[] { "caching", "async", "batching" },
                    ["expertise"] = new[] { "profiling", "optimization", "scalability" }
                }
            },
            ["ux-specialist"] = new SubagentDefinition
            {
                Id = "ux-specialist",
                Name = "UX Specialist",
                Description = "Specializes in user experience and interface design",
                Category = SubagentCategory.Architecture,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "design_ui", "prototype" },
                SystemPrompt = "You are a UX specialist. Focus on usability, accessibility, and user-centered design.",
                Capabilities = new Dictionary<string, object>
                {
                    ["tools"] = new[] { "figma", "sketch", "adobe-xd" },
                    ["expertise"] = new[] { "usability", "accessibility", "research" }
                }
            },

            // Mobile & Web (4)
            ["mobile-ios-specialist"] = new SubagentDefinition
            {
                Id = "mobile-ios-specialist",
                Name = "iOS Specialist",
                Description = "Specializes in iOS development with Swift and SwiftUI",
                Category = SubagentCategory.Mobile,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_swift", "build_ios" },
                SystemPrompt = "You are an iOS specialist. Focus on SwiftUI, UIKit, and iOS best practices.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "swift", "objective-c" },
                    ["frameworks"] = new[] { "swiftui", "uikit", "combine" }
                }
            },
            ["mobile-android-specialist"] = new SubagentDefinition
            {
                Id = "mobile-android-specialist",
                Name = "Android Specialist",
                Description = "Specializes in Android development with Kotlin and Jetpack",
                Category = SubagentCategory.Mobile,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_kotlin", "build_android" },
                SystemPrompt = "You are an Android specialist. Focus on Jetpack Compose, Material Design, and Android best practices.",
                Capabilities = new Dictionary<string, object>
                {
                    ["languages"] = new[] { "kotlin", "java" },
                    ["frameworks"] = new[] { "jetpack", "compose", "material" }
                }
            },
            ["react-native-specialist"] = new SubagentDefinition
            {
                Id = "react-native-specialist",
                Name = "React Native Specialist",
                Description = "Specializes in cross-platform mobile development",
                Category = SubagentCategory.Mobile,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_javascript", "build_mobile" },
                SystemPrompt = "You are a React Native specialist. Focus on cross-platform performance and native modules.",
                Capabilities = new Dictionary<string, object>
                {
                    ["frameworks"] = new[] { "react-native", "expo", "flutter" },
                    ["expertise"] = new[] { "cross-platform", "performance", "native-modules" }
                }
            },
            ["web-fullstack-specialist"] = new SubagentDefinition
            {
                Id = "web-fullstack-specialist",
                Name = "Fullstack Web Specialist",
                Description = "Specializes in end-to-end web application development",
                Category = SubagentCategory.Web,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_javascript", "execute_node" },
                SystemPrompt = "You are a fullstack web specialist. Focus on MERN/MEAN stack and modern web frameworks.",
                Capabilities = new Dictionary<string, object>
                {
                    ["stacks"] = new[] { "mern", "mean", "nextjs", "nuxt" },
                    ["expertise"] = new[] { "frontend", "backend", "deployment" }
                }
            },

            // Domain Specialists (4)
            ["fintech-specialist"] = new SubagentDefinition
            {
                Id = "fintech-specialist",
                Name = "FinTech Specialist",
                Description = "Specializes in financial technology and payment systems",
                Category = SubagentCategory.Domain,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "audit_finance", "test_payment" },
                SystemPrompt = "You are a FinTech specialist. Focus on security, compliance, and payment processing.",
                Capabilities = new Dictionary<string, object>
                {
                    ["domains"] = new[] { "payments", "trading", "blockchain" },
                    ["expertise"] = new[] { "compliance", "security", "regulations" }
                }
            },
            ["healthcare-specialist"] = new SubagentDefinition
            {
                Id = "healthcare-specialist",
                Name = "Healthcare Specialist",
                Description = "Specializes in healthcare IT and HIPAA compliance",
                Category = SubagentCategory.Domain,
                DefaultModel = ModelTier.Opus,
                AssignedTools = new List<string> { "read_file", "write_file", "audit_compliance", "integrate_hl7" },
                SystemPrompt = "You are a healthcare specialist. Focus on HIPAA, HL7, and patient data privacy.",
                Capabilities = new Dictionary<string, object>
                {
                    ["standards"] = new[] { "hipaa", "hl7", "fhir" },
                    ["expertise"] = new[] { "compliance", "privacy", "interoperability" }
                }
            },
            ["ecommerce-specialist"] = new SubagentDefinition
            {
                Id = "ecommerce-specialist",
                Name = "E-commerce Specialist",
                Description = "Specializes in e-commerce platforms and online retail",
                Category = SubagentCategory.Domain,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "integrate_payment", "optimize_cart" },
                SystemPrompt = "You are an e-commerce specialist. Focus on conversion, UX, and payment integration.",
                Capabilities = new Dictionary<string, object>
                {
                    ["platforms"] = new[] { "shopify", "woocommerce", "magento" },
                    ["expertise"] = new[] { "conversion", "payments", "inventory" }
                }
            },
            ["gaming-specialist"] = new SubagentDefinition
            {
                Id = "gaming-specialist",
                Name = "Game Development Specialist",
                Description = "Specializes in game development and real-time systems",
                Category = SubagentCategory.Domain,
                DefaultModel = ModelTier.Sonnet,
                AssignedTools = new List<string> { "read_file", "write_file", "execute_cpp", "execute_csharp" },
                SystemPrompt = "You are a game development specialist. Focus on Unity, Unreal, and game performance.",
                Capabilities = new Dictionary<string, object>
                {
                    ["engines"] = new[] { "unity", "unreal", "godot" },
                    ["expertise"] = new[] { "physics", "networking", "graphics" }
                }
            }
        };
    }

    public SubagentDefinition? GetSubagent(string id)
    {
        return _subagents.TryGetValue(id, out var subagent) ? subagent : null;
    }

    public List<SubagentDefinition> GetSubagentsByCategory(SubagentCategory category)
    {
        return _subagents.Values.Where(s => s.Category == category).ToList();
    }

    public List<SubagentDefinition> GetAllSubagents()
    {
        return _subagents.Values.ToList();
    }

    public SubagentInstance CreateSubagentInstance(string subagentId, Guid parentAgentId)
    {
        var subagent = GetSubagent(subagentId);
        if (subagent == null)
        {
            throw new ArgumentException($"Subagent not found: {subagentId}");
        }

        var instance = new SubagentInstance
        {
            Id = Guid.NewGuid(),
            SubagentId = subagentId,
            ParentAgentId = parentAgentId,
            CurrentModel = subagent.DefaultModel,
            Context = new Dictionary<string, object>(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastUsedAt = null,
            UsageCount = 0
        };

        _instances.Add(instance);
        _logger.LogInformation("Created subagent instance {InstanceId} for subagent {SubagentId}", instance.Id, subagentId);

        return instance;
    }

    public SubagentInstance? GetSubagentInstance(Guid instanceId)
    {
        return _instances.FirstOrDefault(i => i.Id == instanceId);
    }

    public List<SubagentInstance> GetInstancesByParentAgent(Guid parentAgentId)
    {
        return _instances.Where(i => i.ParentAgentId == parentAgentId).ToList();
    }

    public ModelTier DetermineModelTier(string task, string context)
    {
        // Smart routing based on task complexity
        var taskLower = task.ToLower();
        var contextLength = context.Length;

        if (taskLower.Contains("complex") || 
            taskLower.Contains("architecture") || 
            taskLower.Contains("design") ||
            contextLength > 10000)
        {
            return ModelTier.Opus;
        }
        else if (taskLower.Contains("debug") || 
                 taskLower.Contains("fix") ||
                 taskLower.Contains("error"))
        {
            return ModelTier.Sonnet;
        }
        else if (taskLower.Contains("simple") ||
                 taskLower.Contains("quick") ||
                 contextLength < 1000)
        {
            return ModelTier.Haiku;
        }
        else
        {
            return ModelTier.Sonnet;
        }
    }

    public SubagentDefinition? FindBestSubagentForTask(string task, string context)
    {
        var taskLower = task.ToLower();
        
        // Simple keyword matching for subagent selection
        if (taskLower.Contains("c#") || taskLower.Contains("csharp"))
        {
            return GetSubagent("csharp-specialist");
        }
        else if (taskLower.Contains("react") || taskLower.Contains("typescript") || taskLower.Contains("frontend"))
        {
            return GetSubagent("frontend-specialist");
        }
        else if (taskLower.Contains("sql") || taskLower.Contains("database"))
        {
            return GetSubagent("database-specialist");
        }
        else if (taskLower.Contains("ai") || taskLower.Contains("ml") || taskLower.Contains("machine learning"))
        {
            return GetSubagent("ai-specialist");
        }
        else if (taskLower.Contains("rust"))
        {
            return GetSubagent("rust-specialist");
        }

        // Default to C# specialist if no match
        return GetSubagent("csharp-specialist");
    }

    public void UpdateInstanceUsage(Guid instanceId)
    {
        var instance = GetSubagentInstance(instanceId);
        if (instance != null)
        {
            instance.LastUsedAt = DateTimeOffset.UtcNow;
            instance.UsageCount++;
        }
    }

    public void CleanupOldInstances(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var oldInstances = _instances.Where(i => i.LastUsedAt < cutoff).ToList();
        
        foreach (var instance in oldInstances)
        {
            _instances.Remove(instance);
        }

        _logger.LogInformation("Cleaned up {Count} old subagent instances", oldInstances.Count);
    }
}
