# Screen Recording Script

Target length: 3-5 minutes.

## 1. Start Backend

Show terminal:

```powershell
python scripts/seed_sqlite.py
docker compose up --build
```

Explain that the coordinator and three simulated database sites are running.

## 2. Open WPF App

Show:

```powershell
dotnet run --project src/AcademicRecords.App/AcademicRecords.App.csproj
```

Point out the SQL editor, initial tree panel, optimized tree panel, transformation analysis, distributed plan, data preview, joined result, site health, and resizable splitters.

## 3. Analyze Query

Click `Analyze Query`.

Explain:

- SQL is sent to the coordinator.
- Coordinator returns structured JSON.
- WPF visualizes the response instead of showing raw JSON.

## 4. Explain Transformations

Show the transformation analysis:

- pushing down selections
- commutativity of joins
- projection pruning

Connect this to reducing intermediate results and communication cost.

## 5. Failure Demo

In terminal:

```powershell
docker compose stop site-b
```

Return to WPF and click `Refresh Health` or `Analyze Query`.

Expected result:

- Site A online
- Site B offline
- Site C online
- distributed plan reports affected Site B fragments

## 6. Close

Explain that this demonstrates distributed query planning, optimization visualization, and failure awareness in a three-site architecture.
