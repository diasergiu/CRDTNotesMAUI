.PHONY: run_server run_client

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

run_server:
	dotnet run --project Server\Server.csproj --launch-profile "http"
run_client:
	dotnet run --project MAUIClientUI\MAUIClientUI.csproj 

test:
	dotnet test --no-build --verbosity normal
	
	# dotnet	test --filter "CharacterUpdateE2EIntegrationTest"
	
# Catch-all rule to prevent Make from complaining about extra arguments
%:
	@: