#############################
## PACKAGING & PUBLISHING
#############################

.PHONY: pack
pack: #? Packs every package into a .nupkg under artifacts/packages
	@./scripts/utils/print-title.sh 'magenta' 'Packing NuGet Packages'
	${MAKE} nuke SKIP_TITLE=true TARGET=Pack

.PHONY: list_packages
list_packages: #? Lists every package (id + version) produced by the solution
	${MAKE} nuke SKIP_TITLE=true TARGET=ListPackages

.PHONY: publish
publish: #? Packs and pushes all packages to the NuGet feed
#+ API_KEY|str|API key for the feed
#+ SOURCE|str|NuGet feed URL (required)
	@./scripts/utils/print-title.sh 'magenta' 'Publishing NuGet Packages'
	${MAKE} nuke SKIP_TITLE=true TARGET="Publish$(if $(API_KEY), --NuGetApiKey $(API_KEY),)$(if $(SOURCE), --NuGetSource $(SOURCE),)"
