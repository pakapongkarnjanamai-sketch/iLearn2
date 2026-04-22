# Copilot Instructions — iLearn2

## Project Overview
**iLearn2**: ระบบ Internal e-Learning (LMS) รองรับมาตรฐาน SCORM 1.2/2004
- `iLearn.API`: ASP.NET Core Web API (Backend)
- `iLearn.Admin`: ASP.NET Core MVC (Admin UI - Brand Blue #0050b3)
- `iLearn.User`: ASP.NET Core MVC + Razor Pages (Learner UI - Brand Teal #027d83)

## Architecture & Tech Stack
- **Clean Architecture**: Domain -> Application -> Infrastructure -> Presentation
- **Stack**: .NET 9, C# 13, EF Core 9 (SQL Server), Windows Auth
- **Frontend**: DevExtreme 25.2, Bootstrap 5, jQuery, DevExpress dialogs






## DevExpress MCP Server: Configure Your AI-powered Coding Assistant

---
description: 'Answer questions about DevExpress UI Components and their API using the dxdocs server'
---

You are a .NET/JavaScript programmer and DevExpress product expert.

Your task is to answer questions about DevExpress components and their APIs using dxdocs MCP server tools.

When replying to **ANY** question about DevExpress components, use the dxdocs server to construct your answer.

## Workflow:

1. **Call devexpress_docs_search** to obtain help topics related to the user's question
2. **Call devexpress_docs_get_content** to fetch and read the most relevant help topics
3. **Reflect on the obtained content** and how it relates to the question
4. **Provide a comprehensive answer** based solely on retrieved information

## Constraints:

- **Use devexpress_docs_search only once** per question to avoid redundant queries
- **Answer questions based solely** on information obtained from MCP server tools
- If relevant code examples are available in documentation, **include those code examples**
- **Reference specific DevExpress controls and properties** mentioned in the docs
- If a user specifies a version (such as v24.2 or 24.2), invoke MCP server tools corresponding to that version (for example, "dxdocs24_2")


# iLearn — Design Context

> This file provides design context for both iLearn.Admin and iLearn.User. Scope-specific sections are marked.

---

## Design Context

### Users

The product serves two distinct audiences.

**Admin UI** users are HR and training managers, plus division-level administrators, working primarily on desktop during office hours. Their main jobs are managing course catalogs, assigning learning, monitoring progress, and making fast, confident decisions from large datasets. Sessions are task-driven: open the tool, find the data, make a decision, move on. They care about speed and correctness, not exploration.

**Learner UI** users are employees and staff accessing assigned learning content. Their main jobs are finding required courses quickly, understanding progress clearly, and completing learning tasks with minimal friction.

### Brand Personality

**Admin UI voice**: Structured, decisive, trustworthy.

Direct and concise — short labels, terse helper text, no motivational filler. The interface is a tool, not a companion. Copy should tell the admin exactly what happened and what to do next, then get out of the way.

**Emotional target (Admin)**: When an admin finishes a task, they should feel **confident** — "I trust this data and the decision I just made."

**Learner UI voice**: Accessible, encouraging, clear.

The Learner experience should feel welcoming, approachable, and motivating, with lower cognitive load and stronger mobile friendliness.

### Aesthetic Direction

**Admin UI** follows a minimal, flat, data-dense, utilitarian visual system based on brand blue `#0050b3`. Inspired by the clarity and restraint of **Google Admin Console** — clean surfaces, predictable layouts, dense grids that scale. Not flashy, not decorative, just fast and legible. Light theme: white and near-white surfaces (#fff, #fafafa, #f4f8ff) with brand blue as the single dominant accent. The palette is deliberately narrow: one hue for primary actions, semantic colors for status only.

**Typography (Admin)**: Currently Segoe UI (system stack). Open to a custom web font that reinforces brand identity without hurting load time. Any replacement should be highly legible at 12–14px (grid/table scale), support tabular numerals (`tnum`, `lnum`), feel neutral enough for dense data views but distinctive enough to avoid generic, and be available via Google Fonts or self-hosted.

**Learner UI** follows a soft, human-friendly visual system based on brand teal `#027d83`. It should feel calm, readable, and supportive, using rounded shapes, gentle contrast, and mobile-friendly interaction patterns.

**Anti-references** (what the Admin UI must NOT look like):
- Overly colorful or playful dashboards (Notion-style, consumer SaaS)
- Dark-mode developer/tech tools (terminal aesthetics, neon accents)
- Heavily animated SaaS products (transitions for everything, bouncing elements)
- Generic Bootstrap admin templates (AdminLTE, CoreUI out-of-the-box)

**Accessibility**: WCAG AA minimum, with attention to keyboard navigation, high readability, clear contrast, and reduced-motion friendliness.

### Design Principles

1. **Structure over decoration.** Every element earns its space through function. No ornamental shadows, no decorative gradients, no accent stripes. If it doesn't communicate data or state, remove it.
2. **Grids are the interface.** The primary interaction surface is the DevExtreme DataGrid. Optimize for scan speed: tight rows (34px), small type (13px), tabular numerals, sticky headers. Everything else supports getting to and from the grid.
3. **Predictable patterns, zero relearning.** Wizard flows, filter+grid layouts, page headers, and detail views must look and behave identically across every module. A user who learns Courses should already know Assignments.
4. **One accent, used sparingly.** Brand blue (#0050b3) is the only accent color. It marks the single most important action or state on any screen. Semantic colors (success, warning, danger) appear only for status. If everything is blue, nothing is.
5. **Desktop density, not mobile adaptation.** Optimize for 1440px+ viewports. The layout may degrade gracefully on tablets but is not designed for phones. Density is a feature, not a problem.
6. **Minimal motion.** No non-essential animation. Transitions (0.2s ease) only for state changes (active/complete steps, hover feedback). Respect `prefers-reduced-motion`.
7. **Accessibility as baseline.** WCAG AA is the quality bar, not a follow-up task. High contrast, keyboard navigation, focus rings, semantic markup.

### Current UX Priorities (April 2026)

- **Primary redesign scope**: `Assignments/BulkAssign` (Course Selection + Learner Selection), `StudentGroups/AddMembers`, and `StudentGroups/Editor` (Initial Members).
- **Critical pain point #1**: Users lose confidence about what is currently selected when moving across paged DataGrid results.
- **Critical pain point #2**: Cascading filters are not clear enough in behavior and state, especially after value changes and resets.
- **Selection policy**: Keep `allPages` selection behavior, but always expose a visible, immediately scannable list of selected items.
- **Data scale target**: Typical usage is mid-size datasets (~500 to 5,000 rows), so configuration should balance density, scan speed, and responsiveness.
- **Layout success criteria**: Keep key task context within one screen whenever possible and avoid stacked/nested scroll areas that force users to track state in multiple places.
- **Outcome metric**: Users should not lose track of current selections during assignment workflows.

---

## Existing Design System (Admin)

### Tokens (`admin-tokens.css`)
- **Colors**: 45 primitive + semantic tokens. Brand blue (#0050b3), 9-step neutral scale (#fafafa → #262626), semantic status palette (success #52c41a, warning #faad14, danger #ff4d4f, purple #722ed1).
- **Typography**: 5-step size scale (caption 12px → display 28px), 3 weights (500/600/700), Segoe UI system stack. Tabular numerals via `font-feature-settings: "tnum" 1, "lnum" 1`.
- **Spacing**: 12-step 4px-base scale (4px → 96px), semantic names (`--space-xs` through `--space-6xl`).
- **Radius**: 4 sizes (3–8px) + pill (999px).
- **Shadows**: Functional only — dropdown (4px 12px) and overlay (2px 8px), no decorative shadows.

### Component Patterns (`admin-minimal.css`, `admin-wizard.css`)
- **Page shell**: lg 1400px, md 960px. Page header with title + subtitle + actions.
- **Filter+grid layout**: 240px filter sidebar | 1fr grid. Responsive to single column at 992px.
- **Wizard step cards**: Active (blue border, info-subtle bg) / complete (green border, success-subtle bg) states.
- **Grid presets**: default / compact / selection via `buildAdminGridOptions()`. 34px row height, 38px header.
- **Status pills**: Semantic color mapping with uppercase micro-labels.
- **Icon+background pattern**: `.qa-icon-*` classes (accent, warning, success, danger, purple, muted).
- **Summary cards**: Review-step stats with large value + uppercase label.
- **Uppercase micro-labels**: Caption size, 600 weight, 0.4px letter-spacing — used for section titles, column headers, filter labels.
- **Empty states**: Centered, large icon, secondary text.

### Libraries
- DevExtreme 25.2 (light theme), Bootstrap 5.3.8, jQuery, Font Awesome 7.0.1, SweetAlert2.
- Export: ExcelJS 4.4, FileSaver 2.0, html2canvas 1.4.