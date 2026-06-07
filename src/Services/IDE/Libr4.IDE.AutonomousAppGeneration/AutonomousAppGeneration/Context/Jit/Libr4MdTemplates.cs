namespace Libr4.IDE.Application.AutonomousAppGeneration.Context.Jit;

internal static class Libr4MdTemplates
{
    public const string Root = """
        # LIBR4 — CalorieVision monorepo

        - Layout: `backend/` Django REST + `frontend/` SolidJS/Vite
        - API base: `/api/meals/`
        - Error envelope: `{ "error", "code", "message" }`
        - Never use nested package `calorievisionapp.meals` — app is top-level `meals`
        """;

    public const string BackendOverride = """
        # LIBR4 backend override

        - Django app package: `meals` (not `backend.meals` nested under project slug)
        - DRF views return Response with consistent JSON keys
        - OpenAI vision logic lives in `meals/services/openai_vision.py`
        - Migrations under `meals/migrations/`
        """;

    public const string FrontendOverride = """
        # LIBR4 frontend override

        - SolidJS + TypeScript + Vite on port 5173
        - API client: `src/lib/api.ts` → `http://localhost:8000/api/meals/`
        - Keep components presentational; fetch in `PrimaryView`
        """;
}
