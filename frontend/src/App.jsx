import { useCallback, useEffect, useMemo, useState } from "react";

const API_ROOT = import.meta.env.VITE_API_URL || "/api";
const API_URL = `${API_ROOT}/courses`;
const PAGE_SIZE = 10;
const CATEGORIES = ["software engineering", "racing", "finance", "health", "law"];
const STATUSES = ["Draft", "Published", "Archived"];
const EMPTY_FORM = {
  title: "",
  instructorName: "",
  description: "",
  price: "",
  category: CATEGORIES[0],
  status: "Draft",
};

async function request(path = "", options = {}, token = null, baseUrl = API_URL) {
  let response;
  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...options,
      headers: {
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...options.headers,
      },
    });
  } catch (error) {
    if (error instanceof TypeError) {
      throw new Error("Could not reach the API. Check that the backend is running, then try again.");
    }
    throw error;
  }

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const message = body?.errors
      ? Object.values(body.errors).flat().join(" ")
      : body?.detail || body?.title || `Request failed (${response.status}).`;
    throw new Error(message);
  }

  return response.status === 204 ? null : response.json();
}

function formatPrice(price) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(price);
}

export default function App() {
  const [session, setSession] = useState(() => {
    const savedSession = sessionStorage.getItem("courseDeskSession");
    return savedSession ? JSON.parse(savedSession) : null;
  });
  const [authMode, setAuthMode] = useState(null);
  const [authForm, setAuthForm] = useState({ name: "", email: "", password: "" });
  const [courses, setCourses] = useState([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalRecords, setTotalRecords] = useState(0);
  const [form, setForm] = useState(EMPTY_FORM);
  const [editingId, setEditingId] = useState(null);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("All courses");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");

  const loadCourses = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const query = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) });
      if (session?.user?.role === "Admin" && statusFilter !== "All courses") {
        query.set("status", statusFilter);
      }
      const result = await request(`?${query}`, {}, session?.accessToken);
      setCourses(result.courses);
      setTotalPages(result.totalPages);
      setTotalRecords(result.totalRecords);
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setLoading(false);
    }
  }, [page, session?.accessToken, session?.user?.role, statusFilter]);

  useEffect(() => {
    loadCourses();
  }, [loadCourses]);

  const visibleCourses = useMemo(() => {
    const query = search.trim().toLowerCase();
    return courses.filter((course) => {
      const matchesStatus = statusFilter === "All courses" || course.status === statusFilter;
      const matchesSearch =
        !query ||
        [course.title, course.instructorName, course.category]
          .filter(Boolean)
          .some((value) => value.toLowerCase().includes(query));
      return matchesStatus && matchesSearch;
    });
  }, [courses, search, statusFilter]);

  function resetForm() {
    setForm(EMPTY_FORM);
    setEditingId(null);
    setShowForm(false);
  }

  function editCourse(course) {
    setForm({
      title: course.title,
      instructorName: course.instructorName || "",
      description: course.description || "",
      price: String(course.price),
      category: course.category,
      status: course.status,
    });
    setEditingId(course.id);
    setShowForm(true);
    setError("");
  }

  async function saveCourse(event) {
    event.preventDefault();
    setSaving(true);
    setError("");
    setNotice("");

    const payload = {
      ...form,
      price: Number(form.price),
      instructorName: form.instructorName || null,
      description: form.description || null,
    };

    try {
      if (editingId === null) {
        await request("", { method: "POST", body: JSON.stringify(payload) }, session?.accessToken);
        setNotice("Course created successfully.");
      } else {
        await request(`/${editingId}`, { method: "PUT", body: JSON.stringify(payload) }, session?.accessToken);
        setNotice("Course updated successfully.");
      }
      resetForm();
      await loadCourses();
    } catch (saveError) {
      setError(saveError.message);
    } finally {
      setSaving(false);
    }
  }

  async function deleteCourse(course) {
    if (!window.confirm(`Delete "${course.title}"? This cannot be undone.`)) return;

    setError("");
    setNotice("");
    try {
      await request(`/${course.id}`, { method: "DELETE" }, session?.accessToken);
      setCourses((current) => current.filter((item) => item.id !== course.id));
      const nextTotalRecords = Math.max(0, totalRecords - 1);
      const nextTotalPages = Math.ceil(nextTotalRecords / PAGE_SIZE);
      setTotalRecords(nextTotalRecords);
      setTotalPages(nextTotalPages);
      if (page > Math.max(1, nextTotalPages)) {
        setPage(Math.max(1, nextTotalPages));
      } else {
        await loadCourses();
      }
      if (editingId === course.id) resetForm();
      setNotice("Course deleted successfully.");
    } catch (deleteError) {
      setError(deleteError.message);
    }
  }

  async function submitAuth(event) {
    event.preventDefault();
    setSaving(true);
    setError("");
    setNotice("");

    const isRegistering = authMode === "register";
    try {
      const result = await request(
        isRegistering ? "/register" : "/login",
        {
          method: "POST",
          body: JSON.stringify(isRegistering
            ? authForm
            : { email: authForm.email, password: authForm.password }),
        },
        null,
        `${API_ROOT}/auth`,
      );

      if (isRegistering) {
        setAuthMode("login");
        setAuthForm({ name: "", email: authForm.email, password: "" });
        setNotice("Account created. Sign in to continue.");
      } else {
        const nextSession = {
          accessToken: result.accessToken,
          expiresAt: result.expiresAt,
          user: result.user,
        };
        sessionStorage.setItem("courseDeskSession", JSON.stringify(nextSession));
        setSession(nextSession);
        setAuthMode(null);
        setAuthForm({ name: "", email: "", password: "" });
      }
    } catch (authError) {
      setError(authError.message);
    } finally {
      setSaving(false);
    }
  }

  function signOut() {
    sessionStorage.removeItem("courseDeskSession");
    setSession(null);
    setNotice("You have signed out.");
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="CourseDesk home">
          <span className="brand-mark">C</span>
          <span>CourseDesk</span>
        </a>
        <div className="topbar-actions">
          {session ? (
            <>
              <span className="account-label">{session.user.name} · {session.user.role}</span>
              <button className="button-secondary" type="button" onClick={signOut}>Sign out</button>
            </>
          ) : authMode ? (
            <button className="button-secondary" type="button" onClick={() => { setAuthMode(null); setError(""); setNotice(""); }}>
              Back to courses
            </button>
          ) : (
            <>
              <button className="button-secondary" type="button" onClick={() => { setAuthMode("login"); setError(""); setNotice(""); }}>
                Sign in
              </button>
              <button className="button-primary" type="button" onClick={() => { setAuthMode("register"); setError(""); setNotice(""); }}>
                Register
              </button>
            </>
          )}
          {session?.user?.role === "Admin" && !authMode && (
            <button className="button-primary" type="button" onClick={() => { resetForm(); setError(""); setNotice(""); setShowForm(true); }}>
              + New course
            </button>
          )}
        </div>
      </header>

      <main className="main-content" id="courses">
        {authMode ? (
          <section className="auth-card">
            <div className="auth-heading">
              <span className="eyebrow">COURSEDESK ACCOUNT</span>
              <h1>{authMode === "register" ? "Create your account" : "Welcome back"}</h1>
              <p>{authMode === "register" ? "Sign up to keep learning with CourseDesk." : "Sign in to your CourseDesk account."}</p>
            </div>
            {error && <div className="feedback feedback-error" role="alert">{error}</div>}
            {notice && <div className="feedback feedback-success" role="status">{notice}</div>}
            <form onSubmit={submitAuth}>
              {authMode === "register" && (
                <label className="field">
                  <span>Name <b>*</b></span>
                  <input
                    autoComplete="name"
                    maxLength="100"
                    required
                    value={authForm.name}
                    onChange={(event) => setAuthForm({ ...authForm, name: event.target.value })}
                    placeholder="Your name"
                  />
                </label>
              )}
              <label className="field">
                <span>Email <b>*</b></span>
                <input
                  type="email"
                  autoComplete="email"
                  maxLength="256"
                  required
                  value={authForm.email}
                  onChange={(event) => setAuthForm({ ...authForm, email: event.target.value })}
                  placeholder="you@example.com"
                />
              </label>
              <label className="field">
                <span>Password <b>*</b></span>
                <input
                  type="password"
                  autoComplete={authMode === "register" ? "new-password" : "current-password"}
                  minLength={authMode === "register" ? 12 : undefined}
                  maxLength="128"
                  required
                  value={authForm.password}
                  onChange={(event) => setAuthForm({ ...authForm, password: event.target.value })}
                  placeholder={authMode === "register" ? "At least 12 characters" : "Your password"}
                />
              </label>
              <button className="button-primary auth-submit" type="submit" disabled={saving}>
                {saving ? "Please wait..." : authMode === "register" ? "Create account" : "Sign in"}
              </button>
            </form>
            <p className="auth-switch">
              {authMode === "register" ? "Already have an account?" : "New to CourseDesk?"}{" "}
              <button type="button" onClick={() => { setAuthMode(authMode === "register" ? "login" : "register"); setError(""); setNotice(""); }}>
                {authMode === "register" ? "Sign in" : "Create an account"}
              </button>
            </p>
          </section>
        ) : (
          <>
        <section className="page-heading">
          <div>
            <h1>{session?.user?.role === "Admin" ? "Course management" : "Explore courses"}</h1>
            <p className="page-subtitle">
              {session?.user?.role === "Admin"
                ? `${totalRecords} courses`
                : `${totalRecords} published ${totalRecords === 1 ? "course" : "courses"}`}
            </p>
          </div>
        </section>

        {(error || notice) && !showForm && (
          <div className={`feedback ${error ? "feedback-error" : "feedback-success"}`} role="status">
            <span>{error || notice}</span>
            <button type="button" onClick={() => { setError(""); setNotice(""); }} aria-label="Dismiss message">×</button>
          </div>
        )}

        <section className="catalog-panel">
          <div className="toolbar">
            <label className="search-box">
              <span aria-hidden="true">⌕</span>
              <input
                type="search"
                placeholder="Search courses..."
                value={search}
                onChange={(event) => setSearch(event.target.value)}
              />
            </label>
            {session?.user?.role === "Admin" && (
              <select
                value={statusFilter}
                onChange={(event) => {
                  setStatusFilter(event.target.value);
                  setPage(1);
                }}
                aria-label="Filter by status"
              >
                <option>All courses</option>
                {STATUSES.map((status) => <option key={status}>{status}</option>)}
              </select>
            )}
          </div>

          {loading ? (
            <div className="empty-state"><span className="loader" /> Loading courses...</div>
          ) : visibleCourses.length === 0 ? (
            <div className="empty-state">
              <strong>{totalRecords ? "No matching courses" : "No courses yet"}</strong>
              <p>{totalRecords ? "Try another search or filter." : "Create a course to get started."}</p>
            </div>
          ) : (
            <div className="table-scroll">
              <table className="course-table">
                <thead>
                  <tr><th>Course</th><th>Instructor</th><th>Category</th><th>Price</th><th>Status</th>{session?.user?.role === "Admin" && <th aria-label="Actions" />}</tr>
                </thead>
                <tbody>
                  {visibleCourses.map((course) => (
                    <tr key={course.id}>
                      <td className="course-name">{course.title}</td>
                      <td>{course.instructorName || "—"}</td>
                      <td className="category-cell">{course.category}</td>
                      <td>{formatPrice(course.price)}</td>
                      <td><span className={`status-pill status-${course.status.toLowerCase()}`}>{course.status}</span></td>
                      {session?.user?.role === "Admin" && (
                        <td className="row-actions">
                          <button type="button" onClick={() => editCourse(course)}>Edit</button>
                          <button type="button" className="delete-action" onClick={() => deleteCourse(course)}>Delete</button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {!loading && totalPages > 1 && (
            <nav className="pagination" aria-label="Course pages">
              <button
                className="button-secondary"
                type="button"
                onClick={() => setPage((current) => current - 1)}
                disabled={page <= 1}
              >
                Previous
              </button>
              <span>Page {page} of {totalPages}</span>
              <button
                className="button-secondary"
                type="button"
                onClick={() => setPage((current) => current + 1)}
                disabled={page >= totalPages}
              >
                Next
              </button>
            </nav>
          )}
        </section>
        <footer className="page-footer">CourseDesk · {session?.user?.role === "Admin" ? "Course management" : "Browse published courses"}</footer>
          </>
        )}
      </main>

      {showForm && (
        <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) resetForm(); }}>
          <section className="form-panel" role="dialog" aria-modal="true" aria-labelledby="form-title">
            <div className="panel-heading">
              <div>
                <h2 id="form-title">{editingId === null ? "New course" : "Edit course"}</h2>
                <p>Enter the course details below.</p>
              </div>
              <button className="close-button" type="button" onClick={resetForm} aria-label="Close form">×</button>
            </div>
            {error && (
              <div className="feedback feedback-error" role="alert">
                <span>{error}</span>
                <button type="button" onClick={() => setError("")} aria-label="Dismiss message">×</button>
              </div>
            )}
            <form onSubmit={saveCourse}>
              <label className="field">
                <span>Title <b>*</b></span>
                <input
                  required
                  value={form.title}
                  onChange={(event) => setForm({ ...form, title: event.target.value })}
                  placeholder="Course title"
                />
              </label>
              <label className="field">
                <span>Instructor</span>
                <input
                  value={form.instructorName}
                  onChange={(event) => setForm({ ...form, instructorName: event.target.value })}
                  placeholder="Instructor name"
                />
              </label>
              <label className="field">
                <span>Description</span>
                <textarea
                  rows="3"
                  value={form.description}
                  onChange={(event) => setForm({ ...form, description: event.target.value })}
                  placeholder="Course description"
                />
              </label>
              <div className="field-row">
                <label className="field">
                  <span>Price <b>*</b></span>
                  <div className="price-input"><span>$</span><input type="number" min="0" step="1" required value={form.price} onChange={(event) => setForm({ ...form, price: event.target.value })} placeholder="0" /></div>
                </label>
                <label className="field">
                  <span>Status <b>*</b></span>
                  <select value={form.status} onChange={(event) => setForm({ ...form, status: event.target.value })}>
                    {STATUSES.map((status) => <option key={status}>{status}</option>)}
                  </select>
                </label>
              </div>
              <label className="field">
                <span>Category <b>*</b></span>
                <select value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })}>
                  {CATEGORIES.map((category) => <option key={category} value={category}>{category}</option>)}
                </select>
              </label>
              <div className="form-actions">
                <button className="button-secondary" type="button" onClick={resetForm}>Cancel</button>
                <button className="button-primary" type="submit" disabled={saving}>
                  {saving ? "Saving..." : editingId === null ? "Create course" : "Save changes"}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </div>
  );
}
