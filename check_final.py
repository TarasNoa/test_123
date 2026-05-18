import requests
import json

# Login
r = requests.post('http://localhost:5000/api/v1/auth/login', json={
    'email': 'taras.popok.test@libr4.local',
    'password': 'Test1234!'
})
token = r.json()['accessToken']
headers = {'Authorization': f'Bearer {token}'}

# Check skills
r = requests.get('http://localhost:5000/api/v1/users/skills/my', headers=headers)
print(f"Status: {r.status_code}")
if r.status_code == 200:
    data = r.json()
    print(f"\nOverall: {data.get('overallLevel', 'N/A')} - {data.get('primaryExpertise', 'N/A')}")
    print(f"Score: {data.get('overallScore', 0)}")
    print(f"\nSkills ({len(data.get('skills', []))}):")
    for s in data.get('skills', [])[:10]:
        score = s.get('score', 0) / 10
        print(f"  {s.get('name', '?'):20} {score:4.1f}/10  {s.get('level', '?')}")
else:
    print(f"Error: {r.text}")
