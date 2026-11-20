#############################
## PACKAGING & PUBLISHING
#############################
#@ anarchy-release

.PHONY: pack
pack: #? Packs every package into a .nupkg under artifacts/packages
	@$(SCRIPTS_DIR)/console/title.sh 'magenta' 'Packing NuGet Packages'
	${MAKE} anarchy SKIP_TITLE=true TARGET=Pack

.PHONY: list_packages
list_packages: #? Lists every package (id + version) produced by the solution
	${MAKE} anarchy SKIP_TITLE=true TARGET=ListPackages

.PHONY: release_bump
release_bump: #? Bumps a package and its transitive package dependents
#+ RELEASE_PROJECT|str|Project name to release (required, e.g. Core.Events)
#+ RELEASE_BUMP|str|Version increment: patch, minor or major (required)
	@test -n "$(RELEASE_PROJECT)" || { echo 'RELEASE_PROJECT is required'; exit 1; }
	@test -n "$(RELEASE_BUMP)" || { echo 'RELEASE_BUMP is required'; exit 1; }
	@$(SCRIPTS_DIR)/console/title.sh 'magenta' 'Bumping NuGet Package Versions'
	${MAKE} anarchy SKIP_TITLE=true TARGET="GenerateLocalRelease --ReleaseProject $(RELEASE_PROJECT) --ReleaseBump $(RELEASE_BUMP)"

.PHONY: release_bump_changed
release_bump_changed: #? Bumps packages affected by changes between Git commits
#+ RELEASE_BASE|str|Base Git commit or ref (default: saved release.base)
#+ RELEASE_HEAD|str|Head Git commit (defaults to HEAD; must be checked out)
#+ RELEASE_BUMP|str|Version increment: patch (default), minor or major
	${MAKE} anarchy SKIP_TITLE=true TARGET="GenerateReleaseFromChanges$(if $(filter undefined,$(origin RELEASE_BASE)),, --ReleaseBase '$(RELEASE_BASE)')$(if $(RELEASE_HEAD), --ReleaseHead $(RELEASE_HEAD),)$(if $(RELEASE_BUMP), --ReleaseBump $(RELEASE_BUMP),)"

.PHONY: release_notes
release_notes: #? Adds missing README release notes from commits affecting each package
#+ RELEASE_BASE|str|Base Git commit or ref (default: saved release.base)
#+ RELEASE_HEAD|str|Head Git commit (defaults to HEAD; must be checked out)
	${MAKE} anarchy SKIP_TITLE=true TARGET="GenerateReleaseNotes$(if $(filter undefined,$(origin RELEASE_BASE)),, --ReleaseBase '$(RELEASE_BASE)')$(if $(RELEASE_HEAD), --ReleaseHead $(RELEASE_HEAD),)"

.PHONY: publish
publish: #? Packs and pushes all packages to the NuGet feed
#+ API_KEY|str|API key for the feed
#+ SOURCE|str|NuGet feed URL (required)
	@$(SCRIPTS_DIR)/console/title.sh 'magenta' 'Publishing NuGet Packages'
	${MAKE} anarchy SKIP_TITLE=true TARGET="Publish$(if $(API_KEY), --NuGetApiKey $(API_KEY),)$(if $(SOURCE), --NuGetSource $(SOURCE),)"

.PHONY: validate_release
validate_release: #? Checks that changed packages and their dependents have the version bumps they need
#+ RELEASE_BASE|str|Base Git commit or ref (default: saved release.base)
#+ RELEASE_HEAD|str|Head Git commit (defaults to HEAD)
	${MAKE} anarchy SKIP_TITLE=true TARGET="ValidateLocalRelease$(if $(filter undefined,$(origin RELEASE_BASE)),, --ReleaseBase '$(RELEASE_BASE)')$(if $(RELEASE_HEAD), --ReleaseHead $(RELEASE_HEAD),)"
