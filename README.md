# ShipIt Inventory Management

## Setup Instructions
Open the project in VSCode.
VSCode should automatically set up and install everything you'll need apart from the database connection!

### Setting up the Database.
Create 2 new postgres databases - one for the main program and one for our test database.

Create a new user who should be the owner of both those databases. 

Ask a team member for a dump of the production databases to create and populate your tables, or get this from the "trainer note" instructions in Atlassian for this project. Add this file to the root directory twice, once in ShipIt and once in ShipItTest

Then for each of the projects, add a `.env` file at the root of the project.

That file should contain a property named `POSTGRES_CONNECTION_STRING`.
It should look something like this:
```
POSTGRES_CONNECTION_STRING=Server=127.0.0.1;Port=5432;Database=your_database_name;User Id=your_database_user; Password=your_database_password; 
```

Updating your username, database name and password as you set them in PostGres. 

We now need to update our Environment Variables in our laptop system settings. Find 'Edit environment variables for your account..." Navigate to PATH, then to 'Edit', then add a new path which should be the filepath to your PostGres 'bin' folder. 

Once this is added, you can run a command to build your database: 
psql -U [DB USERNAME] -d postgresql://localhost:5432/[DATABASE NAME] -f [DATABASEFILENAME].sql

You may need to wrap your username, database file path and filename in " " to make the command work. You may also need to restart your machine before you can use the psql command (i.e. before your terminal will recognise psql)

## Running The API
Once set up, simply run dotnet run in the ShipIt directory.

## Running The Tests
To run the tests you should be able to run dotnet test in the ShipItTests directory.

## Deploying to Production
TODO
