# Course Management CRM

A basic course-management app with a .NET 10 Web API, SQL Server persistence, and a React/Vite frontend.

## Requirements

- .NET 10 SDK
- Node.js 20.19+ or 22.12+
- A running SQL Server instance

## Configure and run the API

Set `ConnectionStrings__DefaultConnection` to your SQL Server connection string. For example, on a local SQL Server instance:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=CourseManagement;User Id=sa;Password=<your-password>;TrustServerCertificate=True;Encrypt=True'
dotnet run --project BackendPractice --launch-profile http
```

The API creates the database and its initial schema on startup if the database does not already exist. Keep credentials in environment variables or user secrets; do not commit them.

## Run the frontend

In another terminal:

```bash
cd frontend
npm install
npm run dev
```

Open the Vite URL (normally `http://localhost:5173`). The frontend calls the API at `http://localhost:5200`.

## API

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/api/courses` | List courses |
| GET | `/api/courses/{id}` | Get one course |
| POST | `/api/courses` | Create a course |
| PUT | `/api/courses/{id}` | Update a course |
| DELETE | `/api/courses/{id}` | Delete a course |

Course categories are limited to `software engineering`, `racing`, `finance`, `health`, and `law`. Status must be `Draft`, `Published`, or `Archived`. Titles/categories are required and prices cannot be negative. Invalid requests return `400`; missing courses return `404`.