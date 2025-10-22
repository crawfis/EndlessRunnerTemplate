# Recommended Projects v2 (Kanban) configuration & process

Columns
- Backlog
  - All raw ideas and inbound issues live here.
- Ready (Groomed)
  - Items that meet the "Definition of Ready": clear acceptance criteria, estimate, priority, no unknown blocking dependencies.
- In Progress
  - Assigned and currently being worked on.
- In Review
  - PR is open or awaiting review/testing.
- Done
  - Issue closed / merged and verified.

Custom fields (add these to your Project)
- Estimate: number (story points)
- Priority: single-select (critical, high, medium, low)
- Type: single-select (story, task, bug, chore)
- Owner: person (user)

Automations (examples)
- When issue is closed -> move to Done
- When issue has label "ready-for-dev" -> move to Ready
- When Pull Request opened that references issue -> move to In Review
- When PR merged that closes issue -> move to Done

Labels (recommended set)
- type:story, type:task, type:bug, type:chore
- priority:critical, priority:high, priority:medium, priority:low
- status:blocked, status:needs-info, ready-for-dev
- area:<component> (area:gameplay, area:ui, area:audio)

Triaging cadence
- Weekly groom: Product/PM + Tech review backlog -> move top items to Ready and add estimates.
- Daily standups: check In Progress items, update blockers.
- Sprint/release planning (if you have releases): pull from Ready into Milestone/Release.

Task vs Checklist decision
- If a sub-item requires assignment, separate tasks, or will have its own PR -> create a separate issue and link with "part of" or "is child of".
- If a sub-item is trivial and won't be tracked independently -> use a checklist inside the Story.

Naming & branching
- Issue title: [Story] or [Task] short descriptor
- Branch: issue-<number>-short-description
- PR body: include "Closes #<issue>" for auto-linking/closing

Acceptance & Definition of Done examples
- Acceptance Criteria: Given/When/Then style testable statements.
- Definition of Done: Unit tests, integration tests, docs updated, accessibility checks, code reviewed and merged.

Rollout checklist
1. Add the Issue Forms to .github/ISSUE_TEMPLATE/
2. Create labels listed above
3. Create the Projects v2 board and add the custom fields
4. Add a short CONTRIBUTING.md section that references the workflow and naming conventions
5. Run a quick training (15–30 min) with the team to demonstrate creating issues and the board flows
