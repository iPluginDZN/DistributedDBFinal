import sqlite3
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "data"


def seed(db_name: str, script_name: str) -> None:
    db_path = DATA / db_name
    script_path = DATA / script_name

    if db_path.exists():
        db_path.unlink()

    with sqlite3.connect(db_path) as connection:
        connection.executescript(script_path.read_text(encoding="utf-8"))

    print(f"Seeded {db_path.relative_to(ROOT)}")


def main() -> None:
    DATA.mkdir(exist_ok=True)
    seed("site_a.db", "seed_site_a.sql")
    seed("site_b.db", "seed_site_b.sql")
    seed("site_c.db", "seed_site_c.sql")


if __name__ == "__main__":
    main()
