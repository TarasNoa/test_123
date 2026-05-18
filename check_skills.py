import requests
import json

# Login
login_res = requests.post('http://localhost:5000/api/v1/auth/login', json={
    'email': 'taras.popok.test@libr4.local',
    'password': 'Test1234!'
})
token = login_res.json()['accessToken']
headers = {'Authorization': f'Bearer {token}'}

# Check skills
skills_res = requests.get('http://localhost:5000/api/v1/users/skills/my', headers=headers)
print(f"Status: {skills_res.status_code}")
print(json.dumps(skills_res.json(), indent=2))
