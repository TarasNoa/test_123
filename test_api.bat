curl -s -X POST http://localhost:5000/api/v1/auth/login -H "Content-Type: application/json" -d '{"email":"taras.popok.test@libr4.local","password":"Test1234!"}' > token.json
type token.json
echo.
curl -s http://localhost:5000/api/v1/users/skills/my -H "Authorization: Bearer %TOKEN%"
