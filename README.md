# Recognition of Patterns in Flight Tracking Data
Bachelorproject @ SDU, F23.
Software Engineering

Students:
- Jonas Solhaug Kaad (jokaa17@student.sdu.dk)
- Victor Andreas Boye (viboy20@student.sdu.dk)
- Alexander Vinding Nørup (alnoe20@student.sdu.dk)

Supervisor: Kamrul Islam Shahin - kish@mmmi.sdu.dk

In collaboration with ForeFlight. 

## Development / Running locally

Prerequisite: dotnet 7.x: https://dotnet.microsoft.com/en-us/download

1. Clone the repository
2. Copy: `FlightPatternDetection/appsettings.json` to `FlightPatternDetection/appsettings.Development.json` and change: `mysqlConnectionString` to point to a MariaDB server with `FlightPatternDetection/Database_Creation.sql` loaded in.
    - **Optional**: Change `Logging.LogLevel.Default` to `Debug` in `FlightPatternDetection/appsettings.Development.json`
3. Find the latest NAVDB and place it here `FlightPatternDetection/NAVDB.sqlite`. 
4. **Optional**: Load in some fallback flights into `FlightPatternDetection/FallbackHistoryData/`. Get some from a good friend or create your own by using avalaible python scripts in `scripts/` (requires FF-vpn). 
5. Debug and run. Use the `FlightPatternDetection` project as your startup project. Use the `http` or `https` launch-profile.
