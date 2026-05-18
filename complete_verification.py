import requests
import json

# Login
r = requests.post('http://localhost:5000/api/v1/auth/login', json={
    'email': 'taras.popok.test@libr4.local',
    'password': 'Test1234!'
})
token = r.json()['accessToken']
headers = {'Authorization': f'Bearer {token}'}

# Get verification ID
status = requests.get('http://localhost:5000/api/v1/verification/status', headers=headers).json()
vid = status.get('verificationId')
print(f"Verification ID: {vid}")

# Complete with skills data
skills_data = {
    "isApproved": True,
    "reason": "AI verification passed",
    "skills": [
        {"name": "C# / .NET", "score": 92, "level": "Expert", "source": "cv", "experienceYears": 8, "contexts": ["backend", "web"], "assessmentReason": "8+ years experience, multiple complex projects"},
        {"name": "Software Architecture", "score": 88, "level": "Expert", "source": "cv", "experienceYears": 6, "contexts": ["design", "microservices"], "assessmentReason": "Led architecture decisions for multiple projects"},
        {"name": "Docker / Kubernetes", "score": 85, "level": "Advanced", "source": "cv", "experienceYears": 5, "contexts": ["devops", "containers"], "assessmentReason": "5 years container orchestration experience"},
        {"name": "PostgreSQL", "score": 82, "level": "Advanced", "source": "cv", "experienceYears": 6, "contexts": ["database", "sql"], "assessmentReason": "Complex queries and optimization experience"},
        {"name": "React / TypeScript", "score": 78, "level": "Advanced", "source": "cv", "experienceYears": 4, "contexts": ["frontend", "web"], "assessmentReason": "Solid frontend development skills"},
        {"name": "AI Integration", "score": 75, "level": "Advanced", "source": "cv", "experienceYears": 3, "contexts": ["ml", "llm"], "assessmentReason": "Experience with LLM integration and prompt engineering"},
        {"name": "System Design", "score": 85, "level": "Advanced", "source": "cv", "experienceYears": 6, "contexts": ["architecture", "scalability"], "assessmentReason": "Designed scalable distributed systems"},
        {"name": "Team Leadership", "score": 80, "level": "Advanced", "source": "cv", "experienceYears": 4, "contexts": ["management", "leadership"], "assessmentReason": "Led teams of 5-10 developers"}
    ],
    "overallLevel": "Senior",
    "overallScore": 85,
    "primaryExpertise": "Full-Stack Software Architect",
    "secondaryExpertise": ["DevOps", "AI Integration", "System Design", "Team Leadership"],
    "recommendations": ["Consider cloud certifications", "Expand into ML/AI engineering"]
}

r = requests.post(f'http://localhost:5000/api/v1/verification/{vid}/complete', 
                  headers={**headers, 'Content-Type': 'application/json'},
                  json=skills_data)
print(f"Complete status: {r.status_code}")
print(f"Response: {r.text}")

# Check skills saved
skills = requests.get('http://localhost:5000/api/v1/users/skills/my', headers=headers).json()
print(f"\nSkills saved: {len(skills.get('skills', []))}")
print(f"Overall: {skills.get('overallLevel')} - {skills.get('primaryExpertise')}")
