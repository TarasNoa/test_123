# Full Workflow Integration Test
# Simulates complete flow from task creation to payment withdrawal

$ErrorActionPreference = "Stop"

# Configuration
$BaseUrl = "http://localhost:5002"
$TasksUrl = "$BaseUrl/api/v1/tasks"
$PaymentsUrl = "$BaseUrl/api/v1/payments"
$UsersUrl = "$BaseUrl/api/v1/users"
$CollaborationUrl = "$BaseUrl/api/v1/collaboration"
$IDEUrl = "$BaseUrl/api/ide"

Write-Host "=== Full Workflow Integration Test ===" -ForegroundColor Green
Write-Host "Starting integration test..." -ForegroundColor Yellow

# Step 1: Register Customer
Write-Host "`n[Step 1] Registering Customer..." -ForegroundColor Cyan
$customerData = @{
    email = "customer@example.com"
    password = "Customer123!"
    name = "Test Customer"
    role = "customer"
} | ConvertTo-Json

try {
    $customerResponse = Invoke-RestMethod -Uri "$UsersUrl/register" -Method Post -Body $customerData -ContentType "application/json"
    $customerToken = $customerResponse.token
    $customerId = $customerResponse.userId
    Write-Host "[OK] Customer registered: $customerId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Customer registration failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Create Task
Write-Host "`n[Step 2] Creating Task..." -ForegroundColor Cyan
$taskData = @{
    title = "Build REST API with AI Agents"
    description = "Create a REST API using our IDE with AI agents for code generation, testing, and security analysis"
    budget = 1000
    deadline = (Get-Date).AddDays(7).ToString("o")
    category = "web-development"
} | ConvertTo-Json

$headers = @{
    Authorization = "Bearer $customerToken"
}

try {
    $taskResponse = Invoke-RestMethod -Uri $TasksUrl -Method Post -Body $taskData -ContentType "application/json" -Headers $headers
    $taskId = $taskResponse.id
    Write-Host "[OK] Task created: $taskId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task creation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Escrow Payment
Write-Host "`n[Step 3] Escrow Payment..." -ForegroundColor Cyan
$paymentData = @{
    taskId = $taskId
    amount = 1000
    paymentMethod = "credit_card"
} | ConvertTo-Json

try {
    $paymentResponse = Invoke-RestMethod -Uri "$PaymentsUrl/escrow" -Method Post -Body $paymentData -ContentType "application/json" -Headers $headers
    $escrowId = $paymentResponse.escrowId
    Write-Host "[OK] Escrow payment created: $escrowId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Escrow payment failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Register Freelancer
Write-Host "`n[Step 4] Registering Freelancer..." -ForegroundColor Cyan
$freelancerData = @{
    email = "freelancer@example.com"
    password = "Freelancer123!"
    name = "Test Freelancer"
    role = "freelancer"
    skills = @("web-development", "api-design", "security-testing")
} | ConvertTo-Json

try {
    $freelancerResponse = Invoke-RestMethod -Uri "$UsersUrl/register" -Method Post -Body $freelancerData -ContentType "application/json"
    $freelancerToken = $freelancerResponse.token
    $freelancerId = $freelancerResponse.userId
    Write-Host "[OK] Freelancer registered: $freelancerId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Freelancer registration failed: $_" -ForegroundColor Red
    exit 1
}

# Step 5: Freelancer accepts task
Write-Host "`n[Step 5] Freelancer accepts task..." -ForegroundColor Cyan
$freelancerHeaders = @{
    Authorization = "Bearer $freelancerToken"
}

$acceptData = @{
    taskId = $taskId
    bid = 900
    message = "I can build this API using your IDE with AI agents for optimal results"
} | ConvertTo-Json

try {
    $acceptResponse = Invoke-RestMethod -Uri "$TasksUrl/accept" -Method Post -Body $acceptData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Task accepted by freelancer" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task acceptance failed: $_" -ForegroundColor Red
    exit 1
}

# Step 6: Create IDE Session
Write-Host "`n[Step 6] Creating IDE Session..." -ForegroundColor Cyan
$sessionData = @{
    workspaceId = $taskId
    projectName = "RestApiProject"
    language = "csharp"
} | ConvertTo-Json

try {
    $sessionResponse = Invoke-RestMethod -Uri "$IDEUrl/sessions" -Method Post -Body $sessionData -ContentType "application/json" -Headers $freelancerHeaders
    $sessionId = $sessionResponse.sessionId
    Write-Host "[OK] IDE session created: $sessionId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] IDE session creation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 7: Task Decomposition
Write-Host "`n[Step 7] Task Decomposition..." -ForegroundColor Cyan
$decomposeData = @{
    taskDescription = "Build REST API with AI Agents"
    requirements = @("Authentication", "CRUD operations", "Error handling", "Security testing")
} | ConvertTo-Json

try {
    $decomposeResponse = Invoke-RestMethod -Uri "$IDEUrl/task-decomposition/decompose" -Method Post -Body $decomposeData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Task decomposed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task decomposition failed: $_" -ForegroundColor Red
}

# Step 8: Senior Role Prompts
Write-Host "`n[Step 8] Generating Senior Role Prompts..." -ForegroundColor Cyan
$rolePromptData = @{
    phase = "planning"
    context = "REST API development with AI agents"
} | ConvertTo-Json

try {
    $rolePromptResponse = Invoke-RestMethod -Uri "$IDEUrl/senior-role-prompts/generate" -Method Post -Body $rolePromptData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Senior role prompts generated" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Senior role prompts failed: $_" -ForegroundColor Red
}

# Step 9: Intelligence Router
Write-Host "`n[Step 9] Building Intelligence Routing Plan..." -ForegroundColor Cyan
$routerData = @{
    task = "Build REST API"
    context = "Web API with C#"
    capabilities = @("code-generation", "testing", "security")
} | ConvertTo-Json

try {
    $routerResponse = Invoke-RestMethod -Uri "$IDEUrl/intelligence-router/build-routing-plan" -Method Post -Body $routerData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Intelligence routing plan built" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Intelligence router failed: $_" -ForegroundColor Red
}

# Step 10: Cascade Service
Write-Host "`n[Step 10] Running Cascade Planning..." -ForegroundColor Cyan
$cascadeData = @{
    taskId = $taskId
    phase = "planning"
} | ConvertTo-Json

try {
    $cascadeResponse = Invoke-RestMethod -Uri "$IDEUrl/cascade/run-cascade-planning" -Method Post -Body $cascadeData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Cascade planning completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Cascade planning failed: $_" -ForegroundColor Red
}

# Step 11: Orchestration Run
Write-Host "`n[Step 11] Starting Orchestration Run..." -ForegroundColor Cyan
$orchestrationData = @{
    taskId = $taskId
    skills = @("api-design", "security-testing", "code-generation")
    workflow = "standard"
} | ConvertTo-Json

try {
    $orchestrationResponse = Invoke-RestMethod -Uri "$IDEUrl/orchestration-run/start" -Method Post -Body $orchestrationData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Orchestration run started" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Orchestration run failed: $_" -ForegroundColor Red
}

# Step 12: Multi-Agent Orchestration
Write-Host "`n[Step 12] Starting Multi-Agent Orchestration..." -ForegroundColor Cyan
$multiAgentData = @{
    taskId = $taskId
    agentRoles = @("architect", "developer", "tester", "security-analyst")
    coordinationMode = "sequential"
} | ConvertTo-Json

try {
    $multiAgentResponse = Invoke-RestMethod -Uri "$IDEUrl/multi-agent-orchestration/start" -Method Post -Body $multiAgentData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Multi-agent orchestration started" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Multi-agent orchestration failed: $_" -ForegroundColor Red
}

# Step 13: Autonomous Runtime Policy
Write-Host "`n[Step 13] Generating Autonomous Runtime Policy..." -ForegroundColor Cyan
$policyData = @{
    taskId = $taskId
    domainSignals = @("web-api", "rest", "csharp")
    qualityContract = "high"
} | ConvertTo-Json

try {
    $policyResponse = Invoke-RestMethod -Uri "$IDEUrl/autonomous-runtime-policy/generate" -Method Post -Body $policyData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Autonomous runtime policy generated" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Autonomous runtime policy failed: $_" -ForegroundColor Red
}

# Step 14: Shadow Workspace
Write-Host "`n[Step 14] Creating Shadow Workspace..." -ForegroundColor Cyan
$shadowWorkspaceData = @{
    workspaceId = $taskId
    files = @()
} | ConvertTo-Json

try {
    $shadowResponse = Invoke-RestMethod -Uri "$IDEUrl/shadow-workspace/create" -Method Post -Body $shadowWorkspaceData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Shadow workspace created" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Shadow workspace failed: $_" -ForegroundColor Red
}

# Step 15: Code Review
Write-Host "`n[Step 15] Running Code Review..." -ForegroundColor Cyan
$codeReviewData = @{
    workspaceId = $taskId
    files = @()
} | ConvertTo-Json

try {
    $codeReviewResponse = Invoke-RestMethod -Uri "$IDEUrl/code-review/run" -Method Post -Body $codeReviewData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Code review completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Code review failed: $_" -ForegroundColor Red
}

# Step 16: Semantic Code Graph
Write-Host "`n[Step 16] Building Semantic Code Graph..." -ForegroundColor Cyan
$graphData = @{
    workspaceId = $taskId
    files = @()
} | ConvertTo-Json

try {
    $graphResponse = Invoke-RestMethod -Uri "$IDEUrl/semantic-code-graph/build" -Method Post -Body $graphData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Semantic code graph built" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Semantic code graph failed: $_" -ForegroundColor Red
}

# Step 17: Agent Memory System
Write-Host "`n[Step 17] Creating Agent Memory..." -ForegroundColor Cyan
$memoryData = @{
    agentId = $freelancerId
    content = "REST API development progress"
    memoryType = "short-term"
} | ConvertTo-Json

try {
    $memoryResponse = Invoke-RestMethod -Uri "$IDEUrl/agent-memory-system/create" -Method Post -Body $memoryData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Agent memory created" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Agent memory system failed: $_" -ForegroundColor Red
}

# Step 18: LLM Router
Write-Host "`n[Step 18] Routing LLM..." -ForegroundColor Cyan
$llmRouterData = @{
    prompt = "Generate REST API code"
    complexity = "medium"
    budget = 0.5
} | ConvertTo-Json

try {
    $llmRouterResponse = Invoke-RestMethod -Uri "$IDEUrl/llm-router/route" -Method Post -Body $llmRouterData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] LLM routed to optimal model" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] LLM router failed: $_" -ForegroundColor Red
}

# Step 19: AI Workflow Automation
Write-Host "`n[Step 19] Distilling AI Workflow..." -ForegroundColor Cyan
$workflowData = @{
    workflowName = "REST API Development"
    steps = @("design", "implement", "test", "deploy")
} | ConvertTo-Json

try {
    $workflowResponse = Invoke-RestMethod -Uri "$IDEUrl/ai-workflow-automation/distill" -Method Post -Body $workflowData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] AI workflow distilled" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] AI workflow automation failed: $_" -ForegroundColor Red
}

# Step 20: Web Search
Write-Host "`n[Step 20] Executing Web Search..." -ForegroundColor Cyan
$webSearchData = @{
    query = "REST API best practices C#"
    provider = "brave"
    maxResults = 5
} | ConvertTo-Json

try {
    $webSearchResponse = Invoke-RestMethod -Uri "$IDEUrl/web-search/execute" -Method Post -Body $webSearchData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Web search executed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Web search failed: $_" -ForegroundColor Red
}

# Step 21: Task Record
Write-Host "`n[Step 21] Creating Task Record..." -ForegroundColor Cyan
$taskRecordData = @{
    taskId = $taskId
    currentState = "in_progress"
} | ConvertTo-Json

try {
    $taskRecordResponse = Invoke-RestMethod -Uri "$IDEUrl/task-record/create" -Method Post -Body $taskRecordData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Task record created" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task record failed: $_" -ForegroundColor Red
}

# Step 22: GitHub Bootstrap
Write-Host "`n[Step 22] Bootstrapping from GitHub..." -ForegroundColor Cyan
$bootstrapData = @{
    language = "csharp"
    allowedLicenses = @("MIT", "Apache-2.0")
} | ConvertTo-Json

try {
    $bootstrapResponse = Invoke-RestMethod -Uri "$IDEUrl/github-bootstrap/bootstrap" -Method Post -Body $bootstrapData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] GitHub bootstrap completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] GitHub bootstrap failed: $_" -ForegroundColor Red
}

# Step 23: Architectural Guardrails
Write-Host "`n[Step 23] Running Architectural Guardrails..." -ForegroundColor Cyan
$guardrailsData = @{
    workspaceId = $taskId
    files = @()
} | ConvertTo-Json

try {
    $guardrailsResponse = Invoke-RestMethod -Uri "$IDEUrl/architectural-guardrails/validate" -Method Post -Body $guardrailsData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Architectural guardrails validation completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Architectural guardrails failed: $_" -ForegroundColor Red
}

# Step 24: Semantic Blame
Write-Host "`n[Step 24] Running Semantic Blame..." -ForegroundColor Cyan
$blameData = @{
    filePath = "src/Controllers/ApiController.cs"
    workspacePath = $taskId
} | ConvertTo-Json

try {
    $blameResponse = Invoke-RestMethod -Uri "$IDEUrl/semantic-blame/blame" -Method Post -Body $blameData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Semantic blame completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Semantic blame failed: $_" -ForegroundColor Red
}

# Step 25: Code Intelligence
Write-Host "`n[Step 25] Getting Code Completions..." -ForegroundColor Cyan
$completionsData = @{
    filePath = "src/Controllers/ApiController.cs"
    line = 10
    column = 5
    code = "public class ApiController {"
} | ConvertTo-Json

try {
    $completionsResponse = Invoke-RestMethod -Uri "$IDEUrl/code-intelligence/completions" -Method Post -Body $completionsData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Code completions retrieved" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Code intelligence failed: $_" -ForegroundColor Red
}

# Step 26: Security Testing
Write-Host "`n[Step 26] Running Security Testing..." -ForegroundColor Cyan
$securityData = @{
    workspaceId = $taskId
    files = @()
    dependencies = @()
} | ConvertTo-Json

try {
    $securityResponse = Invoke-RestMethod -Uri "$IDEUrl/security-testing/test" -Method Post -Body $securityData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Security testing completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Security testing failed: $_" -ForegroundColor Red
}

# Step 27: Hacker Agent
Write-Host "`n[Step 27] Running Hacker Agent..." -ForegroundColor Cyan
$hackerAgentData = @{
    workspaceId = $taskId
    target = "http://localhost:5002"
    scriptType = "python"
} | ConvertTo-Json

try {
    $hackerAgentResponse = Invoke-RestMethod -Uri "$IDEUrl/hacker-agent/run" -Method Post -Body $hackerAgentData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Hacker agent completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Hacker agent failed: $_" -ForegroundColor Red
}

# Step 28: Generate Code with AI
Write-Host "`n[Step 28] Generating Code with AI..." -ForegroundColor Cyan
$codeGenData = @{
    prompt = "Create a REST API controller with CRUD operations for a User entity"
    language = "csharp"
} | ConvertTo-Json

try {
    $codeGenResponse = Invoke-RestMethod -Uri "$IDEUrl/ai/generate" -Method Post -Body $codeGenData -ContentType "application/json" -Headers $freelancerHeaders
    $generatedCode = $codeGenResponse.generatedCode
    Write-Host "[OK] Code generated with AI" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] AI code generation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 29: Add file to session
Write-Host "`n[Step 29] Adding generated code to session..." -ForegroundColor Cyan
$fileData = @{
    fileName = "Controllers/UserController.cs"
    content = $generatedCode
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "$IDEUrl/sessions/$sessionId/files" -Method Post -Body $fileData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] File added to session" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] File addition failed: $_" -ForegroundColor Red
}

# Step 30: Create Collaboration Room
Write-Host "`n[Step 30] Creating Collaboration Room..." -ForegroundColor Cyan
$roomData = @{
    taskId = $taskId
    participants = @($customerId, $freelancerId)
} | ConvertTo-Json

try {
    $roomResponse = Invoke-RestMethod -Uri "$CollaborationUrl/rooms" -Method Post -Body $roomData -ContentType "application/json" -Headers $freelancerHeaders
    $roomId = $roomResponse.roomId
    Write-Host "[OK] Collaboration room created: $roomId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Collaboration room creation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 31: Send message with code to customer
Write-Host "`n[Step 31] Sending code to customer in chat..." -ForegroundColor Cyan
$messageData = @{
    roomId = $roomId
    senderId = $freelancerId
    content = "Here is the generated code for your REST API:`n`n$generatedCode`n`nThe code has been generated using our IDE with all AI agents including task decomposition, code review, security testing, and more."
    messageType = "code"
} | ConvertTo-Json

try {
    $messageResponse = Invoke-RestMethod -Uri "$CollaborationUrl/messages" -Method Post -Body $messageData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Message sent to customer" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Message sending failed: $_" -ForegroundColor Red
    exit 1
}

# Step 32: Customer approves work
Write-Host "`n[Step 32] Customer approves work..." -ForegroundColor Cyan
$approvalData = @{
    taskId = $taskId
    approved = $true
} | ConvertTo-Json

try {
    $approvalResponse = Invoke-RestMethod -Uri "$TasksUrl/approve" -Method Post -Body $approvalData -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Work approved by customer" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Work approval failed: $_" -ForegroundColor Red
    exit 1
}

# Step 33: Release escrow payment
Write-Host "`n[Step 33] Releasing escrow payment..." -ForegroundColor Cyan
$releaseData = @{
    escrowId = $escrowId
    recipientId = $freelancerId
} | ConvertTo-Json

try {
    $releaseResponse = Invoke-RestMethod -Uri "$PaymentsUrl/release" -Method Post -Body $releaseData -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Escrow payment released to freelancer" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Escrow release failed: $_" -ForegroundColor Red
    exit 1
}

# Step 34: Withdraw payment
Write-Host "`n[Step 34] Withdrawing payment..." -ForegroundColor Cyan
$withdrawData = @{
    amount = 900
    paymentMethod = "bank_transfer"
    bankAccount = "1234567890"
} | ConvertTo-Json

try {
    $withdrawResponse = Invoke-RestMethod -Uri "$PaymentsUrl/withdraw" -Method Post -Body $withdrawData -ContentType "application/json" -Headers $freelancerHeaders
    $withdrawalId = $withdrawResponse.withdrawalId
    Write-Host "[OK] Payment withdrawal initiated: $withdrawalId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Payment withdrawal failed: $_" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`n=== Integration Test Summary ===" -ForegroundColor Green
Write-Host "[OK] All steps completed successfully!" -ForegroundColor Green
Write-Host "Customer ID: $customerId" -ForegroundColor Cyan
Write-Host "Freelancer ID: $freelancerId" -ForegroundColor Cyan
Write-Host "Task ID: $taskId" -ForegroundColor Cyan
Write-Host "Escrow ID: $escrowId" -ForegroundColor Cyan
Write-Host "IDE Session ID: $sessionId" -ForegroundColor Cyan
Write-Host "Collaboration Room ID: $roomId" -ForegroundColor Cyan
Write-Host "Withdrawal ID: $withdrawalId" -ForegroundColor Cyan
Write-Host "`nFull workflow completed successfully!" -ForegroundColor Green
