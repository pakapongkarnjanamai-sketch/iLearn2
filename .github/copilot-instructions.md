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

## Design Context

### Users
The product serves two distinct audiences.

Admin UI users are HR and training managers, plus division-level administrators, working primarily on desktop during office hours. Their main jobs are managing course catalogs, assigning learning, monitoring progress, and making fast, confident decisions from large datasets. For admin feature design, the default interaction model is desktop-first, table-heavy workflows with predictable wizard steps for complex actions.

Learner UI users are employees and staff accessing assigned learning content. Their main jobs are finding required courses quickly, understanding progress clearly, and completing learning tasks with minimal friction.

### Brand Personality
Admin UI voice: focused, structured, trustworthy.

The Admin experience should feel professional, efficient, minimal, and data-confident. It should support dense information without feeling chaotic or inconsistent. Copy should stay concise and practical (short labels, short helper text, no verbose explanations).

Learner UI voice: accessible, encouraging, clear.

The Learner experience should feel welcoming, approachable, and motivating, with lower cognitive load and stronger mobile friendliness.

### Aesthetic Direction
Admin UI follows a minimal, data-heavy, flat visual system based on brand blue #0050b3. It should use consistent spacing, clear hierarchy, restrained surfaces, and standardized page structures so users can move between modules without relearning the interface. Emphasize table-based information display for large datasets and standardized wizard flows for multi-step tasks. Avoid decorative effects: no unnecessary animation and no decorative shadows.

Learner UI follows a soft, human-friendly visual system based on brand teal #027d83. It should feel calm, readable, and supportive, using rounded shapes, gentle contrast, and mobile-friendly interaction patterns.

Accessibility target: WCAG AA minimum, with attention to keyboard navigation, high readability, clear contrast, and reduced-motion friendliness.

### Design Principles
1. Standardize structure and interaction patterns across Admin pages so similar tasks always look and behave the same.
2. Prioritize high-density table layouts for data-heavy work, with concise labels and short action-oriented text.
3. Keep wizard flows linear, predictable, and professional, with clear step status and minimal cognitive overhead.
4. Keep Admin optimized for focused desktop work; treat mobile adaptation as optional unless explicitly required.
5. Use one dominant brand hue per interface context, keep surfaces flat and clean, and avoid decorative shadows.
6. Keep motion minimal by default; avoid non-essential animation unless it communicates state.
7. Use accessibility as a baseline quality bar, not a follow-up task.

