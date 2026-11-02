CREATE TABLE IF NOT EXISTS contacts (
id SERIAL PRIMARY KEY,
first_name VARCHAR(80) NOT NULL,
last_name VARCHAR(80) NOT NULL,
phone VARCHAR(30) NOT NULL UNIQUE,
email VARCHAR(150),
notes TEXT,
created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);
INSERT INTO contacts (first_name, last_name, phone, email, notes)
VALUES
('Mario', 'Rossi', '+39 333 1111111', 'mario.rossi@example.com', 'Contatto dimostrativo'),
('Giulia', 'Bianchi', '+39 333 2222222', 'giulia.bianchi@example.com', 'Cliente')
ON CONFLICT (phone) DO NOTHING;