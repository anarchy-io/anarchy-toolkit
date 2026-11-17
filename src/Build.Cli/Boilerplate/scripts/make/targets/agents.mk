#############################
## AGENT SKILLS
#############################
#@ anarchy-maintenance

.PHONY: install_skills
install_skills: #? Installs the toolkit's agent skills into .agents/skills (existing files are kept)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Installing Agent Skills'
	${MAKE} fallout SKIP_TITLE=true TARGET="SetupBoilerplate --BoilerplateItems skills"

.PHONY: skills_validate
skills_validate: #? Checks that the agent skills required by #@ markers exist and list the current make targets
#+ PREFIX|str|Prefix of skills named by a bare #@ (DEFAULT=the solution's name)
	${MAKE} fallout SKIP_TITLE=true TARGET="ValidateAgentSkills$(if $(PREFIX), --AgentSkillsPrefix $(PREFIX),)"

.PHONY: skills_sync
skills_sync: #? Creates missing agent skills and refreshes their generated commands from the make targets
#+ PREFIX|str|Prefix of skills named by a bare #@ (DEFAULT=the solution's name)
	@$(SCRIPTS_DIR)/console/title.sh 'cyan' 'Syncing Agent Skills'
	${MAKE} fallout SKIP_TITLE=true TARGET="SyncAgentSkills$(if $(PREFIX), --AgentSkillsPrefix $(PREFIX),)"
