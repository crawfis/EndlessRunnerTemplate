# Timebox 1 — Studio Setup & Greenlight

> CSE 5912 Capstone. The complete Timebox 1 assignment, assembled from the course
> pages on 2026-08-27. Hand this file to your AI assistant as context before asking it to
> help you plan, estimate, or draft anything the assignment asks for.

## Overview and Objective

*CSE 5912 Capstone · The AI-Driven Studio*

This is not a homework assignment. This is **pre-production**. The goal of Timebox 1 is to turn
a group of students into a game studio: define your identity, stand up your DevOps pipeline,
and deliver a **Greenlight pitch** that proves you are ready to build a complex software
product.

You will use generative AI not to do the work for you, but to play three roles for your studio
— **Agile Coach**, **Lead Architect**, and **Technical Auditor**. Prompts for each are in the
phase that needs them.

The endless-runner template is your **playground**: it is where you prove the team can code,
review, merge, and build before any of that matters. The bulk of this timebox is therefore
design, documentation, and process — not features.

**You have 1.5 weeks.** Task IDs like **A4** or **I12** point into the [Student Task
Catalog](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/docs/STUDENT_TASKS.md) —
127 scoped projects grouped by sub-specialty. This document says how many to take, who takes
them, and everything else that is due.

**The five phases:**

| Phase | What it produces |
|----|----|
| **1 — Logistics and the team charter** | how you work, agreed in writing, before you decide what to build |
| **2 — Visual identity** | two palettes: the studio brand for presentations, the art direction for the game |
| **3 — The blueprint** | the vision, the coding standards, and the systems architecture |
| **4 — Pipeline validation** | proof the team can code, review, merge, build — and audit its own code |
| **5 — The Greenlight presentation** | the pitch, the demo, and the metrics that back them |

## The Effort Budget

|                                             | Per member    | Team of six |
|---------------------------------------------|---------------|-------------|
| Weekly expectation                          | 10–12 h       | 60–72 h     |
| Timebox 1 (1.5 weeks)                       | 15–18 h       | 90–108 h    |
| Meetings, planning, documents, presentation | ~half → 7–9 h | 45–54 h     |
| Learning the template and building          | ~half → 7–9 h | 45–54 h     |

- The 10–12 hours a week is the expectation for **every** timebox. The half-and-half split is
  specific to this one — later timeboxes tilt hard toward building.
- Seven to nine hours of build time is **two build items per person**, not one: two **S**
  catalog tasks, or one required item (Phase 4) plus an **S** task, or an **S** task plus a
  written **spike** for something bigger — a time-boxed investigation whose deliverable is a
  teardown and an event map rather than working code.
- **This is your backlog math.** Six people have roughly fifty build-hours in this timebox. If
  the board holds two hundred, your estimates are fiction — and the Production Metrics block of
  your presentation is where that shows.
- **The second build item is the first thing to cut.** The documents and the presentation *are*
  the timebox; the runner work is how you learn it. Never the other way round.

## The 48-hour Freeze

Professional release cycles end with a freeze, so yours does too.

- **Code freeze: no new features in the 48 hours before class.** That window is for merging,
  building, fixing, rehearsing, and printing.
- **Tag the release:** `git tag -a v0.1-greenlight -m "Timebox 1 greenlight"` and push the tag.
  That tag is "the version we demoed" and you can check it out later in the semester.
- **The audit reads from before the freeze.** Every metric you present — commits, closed tasks,
  documents, review turnaround — is pulled as of the tag. Work that lands after it does not
  count toward this timebox, which is exactly the incentive a freeze is meant to create.

## The Hats

Every deliverable has one of these next to it. One name per hat; one person may wear more than
one.

| Hat | Owns | Rotates |
|----|----|----|
| **Scrum master** | standups, the board, the deadline calendar, unblocking, the retro. The process owner, not the decision maker | every timebox |
| **Deck owner** | assembles the deck from everyone's artifacts, owns the menu and game flow charts, prints the 4-up copy, uploads it after class | every timebox |
| **Presenter ×2** | carry the deck after the whole-team intros; able to answer on any slide, not just their own | every timebox |
| **Demo & video owner** | cuts the build, records the run, owns the fallback video and the machine it plays from | every timebox |
| **Integrator** | the repository: setup, branch protection, large-file storage, the freeze and the tag, and the fresh-clone check that it still plays | rarely |
| **Tech lead** | architecture doc, coding standards, the AI policy and audit, and sign-off on every third-party import | rarely |
| **Design / PRD owner** | the product requirements document (PRD): hook, pillars, core mechanic, target audience, the minimum viable product (MVP) and the stretch tiers — and the user stories behind it | rarely |
| **Art director** | studio brand palette and slide theme; game art direction and mood board | rarely |
| **QA & triage** | plays daily, files bugs and stories upstream with the Issue forms, owns the code-metrics slide | every timebox |
| **Art & licensing** | placeholder art sourcing, the license register, credits data, the "art we need" ask to the class | rarely |
| **Producer** (9+ only) | the deliverable checklist end to end and the dependencies between pods, so the scrum master can stay on the board | rarely |

## Deliverables Checklist

Everything below goes to your project wiki and your AI workspace. Copy this into your team
charter and fill in names.

| Deliverable | Hat | Phase |
|----|----|----|
| **Team charter** — core hours, conflict resolution, skill gaps, communication agreements, broken-build rule | scrum master | 1 |
| Bios; individual, team, and project S.M.A.R.T. goals | everyone writes their own | 1 |
| Contact list, meeting cadence, channels, hardware limits, help-seeking path | scrum master | 1 |
| Team repository (disposable — Timebox 2 starts a fresh one), collaborators, branch protection, large-file storage, agreed Unity version, visibility | integrator | 1 |
| GitHub → Discord/Slack integration so commits appear in chat | integrator | 1 |
| **Brand guidelines** — studio 3-color palette with hex codes and a slide theme | art director | 2 |
| **Art direction** — game 5-color palette and mood board | art director | 2 |
| Game name, icon, splash; studio name, logo | art director + art & licensing | 2 |
| PRD draft: hook, pillars, core mechanic, target audience, MVP, stretch \#1/#2 | design owner | 3 |
| User stories → features → (very) small tasks, on the board | design owner; board by scrum master | 3 |
| **Technical standards doc** — naming conventions, folder hierarchy, scene naming | tech lead | 3 |
| **Architecture diagram** — systems/UML flow, plus the menu and game flow charts | tech lead + deck owner | 3 |
| Kanban board: columns, estimate/priority/type/owner fields | scrum master | 4 |
| Timebox flow: task assignment, deadlines, PR policy, mentoring, third-party import control, merge and build windows | tech lead + scrum master | 4 |
| Main menu with localization (**I5**) | one engineer | 4 |
| Credits screen and license register (**I12**) | art & licensing + one engineer | 4 |
| Player Settings (company, product, version `0.1.0`); Editor Settings root namespace | integrator | 4 |
| Two build items per person from the catalog | everyone | 4 |
| **Code audit** — AI critique of coupling, readability, flexibility, with before/after | tech lead | 4 |
| Bugs and stories filed upstream on the template repo | QA & triage, everyone contributes | 4 |
| **AI policy** — tools, boundaries, how AI-written code is reviewed | tech lead | — |
| Presentation deck, 16×9, plus printed handouts at 4 slides per page | deck owner + presenters | 5 |
| Build, demo video, wiki post | demo & video owner | 5 |
| **Sprint log** exported from Git and the board; code metrics as of the freeze tag | QA & triage | 5 |
| **Team member evaluation** — the peer rubric, printed and filled in, handed in **before** class | everyone, individually | 5 |

## AI: Coach, Architect, Auditor, ?

Owner: **tech lead**. This gets its own block in the presentation.

The point is not "we used AI." It is that you gave it a **role** and kept the judgment. Each
phase carries a prompt for the role it needs:

| Role | Phase | What it produces |
|----|----|----|
| **Agile Coach** | 1 | interviews the team, then synthesizes the Team Charter |
| **Creative Director** | 2 | the studio brand palette and the game's art direction |
| **Lead Architect** | 3 | naming and folder standards, the systems architecture, a UML description |
| **Technical Auditor** | 4 | a critique of your code for coupling, readability, and flexibility |

**Your policy** — one page, in the repo or on the wiki — has to answer:

- **Per member.** Which assistants each of you uses, or chooses not to. Opting out is a
  legitimate position; state it and say why. Note what your hardware and licenses allow.
- **Per team.** Where AI is welcome (boilerplate, tests, docs, issue triage, review,
  brainstorming) and where it is not. Decide it explicitly rather than discovering the
  disagreement inside a pull request.
- **How AI-written code is labeled and reviewed.** The honest rule: reviewed exactly like
  hand-written code, and the author must be able to explain every line of it. If they can't, it
  doesn't merge.
- **Prompt frameworks.** If you use BMad or another structured method for the PRD and story
  breakdown, say which, and how it went.
- **The retro.** Where did it save you an hour? Where was it confidently wrong, and what did
  you change afterwards? That is the slide the class will actually learn from.

A worked example ships in this repository:
[`CLAUDE.md`](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/CLAUDE.md) is an AI
contract — architecture rules, folder map, a required workflow — alongside skills
(`/list-events`, `/add-event`, `/audit-events`, `/generate-segments`) that encode the project's
conventions so an assistant follows them instead of inventing its own. Read it as a model for
the contract you will write for *your* game, whichever assistant you use.

## Phase 1: Logistics and the Team Charter

**Goal: define HOW you work before you define WHAT you work on.**

Do not just list names. You need a **Team Charter** — a working agreement that covers
availability, conflict resolution, and communication agreements.

### The AI prompt — Agile Coach

```
Act as an Expert Agile Coach. My team is forming a game studio for a Capstone engineering
project. We need to draft a "Team Working Agreement" (Team Charter).

Task: Interview me. Ask me 5 specific questions, one at a time, regarding:
  1. Core Hours: When is everyone guaranteed to be online?
  2. Conflict Resolution: How do we decide if two people disagree on a mechanic?
  3. Skill Gaps: Where are we weak (Art vs. Code) and how will we cover it?
  4. Communication agreements: How fast must we reply to Discord/Slack messages?
  5. Commit Standards: What happens if someone breaks the build?

Goal: After I answer, synthesize this into a professional "Team Manifesto" document.
```

Answer it as a team, in one meeting, with everyone present. A charter written by one person is
a document; a charter answered together is an agreement.

### The rest of Phase 1

- **Know each other.** Names, emails, and how to reach someone who has gone quiet. Values,
  interests, and skills — who wants to write shaders, who wants to write tools, who wants to
  design levels. And **hardware limits**: find the weakest machine on the team in week one, not
  the night before a demo, because it sets your performance budget.
- **Meeting cadence and channels.** A standing time everyone can actually make, plus
  Discord/Slack/Teams — one place, not four.
- **Toolchain.** Link GitHub to your Discord or Slack so commits and pull requests appear in
  chat. It is five minutes of setup and it makes the team's pulse visible without anyone having
  to ask "what is everyone working on?"
- **Seeking help.** Name the path: teammate first, then the team channel, then the instructor.
  A student stuck for three days in silence is a process failure, not a personal one.
- **First presenters.** Pick them now; they rotate every timebox.
- **Bios.** One slide each: what aspects of game *programming* interest you.
- **S.M.A.R.T. goals** at three levels: individual, team, and project. Specific, Measurable,
  Achievable, Relevant, Time-bound — see the
  [Forbes](https://www.forbes.com/advisor/business/smart-goals/) and
  [Smartsheet](https://www.smartsheet.com/blog/essential-guide-writing-smart-goals) guides.
  "Learn Unity" is not one. "Ship a playable dodge roll with sound by the Timebox 2 demo" is.

## The Repository, Licensing, Third-party Assets

Owner: **integrator**, with the tech lead on policy. Part of Phase 1, and due in the first two
days — everything else waits on it.

**This repository is disposable.** At Timebox 2 you throw it away and start a fresh one for the
actual game, and that is the point: this is where you practice the workflow — branch
protection, pull requests, Git LFS, merge drivers, a scene two people both edited — while
breaking it costs nothing. What carries forward is not the code. It is your `.github/`
configuration, the PR template, the coding standards, the AI policy, the charter, and the
muscle memory. Get *those* right here.

- **Start from the template; don't fork.** `crawfis/EndlessRunnerTemplate` (the old
  `crawfis/EndlessRunner` link redirects here) is a GitHub *template repository*: **Use this
  template → Create a new repository** gives your team a clean history it owns. A fork aims its
  pull requests back upstream and drags this repo's history along. Add every team member as a
  collaborator with write access.

- **Agree on one Unity version, and write it down.** Take the latest release the whole team can
  actually install — the template's README says which Unity 6 release it was built on, so start
  there and don't go backwards — then everyone installs exactly that, patch number included. A
  mismatched patch silently rewrites scenes and prefabs, and your teammates will see hundreds
  of changed lines nobody made. Agree in the first meeting, and verify on every machine.

- **Then make the version a fact, not a memory.** The template deliberately ignores
  `ProjectSettings/ProjectVersion.txt` so it never pins anyone; your team repo should do the
  opposite. Delete that line from `.gitignore` and commit the file, so the version lives in the
  repo where a new machine can read it.

- **Everyone clones and plays before anyone edits.** Open the project, enter Play Mode from
  `Assets/GameFlow/Scenes/Boot/0_BootStrap_Game_Only`, confirm a clean console. A "works on my
  machine" problem found in week one is free.

- **Protect `main`.** Settings → Branches: require a pull request, require at least one
  approving review, disallow direct pushes. Branch names are
  `issue-<number>-short-description`; PR bodies say `Closes #<n>`. The conventions are in
  [`.github/CONTRIBUTING.md`](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/.github/CONTRIBUTING.md)
  — the review policy you have to decide yourselves is in Phase 4.

- **Turn on Git LFS before the first real asset.** Large File Storage keeps big binaries out of
  the repository's history and stores a pointer instead. The template ships no LFS setup
  because it ships no binary art. The day someone adds textures, models, or audio:

  ```
  git lfs install
  git lfs track "*.psd" "*.fbx" "*.png" "*.tga" "*.wav" "*.mp3" "*.ogg"
  git add .gitattributes && git commit -m "chore: track binary art with LFS"
  ```

  Do it *before* the assets land — converting afterwards means rewriting history. GitHub's free
  LFS allowance is small (about a gigabyte of storage and the same again in monthly bandwidth),
  so demo videos belong on the wiki or a shared drive, not in the repo.

- **Teach git to merge Unity files.** Scenes and prefabs are YAML and git's default merge
  mangles them. Unity ships `UnityYAMLMerge` (SmartMerge): register it as the merge driver for
  `*.unity`, `*.prefab`, and `*.asset` in `.gitattributes` plus a matching `[merge]` block in
  your global `.gitconfig`, and scene conflicts become survivable instead of fatal. The
  template's `.gitattributes` already pins Unity YAML to LF endings so those files stop
  appearing modified after every editor session — leave those lines alone. Asset serialization
  is already Force Text; don't change it.

- **One owner per scene, per timebox.** Even with SmartMerge, two people editing the same
  `.unity` file is the most common way a game team loses a day. The template is already split
  into additive scenes — `TempleRunTrackPCG`, `TrackVisuals`, `PlayerVisuals`, `Obstacles`,
  `Collectables`, `Sfx`, `Environment`, `GuiOverlay` — so assign them, and put new work in a
  new scene or a prefab rather than in someone else's.

- **Track the template upstream** so you can pull fixes as the semester goes:

  ```
  git remote add upstream https://github.com/crawfis/EndlessRunnerTemplate.git
  git fetch upstream          # then merge or cherry-pick what you want
  ```

- **Plan the hand-off.** When you create the Timebox 2 repository — from the template again, or
  from scratch if you pivot — copy `.github/` (issue forms, labels, the PR template) and your
  policy documents across on day one, so the real repo starts with the process you already
  proved instead of a blank slate.

### Third-party assets, licenses, and repository visibility

- **Go private if a license says you must.** Paid Asset Store packages, licensed audio, and
  most "personal use" downloads may not be redistributed — and a public repository holding them
  *is* redistribution. If anyone imports something like that, switch the repo to private
  (Settings → General → Change visibility). Read the actual license, not the store page; check
  in particular whether it covers the whole team or a single seat.
- **Everything gets attribution.** Every third-party asset — art, audio, fonts, code, packages
  — earns a row in the license register and a line in the credits screen (**I12**) *the day it
  is imported*, not in the last week of the project. Record the name, author, source URL,
  license, and where in the project it is used. That register is what lets you answer "can we
  ship this?" later without archaeology.
- **Consider a `_THIRD_PARTY/` folder that git ignores.** Put every imported package under one
  top-level folder, add that folder to `.gitignore`, and have each member install into it on
  their own machine. The repository stays small and holds nothing it may not be allowed to
  hold. This works because `.unitypackage` imports carry their own `.meta` files, so the GUIDs
  your prefabs and scenes reference are identical on every machine — *provided everyone
  installs the same version*. Keep a manifest beside the license register saying which asset,
  which version, and where it came from. If the license permits sharing inside the team,
  zipping that folder and passing it around is simpler and safer than everyone downloading
  separately, because the bytes are then identical.
- **Never edit anything inside `_THIRD_PARTY/`.** An edit there exists only on your machine:
  everyone else's project silently differs, and your change disappears the next time anyone
  reinstalls or updates the package. If a third-party script needs to behave differently, wrap
  it, subclass it, or copy the file out into your own folder under your own namespace and
  change *that*. The rule holds even if you commit the folder — package upgrades overwrite your
  edits either way.

## Phase 2: Visual Identity

**Goal: professionalize your presentation and align your creative vision.** These are two
different jobs, and teams routinely conflate them.

| Identity | Focus | Deliverable |
|----|----|----|
| **Studio brand** | corporate identity, for presentations and documents | 3-color palette with hex codes, plus a slide theme |
| **Game art direction** | mood and atmosphere | 5-color palette, plus a mood board description |

### The AI prompt — Creative Director

```
Act as a Creative Director. We need to define two distinct visual identities.

Identity 1: The Studio Brand (For Presentations)
  Our Vibe: Professional, Agile, Engineering-focused.
  Task: Suggest a 3-color palette (Primary, Secondary, Accent) with Hex Codes.
  Constraint: The background must be light enough for black text. High contrast is
  required for projector readability.

Identity 2: The Game Art Direction
  The Mood: [INSERT MOOD, e.g., Cyberpunk Horror].
  Mechanics via Color: How will we use color to signal "Danger" vs. "Safety"?
  Deliverable: A description for a "Mood Board" we can generate.
```

The projector constraint is not decoration: dark, busy slide themes are unreadable in the room
and illegible on the printed 4-up handout. Decide the brand palette before the deck exists, not
after.

**Colour as a mechanic** is the part worth arguing about. If red means danger in your game, it
cannot also be the accent colour on your HUD. That decision belongs here, while it is still
cheap.

**Also in Phase 2:** game name, icon, and splash screens; studio name and logo. Placeholder is
fine — *decided* is the point. These land in Player Settings (Phase 4) and on the title slide.

## Phase 3: The Blueprint

**Goal: prove you have a plan before you write production code.**

### The vision

Owner: **design / PRD owner**.

- **The hook and the pillars.** One sentence for what the game is, and three pillars that every
  later decision gets checked against.
- **PRD or specification (draft).** Core mechanic — the one verb your game is about — plus
  target audience, the gameplay loop, and the reference games you are learning from. A draft is
  expected; a blank is not.
- **User stories → features → tasks.** Break stories down until a task is *very* small —
  something one person finishes in a sitting. Include research and design tasks, especially for
  the parts nobody has done before; "spike: how do we do split-screen" is a legitimate,
  estimable task.
- **Three tiers:** MVP, Stretch Goals \#1, Stretch Goals \#2. Be honest about which is which.
- **Timebox 2 plans**, as goals and tasks, not intentions.

### Technical standards

Owner: **tech lead**. Use the Lead Architect role to draft the checklist, then decide as a team
and write it down:

- **Naming conventions.** PascalCase versus camelCase for classes, methods, fields, properties,
  and serialized fields. The template's own convention — `_underscorePrefix` for private
  fields, PascalCase properties — is in
  [`CLAUDE.md`](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/CLAUDE.md); adopt it
  or replace it deliberately.
- **Folder structure.** The exact hierarchy under `Assets/`, and where third-party material
  lives (see `_THIRD_PARTY/` above).
- **Scene naming.** A convention with a load order in it — `00_Boot`, `01_Menu`, `02_Play` —
  and where additive scenes fit. The template's own scheme (`0_BootStrap_Game_Only`,
  `Game_Boot_0/1/2`, then the gameplay scenes) is a worked example of the same idea.

### Systems architecture

- **Decoupling.** Where does the Observer pattern — an event bus — separate UI from gameplay?
  The template answers this one way (three event domains, auto-chaining, a single cross-domain
  bridge), described in
  [ARCHITECTURE.md](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/docs/ARCHITECTURE.md).
  Say whether you are adopting that, and why.
- **Data.** What belongs in ScriptableObjects rather than in code? Track segments, level
  rulesets, and per-mechanic configs are already SOs here; your game will have its own.
- **The diagram.** A systems/UML diagram of the flow, plus the menu flow chart and the game
  progression flow chart. Ask the AI for a text description first, then draw it — a diagram you
  can describe in words is a diagram you understand.

## Phase 4: Pipeline Validation

**Goal: prove your team can code, review, merge, and build.** The endless runner is the
playground; the pipeline is the deliverable.

### The board and the flow

Owner: **scrum master**, with the tech lead on the code policies.

Kanban in GitHub Projects or Trello, with Backlog / Ready / In Progress / In Review / Done and
fields for estimate, priority, type, and owner.
[`.github/PROJECTS_v2.md`](https://github.com/crawfis/EndlessRunnerTemplate/blob/main/.github/PROJECTS_v2.md)
is a ready-made configuration including the automations. At least one scrum cycle happens
inside this timebox; say which cadence you chose and why.

Write down the timebox flow: how tasks get assigned, internal deadlines ahead of the demo, the
mentoring path, who approves third-party imports (licensing rules above), and when the merge
and build windows are — ending at the 48-hour freeze.

### Pull requests

The mechanics are in `.github/CONTRIBUTING.md`; what it leaves to you is **who reviews**.

- **Use a ring, not a volunteer pool.** Order the team; each person reviews the next person's
  PRs, wrapping around. "Whoever's free" means nobody, and then everything merges at 11pm the
  night before the demo.
- **How many approvals:** five or fewer → one. Six to eight → one, plus the tech lead for
  anything touching an event enum, an auto-flow, a bridge, or a new package. Nine or more →
  two, one of them from the other pod.
- **Nobody merges their own PR**, and nobody approves a PR they wrote code in.
- **Every pull request gets a review within 24 hours.** Past that the scrum master reassigns. A
  PR open three days is a standup topic, not a background process.
- **Keep them small** — a few hundred changed lines. One mechanic plus its event is a good PR.
- **Reviewer checklist** — create `.github/pull_request_template.md` and paste it in:
  - plays from `0_BootStrap_Game_Only` with a clean console
  - every `Subscribe` has its `Unsubscribe` in `OnDestroy`
  - no cross-domain event reference outside a bridge file; `/audit-events` comes back clean
  - no `FindObjectOfType` / `SendMessage` / cross-scene `GetComponent` for communication
  - new third-party asset? The tech lead approved it *and* the license register and the credits
    data both gained a row
  - AI-assisted code is reviewed exactly like hand-written code, and the author can explain it
    in the PR body

### The code audit

Before merging, run your scripts past the **Technical Auditor** role for a critique on
**coupling, readability, and flexibility**:

```
Act as a Lead Developer performing code review. Critique the attached Unity C# script on
three axes:
  1. Coupling: what does this class know about that it shouldn't? What breaks if a
     collaborator is replaced?
  2. Readability: what would a new team member misread on first pass?
  3. Flexibility: which decision here will we regret when we add the second variant of
     this feature?
For each finding, say what you would change and why — do not rewrite the file.
```

Keep the before and after: a diff and a two-line summary per script. **That comparison is a
presentation slide**, and it is the most convincing evidence in the deck that the team can
critique its own work. The audit does not replace human review — it is what you run before
asking for one.

### What to build

**Required of every team** — these come out of the same build budget as catalog picks.

1.  **Main menu with localization** — integrate the starter package
    ([ConsumerUI_RxGames](https://github.com/crawfis/ConsumerUI_RxGames)), UI Toolkit only, two
    or more languages. Catalog **I5**.
2.  **Credits screen** — data-driven, with a license register beside it, updated the day you
    import anything third-party. Catalog **I12**.
3.  **Player Settings** — Company, Product, and Version in `0.1.0` format. The template ships
    `CrawfisSoftware` / `Endless Runner`; make them yours.
4.  **Editor Settings** — set the Root namespace.
5.  **File issues upstream** — bugs and user stories on the template repo using its [Issue
    forms](https://github.com/crawfis/EndlessRunnerTemplate/issues). Generating a first pass
    with AI is fair game; read them before you file them.

**Then pick from the catalog: two build items per person**, from *different* sections, so the
demo shows breadth instead of five variations of the same mechanic. Anything tagged **M** or
**L** is a **spike** in this timebox — a one-page teardown plus an event map, not a half-built
system. Picking extra features to research and plan for later is encouraged; those are tasks
too, and they go on the board with estimates.

| Flavor | You care about | Catalog | Timebox 1 starter picks |
|----|----|----|----|
| **Runner-focused** — make *this* game good | feel, pacing, encounter design, progression | A, B, D, E | **A4** dodge roll · **A5** double jump · **A9** near-miss · **A11** checkpoints · **D5** coin choreography · **E6** per-level records |
| **Tech-focused** — build skills that transfer to *any* game | UI, rendering, audio, tooling, architecture | C, F, G, H, I, J, K, L | **I4** game-over celebration · **F4** dash trails · **F5** screen-space feedback · **L6** event console · **L1** `AutoEventFlowBase` |

Catalog sections **M–O** (the explorer pivot, multiplayer, live services) are semester-spine
decisions, not Timebox 1 work. If your team is drawn to one, its spike *is* the deliverable.

None of this code has to survive: Timebox 2 starts in a new repository for the game itself.
Optimize for what each person *learns* and for what the team can demo, not for what you keep.

## Phase 5: The Greenlight Presentation

**Budget: 35–40 minutes of presentation plus about 25 minutes of questions and discussion.**
Thirty-five is a floor, not a ceiling — larger teams should plan on longer, and any team may
run longer when it has more to show. Everyone introduces themselves; then **two presenters**
carry the deck, with hand-offs for the demo and the AI block.

The running order is a suggestion; the right-hand column is what the pitch has to cover
somewhere. Rehearse it once with a timer so you know which blocks are really five minutes and
which are two.

| Time | Block | Who | Must cover |
|----|----|----|----|
| 0:00–0:03 | Team intros | everyone, ~25 s each | name, which part of game programming you want, one thing about your machine |
| 0:03–0:08 | **Studio logistics** (5m) | presenter 1 | studio name, logo, mission; the charter — core hours, conflict resolution, communication agreements, broken-build rule; who wears which hat |
| 0:08–0:18 | **Core vision** (10m) | presenter 1 | the hook, the pillars, target audience, the gameplay loop, brand and art-direction boards, MVP versus stretch \#1/#2 |
| 0:18–0:25 | **Pipeline validation** (7m) | demo & video owner | the repo, PR history, branch protection, the freeze tag — then the runner itself: menu, language switch, each member's features, the credits screen |
| 0:25–0:30 | **Technical architecture** (5m) | presenter 2 | systems/UML diagram, event domains and where UI is decoupled, ScriptableObject data, menu and game flow charts, keyboard *and* gamepad mappings (justify skipping the gamepad) |
| 0:30–0:34 | **AI approach and policy** (4m) | tech lead | the four roles and what each produced, the team policy, how AI code is reviewed, the code audit before/after, one thing it got wrong |
| 0:34–0:42 | **Production metrics** (8m) | scrum master | backlog math against the hour budget, commits and lines of code as of the freeze tag, review turnaround, scrum cadence, S.M.A.R.T. goals for Sprint 2, risk assessment, and the art/audio resources you want the class's help finding |

**Scaling.** Intros run about 25 seconds a person, so a nine-person team spends four minutes
there. Add that to the total rather than squeezing the demo — a bigger team has more work to
show, not less time to show it in.

**The 25 minutes of questions are not a buffer.** Prepare for them: keep an appendix of backup
slides (the event map, the board, the audit diffs, the metrics breakdown), decide who fields
which kind of question, and make sure the two speakers can cover every member's work. It is a
cohesive team effort.

**Slide rules.** Comprehensive visuals throughout, 16×9, and no dark or visually cluttered
backgrounds — your Phase 2 brand palette was chosen for exactly this. Bring a printed copy
formatted **four slides per page**, using the printer's 4-up option rather than PowerPoint's
handout mode. Hand in your **team member evaluation rubric, printed and filled in, before
class**. Upload the materials afterwards.

## Demo and Video

Owner: **demo & video owner**.

- **Record it, then demo live.** Unity Recorder is already a dependency (`com.unity.recorder`
  in `Packages/manifest.json`) — capture a 60–90 second 1080p run from the Boot scene. The
  recording is the demo that cannot fail in the room; the live build is a bonus you offer when
  the machine cooperates.
- **Script the ninety seconds.** ~15 s of menu including the language switch · ~45 s of play
  that hits each member's feature in the order the slides introduce them · ~15 s of the event
  console or event log, because that is what makes the architecture visible to the class · ~15
  s of the credits screen, which is your license discipline on screen.
- **Build from the freeze tag**, named for the version in Player Settings — the chore, the tag,
  and the demo then verify each other.
- **Bring the video as a local file**, and the deck as PDF as well as PPTX. Post the clip on
  the project wiki and link it from the deck. Export a GIF too: GIFs play in any deck without
  codec roulette.

## Sample Team Plans

Five rules make these work:

- **One name per deliverable.** "The team" is not an owner.
- **Two build items per person, from different catalog sections.**
- **Presenters are not the deck owner** (teams of 6+). Whoever assembles the slides has already
  internalized them; make a second person able to explain them out loud.
- **Everyone rotates.** Presenter, scrum master, and demo owner rotate every timebox — track
  who has served, so the last timebox isn't four people's first turn at once.
- **Anything tagged M or L in the catalog is a spike, not a feature.** Nobody has a week of
  build time this timebox, so the deliverable is a written one — a one-page teardown, an event
  map, and an estimated task breakdown Timebox 2 can build from — not a half-finished system.

### Five

Five people, five hats, one review ring. Everyone owns documents *and* two build items.

| \# | Hats | Documents | Build | Reviews |
|----|----|----|----|----|
| 1 | Scrum master | charter, logistics, board, S.M.A.R.T. collection | **A4** dodge roll · **A11** checkpoint milestones | \#5's PRs |
| 2 | Deck owner · art director | brand palette, art direction, menu + game flow charts | **I5** main menu + localization · **F4** dash & slide trails | \#1's |
| 3 | Demo & video · integrator | repository setup, MVP timeline, freeze and tag | **I12** credits & licenses screen · Player Settings, namespace, build | \#2's |
| 4 | Presenter · design/PRD owner | PRD, pillars, user stories, gameplay loop | **D5** coin choreography · **E6** per-level records | \#3's |
| 5 | Presenter · tech lead | architecture diagram, standards, AI policy and code audit | **L6** in-game event console · **A9** near-miss detection | \#4's |

**What the demo shows:** a language switch on the menu → a run with the new dodge roll,
authored coin arcs, near-miss flashes and a checkpoint fanfare → the event console streaming
all of it live → the credits screen. Everyone's work in ninety seconds, and the architecture
visible on screen instead of described on a slide.

**Four — the penalized case.** The deliverable list does not shrink when the team does, which
is exactly what the penalty is about. Pair the hats (scrum master + deck owner, design/PRD
owner + presenter, tech lead + presenter, integrator + demo and video), keep the required items
in Phase 4, and take **one shared vertical slice** rather than eight separate tasks — say
**A4** dodge roll with its sound effects (**J2**) and a HUD hint (**I1**, scoped to one label)
— so the demo is one coherent thing instead of eight half-features. Everything else goes into
the documents.

### Six

The sixth person buys a dedicated QA & triage hat, and that is real work: someone has to file
the upstream issues and own the metrics slide as of the freeze tag.

| \# | Hats | Documents | Build | Reviews |
|----|----|----|----|----|
| 1 | Scrum master | charter, logistics, board, S.M.A.R.T. collection | **A4** dodge roll · **A11** checkpoint milestones | \#6's PRs |
| 2 | Deck owner · art director | brand palette, art direction, flow charts | **F4** dash & slide trails · **F5** screen-space feedback | \#1's |
| 3 | Integrator · demo & video | repository setup, freeze and tag, MVP timeline | **I12** credits & licenses screen · Player Settings, namespace, build, video | \#2's |
| 4 | Presenter · design/PRD owner | PRD, pillars, user stories, gameplay loop | **D5** coin choreography · **E6** per-level records | \#3's |
| 5 | Presenter · tech lead | architecture diagram, standards, AI policy and code audit | **L6** in-game event console · **A9** near-miss detection | \#4's |
| 6 | QA & triage · art & licensing | upstream issues, sprint log and metrics, license register | **I5** main menu + localization · icon and splash screens | \#5's |

At six, the deck owner stops presenting. That is the point of the sixth person.

### Seven

Seven splits the two hats that were quietly overloaded at six: `main` gets its own owner, and
the art direction separates from the deck.

| \# | Hats | Documents | Build | Reviews |
|----|----|----|----|----|
| 1 | Scrum master | charter, logistics, board, S.M.A.R.T. collection | **A4** dodge roll · **A11** checkpoint milestones | \#7's PRs |
| 2 | Deck owner | deck assembly, menu + game flow charts | **F4** dash & slide trails · **F5** screen-space feedback | \#1's |
| 3 | Demo & video owner | demo script, video, wiki post | **I12** credits & licenses screen · **I4** game-over celebration | \#2's |
| 4 | Integrator | repository setup, branch protection, LFS, freeze and tag | **L1** consolidate `AutoEventFlowBase` · Player Settings, namespace, build | \#3's |
| 5 | Presenter · design/PRD owner | PRD, pillars, user stories, gameplay loop | **D5** coin choreography · **E6** per-level records | \#4's |
| 6 | Presenter · tech lead | architecture diagram, standards, AI policy and code audit | **L6** in-game event console · **A9** near-miss detection | \#5's |
| 7 | Art director · art & licensing | brand palette, art direction, license register, icon and splash | **I5** main menu + localization · **A5** double jump | \#6's |

QA & triage rides with the scrum master here, or becomes the eighth person. The second-reviewer
rule starts at this size: anything touching an event enum, an auto-flow, or a bridge gets the
tech lead as a second approver.

### Nine or more

Above eight, the failure mode is not idleness — it is nine people editing the same three files.
Split into **pods that own disjoint folders and scenes**; the event system is exactly what
makes that possible, since pods publish and subscribe rather than calling each other.

| Seat | Count | Who | Timebox 1 output |
|----|----|----|----|
| **Production** | 3 | scrum master · producer · deck owner (with the art director hat) | board, deliverable checklist, charter, brand, deck, both flow charts, and their own build items |
| **Runner pod** — `Assets/TempleRun/**` | 3–4 | pod lead (first reviewer) + 2–3 | six to eight S tasks from A/B/D/E, plus the spine spike |
| **Tech pod** — `Assets/GameFlow/**`, `Assets/_Common/**`, UI | 3–4 | pod lead (also the integrator) + 2–3, one of them the demo & video owner | six to eight S tasks from I/F/J/L, including the required **I5** and **I12** |

- **The spine spike is the real Timebox 1 output at this size.** Nine people need a direction
  before they need features. Pick one of **A1** (real character controller), section **M** (the
  explorer pivot), or **N1** (split-screen), and deliver it as a written design plus event map
  plus task breakdown. That document is what the other eight build against in Timebox 2 — a far
  better slide than three more S tasks.
- **Presenters: one per pod**, so the deck is defended by the people who built the thing. The
  deck owner never presents at this size.
- **Reviews: two approvals** — your pod lead, plus one person from the other pod. The cross-pod
  reviewer is not a formality; they are the one who notices when your pod reaches across the
  domain boundary instead of publishing an event.
- **Producer, not co-scrum-master.** The producer owns the deliverable checklist and the
  inter-pod dependencies; the scrum master owns the board and the standup. If those hats blur,
  both jobs stop happening.

## Easy to Forget

- Everyone on the one agreed Unity version, verified on their own machine.
- LFS configured *before* the first binary asset lands.
- Company, Product, Version `0.1.0`, and the root namespace — all four changed.
- The license register and the credits screen updated the same day as the import, not at the
  end of the semester — and the repo made private if any license requires it.
- The freeze at 48 hours, and `v0.1-greenlight` tagged and pushed.
- The printed 4-up deck, on paper, in the room.
- The team member evaluation rubric, printed and filled in, handed in **before** class.
- A wiki page for the timebox result, with the video linked.
