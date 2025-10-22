# Contributing — Issue & Kanban workflow

This repository uses GitHub Issues + Projects v2 as a Kanban board. Follow these conventions when opening and working on issues.

- Use the Issue forms: choose either "User Story" or "Task" when creating new work.
- Titles: prefix with [Story] or [Task] and keep them short.
- Branches: create a branch per issue named: issue-<number>-short-description
- PRs: reference the issue in the PR description and use "Closes #<issue>" when the PR completes the work.
- Subtasks: create separate issues for assignable or estimable subtasks. Use checklists for trivial steps.
- Definitions:
  - Definition of Ready: clear acceptance criteria, estimate, no unknown blockers.
  - Definition of Done: unit tests, code review, documentation updated, and merged.

If you are not sure where to file something, open an issue in Backlog and tag it type:story or type:task.

## Applying Labels

This repository uses a structured labeling system defined in `.github/labels.json`. Labels help categorize and prioritize issues.

### Label Categories

- **type:** (required) - `type:story`, `type:task`, `type:bug`, `type:chore`
- **priority:** (recommended) - `priority:critical`, `priority:high`, `priority:medium`, `priority:low`
- **status:** (as needed) - `status:blocked`, `status:needs-info`, `ready-for-dev`
- **area:** (optional) - `area:gameplay`, `area:ui`, `area:audio`

### How to Apply Labels

1. **Using Issue Forms:** When creating issues via the User Story or Task forms, the appropriate type label (`type:story` or `type:task`) is automatically applied.
2. **Manual labeling:** Add labels by clicking "Labels" in the right sidebar of any issue.
3. **Using GitHub CLI:** You can bulk-apply labels with `gh issue edit <issue-number> --add-label "priority:high,area:gameplay"`
4. **Syncing labels to your repository:** Use the GitHub API or a tool like [github-label-sync](https://github.com/Financial-Times/github-label-sync) to import labels from `.github/labels.json`.

### Label Application Guidelines

- Always add a **type** label to every issue
- Add **priority** labels during grooming sessions
- Use **status** labels to flag blocked or incomplete issues
- Use **area** labels to help with filtering and assignment

## Rollout Checklist

Use this checklist when setting up the Issue & Projects workflow for a new repository or team:

1. **Add Issue Forms** - Copy `.github/ISSUE_TEMPLATE/user_story.yml` and `task.yml` to your repository
2. **Create labels** - Import labels from `.github/labels.json` using GitHub's API, CLI, or a label sync tool
3. **Create Projects v2 board** - Set up a GitHub Project with columns: Backlog, Ready, In Progress, In Review, Done
4. **Add custom fields** - In your Project, add fields for Estimate, Priority, Type, and Owner (see `PROJECTS_v2.md`)
5. **Configure automations** - Set up automatic movements (e.g., closed issues → Done, PR opened → In Review)
6. **Team training** - Run a 15-30 minute session to demonstrate creating issues, moving cards, and using the workflow
7. **Document** - Add a link to this CONTRIBUTING.md in your README

For detailed guidance on Projects v2 configuration, see `.github/PROJECTS_v2.md`.
