.PHONY: format

format:
	npm run format
	dotnet dnx --yes fantomless-tool --recurse .
