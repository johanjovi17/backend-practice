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
| POST | `/api/auth/register` | Register a Customer account |
| POST | `/api/auth/login` | Verify credentials and return a bearer token |
| GET | `/api/courses` | List courses (paginated; supports `page`, `pageSize`, `category`, `minPrice`, `maxPrice`, `status`, `sortBy`, and `sortOrder`) |
| GET | `/api/courses/{id}` | Get a published course |
| POST | `/api/enrollments` | Enroll the signed-in student in an available course |
| POST | `/api/courses` | Create a course (Admin only) |
| PUT | `/api/courses/{id}` | Update a course (Admin only) |
| DELETE | `/api/courses/{id}` | Delete a course (Admin only) |

Course list and detail endpoints are public and return published courses. An Admin can also view unpublished courses after signing in. New registrations always receive the Customer role; promote an account to Admin only through a trusted administrative process.

The course list response includes `courses`, `totalRecords`, and `totalPages`. Page numbers start at 1; page size defaults to 10 and is capped at 50. Invalid price ranges, sort options, or statuses return `400`.

Enrollment requires a bearer token from `/api/auth/login`; the API uses its `sub` claim as the student ID. It saves the enrollment and an `EnrollmentCreated` outbox message together, then a background publisher places pending messages on a simple in-memory queue. A background worker marks completed messages, retries failures up to three times, and stores exhausted messages in `DeadLetterMessages`. The queue is in memory to keep this example easy to run; email confirmation and analytics are demonstrated with log messages rather than external integrations.

Configure a strong JWT signing key through an environment variable or user secrets. The key must be at least 32 bytes and is deliberately not stored in `appsettings.json`:

```bash
export Jwt__SigningKey="$(openssl rand -base64 48)"
```

The API stores passwords using ASP.NET Core's password hasher and enforces a unique email index. Startup creates the Users table if an existing `EnsureCreated` database does not yet have it.

The frontend includes simple sign-in and registration forms. It keeps the bearer token for the current browser session and sends it only for authenticated requests. The API remains responsible for enforcing course permissions.

Course categories are limited to `software engineering`, `racing`, `finance`, `health`, and `law`. Status must be `Draft`, `Published`, or `Archived`. Titles/categories are required and prices cannot be negative. Invalid requests return `400`; missing courses return `404`.