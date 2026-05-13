#!/usr/bin/env python3
"""
Full Workflow Integration Test for Libr4 microservices.
Simulates: customer registration -> task creation -> freelancer applies -> collaboration -> task completion
"""
import os
import platform
import subprocess
import time
import json
import urllib.request
import urllib.error
from pathlib import Path

BASE = Path(os.environ.get("LIBR4_ROOT", Path(__file__).parent.parent))
if not (BASE / "libr4.sln").exists():
    BASE = Path("d:/lib4_project/libr4")
LOG_DIR = BASE / "e2e-logs"
LOG_DIR.mkdir(exist_ok=True)

APIS = [
    ("Auth",          BASE / "src/Services/Auth/Libr4.Auth.Api/Libr4.Auth.Api.csproj",              5001),
    ("Tasks",         BASE / "src/Services/Tasks/Libr4.Tasks.Api/Libr4.Tasks.Api.csproj",            5012),
    ("Payments",      BASE / "src/Services/Payments/Libr4.Payments.Api/Libr4.Payments.Api.csproj",    5010),
    ("IDE",           BASE / "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj",                  5008),
    ("Collaboration", BASE / "src/Services/Collaboration/Libr4.Collaboration.Api/Libr4.Collaboration.Api.csproj", 5015),
    ("Gateway",       BASE / "src/Gateway/Libr4.Gateway/Libr4.Gateway.csproj",                        5000),
]

processes = []

def start_apis():
    print("\n[1/4] Starting required APIs...")
    for name, proj, port in APIS:
        out = open(LOG_DIR / f"{name}-out.log", "w")
        err = open(LOG_DIR / f"{name}-err.log", "w")
        proc = subprocess.Popen(
            ["dotnet", "run", "--no-build", "--project", str(proj), "--urls", f"http://localhost:{port}"],
            stdout=out, stderr=err,
            env={**dict(subprocess.os.environ), "ASPNETCORE_ENVIRONMENT": "Development"},
            creationflags=(subprocess.CREATE_NEW_PROCESS_GROUP if platform.system() == "Windows" else 0)
        )
        processes.append((name, port, proc))
        print(f"  {name} -> PID {proc.pid} : {port}")
        time.sleep(2)

def stop_apis():
    print("\nStopping all API processes...")
    for name, port, proc in processes:
        try:
            proc.terminate()
            proc.wait(timeout=5)
        except Exception:
            try:
                proc.kill()
            except Exception:
                pass
    print("Done.")

def wait_for_apis(timeout=60):
    print(f"\n[2/4] Waiting for APIs to be ready (timeout={timeout}s)...")
    deadline = time.time() + timeout
    for name, _, port in APIS:
        while time.time() < deadline:
            try:
                status, _ = request("GET", f"http://localhost:{port}/health")
                if status == 200:
                    print(f"  {name} ready")
                    break
            except Exception:
                pass
            time.sleep(1)
        else:
            print(f"  WARNING: {name} not ready in {timeout}s")


def request(method, url, data=None, headers=None, timeout=10):
    req = urllib.request.Request(url, method=method)
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json")
    if headers:
        for k, v in headers.items():
            req.add_header(k, v)
    body = json.dumps(data).encode("utf-8") if data else None
    try:
        with urllib.request.urlopen(req, data=body, timeout=timeout) as resp:
            return resp.status, resp.read().decode("utf-8", errors="ignore")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", errors="ignore")
    except Exception as e:
        return 0, str(e)

def print_response(status, body):
    if status >= 400 or status == 0:
        print(f"    -> Error body: {body[:200]}")

def main():
    start_apis()
    wait_for_apis()

    results = []
    customer_token = None
    customer_id = None
    freelancer_token = None
    freelancer_id = None
    task_id = None
    app_id = None
    room_id = None
    session_id = None

    ts = str(int(time.time()))
    customer_email = f"customer-{ts}@example.com"
    freelancer_email = f"freelancer-{ts}@example.com"

    # Step 1: Register Customer
    print("\n[3/4] Running workflow steps...")
    print("\n[Step 1] Register Customer...")
    status, body = request("POST", "http://localhost:5001/api/v1/auth/register",
        {"email": customer_email, "password": "Customer123!", "displayName": "Test Customer"})
    ok = status == 201 or status == 200
    results.append(("Register Customer", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            customer_id = j.get("id") or ""
        except Exception:
            pass

    # Step 2: Login Customer
    print("\n[Step 2] Login Customer...")
    status, body = request("POST", "http://localhost:5001/api/v1/auth/login",
        {"email": customer_email, "password": "Customer123!"})
    ok = status == 200
    results.append(("Login Customer", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            customer_token = j.get("accessToken") or j.get("token") or ""
            customer_id = j.get("id") or j.get("userId") or ""
        except Exception:
            pass

    # Step 3: Create Task
    print("\n[Step 3] Create Task...")
    headers = {"Authorization": f"Bearer {customer_token}"} if customer_token else {}
    status, body = request("POST", "http://localhost:5012/api/v1/tasks",
        {"title": "Build REST API for E2E Test", "description": "Create a comprehensive REST API with authentication, CRUD operations, error handling, and security testing for end-to-end validation.", "category": "Development", "budget": 1000, "currency": "USD", "deadline": "2026-12-31T00:00:00Z"},
        headers)
    ok = status == 200 or status == 201
    results.append(("Create Task", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            task_id = j.get("id") or j.get("taskId") or ""
        except Exception:
            pass

    # Step 4: Register Freelancer
    print("\n[Step 4] Register Freelancer...")
    status, body = request("POST", "http://localhost:5001/api/v1/auth/register",
        {"email": freelancer_email, "password": "Freelancer123!", "displayName": "Test Freelancer"})
    ok = status == 201 or status == 200
    results.append(("Register Freelancer", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)

    # Step 5: Login Freelancer
    print("\n[Step 5] Login Freelancer...")
    status, body = request("POST", "http://localhost:5001/api/v1/auth/login",
        {"email": freelancer_email, "password": "Freelancer123!"})
    ok = status == 200
    results.append(("Login Freelancer", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            freelancer_token = j.get("accessToken") or j.get("token") or ""
            freelancer_id = j.get("id") or j.get("userId") or ""
        except Exception:
            pass

    # Step 6: Publish Task
    print("\n[Step 6] Publish Task...")
    headers = {"Authorization": f"Bearer {customer_token}"} if customer_token else {}
    status, body = request("POST", f"http://localhost:5012/api/v1/tasks/{task_id}/publish",
        {}, headers)
    ok = status == 200 or status == 201
    results.append(("Publish Task", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)

    # Step 7: Apply to Task
    print("\n[Step 7] Apply to Task...")
    headers = {"Authorization": f"Bearer {freelancer_token}"} if freelancer_token else {}
    status, body = request("POST", f"http://localhost:5012/api/v1/tasks/{task_id}/apply",
        {"proposal": "I have extensive experience building REST APIs with C# and can deliver a high-quality solution with authentication and CRUD operations.", "proposedBudget": 900},
        headers)
    ok = status == 200 or status == 201
    results.append(("Apply to Task", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            app_id = j.get("applicationId") or j.get("id") or ""
        except Exception:
            pass

    # Step 8: Accept Application
    print("\n[Step 8] Accept Application...")
    headers = {"Authorization": f"Bearer {customer_token}"} if customer_token else {}
    status, body = request("POST", f"http://localhost:5012/api/v1/tasks/{task_id}/applications/{app_id}/accept",
        {}, headers)
    ok = status == 200 or status == 201
    results.append(("Accept Application", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")

    # Step 9: Create Collaboration Room
    print("\n[Step 9] Create Collaboration Room...")
    headers = {"Authorization": f"Bearer {freelancer_token}"} if freelancer_token else {}
    status, body = request("POST", "http://localhost:5015/api/collaboration/rooms",
        {"name": "Project Room", "description": "REST API project", "type": 2},
        headers)  # 2 = ProjectRoom enum value
    ok = status == 201 or status == 200
    results.append(("Create Collaboration Room", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            room_id = j.get("roomId") or j.get("id") or ""
        except Exception:
            pass

    # Step 10: IDE Session
    print("\n[Step 10] Create IDE Session...")
    headers = {"Authorization": f"Bearer {freelancer_token}"} if freelancer_token else {}
    status, body = request("POST", "http://localhost:5008/api/ide/sessions",
        {"title": "RestApiProject", "description": "REST API development", "language": "csharp", "projectId": str(task_id) if task_id else "test-project", "creatorId": str(freelancer_id) if freelancer_id else "00000000-0000-0000-0000-000000000000"},
        headers)
    ok = status == 200 or status == 201
    results.append(("Create IDE Session", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")
    print_response(status, body)
    if ok:
        try:
            j = json.loads(body)
            session_id = j.get("sessionId") or j.get("id") or ""
        except Exception:
            pass

    # Step 11: Complete Task
    print("\n[Step 11] Complete Task...")
    headers = {"Authorization": f"Bearer {freelancer_token}"} if freelancer_token else {}
    status, body = request("POST", f"http://localhost:5012/api/v1/tasks/{task_id}/complete",
        {}, headers)
    ok = status == 200 or status == 201
    results.append(("Complete Task", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")

    # Step 12: Gateway routing check
    print("\n[Step 12] Gateway routing...")
    status, body = request("GET", "http://localhost:5000/api/auth/health")
    ok = status == 200
    results.append(("Gateway -> Auth", ok, status))
    print(f"  {'PASS' if ok else 'FAIL'} (HTTP {status})")

    # Summary
    print("\n" + "="*60)
    print("        FULL WORKFLOW INTEGRATION TEST SUMMARY")
    print("="*60)
    passed = sum(1 for _, ok, _ in results if ok)
    total = len(results)
    print(f"  Passed: {passed} / {total}")
    print(f"  Failed: {total - passed} / {total}")
    print("\n  Details:")
    for name, ok, status in results:
        mark = "[OK]" if ok else "[FAIL]"
        print(f"    {mark} {name}: HTTP {status}")
    print("="*60)

    stop_apis()

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        stop_apis()
