# Transaction Ingest

This project is a small **.NET console application** that simulates ingesting retail transaction data from an external feed. The goal is to process a **24-hour snapshot of transactions** and keep a local database in sync with that snapshot.

The application:

- Inserts new transactions
- Updates existing ones when relevant fields change
- Marks transactions as **revoked** if they disappear from the feed while still within the 24-hour window

The external API described in the assignment is **mocked using a local JSON file**.

---

# Tech Stack

- .NET Console Application
- Entity Framework Core
- SQLite
- xUnit (for tests)

---

# Project Structure

src/
TransactionIngest/
Data/
Models/
Services/
Mock/
Program.cs

tests/
TransactionIngest.Tests/


- **src** contains the main application code and database configuration.  
- **tests** contains unit tests for the ingestion logic.

---

# How It Works

Each run of the application performs the following steps:

1. Reads a snapshot of transactions from the mocked JSON feed.

2. For each transaction:
   - Inserts it if the `TransactionId` does not exist.
   - Updates it if tracked fields have changed.

3. After processing the snapshot, the system checks for transactions that were previously seen but are **missing from the current snapshot** and still within the **24-hour window**.

4. These transactions are marked as **revoked**.

5. An **audit record** is written for every:
   - create
   - update
   - revoke action

Running the program multiple times with the same input **does not create duplicate records or unnecessary updates**.

---

# Running the Application

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project src/TransactionIngest


The SQLite database file will be created automatically on first run.


Running Tests

dotnet test

The tests cover the main ingestion behaviors:

inserting new transactions

updating existing transactions

revoking missing transactions

ensuring repeated runs with unchanged input do not cause additional changes


Configuration

Configuration is stored in:

src/TransactionIngest/appsettings.json

Important settings include:

database connection string

location of the mocked transaction feed

Assumptions

Some assumptions were made while implementing the solution:

TransactionId is treated as the stable business key.

Only the last 4 digits of the card number are stored.

The transaction feed is represented by a local JSON file instead of a real API.

Transactions older than 24 hours are not modified by the revocation logic.

Notes

This project was implemented as a console application to keep the solution simple and focused on the ingestion logic.

The design keeps the snapshot reader separate from the ingestion service, so the feed source could easily be replaced with a real API client in the future.