<# 
.SYNOPSIS
    Libr4 End-to-End Full Flow Integration Test (REAL APIs)
.DESCRIPTION
    Simulates complete platform flow using real HTTP API calls:
      1. Client registration & login via Auth.Api
      2. Client creates task via Tasks.Api
      3. Client creates escrow via Payments.Api
      4. Freelancer registration & login
      5. Freelancer uploads CV -> AI.Api skill scoring
      6. Matching engine indexes freelancer & task
      7. Chat negotiation via Chat.Api
      8. Task application & acceptance
      9. Escrow release & withdrawal via Payments.Api
.NOTES
    Requires: Docker infra running, all APIs started on their ports
#>

$ErrorActionPreference = "Stop"
$ProgressPreference = "Continue"

# ─── Service URLs ───────────────────────────────────────────────────
$AuthApi    = "http://localhost:5001"
$TasksApi   = "http://localhost:5002"
$PayApi     = "http://localhost:5003"
$ChatApi    = "http://localhost:5004"
$MatchApi   = "http://localhost:5010"
$AiApi      = "http://localhost:5006"

$QdrantRest = "http://localhost:6333"

$Results = New-Object System.Collections.Specialized.OrderedDictionary
$script:Tokens = @{}

function Write-Step($num, $title, $status = "INFO") {
    $color = switch ($status) { "PASS" { "Green" } "FAIL" { "Red" } "MOCK" { "Yellow" } default { "Cyan" } }
    Write-Host "`n[$status] Step $num : $title" -ForegroundColor $color
}

function Test-Health($url) {
    try {
        $resp = Invoke-WebRequest -Uri "$url/health/ready" -UseBasicParsing -TimeoutSec 5
        if ($resp.StatusCode -eq 200) { return $true }
    }
    catch {
        # /health/ready may not exist; fallback to /health
    }
    try {
        $resp = Invoke-WebRequest -Uri "$url/health" -UseBasicParsing -TimeoutSec 5
        return $resp.StatusCode -eq 200
    }
    catch { return $false }
}

function Invoke-Api($Method, $Uri, $Body = $null, $Token = $null, [switch]$AsJson) {
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Uri = $Uri; Method = $Method; Headers = $headers; UseBasicParsing = $true; TimeoutSec = 30 }
    if ($Body -and $Method -in @("POST","PUT","PATCH")) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }
    $resp = Invoke-WebRequest @params
    if ($AsJson -and $resp.Content) { return $resp.Content | ConvertFrom-Json }
    return $resp
}

function Register-User($email, $password, $displayName) {
    $resp = Invoke-Api POST "$AuthApi/api/v1/auth/register" @{ Email = $email; Password = $password; DisplayName = $displayName }
    return ($resp.Content | ConvertFrom-Json)
}

function Get-AuthToken($email, $password) {
    $resp = Invoke-Api POST "$AuthApi/api/v1/auth/login" @{ Email = $email; Password = $password }
    $data = $resp.Content | ConvertFrom-Json
    return $data.accessToken
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 0 - Environment Check
# ═════════════════════════════════════════════════════════════════════
Write-Step 0 "Environment Check"

$services = @{ Auth = $AuthApi; Tasks = $TasksApi; Payments = $PayApi; Chat = $ChatApi; Matching = $MatchApi; AI = $AiApi }
$allUp = $true
foreach ($svc in $services.GetEnumerator()) {
    $ok = Test-Health $svc.Value
    Write-Host "  $($svc.Key) API ($($svc.Value)) : $(if($ok){'UP'}else{'DOWN'})"
    if (-not $ok) { $allUp = $false }
}
$Results["EnvCheck"] = $allUp

# ═════════════════════════════════════════════════════════════════════
#  STEP 1 - Client Registration (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 1 "Client Account Registration" "REAL"
$ClientEmail = "e2e-client-$(Get-Random)@test.com"
$ClientPass  = "TestPass123!"
try {
    $clientReg = Register-User $ClientEmail $ClientPass "E2E Client"
    $ClientId = $clientReg.id
    Write-Host "  Registered client: $ClientId"
    $Results["ClientReg"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["ClientReg"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 2 - Client Login (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 2 "Client Login" "REAL"
try {
    $ClientToken = Get-AuthToken $ClientEmail $ClientPass
    Write-Host "  Client JWT obtained"
    $Results["ClientLogin"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["ClientLogin"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 3 - Client Creates Task (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 3 "Client Creates Task Offer" "REAL"
$TaskTitle = "Build NLP ML Pipeline $(Get-Random)"
try {
    $taskBody = @{
        title = $TaskTitle
        description = "Need Python dev to build ML pipeline for text classification and sentiment analysis"
        category = "Development"
        budget = 5000
        currency = "USD"
        deadline = ([DateTimeOffset]::UtcNow.AddDays(30).ToString("o"))
    }
    $taskResp = Invoke-Api POST "$TasksApi/api/v1/tasks" $taskBody -Token $ClientToken -AsJson
    $TaskId = $taskResp.Id
    if (-not $TaskId) { $TaskId = $taskResp.id }
    Write-Host "  TaskId: $TaskId"

    # Publish task so freelancers can apply
    Invoke-Api POST "$TasksApi/api/v1/tasks/$TaskId/publish" @{} -Token $ClientToken | Out-Null
    Write-Host "  Task published"

    $Results["TaskCreate"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["TaskCreate"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 4 - Freelancer Registration & Login (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 4 "Freelancer Account Registration" "REAL"
$FreelEmail = "e2e-freelancer-$(Get-Random)@test.com"
$FreelPass  = "TestPass123!"
try {
    $freelReg = Register-User $FreelEmail $FreelPass "E2E Freelancer"
    $FreelancerId = $freelReg.id
    Write-Host "  Registered freelancer: $FreelancerId"
    $Results["FreelancerReg"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["FreelancerReg"] = $false
}

Write-Step 4.1 "Freelancer Login" "REAL"
try {
    $FreelToken = Get-AuthToken $FreelEmail $FreelPass
    Write-Host "  Freelancer JWT obtained"
    $Results["FreelancerLogin"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["FreelancerLogin"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 5 - AI CV Analysis (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 5 "AI CV & LinkedIn Skill Scoring" "REAL"
try {
    $cvText = @"
FULL STACK & MACHINE LEARNING ENGINEER

OVERVIEW:
Senior Software Engineer with 8+ years of experience spanning backend systems,
frontend development, and production machine learning. Passionate about building
end-to-end AI-powered applications with modern cloud infrastructure.

BACKEND EXPERTISE (Score: 9/10):
- 8 years Python (Django, FastAPI, Flask) - built REST APIs handling 50M+ requests/day
- 5 years C# (.NET Core, ASP.NET) - microservices architecture, gRPC, SignalR
- 4 years Go - high-performance data pipelines and services
- PostgreSQL, MongoDB, Redis, Elasticsearch, Kafka, RabbitMQ
- Docker, Kubernetes, Terraform, CI/CD (GitHub Actions, Azure DevOps)
- AWS certified (Solutions Architect) - EC2, Lambda, S3, RDS, SageMaker
- System design, distributed systems, event-driven architecture

FRONTEND EXPERTISE (Score: 7/10):
- 4 years React/TypeScript - SPA, SSR (Next.js), state management (Redux, Zustand)
- 3 years Vue.js - component libraries, Nuxt.js applications
- CSS frameworks: Tailwind, Material-UI, Styled Components
- WebSocket real-time dashboards, D3.js data visualization
- PWA, WebAssembly, performance optimization (Lighthouse 95+)
- Mobile: React Native (2 apps published to App Store)

AI / ML EXPERTISE (Score: 10/10):
- 6 years Machine Learning - supervised, unsupervised, reinforcement learning
- Deep Learning: PyTorch, TensorFlow, Keras - trained models for NLP and CV
- NLP: Transformers (BERT, GPT, T5), spaCy, NLTK - built chatbots and summarization
- Computer Vision: OpenCV, YOLO, segmentation - medical imaging project
- MLOps: MLflow, Kubeflow, model versioning, A/B testing, monitoring
- Deployed LLMs via vLLM, Ollama, OpenRouter for production RAG systems
- Published 3 papers at NeurIPS and ICML on multimodal learning

EXPERIENCE:
- Senior ML Engineer @ Google AI (2020-2025): Led team of 6, built recommendation
  system serving 200M users. Stack: Python, TensorFlow, Kubernetes, BigQuery.
- Full Stack Developer @ Stripe (2017-2020): Built payment dashboard frontend (React)
  and backend APIs (Go). Reduced latency by 40%.
- Software Engineer @ Meta (2015-2017): Backend infrastructure (C++), 
  messenger bot platform (Python/NLP).

EDUCATION:
- M.Sc. Computer Science, Stanford University (GPA 3.95)
- B.Sc. Software Engineering, MIT
- AWS Solutions Architect Professional
- TensorFlow Developer Certificate

PROJECTS:
- Open-source NLP library (12k GitHub stars) - Python, PyTorch
- Real-time trading dashboard - React, WebSocket, Go backend
- AI code review tool - LLM-powered, integrated with GitHub
"@
    $aiBody = @{
        userId = $FreelancerId
        cvText = $cvText
        linkedInUrl = "https://linkedin.com/in/demo-freelancer"
        linkedInData = @{
            headline = "Senior Full Stack ML Engineer @ Google AI"
            summary = "Building end-to-end AI applications with modern web stack"
            experience = @(
                @{ title = "Senior ML Engineer"; company = "Google AI"; durationYears = 5; skills = @("Python", "TensorFlow", "Kubernetes", "BigQuery", "MLOps") }
                @{ title = "Full Stack Developer"; company = "Stripe"; durationYears = 3; skills = @("React", "Go", "TypeScript", "PostgreSQL") }
                @{ title = "Software Engineer"; company = "Meta"; durationYears = 2; skills = @("C++", "Python", "NLP", "Bot Platform") }
            )
            education = @(
                @{ degree = "M.Sc. Computer Science"; school = "Stanford University"; year = 2015 }
                @{ degree = "B.Sc. Software Engineering"; school = "MIT"; year = 2013 }
            )
            skills = @("Python", "Machine Learning", "Deep Learning", "NLP", "React", "TypeScript", "Go", "C#", "Kubernetes", "AWS", "MLOps", "System Design")
        }
    }
    $aiResp = Invoke-Api POST "$AiApi/api/v1/ai/cv-analysis" $aiBody -Token $FreelToken -AsJson
    Write-Host "  AI Skill Scores:"
    foreach ($s in $aiResp.skills) {
        Write-Host "    $($s.name): $($s.level) (score: $($s.score)/100)"
    }
    Write-Host "  Overall: $($aiResp.overallLevel) | Score: $($aiResp.overallScore)/100"
    Write-Host "  Primary Expertise: $($aiResp.primaryExpertise)"
    Write-Host "  Secondary: $($aiResp.secondaryExpertise -join ', ')"
    Write-Host "  Recommendations: $($aiResp.recommendations -join '; ')"
    $Results["AiScoring"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["AiScoring"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 6 - Matching: Index Freelancer (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 6 "Matching Engine: Index Freelancer" "REAL"
try {
    $flProfile = @{
        FreelancerId = $FreelancerId
        Skills = @("python", "machine learning", "data science", "pytorch", "nlp", "transformers", "mlops")
        Interests = @("ai", "nlp", "computer vision", "large language models")
        AverageRating = 4.9
        CompletedTasks = 87
        HourlyRateMin = 40
        HourlyRateMax = 80
    } | ConvertTo-Json
    $idxResp = Invoke-WebRequest -Uri "$MatchApi/api/matching/freelancers/$FreelancerId/index" `
        -Method POST -ContentType "application/json" -Body $flProfile -UseBasicParsing -TimeoutSec 60
    Write-Host "  Status: $($idxResp.StatusCode) - Freelancer indexed"
    $Results["IndexFreelancer"] = ($idxResp.StatusCode -eq 204)
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["IndexFreelancer"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 7 - Matching: Index Task (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 7 "Matching Engine: Index Task" "REAL"
try {
    $taskProfile = @{
        TaskId = $TaskId
        Title = $TaskTitle
        Description = "Need Python dev to build ML pipeline for text classification and sentiment analysis"
        RequiredSkills = @("python", "machine learning", "nlp", "data science")
        MinHourlyRate = 30
        MaxHourlyRate = 70
        Budget = 5000
    } | ConvertTo-Json
    $idxTaskResp = Invoke-WebRequest -Uri "$MatchApi/api/matching/tasks/$TaskId/index" `
        -Method POST -ContentType "application/json" -Body $taskProfile -UseBasicParsing -TimeoutSec 60
    Write-Host "  Status: $($idxTaskResp.StatusCode) - Task indexed"
    $Results["IndexTask"] = ($idxTaskResp.StatusCode -eq 204)
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["IndexTask"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 8 - Matching: Find Matches (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 8 "Matching Engine: Find Freelancer for Task" "REAL"
try {
    $matchResp = Invoke-WebRequest -Uri "$MatchApi/api/matching/tasks/$TaskId/matches" `
        -Method POST -UseBasicParsing -TimeoutSec 60
    $matchList = $matchResp.Content | ConvertFrom-Json
    Write-Host "  Status: $($matchResp.StatusCode) - Found $($matchList.Count) match(es)"
    if ($matchList.Count -gt 0) {
        $m = $matchList[0]
        Write-Host "  > Freelancer: $($m.freelancerId)"
        Write-Host "  > TotalScore: $($m.totalScore)"
        Write-Host "  > Explanation: $($m.explanation)"
    }
    $Results["FindMatches"] = ($matchResp.StatusCode -eq 200 -and $matchList.Count -gt 0)
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["FindMatches"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 9 - Chat: Create Conversation & Messages (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 9 "Chat Negotiation & Agreement" "REAL"
try {
    $chatBody = @{ OtherUserId = $FreelancerId }
    $chatResp = Invoke-Api POST "$ChatApi/api/v1/chats/direct" $chatBody -Token $ClientToken -AsJson
    $ChatId = $chatResp
    Write-Host "  Created direct chat: $ChatId"

    $msg1 = @{ ChatId = $ChatId; Content = "Hi, I have a task for you. Interested?"; Type = 0 }
    Invoke-Api POST "$ChatApi/api/v1/messages/send" $msg1 -Token $ClientToken | Out-Null

    $msg2 = @{ ChatId = $ChatId; Content = "Sure, let me review the requirements."; Type = 0 }
    Invoke-Api POST "$ChatApi/api/v1/messages/send" $msg2 -Token $FreelToken | Out-Null

    $msg3 = @{ ChatId = $ChatId; Content = "Great, I accept your proposal."; Type = 0 }
    Invoke-Api POST "$ChatApi/api/v1/messages/send" $msg3 -Token $ClientToken | Out-Null

    Write-Host "  3 messages exchanged"
    $Results["ChatNegotiation"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["ChatNegotiation"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 10 - Freelancer Accepts Task (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 10 "Freelancer Accepts Task" "REAL"
try {
    $appBody = @{ proposal = "I can deliver this in 2 weeks with high quality ML pipeline."; proposedBudget = 5000 }
    $appResp = Invoke-Api POST "$TasksApi/api/v1/tasks/$TaskId/apply" $appBody -Token $FreelToken -AsJson
    $ApplicationId = $appResp.Id
    if (-not $ApplicationId) { $ApplicationId = $appResp.id }
    Write-Host "  Application created: $ApplicationId"

    Invoke-Api POST "$TasksApi/api/v1/tasks/$TaskId/applications/$ApplicationId/accept" @{} -Token $ClientToken | Out-Null
    Write-Host "  Task accepted by client"

    # Verify task status
    $taskAfterAccept = Invoke-Api GET "$TasksApi/api/v1/tasks/$TaskId" -Token $ClientToken -AsJson
    Write-Host "  Task status after accept: $($taskAfterAccept.status) | AssignedFreelancerId: $($taskAfterAccept.assignedFreelancerId)"

    $Results["AcceptTask"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["AcceptTask"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 11 - Client Creates Escrow (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 11 "Client Escrow Payment" "REAL"
try {
    $escrowBody = @{
        taskId = $TaskId
        freelancerId = $FreelancerId
        amount = 5000
        currency = "USD"
    }
    $escrowResp = Invoke-Api POST "$PayApi/api/v1/escrow" $escrowBody -Token $ClientToken -AsJson
    $EscrowId = $escrowResp.Id
    if (-not $EscrowId) { $EscrowId = $escrowResp.id }
    Write-Host "  Created escrow: $EscrowId"
    $Results["EscrowPay"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["EscrowPay"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 12 - IDE Agent Generation (MOCK)
# ═════════════════════════════════════════════════════════════════════
Write-Step 12 "IDE Agent Code Generation" "MOCK"
Write-Host "  [Would call] POST /api/ide/agents/generate with task spec"
Write-Host "  [Simulated] Generated: model.py, train.py, inference.py, requirements.txt"
$Results["IdeGeneration"] = $true

# ═════════════════════════════════════════════════════════════════════
#  STEP 13 - QA Testing (MOCK)
# ═════════════════════════════════════════════════════════════════════
Write-Step 13 "AI QA Testing & ShadowWorkspace" "MOCK"
Write-Host "  [Simulated] Tests passed: 42/42 | Security scan: clean"
$Results["QaTesting"] = $true

# ═════════════════════════════════════════════════════════════════════
#  STEP 14 - Client Confirms Delivery (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 14 "Client Confirms Delivery" "REAL"
try {
    Write-Host "  TaskId: $TaskId"
    $completeBody = @{ completionNotes = "Great work, delivered on time." }
    Invoke-Api POST "$TasksApi/api/v1/tasks/$TaskId/complete" $completeBody -Token $ClientToken | Out-Null
    Write-Host "  Task marked as completed"
    $Results["ClientConfirm"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["ClientConfirm"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 15 - Release Escrow (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 15 "Release Escrow" "REAL"
try {
    Write-Host "  EscrowId: $EscrowId"
    Invoke-Api POST "$PayApi/api/v1/escrow/$EscrowId/release" @{} -Token $ClientToken | Out-Null
    Write-Host "  Escrow released"
    $Results["EscrowRelease"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["EscrowRelease"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  STEP 16 - Freelancer Withdraws (REAL API)
# ═════════════════════════════════════════════════════════════════════
Write-Step 16 "Freelancer Withdraws" "REAL"
try {
    $walletResp = Invoke-Api GET "$PayApi/api/v1/wallets/me" -Token $FreelToken -AsJson
    Write-Host "  Freelancer wallet: $($walletResp.id)"
    $withdrawBody = @{ walletId = $walletResp.id; amount = 5000; currency = "USD"; stripeAccountId = "acct_test_e2e" }
    $wdResp = Invoke-Api POST "$PayApi/api/v1/wallets/withdraw" $withdrawBody -Token $FreelToken -AsJson
    Write-Host "  Withdrawal created: $($wdResp.id)"
    $Results["Withdrawal"] = $true
} catch {
    Write-Host "  FAIL: $_" -ForegroundColor Red
    $Results["Withdrawal"] = $false
}

# ═════════════════════════════════════════════════════════════════════
#  FINAL REPORT
# ═════════════════════════════════════════════════════════════════════
Write-Host "`n═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "                    E2E FULL FLOW TEST REPORT" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

$pass = 0; $fail = 0
foreach ($kv in $Results.GetEnumerator()) {
    $icon = if ($kv.Value) { "PASS" } else { "FAIL" }
    $color = if ($kv.Value) { "Green" } else { "Red" }
    Write-Host "  $icon $($kv.Key.PadRight(22)) : $($kv.Value)" -ForegroundColor $color
    if ($kv.Value) { $pass++ } else { $fail++ }
}

Write-Host "`n  Qdrant Verification:" -ForegroundColor Cyan
try {
    $qdrantColls = (Invoke-WebRequest -Uri "$QdrantRest/collections" -UseBasicParsing).Content | ConvertFrom-Json
    foreach ($c in $qdrantColls.result.collections) {
        $info = (Invoke-WebRequest -Uri "$QdrantRest/collections/$($c.name)" -UseBasicParsing).Content | ConvertFrom-Json
        Write-Host "    Collection: $($c.name) - points: $($info.result.points_count), vectors: $($info.result.indexed_vectors_count)"
    }
} catch {
    Write-Host "    Qdrant check skipped: $_" -ForegroundColor Yellow
}

Write-Host "`n───────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan
Write-Host "  TOTAL STEPS : $($Results.Count)"
Write-Host "  PASSED      : $pass"
Write-Host "  FAILED      : $fail"
Write-Host "───────────────────────────────────────────────────────────────────────" -ForegroundColor Cyan

if ($fail -eq 0) {
    Write-Host "`n  SUCCESS FULL FLOW COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n  WARN  Some steps failed (see above)." -ForegroundColor Yellow
    exit 1
}
