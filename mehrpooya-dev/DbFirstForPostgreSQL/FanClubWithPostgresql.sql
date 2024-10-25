CREATE DATABASE fanclub;

\c fanclub;

CREATE TABLE IF NOT EXISTS clubs (
    club_id SERIAL PRIMARY KEY,
    club_name VARCHAR(100) NOT NULL,
    established_year INT NOT NULL,
    country VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS players (
    player_id SERIAL PRIMARY KEY,
    player_name VARCHAR(100) NOT NULL,
    position VARCHAR(50) NOT NULL,
    birth_date DATE NOT NULL,
    club_id INT,
    FOREIGN KEY (club_id) REFERENCES clubs (club_id) ON DELETE CASCADE
);

INSERT INTO clubs (club_name, established_year, country)
VALUES 
('FC Barcelona', 1899, 'Spain'),
('Real Madrid CF', 1902, 'Spain');

INSERT INTO players (player_name, position, birth_date, club_id)
VALUES 
('Lionel Messi', 'Forward', '1987-06-24', (SELECT club_id FROM clubs WHERE club_name = 'FC Barcelona')),
('Gerard Piqué', 'Defender', '1987-02-02', (SELECT club_id FROM clubs WHERE club_name = 'FC Barcelona')),
('Sergio Busquets', 'Midfielder', '1988-07-16', (SELECT club_id FROM clubs WHERE club_name = 'FC Barcelona')),
('Jordi Alba', 'Defender', '1989-03-21', (SELECT club_id FROM clubs WHERE club_name = 'FC Barcelona')),
('Marc-André ter Stegen', 'Goalkeeper', '1992-04-30', (SELECT club_id FROM clubs WHERE club_name = 'FC Barcelona')),
('Karim Benzema', 'Forward', '1987-12-19', (SELECT club_id FROM clubs WHERE club_name = 'Real Madrid CF')),
('Sergio Ramos', 'Defender', '1986-03-30', (SELECT club_id FROM clubs WHERE club_name = 'Real Madrid CF')),
('Luka Modrić', 'Midfielder', '1985-09-09', (SELECT club_id FROM clubs WHERE club_name = 'Real Madrid CF')),
('Toni Kroos', 'Midfielder', '1990-01-04', (SELECT club_id FROM clubs WHERE club_name = 'Real Madrid CF')),
('Thibaut Courtois', 'Goalkeeper', '1992-05-11', (SELECT club_id FROM clubs WHERE club_name = 'Real Madrid CF'));
