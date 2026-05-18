import requests
import json

# Login
r = requests.post('http://localhost:5000/api/v1/auth/login', json={
    'email': 'taras.popok.test@libr4.local',
    'password': 'Test1234!'
})
token = r.json()['accessToken']
headers = {'Authorization': f'Bearer {token}'}

# Get verification ID first
status = requests.get('http://localhost:5000/api/v1/verification/status', headers=headers).json()
print(f"Verification status: {status}")
