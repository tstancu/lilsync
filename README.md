# lilsync

### A small one-way local folder sync solution

Usage: 

*run the app
`lilSync <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalInSeconds>`

*delete replica and log folders/file
`lilSync <sourceFolder> <replicaFolder> <logFilePath> <syncIntervalInSeconds> --cleanup`

*clean build artifacts
`dotnet clean lilsyncSolution.sln`

*build solution
`dotnet build lilsyncSolution.sln`

*test solution
`dotnet test lilsyncSolution.sln`