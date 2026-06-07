namespace Libr4.IDE.Application.AutonomousAppGeneration.Verify;

public static class VerifyRecipeCatalog
{
    public static IReadOnlyList<VerifyRecipe> BuildAll() =>
    [
        CalorieVision(),
        Banking(),
        Django(),
        FastApi(),
        Vite(),
        SolidJs(),
        NextJs(),
        SpringBoot(),
        DotNet(),
        Express(),
        GenericFallback()
    ];

    private static VerifyRecipe CalorieVision() => new(
        Id: "calorie-vision",
        DisplayName: "CalorieVision (Django + SolidJS)",
        InstallCommands:
        [
            "cd backend && python -m pip install -r requirements.txt",
            "cd frontend && npm ci"
        ],
        BuildCommands:
        [
            "cd backend && python manage.py check",
            "cd frontend && npm run build"
        ],
        TestCommands:
        [
            "cd backend && python manage.py test",
            "cd frontend && npm test -- --watch=false"
        ],
        StartCommands:
        [
            "cd backend && python manage.py runserver 0.0.0.0:8000",
            "cd frontend && npm run dev -- --host 0.0.0.0 --port 5173"
        ],
        SmokeTargets:
        [
            new("backend", "http://localhost:8000/", 8000, VerifySmokeKind.Http),
            new("frontend", "http://localhost:5173/", 5173, VerifySmokeKind.Browser)
        ],
        SmokeKind: VerifySmokeKind.Browser);

    private static VerifyRecipe Banking() => new(
        Id: "banking",
        DisplayName: "Banking (Spring Boot + React)",
        InstallCommands:
        [
            "cd backend && mvn -B -ntp dependency:resolve",
            "cd frontend && npm ci"
        ],
        BuildCommands:
        [
            "cd backend && mvn -B -ntp -DskipTests package",
            "cd frontend && npm ci && npm run build"
        ],
        TestCommands:
        [
            "cd backend && mvn -B -ntp test",
            "cd frontend && npm test -- --watch=false"
        ],
        StartCommands:
        [
            "cd backend && mvn -B -ntp spring-boot:run",
            "cd frontend && npm run dev -- --host 0.0.0.0 --port 3000"
        ],
        SmokeTargets:
        [
            new("backend", "http://localhost:8080/actuator/health", 8080, VerifySmokeKind.Http),
            new("frontend", "http://localhost:3000/", 3000, VerifySmokeKind.Browser)
        ],
        SmokeKind: VerifySmokeKind.Browser);

    private static VerifyRecipe Django() => new(
        Id: "django",
        DisplayName: "Python Django",
        InstallCommands: ["cd backend && python -m pip install -r requirements.txt"],
        BuildCommands: ["cd backend && python manage.py check"],
        TestCommands: ["cd backend && python manage.py test"],
        StartCommands: ["cd backend && python manage.py runserver 0.0.0.0:8000"],
        SmokeTargets: [new("app", "http://localhost:8000/", 8000)],
        SmokeKind: VerifySmokeKind.Http);

    private static VerifyRecipe FastApi() => new(
        Id: "fastapi",
        DisplayName: "Python FastAPI",
        InstallCommands: ["cd backend && python -m pip install -r requirements.txt"],
        BuildCommands: ["cd backend && python -m compileall ."],
        TestCommands: ["cd backend && pytest -q"],
        StartCommands: ["cd backend && uvicorn main:app --host 0.0.0.0 --port 8000"],
        SmokeTargets: [new("api", "http://localhost:8000/docs", 8000)],
        SmokeKind: VerifySmokeKind.Http);

    private static VerifyRecipe Vite() => new(
        Id: "vite",
        DisplayName: "Vite Frontend",
        InstallCommands: ["cd frontend && npm ci"],
        BuildCommands: ["cd frontend && npm run build"],
        TestCommands: ["cd frontend && npm test -- --watch=false"],
        StartCommands: ["cd frontend && npm run dev -- --host 0.0.0.0 --port 5173"],
        SmokeTargets: [new("frontend", "http://localhost:5173/", 5173, VerifySmokeKind.Browser)],
        SmokeKind: VerifySmokeKind.Browser);

    private static VerifyRecipe SolidJs() => new(
        Id: "solidjs",
        DisplayName: "SolidJS Frontend",
        InstallCommands: ["cd frontend && npm ci"],
        BuildCommands: ["cd frontend && npm run build"],
        TestCommands: ["cd frontend && npm test -- --watch=false"],
        StartCommands: ["cd frontend && npm run dev -- --host 0.0.0.0 --port 5173"],
        SmokeTargets: [new("frontend", "http://localhost:5173/", 5173, VerifySmokeKind.Browser)],
        SmokeKind: VerifySmokeKind.Browser);

    private static VerifyRecipe NextJs() => new(
        Id: "nextjs",
        DisplayName: "Next.js Fullstack",
        InstallCommands: ["npm ci"],
        BuildCommands: ["npm run build"],
        TestCommands: ["npm test -- --watch=false"],
        StartCommands: ["npm run dev -- -p 3000"],
        SmokeTargets: [new("app", "http://localhost:3000/", 3000, VerifySmokeKind.Browser)],
        SmokeKind: VerifySmokeKind.Browser);

    private static VerifyRecipe SpringBoot() => new(
        Id: "spring-boot",
        DisplayName: "Java Spring Boot",
        InstallCommands: ["cd backend && mvn -B -ntp dependency:resolve"],
        BuildCommands: ["cd backend && mvn -B -ntp -DskipTests package"],
        TestCommands: ["cd backend && mvn -B -ntp test"],
        StartCommands: ["cd backend && mvn -B -ntp spring-boot:run"],
        SmokeTargets: [new("api", "http://localhost:8080/actuator/health", 8080)],
        SmokeKind: VerifySmokeKind.Http);

    private static VerifyRecipe DotNet() => new(
        Id: "dotnet",
        DisplayName: ".NET ASP.NET Core",
        InstallCommands: ["cd backend && dotnet restore"],
        BuildCommands: ["cd backend && dotnet build"],
        TestCommands: ["cd backend && dotnet test"],
        StartCommands: ["cd backend && dotnet run --urls http://0.0.0.0:5000"],
        SmokeTargets: [new("api", "http://localhost:5000/health", 5000)],
        SmokeKind: VerifySmokeKind.Http);

    private static VerifyRecipe Express() => new(
        Id: "express",
        DisplayName: "Node.js Express",
        InstallCommands: ["npm ci"],
        BuildCommands: ["npm run build"],
        TestCommands: ["npm test"],
        StartCommands: ["npm start"],
        SmokeTargets: [new("api", "http://localhost:3000/", 3000)],
        SmokeKind: VerifySmokeKind.Http);

    private static VerifyRecipe GenericFallback() => new(
        Id: "generic-fallback",
        DisplayName: "Generic Fallback",
        InstallCommands: [],
        BuildCommands: [],
        TestCommands: [],
        StartCommands: [],
        SmokeTargets: [],
        SmokeKind: VerifySmokeKind.None);
}
