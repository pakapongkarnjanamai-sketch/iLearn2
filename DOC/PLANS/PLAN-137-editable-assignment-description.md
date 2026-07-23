# PLAN-137: Editable Assignment Description in Assignment Detail Page

Status: VERIFIED
Assigned: Gemini

## Overview
User requested the ability to edit the Description on the Assignment Detail page (`/assignments/:id`). Currently, the backend `Assignment` entity and DTO contain a `Description` property, but there is no endpoint to update it, nor is it displayed or editable on the React `AssignmentDetailPage`.

## Scope
1. **Backend (.NET 9)**
   - Add `UpdateAssignmentDescriptionDto` in `iLearn.Application/DTOs/BulkAssignDto.cs`.
   - Add `UpdateDescriptionAsync` method in `IAssignmentService` / `AssignmentService` that updates `Description` on all batch rules.
   - Add `[HttpPatch("{id}/description")]` endpoint in `AssignmentsController.cs`.
   - Add unit test in `iLearn.Tests` to verify description updates across assignment batch rules.

2. **Frontend (React)**
   - Render `Description` in Overview card's `FactGrid` on `AssignmentDetailPage.tsx`.
   - Add "Edit Description" action to `ControlsSidebar` and edit button next to description in Overview card.
   - Add Edit Description modal for updating text.
   - Connect save action to `PATCH Assignments/${id}/description`.

3. **Verification & Log**
   - Run `dotnet test` and `npm run lint` + `npm run build`.
   - Record entry in `DOC/AGENT_LOG.md`.
