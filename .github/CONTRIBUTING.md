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

The label set lives in [`labels.json`](labels.json). Import it into a new repository with the
GitHub API, `gh`, or a sync tool such as
[github-label-sync](https://github.com/Financial-Times/github-label-sync).

| Prefix | When | Values |
|--------|------|--------|
| `type:` | Required on every issue | `type:story`, `type:task`, `type:bug`, `type:chore` |
| `priority:` | Added during grooming | `priority:critical`, `priority:high`, `priority:medium`, `priority:low` |
| `status:` | As needed, to flag blocked or incomplete work | `status:blocked`, `status:needs-info`, `ready-for-dev` |
| `area:` | Optional, helps filtering and assignment | `area:gameplay`, `area:ui`, `area:audio` |

The User Story and Task forms apply their `type:` label automatically. The Bug form does not —
it currently applies `bug` and `needs-triage`, neither of which is in `labels.json`; add
`type:bug` by hand until that is reconciled. Other labels are applied from the Labels sidebar,
or in bulk:

```
gh issue edit <issue-number> --add-label "priority:high,area:gameplay"
```

## Setting This Up Elsewhere

To adopt this workflow in another repository, follow the rollout checklist in
[`PROJECTS_v2.md`](PROJECTS_v2.md), which also covers the board columns, custom fields, and
automations. Copy `.github/ISSUE_TEMPLATE/` and `labels.json` from here as your starting point,
and link the resulting CONTRIBUTING.md from your README.
