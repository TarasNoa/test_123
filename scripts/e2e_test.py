#!/usr/bin/env python3
"""Full E2E test for all Libr4 APIs."""
import subprocess
import time
import sys
import os
import signal
import urllib.request
import urllib.error
import json
from pathlib import Path

BASE = Path("d:/lib4_project/libr4")
LOG_DIR = BASE / "e2e-logs"
LOG_DIR.mkdir(exist_ok=True)

APIS = [
    ("Gateway",       BASE / "src/Gateway/Libr4.Gateway/Libr4.Gateway.csproj",                       5000),
    ("Auth",          BASE / "src/Services/Auth/Libr4.Auth.Api/Libr4.Auth.Api.csproj",              5001),
    ("Chat",          BASE / "src/Services/Chat/Libr4.Chat.Api/Libr4.Chat.Api.csproj",              5004),
    ("AI",            BASE / "src/Services/AI/Libr4.AI.Api/Libr4.AI.Api.csproj",                    5006),
    ("Analytics",     BASE / "src/Services/Analytics/Libr4.Analytics.Api/Libr4.Analytics.Api.csproj", 5007),
    ("IDE",           BASE / "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj",                  5008),
    ("Matching",      BASE / "src/Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj",  5009),
    ("Payments",      BASE / "src/Services/Payments/Libr4.Payments.Api/Libr4.Payments.Api.csproj",    5010),
    ("Social",        BASE / "src/Services/Social/Libr4.Social.Api/Libr4.Social.Api.csproj",          5011),
    ("Tasks",         BASE / "src/Services/Tasks/Libr4.Tasks.Api/Libr4.Tasks.Api.csproj",            5012),
    ("Trading",       BASE / "src/Services/Trading/Libr4.Trading.Api/Libr4.Trading.Api.csproj",        5013),
    ("Collaboration", BASE / "src/Services/Collaboration/Libr4.Collaboration.Api/Libr4.Collaboration.Api.csproj", 5015),
]

processes = []

def start_apis():
    print("\n[1/5] Starting all APIs...")
    for name, proj, port in APIS:
        out = open(LOG_DIR / f"{name}-out.log", "w")
        err = open(LOG_DIR / f"{name}-err.log", "w")
        proc = subprocess.Popen(
            ["dotnet", "run", "--project", str(proj), "--urls", f"http://localhost:{port}"],
            stdout=out, stderr=err,
            env={**os.environ, "ASPNETCORE_ENVIRONMENT": "Development"},
            creationflags=subprocess.CREATE_NEW_PROCESS_GROUP
        )
        processes.append((name, port, proc))
        print(f"  {name} -> PID {proc.pid} : {port}")

def wait_for_apis():
    print("\n[2/5] Waiting for APIs to compile & start (20s)...")
    time.sleep(20)

def check_health(name, port, max_retries=20, retry_delay=3):
    url = f"http://localhost:{port}/health"
    for attempt in range(max_retries):
        start = time.time()
        try:
            req = urllib.request.Request(url, method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                elapsed = int((time.time() - start) * 1000)
                return True, resp.status, elapsed, ""
        except urllib.error.HTTPError as e:
            elapsed = int((time.time() - start) * 1000)
            body = e.read().decode("utf-8", errors="ignore")[:200]
            return False, e.code, elapsed, body
        except Exception:
            if attempt < max_retries - 1:
                time.sleep(retry_delay)
    elapsed = int((time.time() - start) * 1000)
    return False, 0, elapsed, "Connection refused / not ready"

def run_smoke_tests():
    print("\n[3/5] Running health smoke tests...")
    results = []
    for name, port, _ in processes:
        ok, code, ms, detail = check_health(name, port)
        results.append((name, ok, code, ms, detail))
        status = "PASS" if ok else "FAIL"
        print(f"  {name}: {status} (HTTP {code}, {ms}ms)")
        if detail and not ok:
            print(f"      -> {detail[:120]}")
    return results

def gateway_routing_tests():
    print("\n[4/5] Gateway routing smoke tests...")
    tests = [
        ("Gateway -> Auth",   "http://localhost:5000/api/auth/health"),
        ("Gateway -> Social", "http://localhost:5000/api/social/health"),
        ("Gateway -> Analytics", "http://localhost:5000/api/analytics/health"),
    ]
    for label, url in tests:
        try:
            req = urllib.request.Request(url, method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                print(f"  {label}: PASS (HTTP {resp.status})")
        except urllib.error.HTTPError as e:
            body = e.read().decode("utf-8", errors="ignore")[:200]
            print(f"  {label}: FAIL (HTTP {e.code}) {body[:80]}")
        except Exception as e:
            print(f"  {label}: FAIL ({str(e)[:80]})")

def basic_api_tests():
    print("\n[5/5] Basic API endpoint tests...")
    # Auth register/login would need real endpoints; test swagger instead
    swagger_tests = [
        ("Auth Swagger",    "http://localhost:5001/swagger/index.html"),
        ("Social Swagger",  "http://localhost:5011/swagger/index.html"),
        ("Analytics Swagger","http://localhost:5007/swagger/index.html"),
    ]
    for label, url in swagger_tests:
        try:
            req = urllib.request.Request(url, method="GET")
            with urllib.request.urlopen(req, timeout=5) as resp:
                print(f"  {label}: PASS (HTTP {resp.status})")
        except Exception as e:
            print(f"  {label}: FAIL ({str(e)[:80]})")

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
    print("Done.\n")

def main():
    try:
        start_apis()
        wait_for_apis()
        results = run_smoke_tests()
        gateway_routing_tests()
        basic_api_tests()

        passed = sum(1 for _, ok, _, _, _ in results if ok)
        failed = len(results) - passed

        print("\n" + "="*50)
        print("           E2E TEST SUMMARY")
        print("="*50)
        print(f"  Passed : {passed} / {len(results)}")
        print(f"  Failed : {failed} / {len(results)}")
        print("="*50)

        if failed > 0:
            print("\nFailed APIs details:")
            for name, ok, code, ms, detail in results:
                if not ok:
                    print(f"  - {name}: HTTP {code} ({ms}ms) {detail[:100]}")
    finally:
        stop_apis()

if __name__ == "__main__":
    main()
