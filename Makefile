.PHONY: migrate_client migrate_server

# Client migration - usage: make migrate_client AddUserTable
migrate_client:
	@if "$(filter-out $@,$(MAKECMDGOALS))" == "" (echo Error: Provide migration name like 'make migrate_client AddUserTable' & exit /b 1)
	dotnet ef migrations add $(filter-out $@,$(MAKECMDGOALS)) --project DatabaseLibrary --context DbContextClient -v
	dotnet ef database update --project DatabaseLibrary --context DbContextClient -v

# Server migration - usage: make migrate_server AddNotesTable  
migrate_server:
	@if "$(filter-out $@,$(MAKECMDGOALS))" == "" (echo Error: Provide migration name like 'make migrate_server AddNotesTable' & exit /b 1)
	dotnet ef migrations add $(filter-out $@,$(MAKECMDGOALS)) --project DatabaseLibrary --context DbContextServer -v
	dotnet ef database update --project DatabaseLibrary --context DbContextServer -v

run_client:
	start MAUIClientUI/bin/Debug/net10.0-windows10.0.19041.0/win-x64/MAUIClientUI.exe $(arg1) $(arg2)
# Catch-all rule to prevent Make from complaining about extra arguments
%:
	@: