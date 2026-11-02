import { useEffect, useState } from "react";
import "./App.css";

const API_URL = "http://localhost:3001";

const emptyForm = {
  firstName: "",
  lastName: "",
  phone: "",
  email: "",
  notes: "",
};

function App() {
  const [contacts, setContacts] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState("");

  async function loadContacts(searchValue = "") {
    setLoading(true);
    setMessage("");

    try {
      const response = await fetch(
        `${API_URL}/contacts?search=${encodeURIComponent(searchValue)}`
      );

      if (!response.ok) {
        throw new Error("Errore durante il caricamento");
      }

      setContacts(await response.json());
    } catch (error) {
      setMessage(error.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadContacts();
  }, []);

  function handleChange(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  function resetForm() {
    setForm(emptyForm);
    setEditingId(null);
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setMessage("");

    const url = editingId
      ? `${API_URL}/contacts/${editingId}`
      : `${API_URL}/contacts`;

    const method = editingId ? "PUT" : "POST";

    try {
      const response = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });

      const data = response.status === 204 ? null : await response.json();

      if (!response.ok) {
        throw new Error(data?.error || "Operazione non riuscita");
      }

      resetForm();
      setMessage(editingId ? "Contatto modificato" : "Contatto inserito");
      await loadContacts(search);
    } catch (error) {
      setMessage(error.message);
    }
  }

  function startEdit(contact) {
    setEditingId(contact.id);
    setForm({
      firstName: contact.first_name,
      lastName: contact.last_name,
      phone: contact.phone,
      email: contact.email || "",
      notes: contact.notes || "",
    });

    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function deleteContact(contact) {
    const confirmed = window.confirm(
      `Eliminare ${contact.first_name} ${contact.last_name}?`
    );

    if (!confirmed) return;

    try {
      const response = await fetch(`${API_URL}/contacts/${contact.id}`, {
        method: "DELETE",
      });

      if (!response.ok) {
        const data = await response.json();
        throw new Error(data.error || "Eliminazione non riuscita");
      }

      setMessage("Contatto eliminato");
      await loadContacts(search);
    } catch (error) {
      setMessage(error.message);
    }
  }

  function handleSearch(event) {
    event.preventDefault();
    loadContacts(search);
  }

  return (
    <main className="container">
      <header>
        <p className="eyebrow">Progetto da colloquio</p>
        <h1>Rubrica telefonica</h1>
        <p>React, ASP.NET Core, PostgreSQL e Docker Compose</p>
      </header>

      <section className="panel">
        <h2>{editingId ? "Modifica contatto" : "Nuovo contatto"}</h2>

        <form className="contact-form" onSubmit={handleSubmit}>
          <label>
            Nome
            <input
              name="firstName"
              value={form.firstName}
              onChange={handleChange}
              required
            />
          </label>

          <label>
            Cognome
            <input
              name="lastName"
              value={form.lastName}
              onChange={handleChange}
              required
            />
          </label>

          <label>
            Telefono
            <input
              name="phone"
              value={form.phone}
              onChange={handleChange}
              required
            />
          </label>

          <label>
            Email
            <input
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
            />
          </label>

          <label className="full-width">
            Note
            <textarea
              name="notes"
              value={form.notes}
              onChange={handleChange}
              rows="3"
            />
          </label>

          <div className="form-actions full-width">
            <button type="submit">
              {editingId ? "Salva modifiche" : "Aggiungi"}
            </button>

            {editingId && (
              <button
                type="button"
                className="secondary"
                onClick={resetForm}
              >
                Annulla
              </button>
            )}
          </div>
        </form>
      </section>

      <section className="panel">
        <div className="list-heading">
          <h2>Contatti</h2>

          <form className="search" onSubmit={handleSearch}>
            <input
              aria-label="Cerca contatti"
              placeholder="Cerca nome, telefono o email"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />

            <button type="submit">Cerca</button>
          </form>
        </div>

        {message && <p className="message">{message}</p>}

        {loading && <p>Caricamento...</p>}

        {!loading && contacts.length === 0 && (
          <p>Nessun contatto trovato.</p>
        )}

        <div className="contact-list">
          {contacts.map((contact) => (
            <article className="contact-card" key={contact.id}>
              <div>
                <h3>
                  {contact.first_name} {contact.last_name}
                </h3>

                <p>
                  <strong>Telefono:</strong> {contact.phone}
                </p>

                <p>
                  <strong>Email:</strong>{" "}
                  {contact.email || "Non indicata"}
                </p>

                {contact.notes && (
                  <p>
                    <strong>Note:</strong> {contact.notes}
                  </p>
                )}
              </div>

              <div className="card-actions">
                <button
                  type="button"
                  onClick={() => startEdit(contact)}
                >
                  Modifica
                </button>

                <button
                  type="button"
                  className="danger"
                  onClick={() => deleteContact(contact)}
                >
                  Elimina
                </button>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

export default App;
