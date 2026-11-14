import { useCallback, useEffect, useMemo, useState } from "react";

const API_URL = import.meta.env.VITE_API_URL
  ? `${import.meta.env.VITE_API_URL}/courses`
  : "/api/courses";
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

async function request(path = "", options = {}) {
  let response;
  try {
    response = await fetch(`${API_URL}${path}`, {
      ...options,
      headers: {
        ...(options.body ? { "Content-Type": "application/json" } : {}),
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
  const [courses, setCourses] = useState([]);
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
      setCourses(await request());
    } catch (loadError) {
      setError(loadError.message);
    } finally {
      setLoading(false);
    }
  }, []);

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

  const publishedCount = courses.filter((course) => course.status === "Published").length;

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
        await request("", { method: "POST", body: JSON.stringify(payload) });
        setNotice("Course created successfully.");
      } else {
        await request(`/${editingId}`, { method: "PUT", body: JSON.stringify(payload) });
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
      await request(`/${course.id}`, { method: "DELETE" });
      setCourses((current) => current.filter((item) => item.id !== course.id));
      if (editingId === course.id) resetForm();
      setNotice("Course deleted successfully.");
    } catch (deleteError) {
      setError(deleteError.message);
    }
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <a className="brand" href="/" aria-label="CourseDesk home">
          <span className="brand-mark">C</span>
          <span>CourseDesk</span>
        </a>
        <button className="button-primary" type="button" onClick={() => { resetForm(); setError(""); setNotice(""); setShowForm(true); }}>
          + New course
        </button>
      </header>

      <main className="main-content" id="courses">
        <section className="page-heading">
          <div>
            <h1>Courses</h1>
            <p className="page-subtitle">{courses.length} courses · {publishedCount} published</p>
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
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} aria-label="Filter by status">
              <option>All courses</option>
              {STATUSES.map((status) => <option key={status}>{status}</option>)}
            </select>
          </div>

          {loading ? (
            <div className="empty-state"><span className="loader" /> Loading courses...</div>
          ) : visibleCourses.length === 0 ? (
            <div className="empty-state">
              <strong>{courses.length ? "No matching courses" : "No courses yet"}</strong>
              <p>{courses.length ? "Try another search or filter." : "Create a course to get started."}</p>
            </div>
          ) : (
            <div className="table-scroll">
              <table className="course-table">
                <thead>
                  <tr><th>Course</th><th>Instructor</th><th>Category</th><th>Price</th><th>Status</th><th aria-label="Actions" /></tr>
                </thead>
                <tbody>
                  {visibleCourses.map((course) => (
                    <tr key={course.id}>
                      <td className="course-name">{course.title}</td>
                      <td>{course.instructorName || "—"}</td>
                      <td className="category-cell">{course.category}</td>
                      <td>{formatPrice(course.price)}</td>
                      <td><span className={`status-pill status-${course.status.toLowerCase()}`}>{course.status}</span></td>
                      <td className="row-actions">
                        <button type="button" onClick={() => editCourse(course)}>Edit</button>
                        <button type="button" className="delete-action" onClick={() => deleteCourse(course)}>Delete</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
        <footer className="page-footer">CourseDesk · Course management</footer>
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
