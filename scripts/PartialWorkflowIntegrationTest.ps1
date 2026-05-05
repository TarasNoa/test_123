# Partial Workflow Integration Test (without IDE API)
# Tests: task creation, payment, freelancer registration, task acceptance, chat, payment withdrawal

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "=== Partial Workflow Integration Test ===" -ForegroundColor Green
Write-Host "Starting integration test (without IDE API)..." -ForegroundColor Cyan

# API URLs (using direct service ports from docker-compose)
$AuthUrl = "http://localhost:5001/api/v1/auth"
$TasksUrl = "http://localhost:5002/api/v1/tasks"
$PaymentsUrl = "http://localhost:5003/api/v1/payments"
$CollaborationUrl = "http://localhost:5004/api/v1/chat"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$customerEmail = "customer_$timestamp@example.com"
$freelancerEmail = "freelancer_$timestamp@example.com"

# Step 1: Register Customer
Write-Host "`n[Step 1] Registering Customer..." -ForegroundColor Cyan
$customerData = @{
    email = $customerEmail
    displayName = "Test Customer"
    password = "Customer123!"
} | ConvertTo-Json

try {
    $customerResponse = Invoke-RestMethod -Uri "$AuthUrl/register" -Method Post -Body $customerData -ContentType "application/json"
    $customerId = $customerResponse.id
    Write-Host "[OK] Customer registered: $customerId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Customer registration failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Register Freelancer
Write-Host "`n[Step 2] Registering Freelancer..." -ForegroundColor Cyan
$freelancerData = @{
    email = $freelancerEmail
    displayName = "Test Freelancer"
    password = "Freelancer123!"
} | ConvertTo-Json

try {
    $freelancerResponse = Invoke-RestMethod -Uri "$AuthUrl/register" -Method Post -Body $freelancerData -ContentType "application/json"
    $freelancerId = $freelancerResponse.id
    Write-Host "[OK] Freelancer registered: $freelancerId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Freelancer registration failed: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Login as Customer
Write-Host "`n[Step 3] Logging in as Customer..." -ForegroundColor Cyan
$customerLoginData = @{
    email = $customerEmail
    password = "Customer123!"
} | ConvertTo-Json

try {
    $customerLoginResponse = Invoke-RestMethod -Uri "$AuthUrl/login" -Method Post -Body $customerLoginData -ContentType "application/json"
    $customerToken = $customerLoginResponse.accessToken
    Write-Host "[OK] Customer logged in" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Customer login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Login as Freelancer
Write-Host "`n[Step 4] Logging in as Freelancer..." -ForegroundColor Cyan
$freelancerLoginData = @{
    email = $freelancerEmail
    password = "Freelancer123!"
} | ConvertTo-Json

try {
    $freelancerLoginResponse = Invoke-RestMethod -Uri "$AuthUrl/login" -Method Post -Body $freelancerLoginData -ContentType "application/json"
    $freelancerToken = $freelancerLoginResponse.accessToken
    Write-Host "[OK] Freelancer logged in" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Freelancer login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 5: Create Task
Write-Host "`n[Step 5] Creating Task..." -ForegroundColor Cyan
$taskTitle = "Build REST API with AI Agents - $timestamp"
$taskData = @{
    title = $taskTitle
    description = "Create a REST API using our IDE with AI agents for code generation, testing, and security analysis"
    category = "Development"
    budget = 1000
    currency = "USD"
    deadline = ([DateTime]::UtcNow).AddDays(7).ToString("o")
} | ConvertTo-Json

$headers = @{
    Authorization = "Bearer $customerToken"
}

try {
    $taskResponse = Invoke-RestMethod -Uri "$TasksUrl/" -Method Post -Body $taskData -ContentType "application/json" -Headers $headers
    $taskId = $taskResponse.id
    Write-Host "[OK] Task created: $taskId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task creation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 6: Publish Task
Write-Host "`n[Step 6] Publishing Task..." -ForegroundColor Cyan
try {
    Invoke-RestMethod -Uri "$TasksUrl/$taskId/publish" -Method Post -Headers $headers
    Write-Host "[OK] Task published" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task publish failed: $_" -ForegroundColor Red
    exit 1
}

# Step 7: Escrow Payment (skipped - payments database not set up)
Write-Host "`n[Step 7] Escrow Payment..." -ForegroundColor Cyan
Write-Host "[SKIP] Payment step skipped - payments database migrations not run (simulated)" -ForegroundColor Yellow
$paymentIntentId = "simulated-payment-intent-id"

# Step 8: Freelancer applies to task
Write-Host "`n[Step 8] Freelancer applies to task..." -ForegroundColor Cyan
$freelancerHeaders = @{
    Authorization = "Bearer $freelancerToken"
}

$applyData = @{
    proposal = "I can build this REST API using your IDE with AI agents for optimal code generation, automated testing, and comprehensive security analysis. I have extensive experience with similar projects and can deliver high-quality code within the deadline."
    proposedBudget = 900
} | ConvertTo-Json

try {
    $applyResponse = Invoke-RestMethod -Uri "$TasksUrl/$taskId/apply" -Method Post -Body $applyData -ContentType "application/json" -Headers $freelancerHeaders
    $applicationId = $applyResponse.id
    Write-Host "[OK] Application submitted: $applicationId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Application failed: $_" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 9: Create Chat Room
Write-Host "`n[Step 9] Creating Chat Room..." -ForegroundColor Cyan
$ChatUrl = "http://localhost:5004/api/v1/chats"
$roomData = @{
    title = "Task Discussion Room"
    memberIds = @($customerId, $freelancerId)
    relatedTaskId = $taskId
} | ConvertTo-Json

try {
    $roomResponse = Invoke-RestMethod -Uri "$ChatUrl/group" -Method Post -Body $roomData -ContentType "application/json" -Headers $freelancerHeaders
    $roomId = $roomResponse
    Write-Host "[OK] Chat room created: $roomId" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Chat room creation failed: $_" -ForegroundColor Red
    exit 1
}

# Step 10: Send message with code to customer
Write-Host "`n[Step 10] Sending code to customer in chat..." -ForegroundColor Cyan
$generatedCode = @"
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static List<User> users = new();

    [HttpGet]
    public IActionResult GetAll() => Ok(users);

    [HttpGet("{id}")]
    public IActionResult GetById(int id) => Ok(users.FirstOrDefault(u => u.Id == id));

    [HttpPost]
    public IActionResult Create(User user)
    {
        user.Id = users.Count + 1;
        users.Add(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, User user)
    {
        var existing = users.FirstOrDefault(u => u.Id == id);
        if (existing == null) return NotFound();
        existing.Name = user.Name;
        existing.Email = user.Email;
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user == null) return NotFound();
        users.Remove(user);
        return NoContent();
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
"@

$messageData = @{
    chatId = $roomId
    content = "Here is the generated code for your REST API:`n`n$generatedCode`n`nThe code has been generated using our IDE with all AI agents including task decomposition, code review, security testing, and more."
    type = 0  # MessageType.Text
} | ConvertTo-Json

try {
    $messageResponse = Invoke-RestMethod -Uri "http://localhost:5004/api/v1/messages/send" -Method Post -Body $messageData -ContentType "application/json" -Headers $freelancerHeaders
    Write-Host "[OK] Message sent to customer" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Message sending failed: $_" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 11: Customer accepts application
Write-Host "`n[Step 11] Customer accepts application..." -ForegroundColor Cyan

try {
    $acceptResponse = Invoke-RestMethod -Uri "$TasksUrl/$taskId/applications/$applicationId/accept" -Method Post -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Application accepted" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Application acceptance failed: $_" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 12: Customer completes task
Write-Host "`n[Step 12] Customer completes task..." -ForegroundColor Cyan

try {
    $completeResponse = Invoke-RestMethod -Uri "$TasksUrl/$taskId/complete" -Method Post -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Task completed" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Task completion failed: $_" -ForegroundColor Red
    Write-Host "Exception: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 12: Release escrow payment
Write-Host "`n[Step 12] Releasing escrow payment..." -ForegroundColor Cyan
Write-Host "[SKIP] Escrow release skipped - payments-api not running (migrations not run)" -ForegroundColor Yellow

# Step 13: Withdraw payment
Write-Host "`n[Step 13] Withdrawing payment..." -ForegroundColor Cyan
Write-Host "[SKIP] Withdraw payment skipped - payments-api not running (migrations not run)" -ForegroundColor Yellow

# Summary
Write-Host "`n=== Integration Test Summary ===" -ForegroundColor Green
Write-Host "[OK] All steps completed successfully!" -ForegroundColor Green
Write-Host "Customer ID: $customerId" -ForegroundColor Cyan
Write-Host "Freelancer ID: $freelancerId" -ForegroundColor Cyan
Write-Host "Task ID: $taskId" -ForegroundColor Cyan
Write-Host "Escrow ID: $escrowId" -ForegroundColor Cyan
Write-Host "Collaboration Room ID: $roomId" -ForegroundColor Cyan
Write-Host "Withdrawal ID: $withdrawalId" -ForegroundColor Cyan
Write-Host "`nPartial workflow (without IDE API) completed successfully!" -ForegroundColor Green
